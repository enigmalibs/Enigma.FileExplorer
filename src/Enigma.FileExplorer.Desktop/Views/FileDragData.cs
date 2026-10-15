using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// What a drag of files carries: the files themselves, so other applications receive real files, and their
/// paths in the explorer's own format, so drops between panes do not depend on the platform's round trip.
/// </summary>
internal static class FileDragData
{
    /// <summary>
    /// The explorer's format: absolute paths, one per line.
    /// </summary>
    public static readonly DataFormat<string> PathsFormat = DataFormat.CreateStringApplicationFormat("enigma-fileexplorer.paths");

    /// <summary>
    /// Builds what a drag of entries carries.
    /// </summary>
    /// <param name="entries">The entries dragged.</param>
    /// <param name="storage">The window's storage provider, which turns paths into files for other
    /// applications; <see langword="null"/> where the platform has none.</param>
    /// <returns>The data.</returns>
    public static async Task<DataTransfer> CreateAsync(IReadOnlyList<EntryViewModel> entries, IStorageProvider? storage)
    {
        DataTransfer data = new();
        data.Add(DataTransferItem.Create(PathsFormat, string.Join('\n', entries.Select(entry => entry.FullPath))));

        if (storage is null)
        {
            return data;
        }

        foreach (EntryViewModel entry in entries)
        {
            IStorageItem? item = entry.IsDirectory
                ? await storage.TryGetFolderFromPathAsync(entry.FullPath)
                : await storage.TryGetFileFromPathAsync(entry.FullPath);

            if (item is not null)
            {
                data.Add(DataTransferItem.CreateFile(item));
            }
        }

        return data;
    }

    /// <summary>
    /// Reads the paths a drag carries: the explorer's own list, or else the local files another application
    /// offers.
    /// </summary>
    /// <param name="data">The drag's data.</param>
    /// <returns>The absolute paths; empty when the drag carries no local files.</returns>
    public static IReadOnlyList<string> ReadPaths(IDataTransfer data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.TryGetValue(PathsFormat) is { Length: > 0 } text)
        {
            return text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        return data.TryGetFiles() is { } files ? [.. files.Select(file => file.TryGetLocalPath()).OfType<string>()] : [];
    }
}
