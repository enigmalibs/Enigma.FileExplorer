namespace Enigma.FileExplorer.Desktop.Commands;

/// <summary>
/// A group of command handlers belonging to one feature (navigation, splits, files…). Every module
/// registered in the container is handed to the dispatcher when it is built.
/// </summary>
public interface ICommandModule
{
    /// <summary>
    /// Registers the module's handlers.
    /// </summary>
    /// <param name="registry">Where to register them.</param>
    void Register(ICommandRegistry registry);
}
