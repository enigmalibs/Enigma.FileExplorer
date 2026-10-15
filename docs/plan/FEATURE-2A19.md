# FEATURE-2A19 — View modes, sorting & hidden files

**Status:** DONE — see `docs/done/FEATURE-2A19.md`
**Type:** FEATURE
**Branch:** `feature/feature-2a19-view-modes`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Give every pane three view modes — **details list**, **medium icons**, **large icons** — switchable
from the keyboard (`zd`/`zm`/`zl`, `<C-S-6>`/`<C-S-3>`/`<C-S-2>`, `:view`), plus sorting (column
headers, `on`/`om`/`os`/`ot`/`or`, `:sort`) and a hidden-files toggle (`zh`, `:set hidden`).

## Context & constraints

- Builds on FEATURE-C4E7 (pane, details rows, `EntrySorter`, `HiddenRules`, `FileIcons`), FEATURE-2E0F
  (catalog ids for these keys already declared) and FEATURE-95D6 (per-pane state in a workspace).
- View mode and sort are **per pane**; show-hidden is **global** (one toggle, all panes refresh) — the
  ranger convention, and what the user will expect after `zh`.
- All three views must stay virtualized: directories with thousands of entries (e.g.
  `C:\Windows\System32`) must open quickly in every mode.
- The cursor/selection model is unchanged; only presentation and cursor *geometry* change: in icon
  views `j`/`k`/Up/Down move by a row, Left/Right move by one item, `h`/`l` keep parent/open so the
  muscle memory is the same in all views.

## Steps

1. `ViewMode` (Details, MediumIcons, LargeIcons) on `PaneViewModel`; commands `view.details`,
   `view.medium`, `view.large` + `:view <mode>`.
2. Details view: column header row (Name, Modified, Type, Size) — clicking a header sorts by it,
   clicking again reverses; the active sort shows an arrow glyph.
3. Icon views: tiles (medium: 48 px glyph in a ~96 px tile; large: 96 px glyph in a ~144 px tile, name
   wrapped to two lines with ellipsis). Virtualized by rows: the pane exposes rows of N items, N
   computed from the viewport width and the tile width, rebuilt on resize/mode change; a virtualized
   `ItemsControl` of rows.
4. Grid-aware cursor motions: `PaneViewModel` knows `ItemsPerRow` (1 in details) and page size;
   motions use them; scroll-into-view by row.
5. Sorting: `on`/`om`/`os`/`ot` set the key (keeping direction ascending, except Modified/Size which
   start descending — newest/largest first), `or` reverses; `:sort key` / `:sort! key`.
6. Hidden toggle: `zh`, `:set hidden|nohidden|hidden!` — global setting held by a singleton service,
   every pane re-filters without re-reading the disk.
7. Tests: rows chunking and cursor geometry, sort command semantics, hidden toggle re-filtering,
   headless render of each mode.

## Acceptance criteria

- `zd`/`zm`/`zl` (and the Ctrl+Shift chords, and `:view`) switch the active pane's mode; the cursor
  item stays selected and visible.
- Icon views navigate spatially with `j`/`k`/arrows; a 5,000-entry directory opens without a visible
  stall in every mode.
- Header clicks and `o*` keys sort; `zh` shows/hides dot-files and Hidden-attribute files in all panes.
- Build zero warnings, all tests green.

## Out of scope

- Thumbnails/previews, a small-icons or list mode, column resizing/reordering, per-directory view
  memory (view mode is per pane), rubber-band (lasso) selection.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The three modes | Details, medium icons, large icons | Exactly the draft's list | Adding small icons / list |
| Icon-view virtualization | Rows of N items in a virtualized list | Keeps every mode fast on huge directories without a new dependency | Non-virtualized `WrapPanel` (slow on big dirs); adding `ItemsRepeater` package |
| Icon-view motions | `j`/`k` by row, Left/Right by item, `h`/`l` parent/open | Spatial in the grid while keeping `h`/`l` muscle memory across views | `h`/`l` move horizontally in grids (breaks parent/open) |
| Default sort directions | Name/Type ascending, Modified/Size descending first | What users want first for dates and sizes | Always ascending |
| Hidden toggle scope | Global | ranger convention; a single mental state | Per pane |
| View/sort scope | Per pane | Different panes often serve different purposes | Global |
