using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The freedesktop.org trash (Linux desktops: Dolphin, Nautilus and every other file manager read it):
/// the entry moves to <c>Trash/files</c> and a <c>Trash/info/&lt;name&gt;.trashinfo</c> records where it
/// came from and when, so it can be restored.
/// </summary>
/// <param name="trashDirectory">The trash's root, normally <c>$XDG_DATA_HOME/Trash</c>.</param>
/// <param name="clock">The time recorded as the deletion date.</param>
public sealed class FreedesktopTrash(string trashDirectory, TimeProvider clock) : ITrash
{
    /// <summary>
    /// The user's home trash: <c>$XDG_DATA_HOME/Trash</c>, or <c>~/.local/share/Trash</c>.
    /// </summary>
    /// <param name="home">The home directory.</param>
    /// <returns>The trash's root.</returns>
    public static string HomeTrash(string home)
    {
        string? dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        return Path.Combine(string.IsNullOrEmpty(dataHome) ? Path.Combine(home, ".local", "share") : dataHome, "Trash");
    }

    /// <inheritdoc />
    public bool CanTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.Exists(path) || Directory.Exists(path);
    }

    /// <inheritdoc />
    public void MoveToTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string source = Path.GetFullPath(path);
        string name = Path.GetFileName(source);
        bool isDirectory = Directory.Exists(source);

        if (!isDirectory && !File.Exists(source))
        {
            throw new FileOperationException($"'{name}' does not exist");
        }

        string files = Path.Combine(trashDirectory, "files");
        string info = Path.Combine(trashDirectory, "info");

        try
        {
            Directory.CreateDirectory(files);
            Directory.CreateDirectory(info);

            (string trashName, string infoPath) = ReserveInfo(files, info, name, isDirectory, source);
            string destination = Path.Combine(files, trashName);

            try
            {
                if (isDirectory)
                {
                    Directory.Move(source, destination);
                }
                else
                {
                    File.Move(source, destination);
                }
            }
            catch
            {
                // Nothing moved: the info file must not announce a trashed entry that is not there.
                File.Delete(infoPath);
                throw;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileOperationException($"'{name}' could not be moved to the trash: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Writes the <c>.trashinfo</c> line that describes an entry: its original path, percent-encoded, and the
    /// deletion date in local time.
    /// </summary>
    /// <param name="originalPath">The entry's absolute path.</param>
    /// <param name="deletedAt">When it was deleted.</param>
    /// <returns>The file's content.</returns>
    internal static string InfoContent(string originalPath, DateTimeOffset deletedAt)
        => $"[Trash Info]\nPath={Encode(originalPath)}\nDeletionDate={deletedAt.LocalDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}\n";

    private static string Encode(string path)
    {
        // RFC 2396 escaping as the specification asks, keeping '/' so the path stays readable.
        StringBuilder builder = new();

        foreach (byte b in Encoding.UTF8.GetBytes(path))
        {
            char c = (char)b;

            if (char.IsAsciiLetterOrDigit(c) || "/-_.~!*'()".Contains(c))
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private (string TrashName, string InfoPath) ReserveInfo(string files, string info, string name, bool isDirectory, string source)
    {
        // The info file is created first and exclusively: it is the lock that makes the chosen name ours,
        // even if another program trashes a same-named entry at the same moment.
        while (true)
        {
            string candidate = UniqueNameGenerator.FirstFree(
                files,
                name,
                isDirectory,
                path => UniqueNameGenerator.Exists(path) || File.Exists(Path.Combine(info, Path.GetFileName(path) + ".trashinfo")));

            string infoPath = Path.Combine(info, candidate + ".trashinfo");

            try
            {
                using FileStream stream = new(infoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                byte[] content = Encoding.UTF8.GetBytes(InfoContent(source, clock.GetLocalNow()));
                stream.Write(content);
                return (candidate, infoPath);
            }
            catch (IOException) when (File.Exists(infoPath))
            {
                // Taken between the check and the create; try the next name.
            }
        }
    }
}
