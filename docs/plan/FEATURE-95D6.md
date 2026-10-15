# FEATURE-95D6 — Split groups

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-95d6-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Let the user split the explorer vertically and horizontally, to any depth, like vim windows: every
split group (pane) is an independent browser with its own directory, history, cursor and view mode;
the user moves focus between panes spatially from the keyboard, resizes them with the mouse or keys,
and closes them.

## Context & constraints

- Builds on FEATURE-C4E7 (`PaneViewModel`, `PaneView`, places sidebar) and FEATURE-2E0F (command
  catalog, dispatcher, `:` commands). The `<C-w>` keys and the `:split`/`:vsplit`/`:close`/`:only`/
  `:q`/`:qa` ex commands are already declared in the catalog; this item wires them.
- vim semantics: **vertical split** (`<C-w>v`, `:vsplit`) puts panes side by side (vertical divider);
  **horizontal split** (`<C-w>s`, `:split`) stacks them. The new pane opens on the same directory and
  view mode as the pane it was split from (or on the path argument) and receives focus.
- The layout model is UI-free and lives in Core so its rules (split, close, equalize, resize,
  neighbour search) are unit-tested; FEATURE-CA94 serializes it.

## Design

```
Core/Layout/   LayoutNode (abstract), PaneNode (leaf, PaneId), SplitNode (orientation, first, second,
               ratio), SplitOrientation, LayoutTree (root + operations), FocusDirection,
               LayoutGeometry (normalized rectangles), LayoutSnapshot (serializable DTO)
Desktop/       WorkspaceViewModel (tree of SplitViewModel / PaneViewModel, active pane),
               SplitHost control (recursive Grid + GridSplitter built from the tree)
```

## PHASE01 — Core split-tree model

**Status:** DONE — see `docs/done/FEATURE-95D6-PHASE01.md`
**Branch:** `feature/feature-95d6-phase01-split-tree`

### Steps

1. Binary tree: `SplitNode` with orientation and a ratio (first child share, clamped to
   [0.1, 0.9]); `PaneNode` leaves identified by a `PaneId`.
2. `LayoutTree.Split(pane, orientation)` → new pane id inserted after `pane`, ratio 0.5.
3. `Close(pane)` → the sibling takes the parent's place; closing the last pane is refused (the caller
   decides to quit).
4. `Only(pane)`, `Equalize()` (ratios weighted by the number of panes along the split axis so three
   side-by-side panes get thirds), `Resize(pane, axis, delta)` (adjusts the nearest ancestor split on
   that axis).
5. `LayoutGeometry`: normalized rectangles per pane; `FindNeighbor(pane, direction)` picks the
   adjacent pane overlapping the source's span, preferring the one closest to the source's centre;
   `Next`/`Previous` in reading order (depth-first).
6. `LayoutSnapshot` round-trip (DTO without behaviour).
7. Tests for all of the above, including nested mixed orientations.

### Acceptance criteria

- Split/close/only/equalize/resize/neighbour/next/previous behave as specified across nested trees.
- Build zero warnings, all tests green.

## PHASE02 — Split UI & window commands

**Status:** DONE — see `docs/done/FEATURE-95D6-PHASE02.md`
**Branch:** `feature/feature-95d6-phase02-split-ui`

### Steps

1. `WorkspaceViewModel`: owns the `LayoutTree`, a `PaneViewModel` per `PaneId` (created through a
   factory from DI), `ActivePane`; replaces the single pane in `MainWindowViewModel`. Commands and the
   places sidebar target the active pane.
2. `SplitHost` view: builds a `Grid` per split node (two star rows/columns + a `GridSplitter`) from the
   tree, rebuilt on structural change; splitter drags write the ratio back; panes are reused (not
   recreated) when the tree changes.
3. Active pane: highlighted border (Enigma accent brush), focused on activation; clicking anywhere in
   a pane activates it; the window title shows the active pane's directory.
4. Wire `<C-w>v/s/h/j/k/l/arrows/w/W/c/q/o/=/+/-/>/<` and `:split`/`:vsplit [path]`/`:close`/`:only`/
   `:q` (last pane → quit the app)/`:qa`.
5. Toolbar buttons (split vertical, split horizontal, close pane) for mouse users, with tooltips naming
   the key.
6. Tests: workspace operations update panes and focus; `:vsplit path` opens the path; closing the
   active pane moves focus to the neighbour that takes its place; headless render of a 3-pane layout.

### Acceptance criteria

- Arbitrary nesting of vertical/horizontal splits; each pane browses independently.
- Focus moves spatially with `<C-w>h/j/k/l`; mouse click focuses; the active pane is visible.
- Resizing by splitter drag and by `<C-w>+/-/</>`; `<C-w>=` equalizes.
- `:q` on the last pane exits.
- Build zero warnings, all tests green.

## Out of scope

- Tabs inside a split group; moving a pane to another position (`<C-w>H/J/K/L`, `<C-w>r`); maximize
  toggle. Session persistence of the layout is FEATURE-CA94.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Split model | Recursive binary tiling tree | Any arrangement the user can reach with vim splits; simple invariant | Fixed dual pane (too rigid); Enigma `DockingHost` (IDE-style, mouse-oriented, heavier) |
| Split vocabulary | vim's (vertical = side by side) | The user asked for vim commands | Inverted "horizontal = side by side" |
| Pane content | One directory per pane, no tabs | Narrowest reading of "split groups"; tabs are a separate feature | Tabs per group |
| New pane start | Same directory and view as the source | vim behaviour; the common case is "compare / copy within the same area" | Home directory |
| Equalize | Weighted by panes along the axis | Three side-by-side panes really get thirds | Every ratio 0.5 |
| Closing the last pane | `:q` quits the app; `<C-w>c` refuses | vim behaviour | Never quit from `:q` |
| Layout model location | Core | Pure logic, unit-tested, serialized by FEATURE-CA94 | View code-behind |
