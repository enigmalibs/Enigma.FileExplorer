using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// Turns <see cref="MenuEntry"/> lists into Avalonia menus. The key is drawn in the header rather than set
/// as the item's input gesture, which holds a single chord and could not show <c>yy</c> or <c>Ctrl+W v</c>.
/// </summary>
internal static class ContextMenuFactory
{
    /// <summary>
    /// The shared size group that lines the keys up in a column, as in every desktop menu.
    /// </summary>
    private const string TitleColumn = "MenuEntryTitle";

    /// <summary>
    /// Builds a menu.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="chosen">Called with the entry clicked.</param>
    /// <returns>The menu, closed.</returns>
    public static ContextMenu Build(IReadOnlyList<MenuEntry> entries, Action<MenuEntry> chosen)
    {
        ContextMenu menu = new() { ItemsSource = Items(entries, chosen) };
        Grid.SetIsSharedSizeScope(menu, true);
        return menu;
    }

    /// <summary>
    /// Opens a menu with its top-left corner at the bottom-left of an anchor, turned around by the
    /// platform where it would leave the screen.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <param name="owner">The control it belongs to.</param>
    /// <param name="anchor">Where it opens, relative to <paramref name="owner"/>: a point, or an entry it
    /// opens below.</param>
    public static void Open(ContextMenu menu, Control owner, Rect anchor)
    {
        menu.Placement = PlacementMode.AnchorAndGravity;
        menu.PlacementRect = anchor;
        menu.PlacementAnchor = PopupAnchor.BottomLeft;
        menu.PlacementGravity = PopupGravity.BottomRight;
        menu.Open(owner);
    }

    private static List<Control> Items(IReadOnlyList<MenuEntry> entries, Action<MenuEntry> chosen)
        => [.. entries.Select(entry => Item(entry, chosen))];

    private static Control Item(MenuEntry entry, Action<MenuEntry> chosen)
    {
        if (entry.IsSeparator)
        {
            return new Separator();
        }

        MenuItem item = new()
        {
            Header = Header(entry),
            IsEnabled = entry.IsEnabled,
            ToggleType = entry.Toggle switch
            {
                MenuToggle.Check => MenuItemToggleType.CheckBox,
                MenuToggle.Radio => MenuItemToggleType.Radio,
                _ => MenuItemToggleType.None,
            },
            IsChecked = entry.IsChecked,
            Tag = entry,
        };

        AutomationProperties.SetName(item, entry.Title);

        if (entry.Children.Count > 0)
        {
            item.ItemsSource = Items(entry.Children, chosen);
        }
        else
        {
            item.Click += (_, _) => chosen(entry);
        }

        return item;
    }

    private static Grid Header(MenuEntry entry)
    {
        Grid header = new();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = TitleColumn });
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.Children.Add(new TextBlock { Text = entry.Title });

        if (entry.Gesture is { } gesture)
        {
            TextBlock keys = new() { Text = gesture };
            keys.Classes.Add("menuGesture");
            Grid.SetColumn(keys, 1);
            header.Children.Add(keys);
        }

        return header;
    }
}
