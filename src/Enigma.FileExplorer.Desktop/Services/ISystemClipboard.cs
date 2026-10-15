using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The operating system's clipboard, for exchanging files and text with other applications. Every call is
/// best effort: a platform that refuses is logged, never thrown.
/// </summary>
public interface ISystemClipboard
{
    /// <summary>
    /// Puts text on the clipboard.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A task that completes when the clipboard has it.</returns>
    Task SetTextAsync(string text);

    /// <summary>
    /// Puts files and folders on the clipboard, as a file manager's Copy does.
    /// </summary>
    /// <param name="paths">Their absolute paths.</param>
    /// <returns>A task that completes when the clipboard has them.</returns>
    Task SetFilesAsync(IReadOnlyList<string> paths);

    /// <summary>
    /// Reads the files and folders on the clipboard.
    /// </summary>
    /// <returns>Their local paths; empty when the clipboard holds none.</returns>
    Task<IReadOnlyList<string>> GetFilesAsync();
}
