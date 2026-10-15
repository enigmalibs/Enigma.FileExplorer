namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// A line of feedback for the status bar.
/// </summary>
/// <param name="Text">What to tell the user.</param>
/// <param name="IsError">Whether it reports a failure, which the status bar colours as one.</param>
public sealed record StatusMessage(string Text, bool IsError);
