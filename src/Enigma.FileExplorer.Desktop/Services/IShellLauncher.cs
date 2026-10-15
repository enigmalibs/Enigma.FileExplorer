using System.Diagnostics.CodeAnalysis;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Hands files to the operating system to open with their default application.
/// </summary>
public interface IShellLauncher
{
    /// <summary>
    /// Opens a file with the application the operating system associates with it.
    /// </summary>
    /// <param name="path">The file to open.</param>
    /// <param name="error">Why it could not be opened, when it could not.</param>
    /// <returns><see langword="true"/> when the operating system accepted the request.</returns>
    bool TryOpen(string path, [NotNullWhen(false)] out string? error);
}
