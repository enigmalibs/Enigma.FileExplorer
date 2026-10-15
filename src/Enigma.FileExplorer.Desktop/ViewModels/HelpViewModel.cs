using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.Icons.Phosphor;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One line of the key-binding help.
/// </summary>
/// <param name="Keys">The keys, as people read them: <c>j, ↓</c>.</param>
/// <param name="Title">What the command does.</param>
/// <param name="ExCommand">The ex command that also runs it, vim-style: <c>:vs[plit]</c>; empty when there is none.</param>
public sealed record HelpRow(string Keys, string Title, string ExCommand);

/// <summary>
/// One section of the key-binding help, shown as a card of its own.
/// </summary>
/// <param name="Category">The section's title.</param>
/// <param name="Rows">Its lines.</param>
/// <param name="Icon">The card's icon.</param>
/// <param name="Description">The card's subtitle: how many commands, and anything worth knowing about them.</param>
public sealed record HelpSection(string Category, IReadOnlyList<HelpRow> Rows, PhosphorIcon Icon, string Description);

/// <summary>
/// The key-binding help, generated from the command catalog so it can never fall behind the keymap.
/// </summary>
public sealed class HelpViewModel
{
    private static readonly FrozenDictionary<string, PhosphorIcon> Icons = new Dictionary<string, PhosphorIcon>(StringComparer.Ordinal)
    {
        [CommandCatalog.NavigationCategory] = PhosphorIcon.Compass,
        [CommandCatalog.MarksCategory] = PhosphorIcon.CheckSquare,
        [CommandCatalog.WindowsCategory] = PhosphorIcon.Columns,
        [CommandCatalog.ViewCategory] = PhosphorIcon.Eye,
        [CommandCatalog.FilesCategory] = PhosphorIcon.Files,
        [CommandCatalog.SearchCategory] = PhosphorIcon.MagnifyingGlass,
        [CommandCatalog.BookmarksCategory] = PhosphorIcon.BookmarkSimple,
        [CommandCatalog.GeneralCategory] = PhosphorIcon.Keyboard,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Builds the help from a catalog.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="close">Closes the help (<c>q</c>).</param>
    public HelpViewModel(CommandCatalog catalog, Action close)
    {
        ArgumentNullException.ThrowIfNull(close);

        Sections = Build(catalog);
        CloseCommand = new RelayCommand(close);
    }

    /// <summary>
    /// The sections, in catalog order.
    /// </summary>
    public IReadOnlyList<HelpSection> Sections { get; }

    /// <summary>
    /// Closes the help.
    /// </summary>
    public RelayCommand CloseCommand { get; }

    /// <summary>
    /// Builds the help sections: one line per command that has a key or an ex command.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <returns>The sections.</returns>
    public static IReadOnlyList<HelpSection> Build(CommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return [.. catalog.Commands
            .GroupBy(command => command.Category)
            .Select(group =>
            {
                HelpRow[] rows = [.. group
                    .Select(command => new HelpRow(Keys(catalog, command.Id), command.Title, ExCommand(catalog, command.Id)))
                    .Where(row => (row.Keys.Length > 0) || (row.ExCommand.Length > 0))];

                return new HelpSection(group.Key, rows, Icons.GetValueOrDefault(group.Key, PhosphorIcon.Info), Describe(catalog, group.Key, rows.Length));
            })];
    }

    private static string Describe(CommandCatalog catalog, string category, int count)
    {
        string commands = string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? "command" : "commands")}");

        // The leader is the user's choice, so its card says which key it is and where it is set.
        return category == CommandCatalog.WindowsCategory
            ? $"{commands} · Leader key: {KeyNotation.Describe([catalog.Leader])} (leaderKey in settings.json)"
            : commands;
    }

    private static string Keys(CommandCatalog catalog, string commandId)
    {
        IReadOnlyList<KeyBinding> bindings = catalog.Keymap.BindingsFor(commandId);

        // g1 to g9 are one idea; nine lines would bury it.
        if ((bindings.Count > 2) && bindings.All(binding => binding.Argument is not null))
        {
            return $"{bindings[0].Description} … {bindings[^1].Description}";
        }

        return string.Join(", ", bindings.Select(binding => binding.Description));
    }

    private static string ExCommand(CommandCatalog catalog, string commandId)
    {
        foreach (ExCommandDefinition definition in catalog.ExCommands)
        {
            if (definition.CommandId == commandId)
            {
                return Format(definition, bang: false);
            }

            if (definition.BangCommandId == commandId)
            {
                return Format(definition, bang: true);
            }
        }

        return string.Empty;
    }

    private static string Format(ExCommandDefinition definition, bool bang)
    {
        string name = definition.MinimumLength < definition.Name.Length
            ? $"{definition.ShortName}[{definition.Name[definition.MinimumLength..]}]"
            : definition.Name;

        return $":{name}{(bang ? "!" : string.Empty)}";
    }
}
