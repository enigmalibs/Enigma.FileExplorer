# FEATURE-773D — Quick-jump match highlight

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-773d-jump-highlight`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

While the user types a quick-jump pattern after `/`, every entry whose name matches is highlighted, not
only the one under the cursor. The highlight stays after Enter, as with vim's `incsearch` and `hlsearch`.

## Context & constraints

- `CommandLineViewModel` handles the jump: `OpenJump`, incremental `Jump(Text)`, `Accept` (keeps
  `LastJumpPattern`), `Cancel` (puts the cursor back), and `RepeatJump` (`n`/`N`).
- `PaneViewModel.FindMatch` matches with `SmartCase.Contains`, and the highlight must use the same rule.
- Rows (details and search results) and tiles (icon views) are styled through classes bound to
  `EntryViewModel` flags such as `cursor`, `marked` and `cut`.

## Steps

1. `EntryViewModel.IsMatch` (observable). `PaneViewModel.HighlightPattern` (string?): setting it
   recomputes `IsMatch` for every item, and a reload or refresh re-applies it to the new items. Navigating
   to **another** directory clears it.
2. `CommandLineViewModel`:
   - typing in jump mode sets the active pane's `HighlightPattern` live;
   - Enter keeps it;
   - Esc restores the pattern the pane had before the jump started;
   - `n`/`N` set it to `LastJumpPattern` again.
3. Clearing:
   - `Esc` in normal mode (`CommandIds.Escape`) also clears the active pane's highlight;
   - new `:noh[lsearch]` ex command (`CommandIds.ClearHighlight`, General category).
4. Styles: a `match` class on rows and tiles. The name gets a warning-tinted background
   (`EnigmaWarningBackgroundBrush` behind the name text, `EnigmaWarningBorderBrush` around tiles), which
   stays readable under the cursor and the mark styles.
5. Tests:
   - highlight follows typing;
   - it stays after Enter and is restored on Esc;
   - Esc in normal mode and `:noh` clear it;
   - `n` brings it back;
   - navigating away clears it, and a refresh keeps it;
   - smart case.

## Acceptance criteria

- `/re` highlights every name containing "re" (smart case) while typing; after Enter they stay
  highlighted.
- `Esc` (normal mode) or `:noh` removes the highlight; `n`/`N` bring it back; entering another folder
  removes it.
- Highlight works in the details, icon and search-results views.
- Build has zero warnings and all tests are green.

## Out of scope

- Highlighting only the matched substring inside the name, and regex patterns.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What is highlighted | The whole name of every matching entry | Simple and readable in all three views; the user asked to see every result | Highlighting the substring (needs custom inline rendering) |
| When it clears | Esc in normal mode, `:noh`, or leaving the folder; `n`/`N` restore it | vim's `hlsearch` model, scoped to "find in this folder" | Never clearing; clearing on every cursor move |
| Scope | Per pane | The jump acts on the active pane only | Global across panes |
