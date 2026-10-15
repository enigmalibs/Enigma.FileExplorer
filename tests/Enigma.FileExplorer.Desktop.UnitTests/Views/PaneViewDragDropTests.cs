using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Operations;
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
/// Drops on the real window: what hovering shows, what the modifiers do, the drop menu, the places, and
/// what a press on a marked entry does before it becomes a drag.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class PaneViewDragDropTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly string Home = PathOf("home");
    private static readonly string Docs = PathOf("home", "docs");
    private static readonly string FileA = PathOf("home", "a.txt");

    [Fact]
    public void Hovering_HighlightsTheFolder_AndRefusesWhatWouldChangeNothing()
    {
        bool folderLit = false;
        bool paneLitOverFolder = true;
        DragDropEffects overFolder = DragDropEffects.None;
        DragDropEffects overFile = DragDropEffects.Copy;
        DragDropEffects folderOverItself = DragDropEffects.Copy;
        bool clearedOnLeave = false;

        RunOnWindow(async (window, viewModel, _) =>
        {
            DragDropEffects last = DragDropEffects.None;
            // Moving onto another element raises DragEnter there rather than DragOver.
            window.AddHandler(DragDrop.DragEnterEvent, (_, e) => last = e.DragEffects, RoutingStrategies.Bubble, handledEventsToo: true);
            window.AddHandler(DragDrop.DragOverEvent, (_, e) => last = e.DragEffects, RoutingStrategies.Bubble, handledEventsToo: true);
            EntryViewModel docs = viewModel.Pane.Items[0];

            Drag(window, RowPoint(window, 0), RawDragEventType.DragEnter, Carrying(FileA));
            Drag(window, RowPoint(window, 0), RawDragEventType.DragOver, Carrying(FileA));
            (folderLit, paneLitOverFolder, overFolder) = (docs.IsDropTarget, viewModel.Pane.IsDropTarget, last);

            // A file is no target of its own: the drop would land in the pane's directory, where a.txt already is.
            Drag(window, RowPoint(window, 2), RawDragEventType.DragOver, Carrying(FileA));
            overFile = last;

            Drag(window, RowPoint(window, 0), RawDragEventType.DragOver, Carrying(Docs));
            folderOverItself = last;

            Drag(window, RowPoint(window, 0), RawDragEventType.DragOver, Carrying(FileA));
            Drag(window, RowPoint(window, 0), RawDragEventType.DragLeave, Carrying(FileA));
            clearedOnLeave = !docs.IsDropTarget && !viewModel.Pane.IsDropTarget;
            await Task.CompletedTask;
        });

        Assert.True(folderLit);
        Assert.False(paneLitOverFolder);
        Assert.Equal(DragDropEffects.Copy, overFolder);
        Assert.Equal(DragDropEffects.None, overFile);
        Assert.Equal(DragDropEffects.None, folderOverItself);
        Assert.True(clearedOnLeave);
    }

    [Fact]
    public void Ctrl_CopiesIntoTheFolder_AndShift_Moves_WithoutAMenu_AlwaysReportingACopy()
    {
        List<TransferRequest> requests = [];
        List<DragDropEffects> reported = [];
        bool menuShown = true;

        RunOnWindow(async (window, _, engine) =>
        {
            window.AddHandler(DragDrop.DropEvent, (_, e) => reported.Add(e.DragEffects), RoutingStrategies.Bubble, handledEventsToo: true);

            DropAt(window, RowPoint(window, 0), Carrying(FileA), RawInputModifiers.Control);
            await DrainAsync();
            DropAt(window, RowPoint(window, 0), Carrying(PathOf("home", "b.txt")), RawInputModifiers.Shift);
            await DrainAsync();

            menuShown = window.ActivePane.OpenMenu is not null;
            requests.AddRange(engine.Requests);
        });

        Assert.Equal([(Docs, TransferMode.Copy), (Docs, TransferMode.Move)], requests.Select(request => (request.TargetDirectory, request.Mode)));
        Assert.Equal([DragDropEffects.Copy, DragDropEffects.Copy], reported);
        Assert.False(menuShown);
    }

    [Fact]
    public void WithoutModifiers_TheDropMenuAsks_AndCancelDoesNothing()
    {
        string[] titles = [];
        string?[] gestures = [];
        List<TransferRequest> requests = [];
        int afterCancel = -1;

        RunOnWindow(async (window, viewModel, engine) =>
        {
            // A second pane on docs: dropping a.txt on its empty area lands in docs.
            await viewModel.Workspace.SplitAsync(SplitAxis.Columns, Docs);
            await DrainAsync();
            PaneView docsView = window.ActivePane;
            Point empty = docsView.ListControl.TranslatePoint(new Point(100, docsView.ListControl.Bounds.Height - 20), window) ?? default;

            DropAt(window, empty, Carrying(FileA));
            await DrainAsync();

            ContextMenu menu = docsView.OpenMenu ?? throw new InvalidOperationException("No drop menu.");
            List<MenuItem> items = [.. menu.Items.OfType<MenuItem>()];
            titles = [.. menu.Items.Select(item => item is MenuItem { Tag: MenuEntry entry } ? entry.Title : "-")];
            gestures = [.. items.Select(item => ((MenuEntry)item.Tag!).Gesture)];

            items.Single(item => ((MenuEntry)item.Tag!).Title == "Move here").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            await DrainAsync();
            requests.AddRange(engine.Requests);

            DropAt(window, empty, Carrying(PathOf("home", "b.txt")));
            await DrainAsync();
            ContextMenu second = docsView.OpenMenu ?? throw new InvalidOperationException("No second drop menu.");
            second.Items.OfType<MenuItem>().Single(item => ((MenuEntry)item.Tag!).Title == "Cancel").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            second.Close();
            await DrainAsync();
            afterCancel = engine.Requests.Count;
        });

        Assert.Equal(["Copy here", "Move here", "-", "Cancel"], titles);
        Assert.Equal(["Ctrl", "Shift", "Esc"], gestures);
        TransferRequest request = Assert.Single(requests);
        Assert.Equal((Docs, TransferMode.Move), (request.TargetDirectory, request.Mode));
        Assert.Equal([FileA], request.Sources);
        Assert.Equal(1, afterCancel);
    }

    [Fact]
    public void DroppingWhereTheEntriesAlreadyAre_DoesNothing()
    {
        int requests = -1;
        bool menuShown = true;

        RunOnWindow(async (window, _, engine) =>
        {
            Point empty = window.ActivePane.ListControl.TranslatePoint(new Point(100, window.ActivePane.ListControl.Bounds.Height - 20), window) ?? default;

            DropAt(window, empty, Carrying(FileA));
            await DrainAsync();

            menuShown = window.ActivePane.OpenMenu is not null;
            requests = engine.Requests.Count;
        });

        Assert.False(menuShown);
        Assert.Equal(0, requests);
    }

    [Fact]
    public void APlace_IsADropTarget()
    {
        bool lit = false;
        TransferRequest? request = null;

        RunOnWindow(async (window, viewModel, engine) =>
        {
            PlacesView places = window.GetVisualDescendants().OfType<PlacesView>().Single();
            Button place = places.GetVisualDescendants().OfType<Button>().Single(button => button.DataContext is PlaceViewModel);
            Point point = place.TranslatePoint(new Point(20, place.Bounds.Height / 2), window) ?? default;

            Drag(window, point, RawDragEventType.DragEnter, Carrying(FileA));
            Drag(window, point, RawDragEventType.DragOver, Carrying(FileA));
            lit = viewModel.Places.Folders[0].IsDropTarget;

            Drag(window, point, RawDragEventType.Drop, Carrying(FileA));
            await DrainAsync();
            ContextMenu menu = places.OpenMenu ?? throw new InvalidOperationException("No drop menu on the place.");
            menu.Items.OfType<MenuItem>().Single(item => ((MenuEntry)item.Tag!).Title == "Copy here").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            await DrainAsync();
            request = engine.Requests.SingleOrDefault();
        }, places: [new Place("Documents", Docs, PlaceKind.Documents)]);

        Assert.True(lit);
        Assert.NotNull(request);
        Assert.Equal((Docs, TransferMode.Copy), (request.TargetDirectory, request.Mode));
    }

    [Fact]
    public void PressingAMarkedEntry_KeepsTheMarksForADrag_AndAPlainReleaseSelectsIt()
    {
        int whilePressed = -1;
        int afterRelease = -1;
        int afterDrag = -1;
        string? cursor = null;

        RunOnWindow(async (window, viewModel, _) =>
        {
            PaneViewModel pane = viewModel.Pane;
            pane.MarkAll();

            window.MouseDown(RowPoint(window, 1), MouseButton.Left);
            whilePressed = pane.MarkedCount;
            window.MouseUp(RowPoint(window, 1), MouseButton.Left);
            afterRelease = pane.MarkedCount;
            cursor = pane.CursorItem?.Name;

            // Moved past the threshold, the press is a drag: the release that ends it selects nothing.
            pane.MarkAll();
            window.MouseDown(RowPoint(window, 1), MouseButton.Left);
            window.MouseMove(RowPoint(window, 1) + new Point(0, 30), RawInputModifiers.LeftMouseButton);
            window.MouseUp(RowPoint(window, 1) + new Point(0, 30), MouseButton.Left);
            await DrainAsync();
            afterDrag = pane.MarkedCount;
        });

        Assert.Equal(3, whilePressed);
        Assert.Equal(0, afterRelease);
        Assert.Equal("a.txt", cursor);
        Assert.Equal(3, afterDrag);
    }

    [Fact]
    public void TheDragData_CarriesThePaths_ReadBackByADrop()
    {
        IReadOnlyList<string> paths = fixture.Run(() =>
        {
            EntryViewModel[] entries = [new(Directory(Home, "docs")), new(File(Home, "a.txt"))];
            DataTransfer data = FileDragData.CreateAsync(entries, storage: null).GetAwaiter().GetResult();
            return FileDragData.ReadPaths(data);
        });

        Assert.Equal([Docs, FileA], paths);
    }

    private static DataTransfer Carrying(params string[] paths)
    {
        DataTransfer data = new();
        data.Add(DataTransferItem.Create(FileDragData.PathsFormat, string.Join('\n', paths)));
        return data;
    }

    private static void Drag(Window window, Point point, RawDragEventType type, DataTransfer data, RawInputModifiers modifiers = RawInputModifiers.None)
        => window.DragDrop(point, type, data, DragDropEffects.Copy | DragDropEffects.Move, modifiers);

    private static void DropAt(Window window, Point point, DataTransfer data, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        // As a platform does: the drop goes to whatever the last DragEnter or DragOver reached.
        Drag(window, point, RawDragEventType.DragEnter, data, modifiers);
        Drag(window, point, RawDragEventType.DragOver, data, modifiers);
        Drag(window, point, RawDragEventType.Drop, data, modifiers);
    }

    private static Point RowPoint(MainWindow window, int index)
    {
        Control row = window.ActivePane.ListControl.ContainerFromIndex(index) ?? throw new InvalidOperationException($"Row {index} is not realized.");
        return row.TranslatePoint(new Point(60, 10), window) ?? default;
    }

    private static async Task DrainAsync()
    {
        for (int i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }

        Dispatcher.UIThread.RunJobs();
    }

    private void RunOnWindow(Func<MainWindow, MainWindowViewModel, FakeTransferEngine, Task> test, Place[]? places = null)
    {
        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(Home, Directory(Home, "docs"), File(Home, "a.txt"), File(Home, "b.txt"));
            reader.Add(Docs, File(Docs, "x.md"));
            FakeTransferEngine engine = new(reader);
            FakePlacesProvider placesProvider = new();
            placesProvider.Places.AddRange(places ?? []);

            using ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(reader);
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(Home));
                services.AddSingleton<IPlacesProvider>(placesProvider);
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
                services.AddSingleton<ISystemClipboard>(new FakeSystemClipboard());
                services.AddSingleton<ITransferEngine>(engine);
                services.AddSingleton<IConflictResolver>(new FixedConflictResolver());
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
                await test(window, viewModel, engine);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
