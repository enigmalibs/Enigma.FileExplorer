# FEATURE-A6C2-PHASE01 — Context menus

**Item:** FEATURE-A6C2 — Context menus & drag-drop
**Branch:** `feature/feature-a6c2-phase01-context-menus`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

Right-click menus on the panes, built from the command catalog.

**Item menu.** Opened by a right-click on an entry, or by the Menu key / Shift+F10 at the cursor.

- Entries: Open · Open in a split to the right · Open in a split below · Cut · Copy · Paste into folder ·
  Copy path(s) · Rename · Move to trash · Delete permanently.
- The split entries and "Paste into folder" appear for folders only. Search results add "Reveal in
  folder".
- A right-click on an unmarked entry makes it the only target. A right-click on a marked entry keeps the
  marks, so the menu acts on all of them, and leaves visual mode.

**Background menu.** Opened by a right-click on the empty area.

- Entries: New folder · New file · Paste · View ▸ (Details, Medium icons, Large icons) · Sort by ▸ (Name,
  Date modified, Size, Type, Reverse) · Show hidden files · Refresh · Split ▸ (Side by side, Top and
  bottom) · Close pane.
- View and Sort by show the current choice with a dot. Reverse and Show hidden files show a check
  mark.

Behaviour shared by both menus:

- Every entry runs a catalog command through the dispatcher, so it does exactly what its key does.
- Each entry shows its key in a column (`yy`, `dD`, `Shift+Del`, `Ctrl+W v`, …).
- Entries that cannot run are disabled: Paste when there is nothing to paste (neither here nor on the
  system clipboard), Close pane with a single pane.
- "Paste into folder" pastes into the folder under the menu, not into the pane's own directory.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/ViewModels/ContextMenus.cs` — builds both menus, runs entries
- `src/Enigma.FileExplorer.Desktop/ViewModels/MenuEntry.cs`, `MenuToggle.cs` — the menu model
- `src/Enigma.FileExplorer.Desktop/Commands/MenuCommands.cs` — `<Apps>` / `<S-F10>` (`CommandIds.ContextMenu`)
- `src/Enigma.FileExplorer.Desktop/Views/ContextMenuFactory.cs` — model → Avalonia `ContextMenu`
- Tests: `ViewModels/ContextMenusTests`, `Views/PaneViewContextMenuTests`

**Modified**

- `Desktop/ViewModels/PaneViewModel.cs` — `ContextClick`, `RequestContextMenu`, `ContextMenuRequested`
- `Desktop/ViewModels/MainWindowViewModel.cs` — exposes `Menus` to the pane views
- `Desktop/Views/PaneView.axaml.cs` — right-press targeting, `ContextRequested`, keyboard-requested
  menu below the cursor entry, focus back to the listing on close
- `Desktop/Services/IFileClipboard.cs`, `FileClipboard.cs` — `CanPasteAsync`
- `Desktop/Themes/Styles.axaml` — the key column style
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — `MenuCommands`, `ContextMenus`
- Tests: `Infrastructure/ExplorerHarness` (menus, menu module), `ViewModels/PaneViewModelTests`
- `docs/roadmap.md`, `docs/plan/FEATURE-A6C2.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the key is shown | A second header column with a shared size group, not `MenuItem.InputGesture` | `InputGesture` holds one chord; it cannot show `yy` or `Ctrl+W v` |
| Which key is shown | The first binding (`CommandCatalog.PrimaryGesture`), the vim one | The explorer is keyboard-first; the menu teaches the keys. Entries acting on another folder show none, since no key does exactly that |
| Split entry titles | "Open in a split to the right" / "… below" | The new pane takes the second half of the split; "vertical/horizontal split" is read both ways (vim vs. tmux) |
| When an entry runs | Posted after the menu closes and the listing has focus again | A command that asks a question (Rename) must get the keyboard for the command line, not lose it to the closing menu |
| Mouse vs. keyboard requests | Right-click via `ContextRequested` (with a position); keyboard via the keymap (`CommandIds.ContextMenu`) | The keymap sees the Menu key first, and a keyboard menu opens below the cursor entry rather than at the mouse |
| Menu model location | `ViewModels/` (`ContextMenus`, `MenuEntry`), exposed through `MainWindowViewModel.Menus` | Needs the dispatcher, which cannot be injected into panes or the workspace without a cycle |
| Right-click on the empty area | Leaves the marks alone | No background entry acts on the targets; narrowest behaviour |

## Deviations & follow-ups

- The plan named the item entries "Open in vertical/horizontal split"; they are titled by where the new
  pane goes (see decisions).
- "Rename" renames the entry under the menu, also when several are marked, which is what `r` does.
- Not checked by hand on a real desktop: keyboard navigation inside the open menu and focus return with
  a real window manager. Both work headlessly. Opening the menu with the mouse and the Menu key, the
  targeting and running an entry are covered by headless tests.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 607  failed: 0  succeeded: 604  skipped: 3
```

The first full build and test run was green, so no fix cycle was used. The three skips are the known
platform-specific ones: the Linux-only launcher, symlink delete and symlink search.

A throwaway headless render (not committed) showed both menus, the aligned key column, dimmed disabled
entries and the radio dot in Sort by. It first revealed a missing key-column style, which was added
before the full run.
