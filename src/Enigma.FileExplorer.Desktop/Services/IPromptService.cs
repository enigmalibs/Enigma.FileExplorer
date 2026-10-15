using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Asks the user for a line of text on the command line, vim-style: "New folder: ", "Rename: report.pdf".
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Shows a prompt and waits for Enter or Esc.
    /// </summary>
    /// <param name="label">What is asked, shown before the text, e.g. <c>"Rename: "</c>.</param>
    /// <param name="initialText">The text the prompt starts with.</param>
    /// <param name="selectionStart">Where the initial selection starts.</param>
    /// <param name="selectionLength">How much of the text is selected; the caret goes to the end when omitted.</param>
    /// <returns>The text entered, or <see langword="null"/> when the prompt was cancelled.</returns>
    Task<string?> PromptAsync(string label, string initialText = "", int selectionStart = 0, int? selectionLength = null);
}
