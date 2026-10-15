namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// The user's well-known folders. A folder the platform does not define is an empty string; a folder
/// may also be defined and not exist.
/// </summary>
public interface IKnownFolders
{
    /// <summary>
    /// The user's home directory: what <c>~</c> means and where a new pane starts.
    /// </summary>
    string Home { get; }

    /// <summary>
    /// The desktop folder.
    /// </summary>
    string Desktop { get; }

    /// <summary>
    /// The documents folder.
    /// </summary>
    string Documents { get; }

    /// <summary>
    /// The downloads folder.
    /// </summary>
    string Downloads { get; }

    /// <summary>
    /// The pictures folder.
    /// </summary>
    string Pictures { get; }

    /// <summary>
    /// The music folder.
    /// </summary>
    string Music { get; }

    /// <summary>
    /// The videos folder.
    /// </summary>
    string Videos { get; }
}
