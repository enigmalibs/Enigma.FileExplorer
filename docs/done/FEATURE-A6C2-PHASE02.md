# FEATURE-A6C2-PHASE02 — Drag & drop with drop menu

**Item:** FEATURE-A6C2 — Context menus & drag-drop
**Branch:** `feature/feature-a6c2-phase02-drag-drop`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

Files and folders can be dragged within a pane, between split groups, onto places, and to or from other
applications.

**Dragging.**

- A drag starts once the pressed pointer has moved 4 px from an entry.
- It carries the marked entries, or the entry under the pointer when nothing is marked.
- A plain press on a marked entry keeps the marks, so they can all be dragged. If the button is released
  without a drag, the press becomes the usual click and selects that entry alone.
- The drag carries the real files (storage items, for other applications) and the explorer's own path
  list (`enigma-fileexplorer.paths`).

**Drop targets.**

- A folder under the pointer, in any pane.
- Anywhere else in a pane: that pane's directory.
- A place in the sidebar.

**While hovering.**

- The target is highlighted: an accent border on the folder row or tile, an accent frame and lighter
  background on the pane, or an accent border on the place.
- These show "no drop": a folder into itself or a subfolder, the background of search results, and
  entries over the directory they already live in.

**At the drop (KDE rules).**

- Ctrl copies and Shift moves, without asking.
- Otherwise a menu opens at the pointer: Copy here (Ctrl), Move here (Shift), Cancel (Esc). Closing it
  any other way cancels.
- Entries dropped where they already are stay put, with no menu, unless Ctrl asks for a copy.
- The transfer runs through the transfer queue, so progress, conflicts and cancel work as for paste.
  Affected panes refresh afterwards.
- The drop always reports Copy to the source, or None, so another file manager never deletes its
  originals behind our back. A move chosen here is done by the explorer itself.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/ViewModels/FileDrops.cs`, `DropAction.cs` — drop rules
  (`ActionFor`, `TargetFor`, `Decide`) and `DropAsync`
- `src/Enigma.FileExplorer.Desktop/Views/FileDragData.cs` — drag payload, reading drops
- `src/Enigma.FileExplorer.Desktop/Views/FileDropTarget.cs`, `DropPlan.cs` — hover and drop effects,
  the drop menu, finishing a drop
- Tests: `ViewModels/FileDropsTests`, `Views/PaneViewDragDropTests`

**Modified**

- `Desktop/Views/PaneView.axaml(.cs)` — drag source (threshold, deferred click on marked entries),
  drop target (folder/pane), highlight bindings
- `Desktop/Views/PlacesView.axaml(.cs)` — places as drop targets
- `Desktop/Views/ContextMenuFactory.cs` — shared `Open` (placement at an anchor)
- `Desktop/ViewModels/WorkspaceViewModel.cs` — `RefreshAfterTransferAsync`, moved from
  `ClipboardCommands`, which now calls it
- `Desktop/ViewModels/EntryViewModel.cs`, `PaneViewModel.cs`, `PlaceViewModel.cs` — `IsDropTarget`
- `Desktop/ViewModels/MainWindowViewModel.cs` — exposes `Drops`
- `Desktop/Commands/ClipboardCommands.cs`, `Desktop/Themes/Styles.axaml`,
  `Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
- Tests: `Infrastructure/ExplorerHarness`
- `docs/roadmap.md`, `docs/plan/FEATURE-A6C2.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Dropping on a file | Lands in the pane's directory (the file is no target) | Explorer and Dolphin do this. In a details view, refusing every file row would make most of a pane a dead zone |
| Drop where the entries already are | Nothing, without a menu; Ctrl still copies (duplicate through the conflict dialog) | A drag that ends where it started must not pop a menu; an explicit copy is still honoured |
| Ctrl+Shift | Asks with the menu | KDE uses it for "link here", which is out of scope; asking is the safe fallback |
| Effects offered by our drags | Copy and Move | The target decides, as with Dolphin as a source. Our source never deletes anything whatever the target reports |
| Effect shown while hovering | Move with Shift, Copy otherwise, within what the source allows | Shows what the drop will do; only the final drop effect is pinned to Copy |
| Completing a drop | Posted after the drop event returns | The platform's drag loop (OLE on Windows) must end before a menu opens; the data is read inside the event, while it still exists |
| Post-transfer refresh | Shared `WorkspaceViewModel.RefreshAfterTransferAsync` | Paste and drop refresh the same panes the same way; one copy of the rule |
| Drag threshold | 4 px | Same value as Enigma.Launchpad; Avalonia exposes no platform threshold |

## Deviations & follow-ups

- The plan listed a file under the pointer as an invalid target; it drops into the pane's directory
  instead (see decisions).
- **Manual check not done.** I could not drive a real desktop drag in this session: an OS file-manager
  drop in either direction, and a real `DoDragDropAsync` drag between panes. Headless tests cover drops
  (with the platform's DragEnter, DragOver, Drop sequence), highlights, modifiers, the menu and places.
  The drag start is covered only up to the threshold and the deferred click. Please check both
  directions with Explorer, Dolphin or Nautilus.
- Follow-up seen in a scratch render, outside this dev's scope: in a narrow split pane (about 350 px)
  the details view's star-sized Name column collapses to zero width behind the fixed Modified, Type and
  Size columns. The fixed columns should shrink or hide first.
- The pane drop highlight uses the same accent as the active pane's frame. The lighter background is
  what tells them apart.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 629  failed: 0  succeeded: 626  skipped: 3
```

The first full build and test run was green, so no fix cycle was used. Before it, three new headless
tests failed during implementation because they sent a Drop without the preceding DragEnter/DragOver,
which Avalonia requires (a drop goes to the last element hovered). The tests were fixed to drive the real
sequence; production code was unchanged. The three skips are the known platform-specific ones. A
throwaway headless render (not committed) showed the folder and pane highlights and the drop menu with
its keys.
