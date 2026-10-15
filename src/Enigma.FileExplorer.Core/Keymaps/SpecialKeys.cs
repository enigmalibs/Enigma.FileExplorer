using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.FileExplorer.Core.Keymaps;

/// <summary>
/// The names of the keys that do not produce a character, with vim's aliases.
/// </summary>
public static class SpecialKeys
{
    /// <summary>Escape.</summary>
    public const string Escape = "Esc";

    /// <summary>Enter (vim's carriage return).</summary>
    public const string Enter = "CR";

    /// <summary>Backspace.</summary>
    public const string Backspace = "BS";

    /// <summary>The space bar.</summary>
    public const string Space = "Space";

    /// <summary>Tab.</summary>
    public const string Tab = "Tab";

    /// <summary>Delete.</summary>
    public const string Delete = "Del";

    /// <summary>Insert.</summary>
    public const string Insert = "Insert";

    /// <summary>Arrow up.</summary>
    public const string Up = "Up";

    /// <summary>Arrow down.</summary>
    public const string Down = "Down";

    /// <summary>Arrow left.</summary>
    public const string Left = "Left";

    /// <summary>Arrow right.</summary>
    public const string Right = "Right";

    /// <summary>Home.</summary>
    public const string Home = "Home";

    /// <summary>End.</summary>
    public const string End = "End";

    /// <summary>Page up.</summary>
    public const string PageUp = "PageUp";

    /// <summary>Page down.</summary>
    public const string PageDown = "PageDown";

    /// <summary>The context-menu (application) key.</summary>
    public const string Apps = "Apps";

    private static readonly FrozenDictionary<string, string> Aliases = BuildAliases();

    private static readonly FrozenDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Escape] = "Esc",
        [Enter] = "Enter",
        [Backspace] = "Backspace",
        [Space] = "Space",
        [Tab] = "Tab",
        [Delete] = "Del",
        [Insert] = "Insert",
        [Up] = "↑",
        [Down] = "↓",
        [Left] = "←",
        [Right] = "→",
        [Home] = "Home",
        [End] = "End",
        [PageUp] = "PageUp",
        [PageDown] = "PageDown",
        [Apps] = "Menu",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The canonical name of a special key, from any of its vim spellings, case-insensitively.
    /// </summary>
    /// <param name="name">A name such as <c>esc</c>, <c>Enter</c>, <c>pagedown</c> or <c>f10</c>.</param>
    /// <returns>The canonical name, or <see langword="null"/> when it names no special key.</returns>
    public static string? Canonical(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (Aliases.TryGetValue(name, out string? canonical))
        {
            return canonical;
        }

        // F1 to F24.
        if ((name.Length is 2 or 3) && ((name[0] == 'F') || (name[0] == 'f'))
            && int.TryParse(name.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            && (number is >= 1 and <= 24))
        {
            return $"F{number}";
        }

        return null;
    }

    /// <summary>
    /// How a special key is shown to people (help, menus).
    /// </summary>
    /// <param name="canonical">The canonical name.</param>
    /// <returns>The label.</returns>
    public static string Label(string canonical)
        => Labels.TryGetValue(canonical, out string? label) ? label : canonical;

    private static FrozenDictionary<string, string> BuildAliases()
    {
        Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase);

        void Add(string canonical, params string[] names)
        {
            aliases[canonical] = canonical;

            foreach (string name in names)
            {
                aliases[name] = canonical;
            }
        }

        Add(Escape, "Escape");
        Add(Enter, "Enter", "Return");
        Add(Backspace, "Backspace");
        Add(Space);
        Add(Tab);
        Add(Delete, "Delete");
        Add(Insert, "Ins");
        Add(Up);
        Add(Down);
        Add(Left);
        Add(Right);
        Add(Home);
        Add(End);
        Add(PageUp);
        Add(PageDown);
        Add(Apps, "Menu");

        return aliases.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
