using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
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
/// Presses real keys on the real window: the whole path from the platform event through the mapper and
/// the keymap to the pane.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MainWindowKeyboardTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Keys_DriveTheActivePane_LayoutAware_AndTextBoxesKeepTheirKeys()
    {
        int afterJ = -1;
        int afterTyping = -1;
        bool commandLineOpened = false;

        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"), File(PathOf("home"), "b.txt"), File(PathOf("home"), "c.txt"), File(PathOf("home"), "d.txt"));

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

            for (int i = 0; i < 3; i++)
            {
                window.KeyPress(Key.J, RawInputModifiers.None, PhysicalKey.J, "j");
            }

            Dispatcher.UIThread.RunJobs();
            afterJ = viewModel.Pane.CursorIndex;

            // Swiss QWERTZ: ':' is Shift+'.', which the keymap must see as ':' and open the command line.
            window.KeyPress(Key.OemPeriod, RawInputModifiers.Shift, PhysicalKey.Period, ":");
            Dispatcher.UIThread.RunJobs();
            commandLineOpened = viewModel.CommandLine.IsOpen;
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "");
            Dispatcher.UIThread.RunJobs();

            viewModel.Pane.BeginEditPath();
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            Dispatcher.UIThread.RunJobs();
            afterTyping = viewModel.Pane.CursorIndex;

            window.Close();
        });

        Assert.Equal(3, afterJ);
        Assert.True(commandLineOpened);
        Assert.Equal(3, afterTyping);
    }
}
