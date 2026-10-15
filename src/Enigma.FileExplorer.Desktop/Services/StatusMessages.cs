using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Holds the status bar's message line. UI-thread only, like every bound object.
/// </summary>
public sealed class StatusMessages : ObservableObject, IStatusMessages
{
    /// <inheritdoc />
    public StatusMessage? Current { get; private set => SetProperty(ref field, value); }

    /// <inheritdoc />
    public void Info(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Current = new StatusMessage(text, IsError: false);
    }

    /// <inheritdoc />
    public void Error(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Current = new StatusMessage(text, IsError: true);
    }

    /// <inheritdoc />
    public void Clear() => Current = null;
}
