using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The command line at the bottom of the window, vim-style: ex commands after <c>:</c>, quick-jump after
/// <c>/</c> (every match highlighted while typing and after, as vim's <c>incsearch</c> and <c>hlsearch</c>),
/// and prompts for names (it is the application's <see cref="IPromptService"/>).
/// </summary>
public sealed class CommandLineViewModel : ObservableObject, IPromptService
{
    private readonly CommandCatalog _catalog;
    private readonly CommandLineCompleter _completer;
    private readonly WorkspaceViewModel _workspace;
    private readonly IKnownFolders _folders;
    private readonly IStatusMessages _status;
    private readonly CommandHistory _history = new();
    private TaskCompletionSource<string?>? _prompt;
    private IReadOnlyList<string>? _completions;
    private int _completionIndex;
    private int _jumpOrigin = -1;
    private string? _highlightBeforeJump;
    private bool _settingText;

    /// <summary>
    /// Creates the command line.
    /// </summary>
    /// <param name="catalog">The commands that can be typed.</param>
    /// <param name="workspace">The panes; quick-jump and path completion use the active one.</param>
    /// <param name="folders">What <c>~</c> means.</param>
    /// <param name="status">Where parse errors and jump misses are reported.</param>
    public CommandLineViewModel(CommandCatalog catalog, WorkspaceViewModel workspace, IKnownFolders folders, IStatusMessages status)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(status);

        _catalog = catalog;
        _completer = new CommandLineCompleter(catalog.Parser);
        _workspace = workspace;
        _folders = folders;
        _status = status;
    }

    /// <summary>
    /// Raised when an ex command line parsed and should run.
    /// </summary>
    public event EventHandler<ExInvocation>? CommandSubmitted;

    /// <summary>
    /// Raised when the command line closes, so the view can give the keyboard back to the listing.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Raised when the ViewModel replaced the text (history, completion, a prompt's initial text), with the
    /// caret or selection the view should apply.
    /// </summary>
    public event EventHandler<TextSelection>? TextReplaced;

    /// <summary>
    /// What the command line is being used for.
    /// </summary>
    public CommandLineMode Mode
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsOpen));
            }
        }
    }

    /// <summary>
    /// Whether the command line is showing.
    /// </summary>
    public bool IsOpen => Mode != CommandLineMode.Closed;

    /// <summary>
    /// What precedes the text: <c>:</c>, <c>/</c> or a prompt's label.
    /// </summary>
    public string Prefix { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// The text typed.
    /// </summary>
    public string Text
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? string.Empty) && !_settingText)
            {
                OnTextEdited();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Whether the quick-jump pattern matches nothing, which the view shows.
    /// </summary>
    public bool HasNoMatch { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// The last confirmed quick-jump pattern, which <c>n</c> and <c>N</c> repeat.
    /// </summary>
    public string? LastJumpPattern { get; private set; }

    /// <summary>
    /// The history of command lines run, oldest first.
    /// </summary>
    public IReadOnlyList<string> History => _history.Entries;

    /// <summary>
    /// Opens the command line for an ex command.
    /// </summary>
    /// <param name="initialText">Text to start with, after the <c>:</c>.</param>
    public void OpenCommand(string initialText = "")
    {
        CancelPrompt();
        Mode = CommandLineMode.Command;
        Prefix = ":";
        _history.ResetBrowsing();
        ReplaceText(initialText, new TextSelection(initialText.Length, initialText.Length));
    }

    /// <summary>
    /// Opens the command line for a quick-jump in the active pane.
    /// </summary>
    public void OpenJump()
    {
        CancelPrompt();
        _jumpOrigin = _workspace.ActivePane.CursorIndex;
        _highlightBeforeJump = _workspace.ActivePane.HighlightPattern;
        HasNoMatch = false;
        Mode = CommandLineMode.Jump;
        Prefix = "/";
        ReplaceText(string.Empty, default);
    }

    /// <inheritdoc />
    public Task<string?> PromptAsync(string label, string initialText = "", int selectionStart = 0, int? selectionLength = null)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(initialText);

        // One question at a time: a new prompt cancels whatever was showing.
        CancelPrompt();

        TaskCompletionSource<string?> prompt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _prompt = prompt;
        Mode = CommandLineMode.Prompt;
        Prefix = label;

        int start = Math.Clamp(selectionStart, 0, initialText.Length);
        int end = selectionLength is { } length ? Math.Clamp(start + length, start, initialText.Length) : initialText.Length;
        ReplaceText(initialText, selectionLength is null ? new TextSelection(initialText.Length, initialText.Length) : new TextSelection(start, end));

        return prompt.Task;
    }

    /// <summary>
    /// Enter: runs the command, confirms the jump, or answers the prompt.
    /// </summary>
    public void Accept()
    {
        switch (Mode)
        {
            case CommandLineMode.Command:
                string line = Text;
                Close();
                _history.Add(line);

                if (line.Trim().Length > 0)
                {
                    ExParseResult result = _catalog.Parser.Parse(line);

                    if (result.Succeeded)
                    {
                        CommandSubmitted?.Invoke(this, result.Invocation);
                    }
                    else
                    {
                        _status.Error(result.Error);
                    }
                }

                break;

            case CommandLineMode.Jump:
                if (Text.Length > 0)
                {
                    LastJumpPattern = Text;

                    if (HasNoMatch)
                    {
                        _status.Error($"Pattern not found: {Text}");
                    }
                }

                Close();
                break;

            case CommandLineMode.Prompt:
                TaskCompletionSource<string?>? prompt = _prompt;
                string answer = Text;
                _prompt = null;
                Close();
                prompt?.TrySetResult(answer);
                break;
        }
    }

    /// <summary>
    /// Esc: closes without running; a jump puts the cursor back where it was, a prompt answers nothing.
    /// </summary>
    public void Cancel()
    {
        if (Mode == CommandLineMode.Jump)
        {
            // An abandoned jump leaves the pane as it found it: the cursor, and the highlight it had.
            _workspace.ActivePane.HighlightPattern = _highlightBeforeJump;

            if (_jumpOrigin >= 0)
            {
                _workspace.ActivePane.MoveCursorTo(_jumpOrigin);
            }
        }

        if (Mode == CommandLineMode.Prompt)
        {
            CancelPrompt();
            return;
        }

        if (IsOpen)
        {
            Close();
        }
    }

    /// <summary>
    /// Up: the previous command line starting with what was typed.
    /// </summary>
    public void HistoryOlder()
    {
        if ((Mode == CommandLineMode.Command) && (_history.Older(Text) is { } older))
        {
            ReplaceText(older, new TextSelection(older.Length, older.Length));
        }
    }

    /// <summary>
    /// Down: the next command line, or back to what was typed.
    /// </summary>
    public void HistoryNewer()
    {
        if ((Mode == CommandLineMode.Command) && (_history.Newer() is { } newer))
        {
            ReplaceText(newer, new TextSelection(newer.Length, newer.Length));
        }
    }

    /// <summary>
    /// Tab (or Shift+Tab): replaces the line with the next (or previous) completion.
    /// </summary>
    /// <param name="backwards">Whether to cycle backwards.</param>
    public void Complete(bool backwards = false)
    {
        if (Mode != CommandLineMode.Command)
        {
            return;
        }

        if (_completions is null)
        {
            PaneViewModel pane = _workspace.ActivePane;
            string current = pane.CurrentPath.Length > 0 ? pane.CurrentPath : _folders.Home;
            _completions = _completer.Complete(Text, current, _folders.Home, ListDirectories);
            _completionIndex = backwards ? _completions.Count : -1;
        }

        if (_completions.Count == 0)
        {
            return;
        }

        int step = backwards ? -1 : 1;
        _completionIndex = (((_completionIndex + step) % _completions.Count) + _completions.Count) % _completions.Count;
        string completion = _completions[_completionIndex];
        ReplaceText(completion, new TextSelection(completion.Length, completion.Length));
    }

    /// <summary>
    /// <c>n</c> or <c>N</c>: moves the active pane's cursor to the next or previous match of the last jump.
    /// </summary>
    /// <param name="forward">Whether to search forward.</param>
    public void RepeatJump(bool forward)
    {
        if (LastJumpPattern is not { } pattern)
        {
            _status.Error("No previous pattern");
            return;
        }

        PaneViewModel pane = _workspace.ActivePane;
        pane.HighlightPattern = pattern;
        int match = pane.FindMatch(pattern, pane.CursorIndex, forward, includeStart: false);

        if (match < 0)
        {
            _status.Error($"Pattern not found: {pattern}");
            return;
        }

        pane.MoveCursorTo(match);
    }

    private static IEnumerable<string> ListDirectories(string path)
    {
        try
        {
            return [.. Directory.EnumerateDirectories(path, "*", new EnumerationOptions { IgnoreInaccessible = true })
                .Select(directory => Path.GetFileName(directory))];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return [];
        }
    }

    private void OnTextEdited()
    {
        _completions = null;
        _history.ResetBrowsing();

        if (Mode == CommandLineMode.Jump)
        {
            Jump(Text);
        }
    }

    private void Jump(string pattern)
    {
        PaneViewModel pane = _workspace.ActivePane;

        if (pattern.Length == 0)
        {
            HasNoMatch = false;
            pane.HighlightPattern = _highlightBeforeJump;
            pane.MoveCursorTo(_jumpOrigin);
            return;
        }

        // Incremental, from where the jump started, as vim's incsearch does; every match is highlighted.
        pane.HighlightPattern = pattern;
        int match = pane.FindMatch(pattern, _jumpOrigin, forward: true, includeStart: true);
        HasNoMatch = match < 0;
        pane.MoveCursorTo(match < 0 ? _jumpOrigin : match);
    }

    private void CancelPrompt()
    {
        TaskCompletionSource<string?>? prompt = _prompt;

        if (prompt is null)
        {
            return;
        }

        _prompt = null;
        Close();
        prompt.TrySetResult(null);
    }

    private void ReplaceText(string text, TextSelection selection)
    {
        _settingText = true;

        try
        {
            Text = text;
        }
        finally
        {
            _settingText = false;
        }

        TextReplaced?.Invoke(this, selection);
    }

    private void Close()
    {
        Mode = CommandLineMode.Closed;
        Prefix = string.Empty;
        HasNoMatch = false;
        _completions = null;
        _jumpOrigin = -1;
        _highlightBeforeJump = null;
        ReplaceText(string.Empty, default);
        Closed?.Invoke(this, EventArgs.Empty);
    }
}
