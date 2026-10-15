using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A prompt that answers from a script and records what it was asked.
/// </summary>
public sealed class ScriptedPrompt : IPromptService
{
    /// <summary>
    /// The answers, in order; an empty script answers <see langword="null"/> (cancelled).
    /// </summary>
    public Queue<string?> Answers { get; } = new();

    /// <summary>
    /// Every prompt shown: label, initial text and selection.
    /// </summary>
    public List<(string Label, string Initial, int SelectionStart, int? SelectionLength)> Asked { get; } = [];

    /// <inheritdoc />
    public Task<string?> PromptAsync(string label, string initialText = "", int selectionStart = 0, int? selectionLength = null)
    {
        Asked.Add((label, initialText, selectionStart, selectionLength));
        return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
    }
}

/// <summary>
/// A confirmation that answers from a script (no by default) and records what it was asked.
/// </summary>
public sealed class ScriptedConfirmation : IConfirmationService
{
    /// <summary>
    /// The answers, in order.
    /// </summary>
    public Queue<bool> Answers { get; } = new();

    /// <summary>
    /// Every message shown.
    /// </summary>
    public List<string> Asked { get; } = [];

    /// <summary>
    /// For every question, whether Enter would have confirmed it.
    /// </summary>
    public List<bool> DefaultedToConfirm { get; } = [];

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string confirmText, bool defaultToConfirm = false)
    {
        Asked.Add(message);
        DefaultedToConfirm.Add(defaultToConfirm);
        return Task.FromResult(Answers.Count > 0 && Answers.Dequeue());
    }
}
