using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

/// <summary>
/// Opens the context menus on the real window, with the mouse and with the Menu key.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class PaneViewContextMenuTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void RightClickingAnEntry_TargetsIt_AndOpensTheItemMenu()
    {
        string? cursor = null;
        int marked = -1;
        string[] titles = [];

        RunOnWindow(async (window, viewModel) =>
        {
            PaneView view = window.ActivePane;
            Control row = view.ListControl.ContainerFromIndex(2) ?? throw new InvalidOperationException("Row 2 is not realized.");
            Point point = row.TranslatePoint(new Point(60, 10), window) ?? default;

            viewModel.Pane.Click(viewModel.Pane.Items[0], ClickModifiers.Toggle);
            window.MouseDown(point, MouseButton.Right);
            window.MouseUp(point, MouseButton.Right);
            await DrainAsync();

            cursor = viewModel.Pane.CursorItem?.Name;
            marked = viewModel.Pane.MarkedCount;
            titles = Titles(view.OpenMenu);
            view.OpenMenu?.Close();
        });

        Assert.Equal("b.txt", cursor);
        Assert.Equal(0, marked);
        Assert.Equal(["Open", "-", "Cut", "Copy", "Copy path", "-", "Rename", "Move to trash", "Delete permanently"], titles);
    }

    [Fact]
    public void RightClickingTheEmptyArea_OpensTheBackgroundMenu_WhoseEntriesRunTheirCommands()
    {
        string[] titles = [];
        ViewMode mode = ViewMode.Details;
        bool closedFocusesTheListing = false;

        RunOnWindow(async (window, viewModel) =>
        {
            PaneView view = window.ActivePane;
            Point point = view.ListControl.TranslatePoint(new Point(200, view.ListControl.Bounds.Height - 20), window) ?? default;

            window.MouseDown(point, MouseButton.Right);
            window.MouseUp(point, MouseButton.Right);
            await DrainAsync();

            ContextMenu menu = view.OpenMenu ?? throw new InvalidOperationException("No menu opened.");
            titles = Titles(menu);
            MenuItem viewMenu = menu.Items.OfType<MenuItem>().Single(item => ((MenuEntry)item.Tag!).Title == "View");
            MenuItem large = viewMenu.Items.OfType<MenuItem>().Single(item => ((MenuEntry)item.Tag!).Title == "Large icons");

            large.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            await DrainAsync();

            mode = viewModel.Pane.ViewMode;
            closedFocusesTheListing = view.IsFocused;
        });

        Assert.Equal(["New folder", "New file", "Paste", "-", "View", "Sort by", "Show hidden files", "Refresh", "-", "Split", "Close pane"], titles);
        Assert.Equal(ViewMode.LargeIcons, mode);
        Assert.True(closedFocusesTheListing);
    }

    [Fact]
    public void TheMenuKey_OpensTheItemMenuAtTheCursor()
    {
        string[] titles = [];
        bool belowTheRow = false;

        RunOnWindow(async (window, viewModel) =>
        {
            viewModel.Pane.MoveCursorTo(1);
            await DrainAsync();

            window.KeyPress(Key.Apps, RawInputModifiers.None, PhysicalKey.ContextMenu, string.Empty);
            await DrainAsync();

            PaneView view = window.ActivePane;
            ContextMenu? menu = view.OpenMenu;
            titles = Titles(menu);
            Control row = view.ListControl.ContainerFromIndex(1) ?? throw new InvalidOperationException("Row 1 is not realized.");
            belowTheRow = (menu is not null) && ((menu.PlacementRect?.Bottom ?? -1) - (row.TranslatePoint(new Point(0, row.Bounds.Height), view)?.Y ?? 0)) is > -0.5 and < 0.5;
            menu?.Close();
        });

        Assert.Equal("Open", titles.FirstOrDefault());
        Assert.True(belowTheRow);
    }

    private static string[] Titles(ContextMenu? menu)
        => menu is null ? [] : [.. menu.Items.Select(item => item is MenuItem { Tag: MenuEntry entry } ? entry.Title : "-")];

    private static async Task DrainAsync()
    {
        // The menu opens after the clipboard is read; let that and the layout settle.
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }

        Dispatcher.UIThread.RunJobs();
    }

    private void RunOnWindow(Func<MainWindow, MainWindowViewModel, Task> test)
    {
        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"));

            using ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(reader);
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
                services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
                services.AddSingleton<ISystemClipboard>(new FakeSystemClipboard());
            });

            MainWindow window = provider.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
            App.RegisterHosts(provider, window);
            window.DataContext = viewModel;
            window.Show();
            await viewModel.StartAsync();
            window.ActivePane.Focus();
            Dispatcher.UIThread.RunJobs();

            try
            {
                await test(window, viewModel);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
