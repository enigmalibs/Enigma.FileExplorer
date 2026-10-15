namespace Enigma.FileExplorer.Core.Places;

/// <summary>
/// What a place is, which decides its icon and its section of the sidebar.
/// </summary>
public enum PlaceKind
{
    /// <summary>
    /// The home directory.
    /// </summary>
    Home,

    /// <summary>
    /// The desktop folder.
    /// </summary>
    Desktop,

    /// <summary>
    /// The documents folder.
    /// </summary>
    Documents,

    /// <summary>
    /// The downloads folder.
    /// </summary>
    Downloads,

    /// <summary>
    /// The pictures folder.
    /// </summary>
    Pictures,

    /// <summary>
    /// The music folder.
    /// </summary>
    Music,

    /// <summary>
    /// The videos folder.
    /// </summary>
    Videos,

    /// <summary>
    /// The root of a Unix file system.
    /// </summary>
    Root,

    /// <summary>
    /// A fixed disk.
    /// </summary>
    FixedDrive,

    /// <summary>
    /// A removable disk, such as a USB stick.
    /// </summary>
    RemovableDrive,

    /// <summary>
    /// A network share.
    /// </summary>
    NetworkDrive,

    /// <summary>
    /// An optical disc.
    /// </summary>
    OpticalDrive,
}
