namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// Creates panes: one per split group, each with its own state over the shared services.
/// </summary>
public interface IPaneFactory
{
    /// <summary>
    /// Creates a pane that has not loaded any directory yet.
    /// </summary>
    /// <returns>The pane.</returns>
    PaneViewModel Create();
}
