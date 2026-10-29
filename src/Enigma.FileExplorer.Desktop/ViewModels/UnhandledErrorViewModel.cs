using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// What the safety net's dialog says: that something went wrong and the application is still running,
/// what went wrong, how many more errors came, and the details for a bug report.
/// </summary>
/// <remarks>
/// It follows its batch: an error counted into it while the dialog is open shows at once, in the count and
/// in the details.
/// </remarks>
public sealed class UnhandledErrorViewModel : ObservableObject
{
    private readonly UnhandledErrorBatch _batch;
    private readonly Func<string, Task> _copy;

    /// <summary>
    /// Initialises a new instance.
    /// </summary>
    /// <param name="batch">The errors to show.</param>
    /// <param name="copy">Puts a text on the clipboard.</param>
    /// <param name="close">Closes the dialog (<c>q</c>, Enter).</param>
    public UnhandledErrorViewModel(UnhandledErrorBatch batch, Func<string, Task> copy, Action close)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(close);

        _batch = batch;
        _copy = copy;

        CopyDetailsCommand = new AsyncRelayCommand(OnCopyDetailsAsync);
        CloseCommand = new RelayCommand(close);

        batch.Changed += OnBatchChanged;
    }

    /// <summary>
    /// Gets the sentence that opens the dialog.
    /// </summary>
    public string Lead
        => "Something went wrong, but the application is still running. You can carry on, or close it and "
            + "start it again if something looks wrong.";

    /// <summary>
    /// Gets what went wrong.
    /// </summary>
    public string Message => _batch.Message;

    /// <summary>
    /// Gets a value indicating whether more errors came after the first.
    /// </summary>
    public bool HasMore => _batch.More > 0;

    /// <summary>
    /// Gets how many more errors came after the first, as a sentence.
    /// </summary>
    public string MoreText
        => _batch.More == 1
            ? "1 more error occurred."
            : $"{_batch.More.ToString(CultureInfo.CurrentCulture)} more errors occurred.";

    /// <summary>
    /// Gets the details of every error, for a bug report.
    /// </summary>
    public string Details => _batch.Details;

    /// <summary>
    /// Gets the command that puts the details on the clipboard.
    /// </summary>
    public AsyncRelayCommand CopyDetailsCommand { get; }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    public RelayCommand CloseCommand { get; }

    private Task OnCopyDetailsAsync() => _copy(Details);

    private void OnBatchChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(MoreText));
        OnPropertyChanged(nameof(Details));
    }
}
