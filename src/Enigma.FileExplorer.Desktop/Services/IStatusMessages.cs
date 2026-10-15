using System.ComponentModel;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The status bar's message line, vim-style: the latest piece of feedback, replaced by the next one.
/// </summary>
public interface IStatusMessages : INotifyPropertyChanged
{
    /// <summary>
    /// The message on display, or <see langword="null"/> when the line is clear.
    /// </summary>
    StatusMessage? Current { get; }

    /// <summary>
    /// Shows an informational message.
    /// </summary>
    /// <param name="text">The message.</param>
    void Info(string text);

    /// <summary>
    /// Shows an error message.
    /// </summary>
    /// <param name="text">The message.</param>
    void Error(string text);

    /// <summary>
    /// Clears the line.
    /// </summary>
    void Clear();
}
