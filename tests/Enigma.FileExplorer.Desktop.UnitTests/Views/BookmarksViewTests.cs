using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.FileSystem;
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
/// The bookmarks dialog in the real window: b opens it with the list taking the keyboard, j and Enter open
/// a bookmark in the active pane, d removes one, q closes.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class BookmarksViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void B_OpensTheBookmarks_JEnterOpensOne_DRemoves_QCloses()
    {
        bool listFocused = false;
        double cardOverflow = double.NaN;
        string? openedIn = null;
        bool closedAfterEnter = false;
        string[] afterRemove = [];
        bool closedAfterQ = false;
        bool paneFocusedAfterQ = false;

        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), Directory(PathOf("home"), "docs"), File(PathOf("home"), "a.txt"));
            reader.Add(PathOf("home", "docs"), File(PathOf("home", "docs"), "b.txt"));

            using ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(reader);
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
                services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
            });

            MainWindow window = provider.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
            App.RegisterHosts(provider, window);
            window.DataContext = viewModel;
            window.Show();
            await viewModel.StartAsync();
            Bookmarks bookmarks = provider.GetRequiredService<Bookmarks>();
            bookmarks.Add(PathOf("home"));
            bookmarks.Add(PathOf("home", "docs"));
            window.ActivePane.Focus();
            Dispatcher.UIThread.RunJobs();

            Press(window, Key.B, "b");
            BookmarksView view = window.HostDialog.GetVisualDescendants().OfType<BookmarksView>().Single();
            listFocused = view.ListControl.IsKeyboardFocusWithin;
            ScrollViewer? card = view.FindAncestorOfType<ScrollViewer>();
            cardOverflow = card is null ? 0 : card.Extent.Height - card.Viewport.Height;

            Press(window, Key.J, "j");
            Press(window, Key.Enter, "\r");
            await CloseIfStillOpenAsync(provider, window);
            await viewModel.Keyboard.LastCommand;
            Dispatcher.UIThread.RunJobs();
            closedAfterEnter = !window.HostDialog.IsOpen;
            openedIn = viewModel.Pane.CurrentPath;

            window.ActivePane.Focus();
            Dispatcher.UIThread.RunJobs();
            Press(window, Key.B, "b");
            Press(window, Key.D, "d");
            afterRemove = [.. bookmarks.Items.Select(bookmark => bookmark.Name)];

            Press(window, Key.Q, "q");
            closedAfterQ = !window.HostDialog.IsOpen;
            await CloseIfStillOpenAsync(provider, window);
            await viewModel.Keyboard.LastCommand;
            Dispatcher.UIThread.RunJobs();
            paneFocusedAfterQ = window.ActivePane.IsKeyboardFocusWithin;

            window.Close();
        });

        Assert.True(listFocused, "The list takes the keyboard as the dialog opens.");
        Assert.True(cardOverflow <= 0.5, $"The dialog card scrolls too, by {cardOverflow}.");
        Assert.True(closedAfterEnter);
        Assert.Equal(PathOf("home", "docs"), openedIn);
        Assert.Equal(["docs"], afterRemove);
        Assert.True(closedAfterQ);
        Assert.True(paneFocusedAfterQ, "Closing the dialog gives the keyboard back to the listing.");
    }

    [Fact]
    public void WithNoBookmark_TheDialogStillTakesTheKeys_AndAAddsTheCurrentFolder()
    {
        bool viewFocused = false;
        string[] added = [];
        bool listFocusedAfterAdd = false;
        bool closedAfterQ = false;

        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));

            using ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(reader);
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
                services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
            });

            MainWindow window = provider.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
            App.RegisterHosts(provider, window);
            window.DataContext = viewModel;
            window.Show();
            await viewModel.StartAsync();
            window.ActivePane.Focus();
            Dispatcher.UIThread.RunJobs();

            Press(window, Key.B, "b");
            BookmarksView view = window.HostDialog.GetVisualDescendants().OfType<BookmarksView>().Single();
            viewFocused = view.IsKeyboardFocusWithin;

            Press(window, Key.A, "a");
            added = [.. provider.GetRequiredService<Bookmarks>().Items.Select(bookmark => bookmark.Name)];
            listFocusedAfterAdd = view.ListControl.IsKeyboardFocusWithin;

            Press(window, Key.Q, "q");
            closedAfterQ = !window.HostDialog.IsOpen;
            await CloseIfStillOpenAsync(provider, window);
            await viewModel.Keyboard.LastCommand;
            window.Close();
        });

        Assert.True(viewFocused, "An empty dialog still takes the keyboard.");
        Assert.Equal(["home"], added);
        Assert.True(listFocusedAfterAdd);
        Assert.True(closedAfterQ);
    }

    private static async Task CloseIfStillOpenAsync(ServiceProvider provider, MainWindow window)
    {
        // A key that failed to close the dialog must fail the test, not hang it on a command awaiting the dialog.
        if (window.HostDialog.IsOpen)
        {
            await provider.GetRequiredService<IContentDialogService>().HideAsync();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Press(Window window, Key key, string symbol)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, symbol);
        Dispatcher.UIThread.RunJobs();
    }
}
