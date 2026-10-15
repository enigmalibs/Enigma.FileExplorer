# BUG-13FA — Split panes overlap after re-split

**Item:** BUG-13FA — Split panes overlap after re-split
**Branch:** `bugfix/bug-13fa-split-overlap`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

Splitting a pane that was itself created by a split now lays the panes out correctly. Before, one cell
stayed empty and two panes were drawn on top of each other.

**Root cause.** On every layout change, `SplitHost` rebuilds its grids but **reuses** the pane views. It set
`Grid.Column`/`Grid.Row` only on the splitter and on the second child of each split. A view that had
been the second child kept `Column = 2` (or `Row = 2`) when the next split made it the *first* child of
a new inner grid. It therefore landed in its new neighbour's cell, and cell 0 stayed empty.

**Fix.** `BuildSplit` now places both children and the splitter on both axes every time: index 0, 1 or 2
on the axis being built, and 0 on the other.

## Files / modules touched

**Modified**

- `src/Enigma.FileExplorer.Desktop/Views/SplitHost.cs`: `Place` helper; every cell is set on both axes.
- `tests/Enigma.FileExplorer.Desktop.UnitTests/Views/SplitHostTests.cs`: new theory
  `SplittingTheNewPaneAgain_GivesEveryPaneItsOwnArea`. It covers columns ×3, rows ×3 and a mixed
  sequence, and then closes a pane. It asserts that every pane has an area and that no two panes
  overlap.
- `docs/roadmap.md`, `docs/plan/BUG-13FA.md`: status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Proving the test catches the bug | The fix line was reverted for one run | Two of the three cases failed with the overlap reported ("Panes 1 and 2 overlap"), then passed with the fix restored |

## Deviations & follow-ups

- No deviation from the plan.
- **Follow-up (equal panes).** vim's `equalalways` gives every pane an equal share after a split. Here a
  split halves the active pane, so a pane split three times yields 50/25/12.5/12.5. `Space =` (or
  `Ctrl+W =` until FEATURE-6526) equalizes. Making splits equalize automatically would be a separate
  decision.
- **Process note.** While verifying, a bare `git checkout --` (no paths) was typed. It is a no-op that only
  lists modified files, and nothing was changed by it.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 674  failed: 0  succeeded: 671  skipped: 3
```

- 3 tests were added (one theory, three cases).
- The first run after the fix was green, so no fix cycle was used.
- The three skips are the known platform-specific ones (symbolic-link privilege and others).
