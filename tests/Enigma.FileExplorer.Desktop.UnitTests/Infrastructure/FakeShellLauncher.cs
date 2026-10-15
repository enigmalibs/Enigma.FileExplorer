using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records the files it is asked to open, and fails on demand.
/// </summary>
public sealed class FakeShellLauncher : IShellLauncher
{
    /// <summary>
    /// Every path opened, in order.
    /// </summary>
    public List<string> Opened { get; } = [];

    /// <summary>
    /// When set, every open fails with this message.
    /// </summary>
    public string? FailWith { get; set; }

    /// <inheritdoc />
    public bool TryOpen(string path, [NotNullWhen(false)] out string? error)
    {
        Opened.Add(path);
        error = FailWith;
        return error is null;
    }
}
