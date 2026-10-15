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
/// Types a command line on the real window: ':' (Swiss layout), the text, Enter.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class CommandLineViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TypingColonCdDotDotEnter_NavigatesAndGivesTheKeyboardBack()
    {
        string? path = null;
        bool openAfterColon = false;
        bool inputFocused = false;
        bool paneFocusedAfter = false;

        fixture.RunAsync(async () =>
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
            reader.Add(Root, Directory(Root, "home"));

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

            window.KeyPress(Key.OemPeriod, RawInputModifiers.Shift, PhysicalKey.Period, ":");
            Dispatcher.UIThread.RunJobs();
            openAfterColon = viewModel.CommandLine.IsOpen;
            inputFocused = window.CommandLine.InputBox.IsFocused;

            window.KeyTextInput("cd ..");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Dispatcher.UIThread.RunJobs();
            await System.Threading.Tasks.Task.Yield();
            Dispatcher.UIThread.RunJobs();

            path = viewModel.Pane.CurrentPath;
            paneFocusedAfter = window.ActivePane.IsFocused;
            window.Close();
        });

        Assert.True(openAfterColon);
        Assert.True(inputFocused);
        Assert.Equal(Root, path);
        Assert.True(paneFocusedAfter);
    }
}
