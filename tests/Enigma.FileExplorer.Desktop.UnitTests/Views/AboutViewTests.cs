using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Diagnostics;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

/// <summary>
/// The About dialog in the real window's dialog: what it shows, that it fits, and the keys that close it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class AboutViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ColonAbout_ShowsTheProductAndItsBuild_UntitledWithOneCloseButton_AndFitsAShortWindow()
    {
        Shown result = new();

        fixture.RunAsync(async () =>
        {
            using ServiceProvider provider = BuildProvider();
            MainWindow window = await OpenAsync(provider, shortest: true);

            Task shown = provider.GetRequiredService<ICommandDispatcher>().ExecuteAsync(new CommandInvocation(CommandIds.About));
            Dispatcher.UIThread.RunJobs();

            ContentDialog dialog = window.HostDialog;
            result.Title = dialog.Title;
            result.HasIcon = dialog.IconData is not null;
            result.Close = dialog.CloseButtonText;
            result.Primary = dialog.PrimaryButtonText;
            result.Secondary = dialog.SecondaryButtonText;
            result.Default = dialog.DefaultButton;

            AboutView? view = dialog.Content as AboutView;
            result.ViewModel = view?.DataContext as AboutViewModel;
            result.Texts = view is null ? [] : [.. view.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible).Select(block => block.Text ?? string.Empty)];
            result.ImageShown = view?.GetVisualDescendants().OfType<Image>().SingleOrDefault() is { Source: not null };
            result.Focused = view?.IsFocused == true;

            ScrollViewer? card = view?.FindAncestorOfType<ScrollViewer>();
            result.CardOverflow = card is null ? 0 : card.Extent.Height - card.Viewport.Height;

            await provider.GetRequiredService<IContentDialogService>().HideAsync();
            await shown;
            window.Close();
        });

        Assert.Null(result.Title);
        Assert.False(result.HasIcon, "The card draws an icon beside the view, which shows its own.");
        Assert.Equal("Close", result.Close);
        Assert.Null(result.Primary);
        Assert.Null(result.Secondary);
        Assert.Equal(DefaultButton.Close, result.Default);

        Assert.NotNull(result.ViewModel);
        Assert.Contains("Enigma File Explorer", result.Texts);
        Assert.Contains(ProductInformation.Version, result.Texts);
        Assert.Contains(ProductInformation.Copyright, result.Texts);
        Assert.True(result.ImageShown, "The About view shows no icon.");
        Assert.True(result.Focused, "The About view takes the keyboard as it opens.");
        Assert.True(result.CardOverflow <= 0.5, $"The dialog card scrolls, by {result.CardOverflow}.");
    }

    [Theory]
    [InlineData(Key.Q, PhysicalKey.Q, "q")]
    [InlineData(Key.Enter, PhysicalKey.Enter, "\r")]
    [InlineData(Key.Escape, PhysicalKey.Escape, "")]
    public void TheKey_ClosesIt_AndGivesTheKeyboardBackToTheListing(Key key, PhysicalKey physical, string text)
    {
        bool openAfterKey = true;
        bool paneFocused = false;

        fixture.RunAsync(async () =>
        {
            using ServiceProvider provider = BuildProvider();
            MainWindow window = await OpenAsync(provider, shortest: false);

            Task shown = provider.GetRequiredService<IAboutDialogService>().ShowAsync();
            Dispatcher.UIThread.RunJobs();

            window.KeyPress(key, RawInputModifiers.None, physical, text);
            Dispatcher.UIThread.RunJobs();
            openAfterKey = window.HostDialog.IsOpen;

            // A key that failed to close the dialog must fail the test, not hang it on the open dialog.
            if (window.HostDialog.IsOpen)
            {
                await provider.GetRequiredService<IContentDialogService>().HideAsync();
            }

            await shown;
            Dispatcher.UIThread.RunJobs();
            paneFocused = window.ActivePane.IsKeyboardFocusWithin;

            window.Close();
        });

        Assert.False(openAfterKey, $"'{key}' leaves the About dialog open.");
        Assert.True(paneFocused, "Closing the About dialog gives the keyboard back to the listing.");
    }

    private static ServiceProvider BuildProvider()
    {
        FakeDirectoryReader reader = new();
        reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));

        return CompositionRootTests.BuildProvider(services =>
        {
            services.AddSingleton<IDirectoryReader>(reader);
            services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
            services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
            services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
        });
    }

    private static async Task<MainWindow> OpenAsync(ServiceProvider provider, bool shortest)
    {
        MainWindow window = provider.GetRequiredService<MainWindow>();
        MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
        App.RegisterHosts(provider, window);
        window.DataContext = viewModel;

        if (shortest)
        {
            window.Height = window.MinHeight;
        }

        window.Show();
        await viewModel.StartAsync();
        window.ActivePane.Focus();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private sealed class Shown
    {
        public string? Title { get; set; } = "unset";

        public bool HasIcon { get; set; }

        public string? Close { get; set; }

        public string? Primary { get; set; }

        public string? Secondary { get; set; }

        public DefaultButton Default { get; set; }

        public AboutViewModel? ViewModel { get; set; }

        public string[] Texts { get; set; } = [];

        public bool ImageShown { get; set; }

        public bool Focused { get; set; }

        public double CardOverflow { get; set; }
    }
}
