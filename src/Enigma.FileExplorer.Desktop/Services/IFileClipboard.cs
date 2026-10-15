using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The explorer's own clipboard of files: what was copied or cut, and how. It is authoritative for cut,
/// which the operating system's clipboard cannot carry portably, and mirrors files to it for other
/// applications.
/// </summary>
public interface IFileClipboard : INotifyPropertyChanged
{
    /// <summary>
    /// What was copied or cut, by absolute path; empty when nothing was.
    /// </summary>
    IReadOnlyList<string> Paths { get; }

    /// <summary>
    /// Whether the paths were copied or cut.
    /// </summary>
    TransferMode Mode { get; }

    /// <summary>
    /// The paths that were cut, which panes show dimmed; empty after a copy.
    /// </summary>
    IReadOnlySet<string> CutPaths { get; }

    /// <summary>
    /// Records a copy or a cut, and offers the files to other applications.
    /// </summary>
    /// <param name="paths">The entries.</param>
    /// <param name="mode">Copy or cut (move).</param>
    /// <returns>A task that completes when the system clipboard has been offered the files.</returns>
    Task SetAsync(IReadOnlyList<string> paths, TransferMode mode);

    /// <summary>
    /// What a paste should transfer: the explorer's own clipboard, unless files copied in another
    /// application have replaced it on the system clipboard since.
    /// </summary>
    /// <returns>The paths and the mode; no paths when there is nothing to paste.</returns>
    Task<(IReadOnlyList<string> Paths, TransferMode Mode)> GetForPasteAsync();

    /// <summary>
    /// Whether a paste would transfer anything: something was copied or cut here, or another application
    /// put files on the system clipboard.
    /// </summary>
    /// <returns><see langword="true"/> when there is something to paste.</returns>
    Task<bool> CanPasteAsync();

    /// <summary>
    /// Forgets the content, as after pasting what was cut.
    /// </summary>
    void Clear();
}
