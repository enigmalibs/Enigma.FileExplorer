using System;
using System.IO;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// One entry of a directory listing, captured at the moment the directory was read.
/// </summary>
/// <param name="Name">The entry's name, without its directory.</param>
/// <param name="FullPath">The entry's absolute path.</param>
/// <param name="Kind">Whether it is a directory or a file.</param>
/// <param name="Size">The size in bytes of a file; 0 for a directory.</param>
/// <param name="LastWriteTime">When the entry was last written, in local time.</param>
/// <param name="IsHidden">Whether the entry is hidden (see <see cref="HiddenRules"/>).</param>
/// <param name="IsSymbolicLink">Whether the entry is a symbolic link, junction or other reparse point.</param>
public sealed record FileSystemEntry(
    string Name,
    string FullPath,
    EntryKind Kind,
    long Size,
    DateTime LastWriteTime,
    bool IsHidden,
    bool IsSymbolicLink)
{
    /// <summary>
    /// Whether the entry is a directory.
    /// </summary>
    public bool IsDirectory => Kind == EntryKind.Directory;

    /// <summary>
    /// The extension of a file, with its leading dot (<c>.txt</c>); empty for a directory or a file without one.
    /// </summary>
    /// <remarks>
    /// A dot-file such as <c>.gitignore</c> has no extension: its whole name is the stem, as every file
    /// manager shows it.
    /// </remarks>
    public string Extension
    {
        get
        {
            if (IsDirectory)
            {
                return string.Empty;
            }

            int dot = Name.LastIndexOf('.');
            return dot > 0 ? Name[dot..] : string.Empty;
        }
    }

    /// <summary>
    /// Captures an entry from a <see cref="FileSystemInfo"/>.
    /// </summary>
    /// <param name="info">The file-system object.</param>
    /// <returns>The entry.</returns>
    public static FileSystemEntry FromInfo(FileSystemInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        bool isDirectory = info is DirectoryInfo;
        FileAttributes attributes = info.Attributes;

        return new FileSystemEntry(
            info.Name,
            info.FullName,
            isDirectory ? EntryKind.Directory : EntryKind.File,
            info is FileInfo file ? file.Length : 0,
            info.LastWriteTime,
            HiddenRules.IsHidden(info.Name, attributes),
            (attributes & FileAttributes.ReparsePoint) != 0);
    }
}
