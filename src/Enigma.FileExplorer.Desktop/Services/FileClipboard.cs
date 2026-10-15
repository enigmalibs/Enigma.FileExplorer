using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The explorer's clipboard of files, mirrored to the system clipboard.
/// </summary>
/// <param name="system">The system clipboard.</param>
public sealed class FileClipboard(ISystemClipboard system) : ObservableObject, IFileClipboard
{
    private static readonly IReadOnlySet<string> NothingCut = new HashSet<string>();

    /// <inheritdoc />
    public IReadOnlyList<string> Paths { get; private set => SetProperty(ref field, value); } = [];

    /// <inheritdoc />
    public TransferMode Mode { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public IReadOnlySet<string> CutPaths { get; private set => SetProperty(ref field, value); } = NothingCut;

    /// <inheritdoc />
    public async Task SetAsync(IReadOnlyList<string> paths, TransferMode mode)
    {
        ArgumentNullException.ThrowIfNull(paths);

        Paths = [.. paths];
        Mode = mode;
        CutPaths = mode == TransferMode.Move ? new HashSet<string>(paths, StringComparer.FromComparison(PathUtilities.Comparison)) : NothingCut;

        // Other applications see a copy either way: cut cannot be expressed portably on the system clipboard.
        await system.SetFilesAsync(Paths);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<string> Paths, TransferMode Mode)> GetForPasteAsync()
    {
        IReadOnlyList<string> systemFiles = await system.GetFilesAsync();

        if ((systemFiles.Count == 0) || SameFiles(systemFiles, Paths))
        {
            return (Paths, Mode);
        }

        // Files copied in another application since: they are what the user means to paste now.
        Clear();
        return (systemFiles, TransferMode.Copy);
    }

    /// <inheritdoc />
    public async Task<bool> CanPasteAsync() => (Paths.Count > 0) || ((await system.GetFilesAsync()).Count > 0);

    /// <inheritdoc />
    public void Clear()
    {
        Paths = [];
        Mode = TransferMode.Copy;
        CutPaths = NothingCut;
    }

    private static bool SameFiles(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        HashSet<string> set = new(first.Select(PathUtilities.Normalize), StringComparer.FromComparison(PathUtilities.Comparison));
        return second.All(path => set.Contains(PathUtilities.Normalize(path)));
    }
}
