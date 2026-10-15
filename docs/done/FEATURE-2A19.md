# FEATURE-2A19 — View modes, sorting & hidden files

**Branch:** `feature/feature-2a19-view-modes`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

**View modes.** Every pane has three: details list, medium icons and large icons. Switch with `zd`/`zm`/
`zl`, the Explorer chords Ctrl+Shift+6/3/2, or `:view details|medium|large` (unambiguous prefixes
accepted).

- **Virtualization.** The icon views stay virtualized: the pane exposes its entries as rows of
  `ItemsPerRow` tiles, rebuilt when the width or mode changes, inside a virtualizing list of rows. A
  5,000-entry directory realizes only the visible rows (headless test).
- **Movement.** In a grid, `j`/`k` move a row, Left/Right move a tile, and `h`/`l` keep parent/open.
  Paging uses rows × tiles.
- **Tiles.** Medium tiles carry a 48 px glyph and large tiles a 96 px glyph, with names wrapped to two
  lines. The cursor fills the tile, a mark draws an accent border, and inactive panes dim their
  cursor.

**Sorting.** `on`/`ot` sort A→Z, while `om`/`os` sort newest/largest first, and `or` reverses.
`:sort key` uses the same natural direction and `:sort! key` reverses it. The details header's columns
are clickable (a second click reverses) and show ↑/↓ on the sort column.

**Hidden files.** One global switch, `ViewOptions.ShowHidden`, toggled by `zh` or set with
`:set hidden|nohidden|hidden!`. Every pane re-filters at once without re-reading the disk, and new
panes follow it. A split copies the source pane's sort and view mode.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Layout/ViewMode.cs`
- `src/Enigma.FileExplorer.Desktop/Commands/ViewCommands.cs`
- `src/Enigma.FileExplorer.Desktop/Services/ViewOptions.cs`
- `src/Enigma.FileExplorer.Desktop/ViewModels/EntryRowViewModel.cs`
- Tests: `Commands/ViewCommandsTests`, `Views/IconViewTests`

**Modified**

- `Core/FileSystem/EntrySort.cs` — `EntrySort.For(key)` (natural direction)
- `Desktop/ViewModels/PaneViewModel.cs` — view mode, tile metrics, `ItemsPerRow`, `RowStep`, `Rows`,
  header texts, `SortBy`/`SortByCommand`
- `Desktop/ViewModels/WorkspaceViewModel.cs` — global hidden setting; split copies sort and view mode
- `Desktop/Commands/NavigationCommands.cs` — row-sized `j`/`k`, tile-wise arrows in grids
- `Desktop/Views/PaneView.axaml(.cs)` — clickable headers, icon list, geometry (tiles per row, page size),
  scroll-into-view per mode
- `Desktop/Themes/Styles.axaml` — tile and header styles
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — view options, view commands
- Tests: `Infrastructure/ExplorerHarness`, `ViewModels/WorkspaceViewModelTests`
- `docs/roadmap.md`, `docs/plan/FEATURE-2A19.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `ViewMode` location | Core (`Layout`) | Session persistence (FEATURE-CA94) stores it next to the layout |
| Row chunking | In the ViewModel, from a tile count the view reports | Testable; the view only measures |
| Arrow keys in grids | Left/Right move a tile; Up/Down move a row like `j`/`k` | What every icon grid does; `h`/`l` keep their meaning everywhere |
| `:sort` direction | The key's natural direction; `!` reverses it | Matches `os`/`om` so key and ex command agree |
| Abbreviated arguments | Unambiguous prefixes for `:view` and `:sort` | Consistent with abbreviated command names |
| Header widget | Borderless buttons, unfocusable | Clickable for the mouse without taking the keyboard from the listing |
| Tile slack | Presenter padding + 4 px | Fluent's scroll bars overlay content; the first, larger allowance cost a whole tile column (seen in a render) |

## Deviations & follow-ups

- Tile names wrap to two lines with trailing trimming. A very long name loses its end without a
  visible ellipsis when the cut falls on a line break. This is cosmetic and is a follow-up.
- Rubber-band (lasso) selection and per-directory view memory remain out of scope, as planned.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 447  failed: 0
```

Fix cycles used: **1 of 3.** The first full run failed one of my own expectations in
`InAGrid_JKMoveARow…`: Down from index 0 in a 4-wide grid lands on index 4, which is `f04.txt` (the
directory `sub` is index 0), not `f03.txt`. The code was right. After the green run, the tile-slack
constant was tightened following a throwaway headless render; the full suite was re-run green.
