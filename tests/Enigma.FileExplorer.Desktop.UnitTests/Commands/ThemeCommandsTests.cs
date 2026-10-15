using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Commands;

public sealed class ThemeCommandsTests
{
    private readonly ExplorerHarness _explorer = new();

    [Theory]
    [InlineData("light", ThemePreference.Light)]
    [InlineData("DARK", ThemePreference.Dark)]
    [InlineData(" sys ", ThemePreference.System)]
    [InlineData("l", ThemePreference.Light)]
    public async Task Theme_SwitchesAtOnce_FromAnUnambiguousPrefix(string argument, ThemePreference expected)
    {
        await _explorer.RunAsync(CommandIds.Theme, argument);

        Assert.Equal([expected], _explorer.Theme.Applied);
        Assert.Equal($"Theme: {expected.ToString().ToLowerInvariant()}", _explorer.Status.Current?.Text);
    }

    [Fact]
    public void ColonTheme_RunsFromTheCommandLine()
    {
        _explorer.Ex("theme light");

        Assert.Equal(ThemePreference.Light, _explorer.Theme.Preference);
    }

    [Theory]
    [InlineData("purple")]
    [InlineData("")]
    public async Task AnUnknownTheme_IsReported_AndChangesNothing(string argument)
    {
        await _explorer.RunAsync(CommandIds.Theme, argument);

        Assert.Empty(_explorer.Theme.Applied);
        Assert.Equal($"Unknown theme: {argument} (dark, light or system)", _explorer.Status.Current?.Text);
    }
}
