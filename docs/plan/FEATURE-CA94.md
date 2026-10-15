# FEATURE-CA94 — Session persistence & settings

**Status:** DONE — see `docs/done/FEATURE-CA94.md`
**Type:** FEATURE
**Branch:** `feature/feature-ca94-session-settings`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Make the explorer pick up where the user left it: persist the split layout, each pane's directory,
view mode and sort, the active pane, the hidden-files toggle, the theme and the window bounds, and
restore them at start. Add `:theme dark|light|system` and honour a path given on the command line.

## Context & constraints

- Builds on the layout model and snapshot (FEATURE-95D6), per-pane view/sort and the global hidden
  toggle (FEATURE-2A19), and the command catalog (`:theme` declared in FEATURE-2E0F).
- Storage: one `settings.json` under the per-user application data directory
  (`%APPDATA%\Enigma.FileExplorer` on Windows, `$XDG_CONFIG_HOME` or `~/.config/Enigma.FileExplorer` on
  Linux, `~/Library/Application Support/Enigma.FileExplorer` on macOS), written atomically (temp file +
  replace) like the sibling apps.
- Reading is tolerant: a missing file → defaults; unknown or missing members → defaults for those
  members; a corrupt file → defaults, logged, and the file is preserved as `settings.json.bad`
  before being overwritten at exit.
- A restored pane whose directory no longer exists opens the nearest existing ancestor (else home).
- Window bounds are restored only when they intersect a current screen; otherwise the default size,
  centred.

## Steps

1. Core `Settings/`: `AppSettings` (schema version, theme, show hidden, window bounds + state,
   `LayoutSnapshot` with per-pane path/view/sort, active pane), `ThemePreference`, `AppPaths`,
   `AtomicFile`, `ISettingsStore`/`JsonSettingsStore` (System.Text.Json, source-generated context,
   tolerant read).
2. Startup: load before the window is shown; apply theme; rebuild the workspace from the snapshot
   (fallbacks above); a command-line path argument (first argument, existing directory) overrides the
   active pane's directory.
3. Shutdown: capture the snapshot and window bounds and save (synchronously at exit — no message pump
   remains). Also save when `:theme` or `zh` change, so a crash does not lose them.
4. `:theme dark|light|system` applies immediately (`ThemeVariant`), persisted.
5. Tests: store round-trip, tolerant read (missing, unknown member, corrupt → `.bad`), atomic write,
   snapshot → workspace restore with missing directories, CLI argument override, theme command.

## Acceptance criteria

- Close and reopen: same layout, directories, view modes, sort, hidden toggle, theme and window
  position/size.
- Corrupt or missing settings never prevent startup.
- `Enigma.FileExplorer.Desktop <path>` opens that directory in the active pane.
- Build zero warnings, all tests green.

## Out of scope

- A settings page/dialog, a user-editable keymap file, per-directory view memory, multiple windows,
  single-instance handling.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Persist the session | Yes — layout, panes, view/sort, hidden, theme, window | A daily-driver tool should reopen as it was; cheap given the layout snapshot | Always start fresh |
| Format & place | `settings.json` in the per-user app-data/config directory, atomic write | House pattern (Launchpad/GitClient); survives crashes mid-write | Registry; next to the executable |
| Corrupt file | Defaults + keep `settings.json.bad` | Never block startup; never silently destroy the user's file | Crash; overwrite silently |
| Theme | Dark default, `:theme dark|light|system` | House default (Enigma theme defines Dark/Light only); command fits the vim UI | A settings page |
| CLI argument | First argument = directory for the active pane | Lets the user bind "open here" in other tools | Ignored |
