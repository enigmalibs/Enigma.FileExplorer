# FEATURE-74BC — Start with a single fresh pane

**Item:** FEATURE-74BC — Start with a single fresh pane
**Branch:** `feature/feature-74bc-single-pane-start`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

The explorer now always starts with **one pane**, in the home directory or in the directory given on the
command line. It no longer reopens the split layout or the directories that were open at exit.

What it still remembers: the theme, the hidden-files switch, the window placement, and — as a preference
— the **view mode and sort** of the pane that was active at exit. These are saved as `pane` in
`settings.json`.

Older `settings.json` files still load. Their `workspace` member is skipped as unknown, and it disappears
the next time the settings are saved.

## Files / modules touched

**Deleted**

- `src/Enigma.FileExplorer.Core/Settings/WorkspaceSettings.cs`

**Modified**

- `Core/Settings/AppSettings.cs` — `Workspace` replaced by `Pane` (`PaneSettings?`).
- `Core/Settings/PaneSettings.cs` — now only `ViewMode`, `SortKey` and `SortDirection` (no `Id`/`Path`).
- `Desktop/ViewModels/WorkspaceViewModel.cs` — `RestoreAsync`/`Capture`/`Describe` replaced by
  `StartAsync(view, path)` and `CaptureView()`.
- `Desktop/ViewModels/MainWindowViewModel.cs` — `StartAsync(path, view)`.
- `Desktop/Services/Session.cs` — saves `Pane`. The layout validation and the logger it needed are gone.
- `Desktop/App.axaml.cs` — passes `settings.Pane` to the start.
- `Desktop/Views/SplitHost.cs` — comment on the view rebinding, which no longer comes from a restore.
- Tests:
  - `Desktop/Services/SessionTests` — the restore tests are replaced by: save captures the view only;
    restart → one pane at home with the last view; a command-line directory; a first start.
  - `Core/Settings/JsonSettingsStoreTests` — round trip with `Pane`, an old file with `workspace` still
    loads, `pane.viewMode: null` is unreadable.
  - `Desktop/Views/SplitHostTests` — the restored-workspace test was removed: no code path restores a
    layout any more.
- `docs/roadmap.md`, `docs/plan/FEATURE-74BC.md` — status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `LayoutTree` snapshot API (`ToSnapshot`/`FromSnapshot`/`Restore`) | Kept | It is part of the Core layout model and is unit-tested on its own. Only the app's use of it is gone |
| `SplitHost` rebinding a kept view to a new pane | Kept, comment updated | A cheap guard that keeps the view in step with the workspace; no longer reachable from a restore |
| Manual run of the real app | Not done | It would rewrite the user's real `%APPDATA%\Enigma.FileExplorer\settings.json`. Behaviour is covered by the session and store tests |

## Deviations & follow-ups

- No deviation from the plan.
- **Follow-up.** If a "restore last session" option is ever wanted, the Core snapshot API is still there.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 672  failed: 0  succeeded: 669  skipped: 3
```

- Seven tests went with the removed restore behaviour: six session tests and one split-host test.
- Five tests were added: four session tests and one store test.
- The first full run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones.
