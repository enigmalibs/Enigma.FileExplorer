# FEATURE-5870 — Help dialog redesign

**Status:** DONE — see `docs/done/FEATURE-5870.md`
**Type:** FEATURE
**Branch:** `feature/feature-5870-help-dialog`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

Make the key-binding help (`?`, `F1`, `:help`) easy to read and drive from the keyboard:

- categories set apart clearly;
- one scroll area that works with the scrollbar, the wheel and the keys;
- `j`/`k` to scroll and `q` to close.

## Context & constraints

- Today `HelpView` is a `ScrollViewer` (`MaxHeight` 520) inside the `ContentDialog` card, which has its
  own scroll area (`DialogMaxHeight` 640).
  - Two nested scrollers: the outer one can scroll a little too, and the auto-hiding Fluent scrollbar is
    thin.
  - The dialog never takes the keyboard, so only the wheel scrolls.
  - The categories are plain bold lines.
- While a dialog is open, `MainWindow` routes no keys to the keymap. The help view must handle its own
  keys.
- The content is generated from `CommandCatalog` (`HelpViewModel.Build`) and stays that way.

## Steps

1. `HelpView`:
   - one `ScrollViewer` (`AllowAutoHide=False`, focusable), sized so the dialog card never needs its
     own scroll — its height follows the window and is capped;
   - inside it, one `SettingsCardExpander` per category (expanded), with a category icon, the category
     as header, and "N commands" as description;
   - rows inside: keys, title, ex command, in aligned columns.
2. Keyboard, in the view:
   - `j`/`↓` and `k`/`↑` scroll a line;
   - `Ctrl+d`/`Ctrl+u` scroll half a page; `PageDown`/`PageUp`/`Space` a page;
   - `g`/`Home` go to the top and `G`/`End` to the bottom;
   - `q` and `Esc` close.
   The view takes focus when it opens.
3. `HelpViewModel`:
   - `HelpSection` gains `Icon` and `Description`;
   - a `CloseCommand` invokes the close callback given by `HelpDialogService`, which hides the dialog.
   - The split-group section's description names the leader key.
4. Tests:
   - VM: sections, icons and descriptions;
   - headless: the dialog opens with the help focused; `j` scrolls down and `k` scrolls up; `G`/`g` reach
     the ends; `q` closes; the dialog card itself does not scroll.

## Acceptance criteria

- `?` opens the help with each category in its own expander card.
- `j`/`k` (and the arrows, `Ctrl+d`/`u`, Page keys, `g`/`G`) scroll it. The scrollbar is always visible
  and can be dragged; the wheel works. There is only one scrollbar.
- `q` or `Esc` closes it and gives the keyboard back to the listing.
- Build has zero warnings and all tests are green.

## Out of scope

- Searching or filtering the help, and collapsing categories from the keyboard.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Category layout | `SettingsCardExpander` per category, expanded by default | The user suggested it; clear separation; collapsible with the mouse | Collapsed by default (more keystrokes to read) |
| Scrolling structure | One owned `ScrollViewer` sized to fit the card | Removes the nested scrolling; lets the view drive it with keys | Relying on the card's internal scroller (template coupling) |
| Scrollbar | `AllowAutoHide=False` | A full-size, always-visible bar is easy to grab | The Fluent auto-hide overlay bar |
| Key handling | View code-behind for scrolling; VM `CloseCommand` for `q` | Scrolling is a view concern; closing goes through the dialog service | Routing through the global keymap |
