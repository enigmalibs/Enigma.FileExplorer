# FEATURE-C732-PHASE03 — Fire-and-forget tasks reach the net

**Item:** FEATURE-C732 — Catch unhandled exceptions
**Branch:** `feature/feature-c732-phase03-fire-and-forget`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

Work the explorer starts and does not await no longer loses its failures.
`Task.Forget()` (`Services/TaskExtensions.cs`, a C# 14 extension block like Enigma.GitClient's and
Enigma.TextEditor's) observes the task. A fault goes to `UnhandledExceptionHooks.Report` as
`FireAndForget`, once per inner exception and from whatever thread the task ends on. A completed task
costs nothing, and a cancelled one says nothing. `Forget(UnhandledErrorSource)` names another source.

Every `_ = <task>` discard in `src/` (19) is now `.Forget()`:

- `App`: the posted `StartAsync` reports as `Startup`;
- the About, Help and Bookmarks dialogs' close callbacks;
- the command line's submitted command;
- a place's navigation;
- the pane's auto-refresh;
- `PaneView`: open, the three context-menu openers, the menu entry, the drop;
- `PlacesView`: the drop;
- the reporter's own `PresentAsync` and `HideAsync`.

## Files/modules touched

- Created:
  - `src/Enigma.FileExplorer.Desktop/Services/TaskExtensions.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Services/TaskExtensionsTests.cs`
- Modified:
  - `src/Enigma.FileExplorer.Desktop/App.axaml.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/AboutDialogService.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/BookmarksDialogService.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/HelpDialogService.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.FileExplorer.Desktop/ViewModels/MainWindowViewModel.cs`
  - `src/Enigma.FileExplorer.Desktop/ViewModels/PaneViewModel.cs`
  - `src/Enigma.FileExplorer.Desktop/Views/PaneView.axaml.cs`
  - `src/Enigma.FileExplorer.Desktop/Views/PlacesView.axaml.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-C732.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where `TaskExtensions` lives | `Services/`, namespace `…Desktop.Services` | GitClient's place; the net it feeds lives there; FE has no `Extensions/` folder |
| The reporter's own `PresentAsync` | `.Forget()` too | It never throws; should it ever, the net logs it rather than losing it |
| `Startup` source | `Forget(UnhandledErrorSource.Startup)` on the posted `StartAsync` | The overload is the same as TextEditor's |
| Test location | `tests/…/Services/TaskExtensionsTests.cs`, in the headless collection | It attaches to the process-wide hooks, as the hooks tests do |

## Deviations & follow-ups

- No deviation. The only `_ =` left in `src/` is `TaskExtensions`' own continuation, which cannot fault.
- `await Task.Run(...)` in `FileCommands` is awaited, so it needs no `Forget()`. `async void` handlers,
  if any, reach the dispatcher hook.

## Documentation sweep

Nothing became wrong.

## Build/test evidence

```
grep -rn "_ = .*Async(" src                      → nothing
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 817, failed 0, succeeded 814, skipped 3
Enigma.FileExplorer.Desktop.exe started for 6 s  → window up, no crash
```

- 5 tests added:
  - a faulted task, reported as `FireAndForget`;
  - a task that faults later;
  - every failure of a `WhenAll`;
  - `Startup`;
  - success and cancellation say nothing.
- No fix cycle was used.
