using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Desktop.Formatting;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.Icons.Phosphor;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One row (or tile) of a pane: an entry plus the cursor and mark state the pane gives it.
/// </summary>
public sealed class EntryViewModel : ObservableObject
{
    /// <summary>
    /// Wraps an entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    public EntryViewModel(FileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entry = entry;
    }

    /// <summary>
    /// The entry.
    /// </summary>
    public FileSystemEntry Entry { get; }

    /// <summary>
    /// The entry's name.
    /// </summary>
    public string Name => Entry.Name;

    /// <summary>
    /// The entry's absolute path.
    /// </summary>
    public string FullPath => Entry.FullPath;

    /// <summary>
    /// Whether the entry is a directory.
    /// </summary>
    public bool IsDirectory => Entry.IsDirectory;

    /// <summary>
    /// Whether the entry is hidden; hidden entries are drawn dimmed when shown.
    /// </summary>
    public bool IsHidden => Entry.IsHidden;

    /// <summary>
    /// The glyph drawn for the entry.
    /// </summary>
    public PhosphorIcon Icon => FileIcons.For(Entry);

    /// <summary>
    /// The Size column: the file's size, nothing for a directory.
    /// </summary>
    public string SizeText => IsDirectory ? string.Empty : ByteSize.Format(Entry.Size);

    /// <summary>
    /// The Modified column, in the user's culture.
    /// </summary>
    public string ModifiedText => Entry.LastWriteTime.ToString("g", CultureInfo.CurrentCulture);

    /// <summary>
    /// The Type column.
    /// </summary>
    public string TypeText => FileTypes.Describe(Entry);

    /// <summary>
    /// For a search hit, its folder relative to where the search started; empty otherwise.
    /// </summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// For a content hit, the line it is on.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// For a content hit, the matching line.
    /// </summary>
    public string? LineText { get; init; }

    /// <summary>
    /// The Line column of content results.
    /// </summary>
    public string LineNumberText => LineNumber?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>
    /// Whether the pane's cursor is on this entry.
    /// </summary>
    public bool IsCursor { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether the entry is marked (part of the selection operations act on).
    /// </summary>
    public bool IsMarked { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether the entry was cut and waits to be pasted; it is drawn faded until then.
    /// </summary>
    public bool IsCut { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether the name matches the pane's quick-jump pattern, which highlights it.
    /// </summary>
    public bool IsMatch { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether files dragged over this folder would be dropped into it.
    /// </summary>
    public bool IsDropTarget { get; set => SetProperty(ref field, value); }
}
