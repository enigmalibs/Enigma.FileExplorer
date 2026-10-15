using System;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private readonly ExplorerHarness _explorer = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelTests()
    {
        _viewModel = _explorer.Window;
        _explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "a.txt", size: 2048), File(PathOf("home"), "b.txt", size: 1024));
    }

    [Fact]
    public void Title_BeforeTheFirstLoad_IsTheProduct()
        => Assert.Equal(MainWindowViewModel.ProductName, _viewModel.Title);

    [Fact]
    public async Task Title_NamesTheCurrentDirectory()
    {
        await _viewModel.StartAsync();

        Assert.Equal($"home — {MainWindowViewModel.ProductName}", _viewModel.Title);
    }

    [Fact]
    public async Task SummaryText_CountsItemsAndMarks()
    {
        await _viewModel.StartAsync();
        Assert.Equal("2 items", _viewModel.SummaryText);

        _viewModel.Pane.MarkAll();

        Assert.StartsWith("2 items · 2 marked (3", _viewModel.SummaryText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task APlaceRequest_NavigatesThePaneAndHighlightsThePlace()
    {
        string docs = _explorer.Reader.Add(PathOf("docs"));
        _explorer.PlacesProvider.Places.Add(new Place("Documents", docs, PlaceKind.Documents));
        await _viewModel.StartAsync();

        _viewModel.Places.Open(_viewModel.Places.All[0]);
        await Task.Yield();

        Assert.Equal(docs, _viewModel.Pane.CurrentPath);
        Assert.True(_viewModel.Places.All[0].IsCurrent);
    }

    [Fact]
    public async Task SummaryText_UsesTheSingularForOneItem()
    {
        _explorer.Reader.Add(PathOf("home"), File(PathOf("home"), "only.txt"));

        await _viewModel.StartAsync();

        Assert.Equal("1 item", _viewModel.SummaryText);
    }

    [Fact]
    public void TheStatusLine_ShowsTheModeAndPendingKeys()
    {
        Assert.Equal("NORMAL", _viewModel.Keyboard.ModeText);

        _explorer.Press("4");

        Assert.Equal("4", _viewModel.Keyboard.PendingKeys);
    }
}
