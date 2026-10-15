using System;
using System.IO;
using Enigma.FileExplorer.Core.Places;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Picks the trash of the platform the application runs on.
/// </summary>
public static class TrashFactory
{
    /// <summary>
    /// Creates the current platform's trash.
    /// </summary>
    /// <param name="folders">The user's folders, for the home trash.</param>
    /// <param name="clock">The clock deletion dates are read from.</param>
    /// <returns>The trash.</returns>
    public static ITrash Create(IKnownFolders folders, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(clock);

        if (OperatingSystem.IsWindows())
        {
            return new WindowsRecycleBin();
        }

        return OperatingSystem.IsMacOS()
            ? new MacTrash(Path.Combine(folders.Home, ".Trash"))
            : new FreedesktopTrash(FreedesktopTrash.HomeTrash(folders.Home), clock);
    }
}
