using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Default <see cref="IUnhandledErrorReporter"/>: the error in the window's content dialog.
/// </summary>
/// <remarks>
/// <para>
/// Enigma.GitClient's reporter, for this application's one window. The log is written at once, on whatever
/// thread reported, because that thread may be about to end the process. What is shown is decided on the
/// UI thread, by <see cref="UnhandledErrorPolicy"/>.
/// </para>
/// <para>
/// The dialog is shown on the window's dialog host, which every question shares: showing it over another
/// question would replace that question's content, so it waits until the host is free — and, before the
/// window hands its host over (<see cref="RegisterHost"/>), until it does. If the dialog itself fails, that
/// is logged and no dialog is tried again in this session: the net must not loop on itself.
/// </para>
/// </remarks>
public sealed class UnhandledErrorReporter : IUnhandledErrorReporter
{
    /// <summary>
    /// The dialog's title.
    /// </summary>
    internal const string Title = "Something went wrong";

    private readonly IContentDialogService _dialogs;
    private readonly ISystemClipboard _clipboard;
    private readonly ILogger<UnhandledErrorReporter> _logger;
    private readonly UnhandledErrorPolicy _policy;

    private ContentDialog? _host;

    // The question the next dialog waits behind, while it waits.
    private ContentDialog? _waitingFor;

    private bool _presenting;
    private bool _broken;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="dialogs">Shows the dialog on the window's host.</param>
    /// <param name="clipboard">Takes the details on <em>Copy details</em>.</param>
    /// <param name="time">Measures how long ago an error was seen.</param>
    /// <param name="logger">Receives every exception, in full.</param>
    public UnhandledErrorReporter(
        IContentDialogService dialogs,
        ISystemClipboard clipboard,
        TimeProvider time,
        ILogger<UnhandledErrorReporter> logger)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);

        _dialogs = dialogs;
        _clipboard = clipboard;
        _logger = logger;
        _policy = new UnhandledErrorPolicy(time);
    }

    /// <summary>
    /// Takes the window's dialog host — the one <see cref="IContentDialogService"/> shows on — so the dialog
    /// can wait for a question on it to be answered, and shows what waited for it.
    /// </summary>
    /// <param name="host">The window's dialog host.</param>
    public void RegisterHost(ContentDialog host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _host = host;

        if (_policy.Pending is not null)
        {
            Dispatcher.UIThread.Post(() => _ = PresentAsync(), DispatcherPriority.Background);
        }
    }

    /// <inheritdoc />
    public void Report(Exception exception, UnhandledErrorSource source)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_policy.IsIgnoredCancellation(exception))
        {
            _logger.LogDebug(exception, "A cancellation reached the safety net ({Source}); not an error", source);
            return;
        }

        _logger.LogError(exception, "An unhandled exception reached the safety net ({Source})", source);

        if (source == UnhandledErrorSource.AppDomain)
        {
            // .NET ends the process once the event returns: no dialog can come.
            return;
        }

        Dispatcher.UIThread.Post(() => Offer(exception));
    }

    /// <inheritdoc />
    public void ShutdownStarted() => _policy.IsShuttingDown = true;

    private void Offer(Exception exception)
    {
        switch (_policy.Offer(exception))
        {
            case UnhandledErrorDecision.Queue:
                _ = PresentAsync();
                break;
            case UnhandledErrorDecision.Suppress:
                _logger.LogDebug("The same error was shown less than {Window} ago; it is not shown again", UnhandledErrorPolicy.RepeatWindow);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Shows the pending batch, once the host is there and free. Never throws: nothing awaits it.
    /// </summary>
    private async Task PresentAsync()
    {
        if (_broken || _presenting || _policy.Pending is null || _host is not { } host)
        {
            // With no host yet, RegisterHost brings one and tries again.
            return;
        }

        if (host.IsOpen)
        {
            WaitFor(host);
            return;
        }

        if (_policy.BeginShow() is not { } batch)
        {
            return;
        }

        _presenting = true;

        try
        {
            UnhandledErrorView view = new()
            {
                DataContext = new UnhandledErrorViewModel(batch, _clipboard.SetTextAsync, () => _ = _dialogs.HideAsync()),
            };

            await _dialogs.ShowAsync(dialog =>
            {
                dialog.Title = Title;
                dialog.Content = view;
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = DefaultButton.Close;
                dialog.IconData = PhosphorIconSet.Instance
                    .GetGlyph(PhosphorIcon.Warning, PhosphorWeight.Regular)
                    .ToGeometry();
                dialog.IconBrush = new SolidColorBrush(Color.Parse("#E8A33D"));
            });
        }
        catch (Exception exception)
        {
            // Logged, never reported: a dialog that cannot be shown would otherwise report itself again and
            // again.
            _broken = true;
            _logger.LogError(exception, "The safety net's dialog could not be shown; later errors are only logged");
        }

        _presenting = false;
        _policy.EndShow();

        if (_policy.Pending is not null)
        {
            _ = PresentAsync();
        }
    }

    /// <summary>
    /// Waits for the question on the host to be answered, then tries again.
    /// </summary>
    private void WaitFor(ContentDialog host)
    {
        if (ReferenceEquals(_waitingFor, host))
        {
            return;
        }

        _waitingFor = host;
        host.Closed += OnWaitedClosed;
    }

    private void OnWaitedClosed(object? sender, DialogResult e)
    {
        if (sender is ContentDialog host)
        {
            host.Closed -= OnWaitedClosed;
        }

        _waitingFor = null;

        // Posted: the question's own continuation runs first, and may ask the next question at once.
        Dispatcher.UIThread.Post(() => _ = PresentAsync(), DispatcherPriority.Background);
    }
}
