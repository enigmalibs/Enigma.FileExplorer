using System.Collections.Generic;
using System.Linq;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// The help's cards: one per category, with an icon, a count, and the leader key where it matters.
/// </summary>
public sealed class HelpViewModelTests
{
    [Fact]
    public void EveryCategory_IsACard_WithItsOwnIcon_AndItsCount()
    {
        IReadOnlyList<HelpSection> sections = HelpViewModel.Build(CommandCatalog.Default);

        Assert.Equal(
            [
                CommandCatalog.NavigationCategory,
                CommandCatalog.MarksCategory,
                CommandCatalog.WindowsCategory,
                CommandCatalog.ViewCategory,
                CommandCatalog.FilesCategory,
                CommandCatalog.SearchCategory,
                CommandCatalog.BookmarksCategory,
                CommandCatalog.GeneralCategory,
            ],
            sections.Select(section => section.Category));
        Assert.Equal(sections.Count, sections.Select(section => section.Icon).Distinct().Count());
        Assert.DoesNotContain(sections, section => section.Icon == PhosphorIcon.Info);
        Assert.All(sections, section => Assert.StartsWith($"{section.Rows.Count} command", section.Description, System.StringComparison.Ordinal));
    }

    [Fact]
    public void TheSplitGroupsCard_NamesTheLeaderKey_AndWhereItIsSet()
    {
        Assert.True(CommandCatalog.TryCreate(",", out CommandCatalog custom, out _));

        HelpSection space = HelpViewModel.Build(CommandCatalog.Default).Single(section => section.Category == CommandCatalog.WindowsCategory);
        HelpSection comma = HelpViewModel.Build(custom).Single(section => section.Category == CommandCatalog.WindowsCategory);

        Assert.EndsWith("· Leader key: Space (leaderKey in settings.json)", space.Description, System.StringComparison.Ordinal);
        Assert.EndsWith("· Leader key: , (leaderKey in settings.json)", comma.Description, System.StringComparison.Ordinal);
        Assert.Contains(comma.Rows, row => row.Keys == ",v");
    }

    [Fact]
    public void CloseCommand_RunsTheCloseCallback()
    {
        int closed = 0;
        HelpViewModel help = new(CommandCatalog.Default, () => closed++);

        help.CloseCommand.Execute(null);

        Assert.Equal(1, closed);
    }
}
