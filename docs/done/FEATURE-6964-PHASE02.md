# FEATURE-6964-PHASE02 — Search results UI

**Item:** FEATURE-6964 — File search
**Branch:** `feature/feature-6964-phase02-search-ui`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

Searching from the active pane, with the two separate commands the draft asked for:

- **Names:** `f` or Ctrl+F prompts "Find name: ", or `:find pattern`.
- **Contents:** `F` or Ctrl+Shift+F prompts "Find in files: ", or `:grep text`.
- Hidden entries follow the global toggle.

The pane switches to a results state:

- **Header line.** "Searching names matching "x" in home…" with a spinner while searching, then
  "12 names matching…", "First 10000…" when capped, or "(stopped)" when cancelled.
- **List.** A virtualized list with Name, Folder (relative to where the search started), and for
  contents Line and the matching text in monospace. It always uses this list layout, even from an
  icon view.
- **Streaming.** Hits flow in a few times a second. The cursor and marks work as usual, so `yy`,
  `dd`, `dD` and the like act on hits. A file listed for several lines counts once as a target.

Moving around results:

- `l`/Enter opens a hit: a folder is entered, a file is opened.
- `gl` reveals a hit in its folder with the cursor on it.
- `h`, `H`, Left and Esc return to the directory. Esc stops a running search first and keeps what was
  found, then clears visual mode or marks, and only then leaves.
- Results are a location in the history: `L`, or `H` after entering a folder from them, brings them
  back without searching again.
- `R` searches again.
- Navigating away stops a running search.
- Results ignore the disk watcher and keep their found order (nearest first) whatever the sort.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Commands/SearchCommands.cs`
- Tests: `Commands/SearchCommandsTests`, `Infrastructure/FakeFileSearch`

**Modified**

- `Desktop/ViewModels/PaneViewModel.cs` — results state (`SearchAsync`, `CancelSearch`, `RevealAsync`,
  `IsSearchResults`, `IsSearching`, `SearchTitle`, `IsListLayout`, `ShowsDetailsList`, `ShowsIconView`),
  results in the history, re-search on refresh, de-duplicated targets
- `Desktop/ViewModels/EntryViewModel.cs` — `Location`, `LineNumber`, `LineText`
- `Desktop/ViewModels/PaneFactory.cs` — `IFileSearch`
- `Desktop/Commands/NavigationCommands.cs` — Esc order, list-layout arrows
- `Desktop/Views/PaneView.axaml(.cs)` — results header, columns and list; geometry and scrolling per layout
- `Desktop/Themes/Styles.axaml`, `Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
- Tests: `Infrastructure/ExplorerHarness`, `Infrastructure/PaneHarness`
- `docs/roadmap.md`, `docs/plan/FEATURE-6964.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Results in history | A history entry carries the hits; going back to it restores them without searching again | `H`/`L` between results and the folders opened from them is the whole point of "results as a location" |
| Order of results | As found, nearest first; sorting is ignored while showing results | Content hits of one file stay together; re-sorting a streaming list would make the cursor jump |
| Esc precedence | Running search → visual mode → marks → leave results | Each press undoes the most transient thing first, as in vim |
| Results layout | Always a list | Folder, line and text need columns; tiles cannot show them |
| `gl` outside results | A one-line hint instead of a no-op | Teaches the key without surprising anyone |

## Deviations & follow-ups

- Opening a file at the matched line in an editor is out of scope (the default application opens the
  file).
- Content results show one row per matching line; collapsing them per file is a possible follow-up.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 587  failed: 0  skipped: 3
```

The first full build and test run was green; no fix cycle was used. A throwaway headless render (not
committed) showed the results header, the Folder/Line/Text columns and the monospace snippets.
