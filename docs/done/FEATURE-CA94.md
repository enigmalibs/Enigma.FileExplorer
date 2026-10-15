# FEATURE-CA94 — Session persistence & settings

**Item:** FEATURE-CA94 — Session persistence & settings
**Branch:** `feature/feature-ca94-session-settings`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The explorer reopens as it was left. It saves the split layout and ratios, each pane's directory, view
mode and sort, the active pane, the hidden-files switch, the theme, and the window's normal bounds and
maximized state.

**Where and how it is stored.**

- One `settings.json` per user: `%APPDATA%\Enigma.FileExplorer` on Windows, `$XDG_CONFIG_HOME` or
  `~/.config/Enigma.FileExplorer` on Linux, `~/Library/Application Support/Enigma.FileExplorer` on
  macOS.
- Each write goes to a temporary file that is flushed to disk and then renamed over the old one.

**Reading is tolerant.**

- A missing file gives the defaults.
- Unknown members are skipped and missing ones keep their defaults.
- Comments and trailing commas are accepted.
- A file that cannot be parsed (bad JSON, an unknown enum name, a null where none is allowed) is kept as
  `settings.json.bad`, logged, and replaced by the defaults.
- A saved layout that cannot be rebuilt is dropped, logged, and replaced by a single pane.

**At start.**

- The settings load before the window is shown. The theme and the hidden-files switch are applied.
- The window goes back where it was, but only if its title bar would land on a current screen.
  Otherwise it opens centred at its default size.
- The workspace is rebuilt from the saved layout. Each pane opens its directory, or the nearest
  existing ancestor if the directory is gone.
- A command-line argument naming an existing directory (relative to the working directory, so `.`
  works) opens in the active pane. The other panes keep their saved directories.

**Saving.**

- Synchronously at exit, while the services are still alive.
- Also as soon as the theme or the hidden-files switch changes, so a crash cannot lose them.

**`:theme dark|light|system`** switches immediately and accepts unambiguous prefixes. "System" is
resolved to Dark or Light from the platform and follows it as it changes. It never leaves the
application at `ThemeVariant.Default`, where the Enigma theme defines no brushes.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Settings/` — `AppSettings`, `WorkspaceSettings`, `PaneSettings`,
  `WindowPlacement`, `ThemePreference`, `AppPaths`, `AtomicFile`, `ISettingsStore`, `JsonSettingsStore`,
  `SettingsJsonContext`
- `src/Enigma.FileExplorer.Desktop/Services/Session.cs`, `IThemeService.cs`, `ThemeService.cs`
- `src/Enigma.FileExplorer.Desktop/Commands/ThemeCommands.cs`
- Tests: Core `Settings/JsonSettingsStoreTests`, `Settings/AtomicFileTests`. Desktop
  `Services/SessionTests`, `Commands/ThemeCommandsTests`, `Views/WindowPlacementTests`,
  `Infrastructure/FakeSettingsStore`, `Infrastructure/FakeThemeService`

**Modified**

- `Core/Layout/LayoutTree.cs` — `Restore` (replace the tree in place, keep new ids unique)
- `Core/DependencyInjection/ServiceCollectionExtensions.cs` — `ISettingsStore`
- `Desktop/ViewModels/WorkspaceViewModel.cs` — `RestoreAsync`, `Capture`
- `Desktop/ViewModels/MainWindowViewModel.cs` — `StartAsync(path, saved)`
- `Desktop/Views/MainWindow.axaml.cs` — `RestorePlacement`, `CapturePlacement`, `IsOnScreen`, normal-bounds
  tracking
- `Desktop/Views/SplitHost.cs` — rebinds a kept view when a restored pane reuses its id
- `Desktop/App.axaml.cs` — load before showing, CLI directory, save at exit
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — theme, session, `:theme` module
- Tests: `Infrastructure/ExplorerHarness` (theme module), `Layout/LayoutTreeTests`, `Views/SplitHostTests`
- `docs/roadmap.md`, `docs/plan/FEATURE-CA94.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Settings DTOs | Records with settable properties | The JSON source generator assigns every init-only property, so a member missing from the file became null or zero instead of keeping its default (found by the tolerance test) |
| Enum spelling in the file | Member names (`"Dark"`, `"LargeIcons"`); reading is case-insensitive | The source-generated converter writes names as declared; a hand-written `"light"` still loads |
| Nulls in the file | `RespectNullableAnnotations`: an explicit null where none is allowed makes the file unreadable (kept as `.bad`) | One clear rule instead of null checks across the restore code |
| Where the restore lives | `WorkspaceViewModel.RestoreAsync`; `Session` validates the layout first and logs | The workspace owns panes and layout. The session owns the file, the theme and the logging |
| Missing pane directory | The existing pane fallback: nearest existing ancestor, else the root of the home volume | Reuses the pane-browsing behaviour; the plan said "else home", and the difference only shows when even the volume is gone |
| Window visibility check | A 120×40 px area at the top-left (the title bar) must intersect a screen's working area | The user can always grab and move the window; a fully on-screen requirement would reject windows overlapping two monitors |
| Saving on change | Theme and hidden files save at once; layout and panes at exit | As planned. Layout changes are frequent, and the exit save covers them |

## Deviations & follow-ups

- **Missing directory fallback.** As noted in the decisions, the final fallback is the root of the home
  volume rather than home itself (existing pane behaviour).
- **Bug found while smoke-testing the real app.** A restored workspace reuses pane id 0, so `SplitHost`
  kept the old view, which was still bound to the disposed pane, and the first pane stayed empty.
  Fixed (the view is rebound) and covered by `SplitHostTests.ARestoredWorkspace_*`.
- **Manual checks.** I ran the real app on Windows:
  - Started with `C:\Windows` as an argument, closed it with WM_CLOSE: the window opened there, the
    process exited 0, and `settings.json` held the window bounds and the pane.
  - Started it against a hand-written two-pane light-theme file: the theme, both panes, the large-icon
    view and the active pane were restored.
  - Afterwards I deleted the `settings.json` I had created under `%APPDATA%\Enigma.FileExplorer`, so
    nothing is left on the machine.
  - Not checked by hand: Linux and macOS paths and window restore, and "System" following a live OS
    theme switch.
- **Follow-up (still open, also noted in A6C2-PHASE02).** In a narrow pane the details view's Name column
  collapses behind the fixed columns. It showed again in the restore screenshot.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 671  failed: 0  succeeded: 668  skipped: 3
```

The first full build and test run was green, so no fix cycle was used. Before it, four test runs during
implementation caught:

- the init-only/source-generator default problem;
- the enum spelling the test expected;
- a test window without a DataContext;
- the stale view on a reused pane id, which the real-app smoke run found.

The three skips are the known platform-specific ones.
