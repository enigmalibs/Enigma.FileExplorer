using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Shows the key-binding help.
/// </summary>
public interface IHelpDialogService
{
    /// <summary>
    /// Shows the help and waits until it is closed.
    /// </summary>
    /// <returns>A task that completes when the help is closed.</returns>
    Task ShowAsync();
}
