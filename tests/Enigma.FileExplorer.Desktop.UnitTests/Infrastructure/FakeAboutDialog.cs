using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Counts how often the About dialog was shown.
/// </summary>
public sealed class FakeAboutDialog : IAboutDialogService
{
    /// <summary>
    /// How many times the dialog was shown.
    /// </summary>
    public int Shown { get; private set; }

    /// <inheritdoc />
    public Task ShowAsync()
    {
        Shown++;
        return Task.CompletedTask;
    }
}
