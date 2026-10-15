using System.Collections.Generic;
using Enigma.FileExplorer.Core.Settings;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Settings in memory: loads what a test put there, records every save.
/// </summary>
public sealed class FakeSettingsStore : ISettingsStore
{
    /// <summary>
    /// What the next load returns.
    /// </summary>
    public AppSettings Stored { get; set; } = new();

    /// <summary>
    /// Every save, in order.
    /// </summary>
    public List<AppSettings> Saved { get; } = [];

    /// <inheritdoc />
    public AppSettings Load() => Stored;

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        Saved.Add(settings);
        Stored = settings;
    }
}
