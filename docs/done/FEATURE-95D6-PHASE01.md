# FEATURE-95D6-PHASE01 — Core split-tree model

**Item:** FEATURE-95D6 — Split groups
**Branch:** `feature/feature-95d6-phase01-split-tree`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The UI-free model of split groups. `LayoutTree` is a binary tree of `SplitNode`s (axis + clamped
ratio) over `PaneNode` leaves, with vim's window operations:

- **Split.** The new pane goes right or below, at a 0.5 ratio.
- **Close.** The sibling takes the space, and the method returns the pane adjacent in reading order
  as the successor for focus. Closing the last pane is refused.
- **Only.** Keeps one pane and reports the panes it closed.
- **Equalize.** Ratios are weighted by how many panes lie along each split's axis, so three
  side-by-side panes really get thirds, even across nested rows.
- **Resize.** Moves the nearest split on the requested axis.
- **`SetRatio`.** Used by splitter drags.
- **Geometry.** Normalized rectangles per pane.
- **`FindNeighbor`.** The pane touching the source on that side that overlaps it, preferring the one
  facing its centre.
- **Next / Previous.** Reading order, wrapping.
- **Snapshot.** A plain `LayoutSnapshot` record tree that round-trips for persistence (FEATURE-CA94),
  rejects malformed trees and duplicate ids, and keeps new ids from colliding with restored ones.

Every mutation raises `Changed`.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Layout/` — `SplitAxis`, `FocusDirection`, `PaneId`, `LayoutNode`,
  `PaneNode`, `SplitNode`, `LayoutRect`, `LayoutSnapshot`, `LayoutTree`
- `tests/Enigma.FileExplorer.Core.UnitTests/Layout/LayoutTreeTests.cs`

**Modified**

- `docs/roadmap.md`, `docs/plan/FEATURE-95D6.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Axis naming | `SplitAxis.Columns` (side by side) / `Rows` (stacked) | "Vertical split" means opposite things in vim and in most GUI toolkits; columns/rows map straight onto the Grid the view builds |
| Successor after closing | The pane adjacent in reading order inside the sibling subtree | It stands where the closed pane stood, which is where the eye already is |
| Neighbour choice | Touching + overlapping; facing the source's centre beats a larger overlap | Deterministic without tracking a cursor position; matches vim in the common layouts |
| Mutable tree with parent links | Yes, mutated only through `LayoutTree` | Split/close are local rewires; an immutable rebuild per keystroke would buy nothing here |
| Snapshot shape | One record type that is either a pane or a split | Serializes to plain JSON with no polymorphism configuration |

## Deviations & follow-ups

- None against the plan.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 409  failed: 0
```

The first full build and test run was green; no fix cycle was used.
