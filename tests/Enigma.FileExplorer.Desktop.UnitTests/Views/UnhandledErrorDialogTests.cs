using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Enigma.FileExplorer.Desktop.UnitTests.Infrastructure.TestEntries;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

/// <summary>
/// The safety net's dialog in the real window: Enigma.GitClient's, one at a time, never over a question,
/// and closed by the explorer's dialog keys.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class UnhandledErrorDialogTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void AReportedError_OpensTheDialog_WithTheMessageAndOneCloseButton()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();

            session.Reporter.Report(new InvalidOperationException("the listing is gone"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();

            ContentDialog host = session.Window.HostDialog;
            Assert.True(host.IsOpen);
            Assert.Equal(UnhandledErrorReporter.Title, host.Title);
            Assert.Null(host.PrimaryButtonText);
            Assert.Equal("Close", host.CloseButtonText);
            Assert.Equal(DefaultButton.Close, host.DefaultButton);
            Assert.NotNull(host.IconData);
            UnhandledErrorViewModel error = ViewModelOf(host);
            Assert.Equal("the listing is gone", error.Message);
            Assert.False(error.HasMore);
            Assert.Contains("System.InvalidOperationException: the listing is gone", error.Details, StringComparison.Ordinal);
        });

    [Fact]
    public void ACallbackThatThrowsOnTheUiThread_ShowsTheDialog_AndTheAppCarriesOn()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();
            UnhandledExceptionHooks.ResetForTests();

            try
            {
                UnhandledExceptionHooks.InstallDispatcher();
                UnhandledExceptionHooks.Attach(session.Reporter);
                bool after = false;

                Dispatcher.UIThread.Post(() => throw new InvalidOperationException("a key handler failed"));
                Dispatcher.UIThread.Post(() => after = true);
                await DrainAsync();

                Assert.True(after, "The dispatcher stopped at the failing callback.");
                Assert.True(session.Window.HostDialog.IsOpen);
                Assert.Equal("a key handler failed", ViewModelOf(session.Window.HostDialog).Message);
            }
            finally
            {
                UnhandledExceptionHooks.ResetForTests();
            }
        });

    [Fact]
    public void ASecondErrorWhileItIsOpen_IsCountedIntoIt()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();

            session.Reporter.Report(new InvalidOperationException("first"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();
            session.Reporter.Report(new InvalidOperationException("second"), UnhandledErrorSource.FireAndForget);
            await DrainAsync();

            UnhandledErrorViewModel error = ViewModelOf(session.Window.HostDialog);
            Assert.Equal("first", error.Message);
            Assert.True(error.HasMore);
            Assert.Equal("1 more error occurred.", error.MoreText);
        });

    [Fact]
    public void AnErrorDuringAQuestion_WaitsForTheAnswer_ThenShows()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();
            Task<DialogResult> question = session.Dialogs.ShowMessageAsync("Delete 'a.txt'?", "It cannot be undone.");
            await DrainAsync();

            session.Reporter.Report(new InvalidOperationException("meanwhile"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();

            Assert.Equal("Delete 'a.txt'?", session.Window.HostDialog.Title);

            await session.Dialogs.HideAsync();
            await DrainAsync();

            Assert.True(question.IsCompleted);
            Assert.True(session.Window.HostDialog.IsOpen);
            Assert.Equal(UnhandledErrorReporter.Title, session.Window.HostDialog.Title);
        });

    [Fact]
    public void AnErrorBeforeTheHost_IsShownOnceTheHostIsRegistered()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();
            UnhandledErrorReporter early = new(session.Dialogs, new FakeSystemClipboard(), new ManualTimeProvider(), NullLogger<UnhandledErrorReporter>.Instance);

            early.Report(new InvalidOperationException("early"), UnhandledErrorSource.Startup);
            await DrainAsync();
            Assert.False(session.Window.HostDialog.IsOpen);

            early.RegisterHost(session.Window.HostDialog);
            await DrainAsync();

            Assert.True(session.Window.HostDialog.IsOpen);
            Assert.Equal("early", ViewModelOf(session.Window.HostDialog).Message);
        });

    [Fact]
    public void ACancellationSomeoneAskedFor_IsNotShown()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();
            using CancellationTokenSource source = new();
            await source.CancelAsync();

            session.Reporter.Report(new OperationCanceledException(source.Token), UnhandledErrorSource.FireAndForget);
            await DrainAsync();

            Assert.False(session.Window.HostDialog.IsOpen);
        });

    [Fact]
    public void AnErrorThatEndsTheProcess_IsOnlyLogged()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();

            session.Reporter.Report(new InvalidOperationException("fatal"), UnhandledErrorSource.AppDomain);
            await DrainAsync();

            Assert.False(session.Window.HostDialog.IsOpen);
        });

    [Fact]
    public void CopyDetails_PutsTheDetailsOnTheClipboard()
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();

            session.Reporter.Report(new InvalidOperationException("to report"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();
            UnhandledErrorViewModel error = ViewModelOf(session.Window.HostDialog);

            await error.CopyDetailsCommand.ExecuteAsync(null);

            Assert.Equal(error.Details, session.Clipboard.Text);
        });

    [Theory]
    [InlineData(Key.Q, PhysicalKey.Q, "q")]
    [InlineData(Key.Enter, PhysicalKey.Enter, "\r")]
    [InlineData(Key.Escape, PhysicalKey.Escape, "")]
    public void TheKey_ClosesIt_AndGivesTheKeyboardBackToTheListing(Key key, PhysicalKey physical, string text)
        => fixture.RunAsync(async () =>
        {
            using Session session = await Session.OpenAsync();

            session.Reporter.Report(new InvalidOperationException("closed by a key"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();
            Assert.True(session.Window.HostDialog.IsOpen);
            Assert.True(Assert.IsType<UnhandledErrorView>(session.Window.HostDialog.Content).IsFocused, "The view takes the keyboard as it opens.");

            session.Window.KeyPress(key, RawInputModifiers.None, physical, text);
            await DrainAsync();

            Assert.False(session.Window.HostDialog.IsOpen, $"'{key}' leaves the error dialog open.");
            Assert.True(session.Window.ActivePane.IsKeyboardFocusWithin, "Closing the error dialog gives the keyboard back to the listing.");
        });

    private static UnhandledErrorViewModel ViewModelOf(ContentDialog host)
        => Assert.IsType<UnhandledErrorViewModel>(Assert.IsType<UnhandledErrorView>(host.Content).DataContext);

    private static async Task DrainAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }

    /// <summary>
    /// The real window, its dialog host and the application's own reporter, over a fake clipboard and a clock
    /// that does not move.
    /// </summary>
    private sealed class Session : IDisposable
    {
        private readonly ServiceProvider _provider;

        private Session(ServiceProvider provider)
        {
            _provider = provider;
            Window = provider.GetRequiredService<MainWindow>();
        }

        public MainWindow Window { get; }

        public IContentDialogService Dialogs => _provider.GetRequiredService<IContentDialogService>();

        public UnhandledErrorReporter Reporter => _provider.GetRequiredService<UnhandledErrorReporter>();

        public required FakeSystemClipboard Clipboard { get; init; }

        public static async Task<Session> OpenAsync()
        {
            FakeDirectoryReader reader = new();
            reader.Add(PathOf("home"), File(PathOf("home"), "a.txt"));
            FakeSystemClipboard clipboard = new();

            ServiceProvider provider = CompositionRootTests.BuildProvider(services =>
            {
                services.AddSingleton<IDirectoryReader>(reader);
                services.AddSingleton<IKnownFolders>(new FakeKnownFolders(PathOf("home")));
                services.AddSingleton<IPlacesProvider>(new FakePlacesProvider());
                services.AddSingleton<IDirectoryWatcherFactory>(new FakeDirectoryWatcherFactory());
                services.AddSingleton<ISystemClipboard>(clipboard);
                services.AddSingleton<TimeProvider>(new ManualTimeProvider());
            });

            Session session = new(provider) { Clipboard = clipboard };
            MainWindowViewModel viewModel = provider.GetRequiredService<MainWindowViewModel>();
            App.RegisterHosts(provider, session.Window);
            session.Window.DataContext = viewModel;
            session.Window.Show();
            await viewModel.StartAsync();
            session.Window.ActivePane.Focus();
            Dispatcher.UIThread.RunJobs();

            return session;
        }

        public void Dispose()
        {
            // A dialog a failed assertion left open must not hold the window.
            if (Window.HostDialog.IsOpen)
            {
                _ = Dialogs.HideAsync();
                Dispatcher.UIThread.RunJobs();
            }

            Window.Close();
            _provider.Dispose();
        }
    }
}
