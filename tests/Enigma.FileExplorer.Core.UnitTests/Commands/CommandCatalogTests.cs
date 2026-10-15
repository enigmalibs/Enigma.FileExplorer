using System.Linq;
using System.Reflection;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Commands;

/// <summary>
/// Guards the consistency of the default catalog: a broken keymap would only show as a dead key at run time.
/// </summary>
public sealed class CommandCatalogTests
{
    private static readonly CommandCatalog Catalog = CommandCatalog.Default;

    [Fact]
    public void EveryCommandId_HasADescriptor()
    {
        string[] ids = [.. typeof(CommandIds).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => (string)field.GetValue(null)!)];

        Assert.All(ids, id => Assert.NotNull(Catalog.Find(id)));
        Assert.Equal(ids.Length, Catalog.Commands.Count);
    }

    [Fact]
    public void EveryBinding_NamesAKnownCommand()
        => Assert.All(Catalog.Keymap.Bindings, binding => Assert.NotNull(Catalog.Find(binding.CommandId)));

    [Fact]
    public void EveryExCommand_NamesKnownCommands()
    {
        Assert.All(Catalog.ExCommands, definition =>
        {
            Assert.NotNull(Catalog.Find(definition.CommandId));

            if (definition.BangCommandId is { } bang)
            {
                Assert.NotNull(Catalog.Find(bang));
            }
        });
    }

    [Fact]
    public void EveryExAbbreviation_IsUnambiguous()
    {
        Assert.All(Catalog.ExCommands, definition =>
        {
            ExCommandDefinition[] matches = [.. Catalog.ExCommands.Where(other => other.Matches(definition.ShortName))];

            Assert.Equal([definition], matches);
        });
    }

    [Fact]
    public void TheDefaultKeymap_IsPrefixFree()
    {
        // The Keymap constructor throws on a prefix; rebuilding proves the default one passed that check.
        Keymap rebuilt = new(Catalog.Keymap.Bindings);

        Assert.Equal(Catalog.Keymap.Bindings.Count, rebuilt.Bindings.Count);
    }

    [Theory]
    [InlineData(CommandIds.Parent, "h")]
    [InlineData(CommandIds.SplitVertical, "Space v")]
    [InlineData(CommandIds.FocusLeft, "gh")]
    [InlineData(CommandIds.ToggleMark, "m")]
    [InlineData(CommandIds.DeletePermanently, "D")]
    [InlineData(CommandIds.Trash, "d")]
    [InlineData(CommandIds.Cut, "x")]
    [InlineData(CommandIds.Theme, null)]
    public void PrimaryGesture_IsTheFirstBinding(string commandId, string? expected)
        => Assert.Equal(expected, Catalog.PrimaryGesture(commandId));

    [Fact]
    public void PrimaryGesture_MatchesAnArgument() => Assert.Equal("g3", Catalog.PrimaryGesture(CommandIds.Place, "3"));

    [Theory]
    [InlineData("j", CommandIds.CursorDown)]
    [InlineData("gg", CommandIds.CursorFirst)]
    [InlineData("gh", CommandIds.FocusLeft)]
    [InlineData("gj", CommandIds.FocusDown)]
    [InlineData("gk", CommandIds.FocusUp)]
    [InlineData("gl", CommandIds.FocusRight)]
    [InlineData("gH", CommandIds.Home)]
    [InlineData("gf", CommandIds.Reveal)]
    [InlineData("m", CommandIds.ToggleMark)]
    [InlineData("<Insert>", CommandIds.ToggleMark)]
    [InlineData("<Space>v", CommandIds.SplitVertical)]
    [InlineData("<Space>w", CommandIds.FocusNext)]
    [InlineData("<Space><lt>", CommandIds.Narrower)]
    [InlineData("<Space>>", CommandIds.Wider)]
    [InlineData("zh", CommandIds.ToggleHidden)]
    [InlineData("d", CommandIds.Trash)]
    [InlineData("<Del>", CommandIds.Trash)]
    [InlineData("D", CommandIds.DeletePermanently)]
    [InlineData("<S-Del>", CommandIds.DeletePermanently)]
    [InlineData("x", CommandIds.Cut)]
    [InlineData("<C-x>", CommandIds.Cut)]
    [InlineData("yp", CommandIds.CopyPath)]
    [InlineData("<C-S-f>", CommandIds.FindContent)]
    [InlineData(":", CommandIds.CommandLine)]
    public void DefaultKeys_ResolveToTheirCommands(string notation, string commandId)
        => Assert.Equal(commandId, Resolve(Catalog, notation));

    [Fact]
    public void CtrlW_IsNoLongerBound()
        => Assert.DoesNotContain(Catalog.Keymap.Bindings, binding => binding.Keys[0] == new KeyStroke("w", StrokeModifiers.Control));

    [Fact]
    public void TheDefaultLeader_IsSpace()
    {
        Assert.Equal(new KeyStroke(SpecialKeys.Space), CommandCatalog.DefaultLeader);
        Assert.Equal(CommandCatalog.DefaultLeader, Catalog.Leader);
    }

    [Theory]
    [InlineData(",", ",v")]
    [InlineData("<C-Space>", "<C-Space>v")]
    [InlineData("\\", "\\v")]
    public void TryCreate_WithAnotherLeader_StartsTheSplitGroupKeysWithIt(string leader, string split)
    {
        Assert.True(CommandCatalog.TryCreate(leader, out CommandCatalog catalog, out string? error));

        Assert.Null(error);
        Assert.Equal(KeyNotation.Parse(leader)[0], catalog.Leader);
        Assert.Equal(CommandIds.SplitVertical, Resolve(catalog, split));
        Assert.Equal(CommandIds.ClosePane, Resolve(catalog, split[..^1] + "c"));
        Assert.DoesNotContain(catalog.Keymap.Bindings, binding => binding.Keys[0] == CommandCatalog.DefaultLeader);

        // Rebuilding proves the keymap with that leader is prefix-free too.
        Assert.Equal(catalog.Keymap.Bindings.Count, new Keymap(catalog.Keymap.Bindings).Bindings.Count);
    }

    [Fact]
    public void TryCreate_WithTheDefaultLeader_GivesTheDefaultCatalog()
    {
        Assert.True(CommandCatalog.TryCreate("<Space>", out CommandCatalog catalog, out string? error));

        Assert.Same(CommandCatalog.Default, catalog);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("ab", "single key")]
    [InlineData("5", "digit")]
    [InlineData("j", "already used")]
    [InlineData("o", "already used")]
    [InlineData("<Esc>", "already used")]
    public void TryCreate_WithAnUnusableLeader_FallsBackToTheDefault(string leader, string reason)
    {
        Assert.False(CommandCatalog.TryCreate(leader, out CommandCatalog catalog, out string? error));

        Assert.Same(CommandCatalog.Default, catalog);
        Assert.Contains(reason, error, System.StringComparison.Ordinal);
    }

    private static string? Resolve(CommandCatalog catalog, string notation)
    {
        KeySequenceResolver resolver = new(catalog.Keymap);
        KeyResolution resolution = default;

        foreach (KeyStroke stroke in KeyNotation.Parse(notation))
        {
            resolution = resolver.Feed(stroke);
        }

        return resolution.Kind == KeyResolutionKind.Matched ? resolution.Binding?.CommandId : null;
    }
}
