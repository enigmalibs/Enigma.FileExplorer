using Enigma.FileExplorer.Core.Places;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Known folders pointing wherever a test wants; the folders other than home are undefined by default.
/// </summary>
/// <param name="home">The home directory.</param>
public sealed class FakeKnownFolders(string home) : IKnownFolders
{
    /// <inheritdoc />
    public string Home { get; set; } = home;

    /// <inheritdoc />
    public string Desktop { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Documents { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Downloads { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Pictures { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Music { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Videos { get; set; } = string.Empty;
}
