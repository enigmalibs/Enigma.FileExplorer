using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A system clipboard in memory.
/// </summary>
public sealed class FakeSystemClipboard : ISystemClipboard
{
    /// <summary>
    /// The files on the clipboard; a test may put some there as another application would.
    /// </summary>
    public List<string> Files { get; } = [];

    /// <summary>
    /// The text on the clipboard.
    /// </summary>
    public string? Text { get; private set; }

    /// <inheritdoc />
    public Task SetTextAsync(string text)
    {
        Text = text;
        Files.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetFilesAsync(IReadOnlyList<string> paths)
    {
        Files.Clear();
        Files.AddRange(paths);
        Text = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetFilesAsync() => Task.FromResult<IReadOnlyList<string>>([.. Files]);
}
