namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Ends the application (<c>:qa</c>, or <c>:q</c> on the last pane).
/// </summary>
public interface IAppShutdown
{
    /// <summary>
    /// Asks the application to shut down.
    /// </summary>
    void Shutdown();
}
