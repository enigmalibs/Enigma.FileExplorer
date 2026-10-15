using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records shutdown requests instead of ending the test run.
/// </summary>
public sealed class FakeAppShutdown : IAppShutdown
{
    /// <summary>
    /// How many times shutdown was requested.
    /// </summary>
    public int Requests { get; private set; }

    /// <inheritdoc />
    public void Shutdown() => Requests++;
}
