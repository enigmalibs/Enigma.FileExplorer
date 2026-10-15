# FEATURE-2E0F-PHASE02 — Normal & visual mode, status line

**Item:** FEATURE-2E0F — Vim keyboard & command system
**Branch:** `feature/feature-2e0f-phase02-normal-visual-mode`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The explorer is now driven from the keyboard:

- **Key routing.** The main window catches keys on the tunnel route, only while the listing has focus
  (never a text box, never an open dialog). `KeyStrokeMapper` turns each key into a layout-aware
  stroke, so `:` typed as Shift+`.` on a Swiss keyboard is `:`. `KeyboardController` feeds the stroke
  to the keymap and runs the command it completes.
- **Commands.** Commands run through `ICommandDispatcher`, whose handlers come from `ICommandModule`s
  registered in the container. A command no module handles yet says so on the status line ("Not
  available yet: Copy"), and a handler that throws is reported, never fatal. `NavigationCommands`
  wires:
  - cursor motions with counts (`j`/`k`/arrows, `gg`/`G`/`5G`, `Ctrl+D`/`Ctrl+U`, PageUp/PageDown);
  - open, parent, back and forward (`l`/`h`/Enter/Backspace/arrows, `H`/`L`/Alt+arrows);
  - `gh`, `g/`, `g1`–`g9` (places), `R`/F5 and `Ctrl+L`;
  - `Space` (mark and move on), `v` (visual mode), `Ctrl+A` and `Esc`.
- **Visual mode.** It lives on the pane: the marks follow the anchor-to-cursor range. `v` leaves it
  and keeps the marks; `Esc` leaves it and clears them.
- **Status line.** A mode badge (NORMAL, or VISUAL in the accent colour), the message line, the
  pending keys/count (vim's showcmd) in monospace, and the summary.
- **Workspace.** `WorkspaceViewModel` now owns the panes and the active one (a single pane until
  FEATURE-95D6); the main window and the commands go through it.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Input/` — `KeyStrokeMapper`, `KeyboardController`
- `src/Enigma.FileExplorer.Desktop/Commands/` — `CommandInvocation`, `ICommandRegistry`, `ICommandModule`,
  `ICommandDispatcher`, `CommandDispatcher`, `NavigationCommands`
- `src/Enigma.FileExplorer.Desktop/ViewModels/WorkspaceViewModel.cs`
- Tests: `Input/KeyStrokeMapperTests`, `Input/NormalModeTests`, `Commands/CommandDispatcherTests`,
  `Views/MainWindowKeyboardTests`, `Infrastructure/ExplorerHarness`

**Modified**

- `Desktop/ViewModels/PaneViewModel.cs` — `PageSize`, visual mode (`StartVisual`, `EndVisual`, range
  marking on cursor moves, reset on directory change)
- `Desktop/ViewModels/MainWindowViewModel.cs` — workspace, keyboard, follows the active pane
- `Desktop/Views/MainWindow.axaml(.cs)` — key routing, mode badge, pending keys
- `Desktop/Views/PaneView.axaml.cs` — page size from the listing's height
- `Desktop/App.axaml` — `MonospaceFontFamily` resource; `Desktop/Themes/Styles.axaml` — status styles
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — workspace, modules, dispatcher, keyboard
- `tests/.../ViewModels/MainWindowViewModelTests.cs` — the new constructor
- `docs/roadmap.md`, `docs/plan/FEATURE-2E0F.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Handler registration | `ICommandModule`s handed to the dispatcher at construction | Each feature adds its own module without touching the others; DI resolves them as a set, with no service locator |
| Where keys are caught | Window, tunnel route, only when focus is in a pane and not in a text box or dialog | The keymap sees a key before any inner control can give it another meaning, while the path box, later the command line and prompts, and dialogs keep theirs |
| Where visual mode lives | On the pane | It is selection state of one listing; switching panes or directories must not drag it along |
| `v` vs `Esc` leaving visual | `v` keeps the marks, `Esc` clears them | Lets `vjjj` then `v` build a selection to act on later; `Esc` stays the universal "never mind" |
| Arrow Left/Right | Parent/open for now | The details view is the only view; FEATURE-2A19 gives them their icon-view meaning |
| Command failures | Logged and shown on the status line; the dispatcher never rethrows | Keys arrive through async-void event handlers, where an escaping exception would end the process |

## Deviations & follow-ups

- **Manual keyboard check not possible here:** synthetic keystrokes (`SendKeys`) were refused with
  "Access is denied" on this session's desktop. Coverage moved to a headless test that presses keys on
  the real `MainWindow` through Avalonia's input pipeline: `j` ×3 moves the cursor, a Swiss
  Shift+`.` arrives as `:`, and keys typed in the path box do not move the cursor. A quick manual pass
  with a real keyboard is still recommended.
- Visual mode keeps its anchor index across a same-directory refresh/re-sort, so the marked range may
  shift if entries appear above it. This is an edge case; a follow-up could re-anchor by name.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 353  failed: 0
```

The first full build and test run was green; no fix cycle was used.
