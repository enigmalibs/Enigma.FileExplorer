# FEATURE-95D6-PHASE02 — Split UI & window commands

**Item:** FEATURE-95D6 — Split groups
**Branch:** `feature/feature-95d6-phase02-split-ui`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The explorer now splits like vim:

- **Workspace.** `WorkspaceViewModel` owns the `LayoutTree`, one `PaneViewModel` per leaf, and the
  active pane.
  - A split opens the same directory with the cursor on the same entry, the same hidden-files and
    sort settings, or a given path, and it becomes active.
  - Closing activates the pane that took the space and disposes the closed pane, stopping its
    watcher.
  - `Only`, spatial focus, next/previous, equalize and resize act on the layout.
- **Rendering.** `SplitHost`, a code-built control, renders the tree as nested grids with a
  `GridSplitter` between each pair. A dragged splitter writes its ratio back to the layout. Pane views
  are reused across rebuilds, so a pane keeps its scroll position when another one splits. Any click
  or focus inside a pane makes it active, and the active pane takes the keyboard.
- **Active-pane cues.** With more than one pane, the active one is framed in the accent colour, and
  the other panes dim their cursor row. Each path bar has split side-by-side / split top-and-bottom
  buttons, plus a close button on the framed pane, with tooltips naming the keys.
- **Commands.** `WindowCommands` wires:
  - `<C-w>v/s`, `h/j/k/l`/arrows, `w/W`, `c/q`, `o`, `=`, `+/-/>/<` (with counts);
  - `:split`/`:vsplit [path]` (resolved like `:cd`), `:close`, `:only`;
  - `:q`, which closes the pane and quits the app on the last one, and `:qa`.

  `<C-w>c` on the last pane says "Cannot close the last pane".

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Views/SplitHost.cs`
- `src/Enigma.FileExplorer.Desktop/Commands/WindowCommands.cs`
- `src/Enigma.FileExplorer.Desktop/Services/IAppShutdown.cs`, `AppShutdown.cs`
- Tests: `ViewModels/WorkspaceViewModelTests`, `Commands/WindowCommandsTests`, `Views/SplitHostTests`,
  `Infrastructure/FakeAppShutdown`

**Modified**

- `Desktop/ViewModels/WorkspaceViewModel.cs` — layout, panes per leaf, operations, toolbar commands,
  batched `LayoutChanged`
- `Desktop/ViewModels/PaneViewModel.cs` — `IsActive`, `IsFramed`
- `Desktop/Views/MainWindow.axaml(.cs)` — `SplitHost` in place of the single pane; `ActivePane` is now
  the active pane's view
- `Desktop/Views/PaneView.axaml` — frame border; `Desktop/Views/PathBarView.axaml` — split/close buttons
- `Desktop/Themes/Styles.axaml` — frame, dimmed inactive cursor, splitter
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — window commands, shutdown
- `tests/.../Infrastructure/ExplorerHarness.cs` — window commands, fake shutdown
- `docs/roadmap.md`, `docs/plan/FEATURE-95D6.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Rebuild strategy | Rebuild the grids on every layout change and reuse the pane views | A change is rare and cheap at this size; reused views keep scroll and focus state |
| When views rebuild | On the workspace's `LayoutChanged`, raised after its own bookkeeping | `LayoutTree.Changed` fires mid-operation, before the new pane exists (the bug fix cycle 1 caught) |
| Splitter write-back | On drag completion, from the two sides' actual sizes | Avoids fighting the grid mid-drag; the layout ends up agreeing with the screen |
| Activation | Tunnel pointer press or focus anywhere in a pane | Clicking a path-bar button of an inactive pane acts on that pane, as expected |
| Close button | Only on the framed (active) pane | Six tiny ✕ buttons invite mis-clicks; closing a pane is one click away once it is active |
| `:q` on the last pane | Shuts the application down via the desktop lifetime | vim behaviour, as planned |
| Resize step | 5% of the split per key, multiplied by the count | Fine enough for `<C-w>>`, fast with `5<C-w>>` |

## Deviations & follow-ups

- The plan's "toolbar buttons" live in each pane's path bar rather than a global toolbar, so they act
  on the pane they sit in.
- Follow-ups (as planned, out of scope): moving panes (`<C-w>H/J/K/L`), tabs, maximize toggle.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 430  failed: 0
```

Fix cycles used: **2 of 3.**

1. `SplitHost` crashed with `KeyNotFoundException` on a split: `LayoutTree.Split` raises `Changed`
   before the workspace has created the pane. The workspace now batches structural changes and raises
   `LayoutChanged` once at the end; views listen to that.
2. The last assertion of my own `SplitHostTests` was inverted against the plan, which focuses the
   active pane on activation. It now asserts that the new split has the keyboard.

A throwaway headless render (not committed) of the main window with three panes confirmed the accent
frame on the active pane, the dimmed cursors of the others, the splitters, and the per-pane buttons.
