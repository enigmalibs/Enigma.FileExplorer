using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.Operations;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Runs transfers one at a time: two large copies to the same disk only slow each other down.
/// </summary>
/// <param name="engine">Does the transfers.</param>
/// <param name="resolver">Asks the user about conflicts.</param>
/// <param name="status">The status line, for the outcome.</param>
/// <param name="report">Lists failures when there are any.</param>
public sealed class TransferQueue(
    ITransferEngine engine,
    IConflictResolver resolver,
    IStatusMessages status,
    ITransferErrorReport report) : ObservableObject, ITransferQueue
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _current;

    /// <inheritdoc />
    public bool IsBusy { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public string? Title { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public string? CurrentItem { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public double Fraction { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public async Task<TransferResult?> RunAsync(TransferRequest request, string title)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(title);

        await _gate.WaitAsync();

        using CancellationTokenSource cancellation = new();
        _current = cancellation;

        try
        {
            Title = title;
            CurrentItem = null;
            Fraction = 0;
            IsBusy = true;

            // Created here, on the caller's (UI) context, so progress lands on the UI thread.
            Progress<TransferProgress> progress = new(update =>
            {
                Fraction = update.Fraction;
                CurrentItem = update.CurrentItem;
            });

            TransferResult result = await engine.RunAsync(request, resolver, progress, cancellation.Token);
            await ReportAsync(title, result);
            return result;
        }
        catch (FileOperationException exception)
        {
            status.Error(exception.Message);
            return null;
        }
        finally
        {
            _current = null;
            IsBusy = false;
            Title = null;
            CurrentItem = null;
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public bool CancelCurrent()
    {
        if (_current is not { } current)
        {
            return false;
        }

        current.Cancel();
        return true;
    }

    private async Task ReportAsync(string title, TransferResult result)
    {
        string done = result.Created.Count == 1 ? "1 item" : $"{result.Created.Count} items";
        string skipped = result.Skipped > 0 ? $", {result.Skipped} skipped" : string.Empty;

        if (result.Cancelled)
        {
            status.Error($"{title}: cancelled after {done}");
        }
        else if (result.Errors.Count > 0)
        {
            status.Error($"{title}: {result.Errors.Count} failed{skipped}");
            await report.ShowAsync(title, result.Errors);
        }
        else
        {
            status.Info($"{title}: done{skipped}");
        }
    }
}
