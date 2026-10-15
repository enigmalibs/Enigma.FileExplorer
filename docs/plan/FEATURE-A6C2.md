# FEATURE-A6C2 — Context menus & drag-drop

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-a6c2-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Make the explorer work well with the mouse: context menus on items and on the empty area of a pane,
and drag & drop of files and directories within a split group, between split groups, onto folders and
places, and to/from the operating system — with a KDE Plasma-style menu on drop that asks whether to
**copy** or **move**.

## Context & constraints

- Builds on everything before it: every menu entry dispatches a catalog command (FEATURE-2E0F) so the
  menus and the keys can never diverge, and drops go through the transfer queue (FEATURE-BF6B).
- Avalonia 12 drag & drop API: `DragDrop.DoDragDropAsync(PointerEventArgs, IDataTransfer, effects)`,
  `DataTransfer`/`DataTransferItem`, `DragDrop.DragOver/Drop` events (as used in Enigma.Launchpad).
- **The drop target performs the operation itself.** The effect reported back to the drag source is
  always `Copy` (or `None` when refused), so a source such as Windows Explorer never deletes its
  originals because we reported `Move`; a chosen move is done by our transfer engine.
- KDE modifiers: Ctrl held at drop → copy without the menu; Shift → move without the menu; otherwise
  the menu (Copy here / Move here / Cancel) appears at the drop point.

## PHASE01 — Context menus

**Status:** DONE — see `docs/done/FEATURE-A6C2-PHASE01.md`
**Branch:** `feature/feature-a6c2-phase01-context-menus`

### Steps

1. Item menu (right-click on an entry; right-clicking an unselected item makes it the selection
   first): Open · Open in vertical split · Open in horizontal split (directories) · Cut · Copy ·
   Paste into folder (directories) · Copy path · Rename · Move to trash · Delete permanently. In search
   results also: Reveal in folder.
2. Background menu (right-click on empty area): New directory · New file · Paste · View ▸ (Details,
   Medium icons, Large icons) · Sort by ▸ (Name, Modified, Size, Type, Reverse) · Show hidden files
   (check) · Refresh · Split ▸ (Vertical, Horizontal) · Close pane.
3. Each entry shows its key gesture (from the keymap, human-readable) and is enabled per context
   (Paste disabled with an empty clipboard, etc.).
4. Keyboard access: `<Apps>` and `<S-F10>` open the item menu at the cursor item.
5. "Paste into folder" pastes into the directory under the cursor/menu (a dispatcher argument).
6. Tests: menu models built from selection/clipboard state with the right entries/enabled flags and
   gestures; each entry dispatches the expected command id.

### Acceptance criteria

- Both menus appear with correct entries and gestures; every entry performs the same action as its key.
- Build zero warnings, all tests green.

## PHASE02 — Drag & drop with drop menu

**Status:** TODO
**Branch:** `feature/feature-a6c2-phase02-drag-drop`

### Steps

1. Drag start: pointer pressed on an item + moved beyond the platform threshold → drag the selection
   (or the item under the pointer when it is not selected), carrying the files
   (`IStorageItem` via the window's storage provider) plus an app format listing the paths, so
   internal drops do not depend on the OS round-trip and external targets receive real files.
2. Drop targets: a pane's empty area (its current directory), a directory item (that directory, in
   the same or another pane), a sidebar place. Drag-over highlights the target (folder row/tile or
   pane border); invalid targets (into itself or a descendant, a file, a search-results background)
   show `None`.
3. Drop: Ctrl → copy, Shift → move, otherwise a popup menu at the pointer: "Copy here", "Move here",
   separator, "Cancel" (Esc closes = cancel). The chosen request goes to the transfer queue. Moving
   into the directory the items already live in is a no-op without a menu.
4. External drops (files from the OS file manager) follow the same rules; dragging out to the OS
   offers the files.
5. Drag exceptions from platform code are swallowed (a drag that never started changed nothing),
   following the Launchpad precedent.
6. Tests: drop-target resolution (pane/folder/place/invalid), modifier → operation mapping, menu
   choice → transfer request, no-op same-directory move; headless drag-over highlighting where the
   headless platform allows.

### Acceptance criteria

- Drag & drop works within a pane (onto a folder), between panes, onto places, and with the OS file
  manager in both directions (manual check), always showing the Copy/Move menu without modifiers.
- Build zero warnings, all tests green.

## Out of scope

- "Link here", spring-loaded folders (hover to open), auto-scroll while dragging, rubber-band
  selection, drag-to-reorder, custom drag visuals.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Drop menu | Copy here / Move here / Cancel at the drop point; Ctrl/Shift skip it | Exactly the KDE behaviour the draft asks for | Always-move within a volume (Explorer rule) |
| "Link here" | Not offered | Windows requires a privilege for symlinks; not asked | Offer it on Linux only |
| Effect reported to the source | Always `Copy` (or `None`) | Prevents a source from deleting originals behind our back; we perform moves ourselves | Reporting `Move` |
| Drag payload | Files (`IStorageItem`) + app path list | One code path for internal and OS drops; internal drops stay fast and reliable | App format only (no OS interop) |
| Menu entries source | The command catalog | Keys and menus can never diverge; gestures shown for learnability | Hand-written handlers per menu |
| Paste target in item menu | Folder under the cursor for "Paste into folder"; pane dir for background "Paste" | Explorer/Dolphin conventions | Single paste entry |
