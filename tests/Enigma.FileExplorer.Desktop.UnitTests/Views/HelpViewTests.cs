using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls;
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
/// The help in the real window's dialog: it takes the keyboard, scrolls with the listing's keys in one
/// scroll area only, and closes with q.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HelpViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheHelp_TakesTheKeyboard_ScrollsWithJkGG_InOneScrollArea_AndQCloses()
    {
        Result result = new();

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

            // Opened the way the user does: '?' typed in the listing.
            window.KeyPress(Key.OemQuestion, RawInputModifiers.Shift, PhysicalKey.Slash, "?");
            Dispatcher.UIThread.RunJobs();
            Task shown = viewModel.Keyboard.LastCommand;

            HelpView help = window.HostDialog.GetVisualDescendants().OfType<HelpView>().Single();
            ScrollViewer scroller = help.ScrollArea;
            ScrollViewer? card = help.FindAncestorOfType<ScrollViewer>();

            result.Focused = scroller.IsFocused;
            result.Cards = help.GetVisualDescendants().OfType<SettingsCardExpander>().Count();
            result.CardOverflow = card is null ? 0 : card.Extent.Height - card.Viewport.Height;
            result.Scrollable = scroller.Extent.Height - scroller.Viewport.Height;

            window.KeyPress(Key.J, RawInputModifiers.None, PhysicalKey.J, "j");
            window.KeyPress(Key.J, RawInputModifiers.None, PhysicalKey.J, "j");
            Dispatcher.UIThread.RunJobs();
            result.AfterJJ = scroller.Offset.Y;

            window.KeyPress(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            Dispatcher.UIThread.RunJobs();
            result.AfterK = scroller.Offset.Y;

            window.KeyPress(Key.G, RawInputModifiers.Shift, PhysicalKey.G, "G");
            Dispatcher.UIThread.RunJobs();
            result.AtBottom = scroller.Offset.Y;

            window.KeyPress(Key.G, RawInputModifiers.None, PhysicalKey.G, "g");
            Dispatcher.UIThread.RunJobs();
            result.AtTop = scroller.Offset.Y;

            window.KeyPress(Key.Q, RawInputModifiers.None, PhysicalKey.Q, "q");
            Dispatcher.UIThread.RunJobs();
            result.OpenAfterQ = window.HostDialog.IsOpen;

            // A q that failed to close the help must fail the test, not hang it on the open dialog.
            if (window.HostDialog.IsOpen)
            {
                await provider.GetRequiredService<IContentDialogService>().HideAsync();
            }

            await shown;
            Dispatcher.UIThread.RunJobs();
            result.PaneFocusedAfterQ = window.ActivePane.IsKeyboardFocusWithin;

            window.Close();
        });

        Assert.True(result.Focused, "The help takes the keyboard as it opens.");
        Assert.Equal(8, result.Cards);
        Assert.True(result.CardOverflow <= 0.5, $"The dialog card scrolls too, by {result.CardOverflow}.");
        Assert.True(result.Scrollable > 2 * HelpView.LineStep, "The test needs a help taller than its scroll area.");
        Assert.Equal(2 * HelpView.LineStep, result.AfterJJ, 3);
        Assert.Equal(HelpView.LineStep, result.AfterK, 3);
        Assert.Equal(result.Scrollable, result.AtBottom, 3);
        Assert.Equal(0, result.AtTop, 3);
        Assert.False(result.OpenAfterQ);
        Assert.True(result.PaneFocusedAfterQ, "Closing the help gives the keyboard back to the listing.");
    }

    [Fact]
    public void InAShortWindow_TheScrollAreaShrinks_SoTheCardStillDoesNotScroll()
    {
        double overflow = double.NaN;
        double height = double.NaN;

        fixture.RunAsync(async () =>
        {
            using ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(new FakeDirectoryReader());
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
                services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
            });

            MainWindow window = provider.GetRequiredService<MainWindow>();
            App.RegisterHosts(provider, window);
            window.DataContext = provider.GetRequiredService<MainWindowViewModel>();
            window.Height = window.MinHeight;
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Task shown = provider.GetRequiredService<IHelpDialogService>().ShowAsync();
            Dispatcher.UIThread.RunJobs();

            HelpView help = window.HostDialog.GetVisualDescendants().OfType<HelpView>().Single();
            ScrollViewer? card = help.FindAncestorOfType<ScrollViewer>();
            overflow = card is null ? 0 : card.Extent.Height - card.Viewport.Height;
            height = help.ScrollArea.Bounds.Height;

            await provider.GetRequiredService<IContentDialogService>().HideAsync();
            Dispatcher.UIThread.RunJobs();
            await shown;
            window.Close();
        });

        Assert.True(overflow <= 0.5, $"The dialog card scrolls too, by {overflow}.");
        Assert.InRange(height, 120, 439);
    }

    private sealed class Result
    {
        public bool Focused { get; set; }

        public int Cards { get; set; }

        public double CardOverflow { get; set; }

        public double Scrollable { get; set; }

        public double AfterJJ { get; set; }

        public double AfterK { get; set; }

        public double AtBottom { get; set; }

        public double AtTop { get; set; }

        public bool OpenAfterQ { get; set; }

        public bool PaneFocusedAfterQ { get; set; }
    }
}
