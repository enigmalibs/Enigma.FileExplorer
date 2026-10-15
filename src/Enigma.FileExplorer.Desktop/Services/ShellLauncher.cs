using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Opens files through the platform's own launcher: the shell on Windows, <c>xdg-open</c> on Linux and
/// <c>open</c> on macOS.
/// </summary>
/// <param name="logger">Where launch failures are recorded.</param>
public sealed class ShellLauncher(ILogger<ShellLauncher> logger) : IShellLauncher
{
    /// <inheritdoc />
    public bool TryOpen(string path, [NotNullWhen(false)] out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            using Process? process = Process.Start(CreateStartInfo(path));
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            logger.LogWarning(exception, "Could not open {Path}", path);
            error = $"Cannot open '{Path.GetFileName(path)}': {exception.Message}";
            return false;
        }
    }

    /// <summary>
    /// Builds the start information that opens a file on the current platform.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The start information.</returns>
    internal static ProcessStartInfo CreateStartInfo(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            // The shell resolves the file association, exactly as a double-click in Explorer does.
            return new ProcessStartInfo(path) { UseShellExecute = true };
        }

        // The path travels as one argument, never through a shell, so no file name can inject a command.
        ProcessStartInfo startInfo = new(OperatingSystem.IsMacOS() ? "open" : "xdg-open") { UseShellExecute = false };
        startInfo.ArgumentList.Add(path);
        return startInfo;
    }
}
