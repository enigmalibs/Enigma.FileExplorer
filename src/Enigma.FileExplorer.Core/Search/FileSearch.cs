using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.FileSystem;
using EntryEnumerable = System.IO.Enumeration.FileSystemEnumerable<Enigma.FileExplorer.Core.FileSystem.FileSystemEntry>;
using IoEntry = System.IO.Enumeration.FileSystemEntry;

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// Searches the local file system, breadth first so the nearest hits come first.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Within a folder, entries are visited in natural name order, so results are stable everywhere.</item>
/// <item>Links and junctions are listed but never entered: no cycles, no wandering off the tree.</item>
/// <item>Unreadable folders are skipped silently; a search reports what it can see.</item>
/// <item>Content search reads text files only: a NUL byte in the first 8 KB marks a binary, and files over
/// 10 MB are skipped. Encodings are detected from the byte order mark, UTF-8 otherwise.</item>
/// </list>
/// </remarks>
public sealed class FileSearch : IFileSearch
{
    /// <summary>
    /// The largest file whose contents are searched.
    /// </summary>
    public const long MaximumContentSize = 10 * 1024 * 1024;

    /// <summary>
    /// The most matching lines reported per file.
    /// </summary>
    public const int MaximumLinesPerFile = 100;

    /// <summary>
    /// The longest line text reported; longer lines are shortened around the match.
    /// </summary>
    public const int MaximumLineLength = 200;

    private const int BinaryProbeSize = 8 * 1024;

    /// <inheritdoc />
    public async IAsyncEnumerable<SearchHit> SearchAsync(SearchQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrEmpty(query.Pattern);

        string root = PathUtilities.Normalize(query.Root);
        Channel<SearchHit> channel = Channel.CreateBounded<SearchHit>(new BoundedChannelOptions(1024) { SingleReader = true, SingleWriter = true });
        using CancellationTokenSource walking = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // The walk runs on the thread pool; the caller (the UI) only receives hits.
        Task producer = Task.Run(() => Walk(query, root, channel.Writer, walking.Token), CancellationToken.None);

        try
        {
            await foreach (SearchHit hit in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return hit;
            }
        }
        finally
        {
            // A consumer that stops early (cap reached, cancelled, disposed) stops the walk with it.
            await walking.CancelAsync();
            await producer;
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Shortens a matching line to its interesting part.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="matchIndex">Where the match starts in it.</param>
    /// <returns>The trimmed line, cut to <see cref="MaximumLineLength"/> characters around the match.</returns>
    internal static string Snippet(string line, int matchIndex)
    {
        string trimmed = line.Trim();
        int shift = line.Length - line.TrimStart().Length;
        int index = Math.Max(0, matchIndex - shift);

        if (trimmed.Length <= MaximumLineLength)
        {
            return trimmed;
        }

        int start = Math.Clamp(index - (MaximumLineLength / 3), 0, trimmed.Length - MaximumLineLength);
        string cut = trimmed.Substring(start, MaximumLineLength);
        return (start > 0 ? "…" : string.Empty) + cut + ((start + MaximumLineLength) < trimmed.Length ? "…" : string.Empty);
    }

    private static async Task Walk(SearchQuery query, string root, ChannelWriter<SearchHit> writer, CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            NamePattern? names = query.Kind == SearchKind.Name ? NamePattern.Parse(query.Pattern) : null;
            StringComparison comparison = SmartCase.ComparisonFor(query.Pattern);
            Queue<string> folders = new([root]);
            int found = 0;

            while ((folders.Count > 0) && (found < query.MaxResults))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string folder = folders.Dequeue();
                string relative = PathUtilities.AreSame(folder, root) ? string.Empty : Path.GetRelativePath(root, folder);

                foreach (FileSystemEntry entry in List(folder))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (entry.IsHidden && !query.IncludeHidden)
                    {
                        continue;
                    }

                    if (entry.IsDirectory && !entry.IsSymbolicLink)
                    {
                        folders.Enqueue(entry.FullPath);
                    }

                    if (names is not null)
                    {
                        if (names.IsMatch(entry.Name))
                        {
                            await writer.WriteAsync(new SearchHit(entry, relative), cancellationToken);

                            if (++found >= query.MaxResults)
                            {
                                break;
                            }
                        }
                    }
                    else if (!entry.IsDirectory && (entry.Size <= MaximumContentSize))
                    {
                        foreach ((int number, string text) in MatchingLines(entry.FullPath, query.Pattern, comparison, cancellationToken))
                        {
                            await writer.WriteAsync(new SearchHit(entry, relative, number, text), cancellationToken);

                            if (++found >= query.MaxResults)
                            {
                                break;
                            }
                        }

                        if (found >= query.MaxResults)
                        {
                            break;
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping is the consumer's decision; it already knows.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failure = exception;
        }
        finally
        {
            writer.TryComplete(failure);
        }
    }

    private static List<FileSystemEntry> List(string folder)
    {
        List<FileSystemEntry> entries = [];

        try
        {
            EnumerationOptions options = new() { IgnoreInaccessible = true, AttributesToSkip = 0, RecurseSubdirectories = false };
            EntryEnumerable enumerable = new(folder, ToEntry, options);
            entries.AddRange(enumerable);

            // Natural order within a folder: the same results in the same order on every file system.
            entries.Sort((x, y) => NaturalStringComparer.Instance.Compare(x.Name, y.Name));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // An unreadable folder is skipped; the rest of the tree is still searched.
        }

        return entries;
    }

    private static FileSystemEntry ToEntry(ref IoEntry entry)
    {
        string name = entry.FileName.ToString();
        FileAttributes attributes = entry.Attributes;

        return new FileSystemEntry(
            name,
            entry.ToFullPath(),
            entry.IsDirectory ? EntryKind.Directory : EntryKind.File,
            entry.IsDirectory ? 0 : entry.Length,
            entry.LastWriteTimeUtc.LocalDateTime,
            HiddenRules.IsHidden(name, attributes),
            (attributes & FileAttributes.ReparsePoint) != 0);
    }

    private static IEnumerable<(int Number, string Text)> MatchingLines(string path, string pattern, StringComparison comparison, CancellationToken cancellationToken)
    {
        FileStream stream;

        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.SequentialScan);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        using (stream)
        {
            if (IsBinary(stream))
            {
                yield break;
            }

            using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int number = 0;
            int reported = 0;

            while (true)
            {
                string? line;

                try
                {
                    line = reader.ReadLine();
                }
                catch (IOException)
                {
                    yield break;
                }

                if (line is null)
                {
                    yield break;
                }

                number++;

                if ((number & 0xFF) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                int index = line.IndexOf(pattern, comparison);

                if (index >= 0)
                {
                    yield return (number, Snippet(line, index));

                    if (++reported >= MaximumLinesPerFile)
                    {
                        yield break;
                    }
                }
            }
        }
    }

    private static bool IsBinary(FileStream stream)
    {
        Span<byte> probe = stackalloc byte[BinaryProbeSize];
        int read = stream.Read(probe);
        stream.Position = 0;

        // UTF-16 text is full of NUL bytes; its byte order mark says it is text all the same.
        bool utf16 = (read >= 2) && (((probe[0] == 0xFF) && (probe[1] == 0xFE)) || ((probe[0] == 0xFE) && (probe[1] == 0xFF)));
        return !utf16 && probe[..read].Contains((byte)0);
    }
}
