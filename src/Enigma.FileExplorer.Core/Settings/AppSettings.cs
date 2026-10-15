using System.Collections.Generic;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// Everything the explorer remembers between runs. Every member has a default, so a file missing some of
/// them (an older version's, or a hand-edited one) still loads.
/// </summary>
/// <remarks>
/// The settings types have settable rather than init-only properties on purpose: the JSON source generator
/// sets every init-only property, so a member missing from the file would become null or zero instead of
/// keeping its default.
/// </remarks>
public sealed record AppSettings
{
    /// <summary>
    /// The schema this build writes.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// The schema the file was written with.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// The colour theme.
    /// </summary>
    public ThemePreference Theme { get; set; } = ThemePreference.Dark;

    /// <summary>
    /// Whether hidden entries are listed.
    /// </summary>
    public bool ShowHidden { get; set; }

    /// <summary>
    /// Whether moving to the trash asks first. On by default; switched off with <c>:set noconfirmtrash</c>
    /// once the user trusts their fingers. Deleting permanently always asks.
    /// </summary>
    public bool ConfirmTrash { get; set; } = true;

    /// <summary>
    /// The leader key, in vim notation: the first key of the split-group commands (<c>&lt;Space&gt;v</c>).
    /// Read at start; an unusable key falls back to <c>&lt;Space&gt;</c>.
    /// </summary>
    public string LeaderKey { get; set; } = "<Space>";

    /// <summary>
    /// The bookmarked folders, in the order they were added.
    /// </summary>
    public IReadOnlyList<BookmarkSettings> Bookmarks { get; set; } = [];

    /// <summary>
    /// Where the main window was; <see langword="null"/> before the first exit.
    /// </summary>
    public WindowPlacement? Window { get; set; }

    /// <summary>
    /// How the explorer's pane shows its entries at start: the active pane's view and sort at the last exit;
    /// <see langword="null"/> before the first exit. The layout and the directories are not kept: the
    /// explorer always starts with a single pane at home.
    /// </summary>
    public PaneSettings? Pane { get; set; }
}
