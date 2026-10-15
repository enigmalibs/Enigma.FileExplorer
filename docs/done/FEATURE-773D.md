# FEATURE-773D — Quick-jump match highlight

**Item:** FEATURE-773D — Quick-jump match highlight
**Branch:** `feature/feature-773d-jump-highlight`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

The quick-jump (`/`) now highlights **every** matching entry, the way vim's `incsearch` and `hlsearch`
do. Matches are found with the same smart-case "contains" rule as the jump itself.

**While typing.** Every entry whose name contains the pattern gets a warning-tinted band behind its name.
The band reads clearly under the cursor and on marked rows.

**After Enter.** The highlight stays.

**Esc in the command line.** The pane goes back to how it was: the cursor, and the highlight it had
before the jump. Clearing the typed text also shows the earlier highlight again.

**Clearing the highlight.**

- `Esc` in normal mode clears it. The usual order still applies: a running search, then visual mode,
  then **the highlight**, then the marks, then the search results.
- `:noh[lsearch]` clears it.
- Showing another folder, or search results, starts without one. A refresh of the same folder keeps it
  on the new entries.
- `n`/`N` bring the last pattern's highlight back, as in vim.

**Where it applies.** The highlight belongs to the pane where the jump was made. It shows in the
details, icon and search-results views.

## Files / modules touched

**Created**

- `tests/Enigma.FileExplorer.Desktop.UnitTests/ViewModels/QuickJumpHighlightTests.cs` — 9 tests.

**Modified**

- `Core/Commands/CommandIds.cs` — `ClearHighlight`.
- `Core/Commands/CommandCatalog.cs` — `ClearHighlight` command and the `:nohlsearch` ex command (minimum
  `:noh`); the Escape title mentions the highlight.
- `Desktop/ViewModels/EntryViewModel.cs` — `IsMatch`.
- `Desktop/ViewModels/PaneViewModel.cs` — `HighlightPattern`. It is applied on rebuilds and to new search
  hits, and cleared on a directory change and when entering search results.
- `Desktop/ViewModels/CommandLineViewModel.cs` — the highlight follows typing; Cancel restores it; `n`/`N`
  set it.
- `Desktop/Commands/CommandLineCommands.cs` — the `:noh` handler.
- `Desktop/Commands/NavigationCommands.cs` — Esc clears the highlight.
- `Desktop/Views/PaneView.axaml` — `match` class on rows, tiles and result rows.
- `Desktop/Themes/Styles.axaml` — the match band: `EnigmaWarningBackgroundBrush` behind the name.
- `tests/…/Views/PaneViewTests.cs` — headless test: matched names carry the band and others do not.
- `docs/roadmap.md`, `docs/plan/FEATURE-773D.md` — status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Help category of `:noh` | Search, beside the quick-jump keys | It belongs with `/`, `n` and `N`. The plan said General |
| Where Esc clears the highlight in its cascade | After visual mode, before the marks | Leaving visual mode is what Esc means there. The marks are more costly to lose than a highlight |
| Clearing the typed pattern during a jump | Shows the highlight the pane had before | Matches Cancel, and vim keeps the previous `hlsearch` visible while nothing is typed |

## Deviations & follow-ups

- **Category.** `:noh` is in the Search help category, not General (see the decisions).
- **Visual check.** A headless frame rendered with the Skia backend in a temporary test, which was
  deleted and not committed. The band is visible and readable on the cursor row and on a marked row
  (dark theme). The light theme was not looked at.
- **Follow-up.** Highlighting only the matched substring, as vim does, would need inline runs in the name.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 720  failed: 0  succeeded: 717  skipped: 3
```

- 10 tests were added: 9 view-model tests and 1 headless view test.
- The first run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones.
