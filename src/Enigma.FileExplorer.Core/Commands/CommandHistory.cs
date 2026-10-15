using System;
using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// The command line's history, as vim keeps it: newest last, no duplicates, and browsing with Up/Down
/// only visits entries that start with what was typed before browsing began.
/// </summary>
/// <param name="capacity">How many entries are kept; the oldest go first.</param>
public sealed class CommandHistory(int capacity = 100)
{
    private readonly List<string> _entries = [];
    private int _index = -1;
    private string _typed = string.Empty;

    /// <summary>
    /// The entries, oldest first.
    /// </summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <summary>
    /// Records a line that was run. A line already in the history moves to the newest place.
    /// </summary>
    /// <param name="line">The line.</param>
    public void Add(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string trimmed = line.Trim();
        ResetBrowsing();

        if (trimmed.Length == 0)
        {
            return;
        }

        _entries.Remove(trimmed);
        _entries.Add(trimmed);

        if (_entries.Count > capacity)
        {
            _entries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Steps back to an older entry that starts with what was typed.
    /// </summary>
    /// <param name="current">The line as it is now; remembered as the filter when browsing starts.</param>
    /// <returns>The older entry, or <see langword="null"/> when there is none.</returns>
    public string? Older(string current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (_index < 0)
        {
            _typed = current;
            _index = _entries.Count;
        }

        for (int i = _index - 1; i >= 0; i--)
        {
            if (_entries[i].StartsWith(_typed, StringComparison.Ordinal))
            {
                _index = i;
                return _entries[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Steps forward to a newer entry; past the newest, back to what was typed.
    /// </summary>
    /// <returns>The newer entry or the typed line, or <see langword="null"/> when not browsing.</returns>
    public string? Newer()
    {
        if (_index < 0)
        {
            return null;
        }

        for (int i = _index + 1; i < _entries.Count; i++)
        {
            if (_entries[i].StartsWith(_typed, StringComparison.Ordinal))
            {
                _index = i;
                return _entries[i];
            }
        }

        _index = -1;
        return _typed;
    }

    /// <summary>
    /// Ends browsing; the next <see cref="Older"/> starts again from the newest entry.
    /// </summary>
    public void ResetBrowsing() => _index = -1;
}
