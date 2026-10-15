using System.Threading.Tasks;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// Asks the user to confirm something that cannot be undone.
/// </summary>
public interface IConfirmationService
{
    /// <summary>
    /// Shows a confirmation and waits for the answer.
    /// </summary>
    /// <param name="title">The question's title, e.g. "Delete permanently".</param>
    /// <param name="message">What will happen.</param>
    /// <param name="confirmText">The confirming button's text, e.g. "Delete".</param>
    /// <param name="defaultToConfirm">
    /// Whether Enter confirms: for what can be undone. Otherwise Enter cancels, the safe answer to what cannot.
    /// </param>
    /// <returns><see langword="true"/> only when the user chose the confirming button; Esc and Cancel are a no.</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmText, bool defaultToConfirm = false);
}
