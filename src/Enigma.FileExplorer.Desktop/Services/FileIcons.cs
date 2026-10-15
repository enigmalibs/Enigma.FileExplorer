using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.Icons.Phosphor;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Chooses the Phosphor glyph for entries and places: one per family of file types, so a listing can
/// be scanned by shape before it is read.
/// </summary>
public static class FileIcons
{
    private static readonly FrozenDictionary<string, PhosphorIcon> ByExtension = BuildExtensionMap();

    /// <summary>
    /// The glyph for an entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The glyph.</returns>
    public static PhosphorIcon For(FileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsDirectory)
        {
            // A dashed folder tells a link or junction apart from the real directory it points to.
            return entry.IsSymbolicLink ? PhosphorIcon.FolderDashed : PhosphorIcon.Folder;
        }

        return ByExtension.GetValueOrDefault(entry.Extension, PhosphorIcon.File);
    }

    /// <summary>
    /// The glyph for a place of the sidebar.
    /// </summary>
    /// <param name="kind">The place's kind.</param>
    /// <returns>The glyph.</returns>
    public static PhosphorIcon For(PlaceKind kind) => kind switch
    {
        PlaceKind.Home => PhosphorIcon.House,
        PlaceKind.Desktop => PhosphorIcon.Desktop,
        PlaceKind.Documents => PhosphorIcon.Files,
        PlaceKind.Downloads => PhosphorIcon.DownloadSimple,
        PlaceKind.Pictures => PhosphorIcon.Image,
        PlaceKind.Music => PhosphorIcon.MusicNotes,
        PlaceKind.Videos => PhosphorIcon.FilmStrip,
        PlaceKind.Root => PhosphorIcon.HardDrives,
        PlaceKind.RemovableDrive => PhosphorIcon.Usb,
        PlaceKind.NetworkDrive => PhosphorIcon.Network,
        PlaceKind.OpticalDrive => PhosphorIcon.Disc,
        _ => PhosphorIcon.HardDrive,
    };

    private static FrozenDictionary<string, PhosphorIcon> BuildExtensionMap()
    {
        Dictionary<string, PhosphorIcon> map = new(StringComparer.OrdinalIgnoreCase);

        void Add(PhosphorIcon icon, params string[] extensions)
        {
            foreach (string extension in extensions)
            {
                map[extension] = icon;
            }
        }

        Add(PhosphorIcon.FileText, ".txt", ".log", ".cfg", ".conf", ".text", ".nfo");
        Add(PhosphorIcon.FileIni, ".ini", ".env", ".editorconfig");
        Add(PhosphorIcon.FileMd, ".md", ".markdown");
        Add(PhosphorIcon.FileCsv, ".csv", ".tsv");
        Add(PhosphorIcon.FileCode, ".json", ".xml", ".yml", ".yaml", ".toml", ".sh", ".bash", ".zsh", ".ps1", ".psm1",
            ".bat", ".cmd", ".go", ".java", ".kt", ".rb", ".php", ".lua", ".swift", ".dart", ".csproj", ".sln", ".slnx",
            ".props", ".targets", ".axaml", ".xaml", ".gradle", ".cmake", ".mk");
        Add(PhosphorIcon.FileCSharp, ".cs", ".csx");
        Add(PhosphorIcon.FileC, ".c", ".h");
        Add(PhosphorIcon.FileCpp, ".cpp", ".cc", ".cxx", ".hpp", ".hh");
        Add(PhosphorIcon.FileJs, ".js", ".mjs", ".cjs");
        Add(PhosphorIcon.FileTs, ".ts", ".mts", ".cts");
        Add(PhosphorIcon.FileJsx, ".jsx");
        Add(PhosphorIcon.FileTsx, ".tsx");
        Add(PhosphorIcon.FilePy, ".py", ".pyw");
        Add(PhosphorIcon.FileRs, ".rs");
        Add(PhosphorIcon.FileSql, ".sql");
        Add(PhosphorIcon.FileHtml, ".html", ".htm", ".xhtml");
        Add(PhosphorIcon.FileCss, ".css", ".scss", ".sass", ".less");
        Add(PhosphorIcon.FileVue, ".vue");
        Add(PhosphorIcon.FilePng, ".png");
        Add(PhosphorIcon.FileJpg, ".jpg", ".jpeg");
        Add(PhosphorIcon.FileSvg, ".svg");
        Add(PhosphorIcon.FileImage, ".gif", ".bmp", ".webp", ".tif", ".tiff", ".ico", ".heic", ".avif", ".raw", ".psd", ".xcf");
        Add(PhosphorIcon.FileAudio, ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac", ".wma", ".opus");
        Add(PhosphorIcon.FileVideo, ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".m4v", ".mpg", ".mpeg");
        Add(PhosphorIcon.FileZip, ".zip");
        Add(PhosphorIcon.FileArchive, ".7z", ".rar", ".tar", ".gz", ".tgz", ".bz2", ".xz", ".zst", ".cab", ".iso");
        Add(PhosphorIcon.FilePdf, ".pdf");
        Add(PhosphorIcon.FileDoc, ".doc", ".docx", ".odt", ".rtf");
        Add(PhosphorIcon.FileXls, ".xls", ".xlsx", ".ods");
        Add(PhosphorIcon.FilePpt, ".ppt", ".pptx", ".odp");
        Add(PhosphorIcon.Package, ".exe", ".msi", ".deb", ".rpm", ".appimage", ".nupkg", ".apk", ".dmg", ".pkg");
        Add(PhosphorIcon.FileLock, ".key", ".pem", ".pfx", ".p12", ".crt", ".cer", ".gpg", ".kdbx");

        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
