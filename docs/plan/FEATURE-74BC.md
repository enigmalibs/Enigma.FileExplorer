# FEATURE-74BC — Start with a single fresh pane

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-74bc-single-pane-start`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

The explorer always starts with **one pane**, in the home directory (or the directory given on the
command line). It no longer reopens the split layout or the directories that were open when it was
closed.

## Context & constraints

- FEATURE-CA94 saves `AppSettings.Workspace` (layout, each pane's path, view and sort, active pane) at
  exit and rebuilds it at start (`WorkspaceViewModel.RestoreAsync`).
- These settings stay as they are: the theme, the hidden-files switch and the window placement.
- The view mode and sort order are a preference, not a location. Dropping them would be a regression the
  user did not ask for, so the start pane keeps the active pane's view mode and sort from the last exit.
- Settings files written by earlier builds must still load. The old `workspace` member is skipped as an
  unknown member and disappears at the next save.

## Steps

1. Core settings: replace `AppSettings.Workspace` (`WorkspaceSettings`) with `AppSettings.Pane`, a
   `PaneSettings` that holds only `ViewMode`, `SortKey` and `SortDirection`. Delete `WorkspaceSettings`
   and the `Id`/`Path` members of `PaneSettings`. The schema version stays 1.
2. `WorkspaceViewModel`: replace `RestoreAsync`/`Capture` with:
   - `StartAsync(PaneSettings? view, string? path)` — one pane, view and sort applied, opened at `path`
     or at home;
   - `CaptureView()` — the active pane's view mode and sort.
3. `Session`: save `Pane = workspace.CaptureView()`. Drop the layout validation.
4. `MainWindowViewModel.StartAsync(path, view)` and `App` pass the saved view instead of the workspace.
5. Tests:
   - the session saves the view and not the layout;
   - a restart gives one pane at home, with the saved view and sort;
   - a command-line directory opens in that single pane;
   - an old settings file that still has `workspace` loads (Core store test).
   Remove the layout-restore tests, whose behaviour is gone.

## Acceptance criteria

- Close the explorer with several panes in various directories, then reopen it: it shows one pane at
  home, with the last active pane's view mode and sort.
- `Enigma.FileExplorer.Desktop <dir>` opens `<dir>` in that single pane.
- An existing `settings.json` that contains `workspace` still loads without becoming `.bad`.
- Build has zero warnings and all tests are green.

## Out of scope

- An option to restore the session again, and a settings page.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What the single pane opens | Home, or the command-line directory | "Never on the last directory." Home is the existing default start | The last directory of the active pane |
| View mode and sort | Keep the last active pane's | Preference, not location; dropping it would be an unrequested regression | Always details and name order; persisting per-pane data |
| Old code paths | Remove `WorkspaceSettings`, `RestoreAsync`, `Capture` and their tests | Dead code once the behaviour is gone; git keeps the history | Keep them behind an option nobody asked for |
| Schema version | Stays 1 | Reading is tolerant: an unknown member is skipped and a missing one gets its default | Bumping the version with a migration step |
