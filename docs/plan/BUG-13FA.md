# BUG-13FA — Split panes overlap after re-split

**Status:** TODO
**Type:** BUG
**Branch:** `bugfix/bug-13fa-split-overlap`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

Splitting the same way several times must give every pane its own space. Today, with one pane, splitting
vertically twice leaves one area empty, and two panes are drawn on top of each other.

## Context & constraints

- `SplitHost` rebuilds its grids on every layout change and **reuses** the `PaneView` of each pane, so a
  pane keeps its scroll position.
- `BuildSplit` sets `Grid.Column`/`Grid.Row` only on the splitter and on the *second* child. The first
  child keeps whatever attached value it had before.
- Root cause: after `split(A) → [A | B]`, B carries `Grid.Column = 2`. Splitting B again turns it into the
  *first* child of a new inner grid (`[B | C]`). B keeps `Grid.Column = 2`, so it lands in C's column, and
  column 0 of the inner grid stays empty. That is exactly the reported "one shows nothing, the others go
  over each other". The same stale value can come from `Grid.Row` when a pane moves between a row split
  and a column split.

## Steps

1. In `SplitHost.BuildSplit`, set both attached properties on **both** children and the splitter every
   time: the axis being built gets its index (0, 1, 2), and the other axis is reset to 0.
2. Add a headless test: split one pane vertically three times (and a mixed vertical/horizontal
   sequence). Every `PaneView` must have a non-empty area, and no two panes may overlap.

## Acceptance criteria

- One pane split vertically N times shows N+1 panes side by side, none empty or overlapping. The same
  holds for horizontal and mixed sequences, and after closing panes.
- A regression test covers the scenario.
- Build has zero warnings and all tests are green.

## Out of scope

- vim's `equalalways` (making every pane equal after a split). Today a split halves the active pane.
  This is noted as a follow-up.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Fix location | Reset `Grid.Row`/`Grid.Column` on every child at each rebuild | Fixes the root cause and keeps the views reused (scroll positions survive) | Recreating the `PaneView`s on each rebuild (loses scroll and focus state) |
| Equalize panes after a split | Not in this fix | The report is about the overlap. Changing split sizing is a behaviour change nobody asked for | Equalizing on every split |
