namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// A selection the view should apply to the command line's text box after the ViewModel replaced its text.
/// </summary>
/// <param name="Start">Where the selection starts.</param>
/// <param name="End">Where it ends; equal to <paramref name="Start"/> for a bare caret.</param>
public readonly record struct TextSelection(int Start, int End);
