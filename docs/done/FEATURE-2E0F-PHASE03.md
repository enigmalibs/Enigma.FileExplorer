# FEATURE-2E0F-PHASE03 — Command line, quick-jump & help

**Item:** FEATURE-2E0F — Vim keyboard & command system
**Branch:** `feature/feature-2e0f-phase03-command-line`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The vim command line arrives, shown below the status line and hidden until it is used. One
`CommandLineViewModel` serves three modes:

- **`:` ex commands.** The line is parsed by the catalog's parser and run through the dispatcher.
  Parse errors go to the message line in vim's terse style. Up/Down browse a de-duplicated history
  filtered by what was typed, as vim does. Tab/Shift+Tab cycle completions: command names, fixed
  argument values (`:view`, `:sort`, `:set`, `:theme`), and directory paths for `:cd`, `:split` and
  `:vsplit`, relative to the active pane, with `~` and dot-directories only when typed.
- **`/` quick-jump.** An incremental, smart-case match moves the cursor as you type, from where the
  jump started, and the text turns red on a miss. Enter remembers the pattern for `n`/`N`, which
  cycle with wrap-around. Esc puts the cursor back.
- **Prompts.** `IPromptService.PromptAsync(label, initialText, selection)` asks for a line ("Rename: …"
  with the stem pre-selected) and answers `null` when cancelled. File operations will use it.

`:cd` handles `~`, relative paths, `..` and drive roots; a bare `:cd` goes home. `?`, `F1` and `:help`
open a help dialog generated from the catalog: one section per category, every key binding, and the
ex command in vim form (`:vs[plit]`, `:del[ete]!`), with `g1 … g9` collapsed into one line. Closing
the command line or a dialog gives the keyboard back to the listing.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Commands/` — `CommandHistory`, `CommandLineCompleter`
- `src/Enigma.FileExplorer.Core/Search/SmartCase.cs`
- `src/Enigma.FileExplorer.Desktop/ViewModels/` — `CommandLineViewModel`, `CommandLineMode`,
  `TextSelection`, `HelpViewModel` (+ `HelpSection`, `HelpRow`)
- `src/Enigma.FileExplorer.Desktop/Services/` — `IPromptService`, `IHelpDialogService`, `HelpDialogService`
- `src/Enigma.FileExplorer.Desktop/Commands/CommandLineCommands.cs`
- `src/Enigma.FileExplorer.Desktop/Views/` — `CommandLineView.axaml(.cs)`, `HelpView.axaml(.cs)`
- Tests: `Core.UnitTests/Commands/CommandLineToolsTests`; `Desktop.UnitTests/ViewModels/CommandLineViewModelTests`,
  `Views/CommandLineViewTests`, `Infrastructure/FakeHelpDialog`

**Modified**

- `Desktop/ViewModels/PaneViewModel.cs` — `FindMatch` (smart-case, wrapping, either direction)
- `Desktop/ViewModels/MainWindowViewModel.cs` — the command line, and running submitted lines
- `Desktop/Views/MainWindow.axaml(.cs)` — the command line row; refocus after it or a dialog closes
- `Desktop/Themes/Styles.axaml` — command line and help styles
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — command line, prompt, help, module
- Tests: `Infrastructure/ExplorerHarness` (command line, help, window ViewModel, `Ex`),
  `ViewModels/MainWindowViewModelTests`, `Views/MainWindowKeyboardTests`
- `docs/roadmap.md`, `docs/plan/FEATURE-2E0F.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who runs a submitted line | The command line raises `CommandSubmitted`; `MainWindowViewModel` hands it to the dispatcher | The dispatcher owns the modules and one module owns the command line, so a direct dependency would be a cycle |
| One bar or three | One `CommandLineViewModel` for `:`, `/` and prompts | vim's model; one focus target and one set of key rules |
| History browsing | Filtered by the text typed before browsing began | vim's behaviour; `:cd` + Up recalls only earlier `:cd` lines |
| Completion candidates | Whole replacement lines, cycled; any edit restarts completion | Simple, predictable, and testable without a UI |
| Path completion | Directories only; dot-directories only when the typed part starts with a dot; at most 200 | `:cd`/`:split` take directories; shells hide dot-entries the same way |
| Jump start point | Incremental from the cursor where `/` was pressed, wrapping | vim's `incsearch`; Esc returns exactly there |
| Help size | Fixed 720 px content with its own scroll | The dialog's size properties persist across dialogs, so they are left untouched |
| Losing focus | Abandons the line (a prompt answers `null`) | Matches the path bar; a half-typed command must never run by accident |

## Deviations & follow-ups

- **Manual keyboard check:** synthetic key injection is still refused on this desktop. The headless
  `CommandLineViewTests` types `:` (Swiss Shift+`.`), the text `cd ..` and Enter on the real window,
  and checks the navigation and the focus hand-back. A throwaway headless render (not committed)
  confirmed the command line and the help dialog lay out as intended.
- Quick-jump has no history of its own (vim's `/` history); a cheap follow-up if wanted.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 389  failed: 0
```

Fix cycles used: **1 of 3.** The first full run failed one PHASE02 test that expected `:` to report
"Not available yet". `:` now opens the command line, so the test asserts that instead; it still
exercises the Swiss Shift+`.` mapping. Run 2 was green.
