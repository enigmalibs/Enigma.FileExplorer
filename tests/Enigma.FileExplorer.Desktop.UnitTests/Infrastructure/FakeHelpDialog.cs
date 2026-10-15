using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Counts how often the help was shown.
/// </summary>
public sealed class FakeHelpDialog : IHelpDialogService
{
    /// <summary>
    /// How many times the help was shown.
    /// </summary>
    public int Shown { get; private set; }

    /// <inheritdoc />
    public Task ShowAsync()
    {
        Shown++;
        return Task.CompletedTask;
    }
}
