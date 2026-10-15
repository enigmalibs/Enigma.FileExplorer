using System.Collections.Generic;
using Enigma.FileExplorer.Desktop.Commands;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One entry of a context menu: a command, a submenu or a separator. A command entry runs a catalog
/// command through the dispatcher, so a menu can never do something its key does not.
/// </summary>
public sealed record MenuEntry
{
    /// <summary>
    /// The line between groups of entries.
    /// </summary>
    public static MenuEntry Separator { get; } = new() { IsSeparator = true };

    /// <summary>
    /// What the entry says.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The command it runs; <see langword="null"/> for a submenu or a separator.
    /// </summary>
    public CommandInvocation? Invocation { get; init; }

    /// <summary>
    /// The key that does the same, as people read it (<c>yy</c>, <c>Ctrl+W v</c>); <see langword="null"/>
    /// when no key does exactly this.
    /// </summary>
    public string? Gesture { get; init; }

    /// <summary>
    /// Whether the entry can be chosen now.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Whether the entry shows a check mark or a dot.
    /// </summary>
    public MenuToggle Toggle { get; init; }

    /// <summary>
    /// Whether the check mark or the dot is shown.
    /// </summary>
    public bool IsChecked { get; init; }

    /// <summary>
    /// The entries of a submenu; empty for anything else.
    /// </summary>
    public IReadOnlyList<MenuEntry> Children { get; init; } = [];

    /// <summary>
    /// Whether this is <see cref="Separator"/>.
    /// </summary>
    public bool IsSeparator { get; init; }
}
