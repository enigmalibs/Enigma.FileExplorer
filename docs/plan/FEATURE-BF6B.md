# FEATURE-BF6B — File operations

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-bf6b-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Create files and directories, rename, delete (to the trash or permanently), and copy / cut / paste
files and directories — from the keyboard (`a`, `A`, `r`, `dD`, `yy`, `dd`, `p`, `:mkdir`, …) and with
the standard desktop shortcuts — with a background transfer engine that reports progress, can be
cancelled, and resolves name conflicts.

## Context & constraints

- Builds on the pane (FEATURE-C4E7), the command catalog, dispatcher and prompt API (FEATURE-2E0F) and
  the workspace (FEATURE-95D6). Drag & drop (FEATURE-A6C2) reuses the transfer queue built here.
- Core holds every file-system mutation (UI-free, tested against temp directories); Desktop holds
  prompts, dialogs, the clipboard and the queue.
- Names typed by the user are validated per platform before touching the disk: no path separators,
  not `.`/`..`, no invalid characters, no Windows reserved names (`CON`, `PRN`, `AUX`, `NUL`,
  `COM1`–`COM9`, `LPT1`–`LPT9`), no trailing dot/space on Windows, not empty.
- Copy/move of large trees runs off the UI thread, one transfer at a time, cancellable; create,
  rename and trash are immediate.
- Every operation reports per-item failures (access denied, file in use, vanished source) without
  aborting the rest, then summarizes them.

## Design

```
Core/Operations/  NameValidator, UniqueNameGenerator, IFileOperations/FileOperations
                  (CreateDirectory, CreateFile, Rename, DeletePermanently),
                  ITrash + WindowsRecycleBin, FreedesktopTrash, MacTrash, TrashFactory,
                  ITransferEngine/TransferEngine, TransferRequest (sources, target dir, Copy|Move),
                  TransferProgress, ConflictInfo, ConflictResolution (Overwrite, Skip, KeepBoth,
                  Cancel + apply-to-all), IConflictResolver, TransferResult (per-item errors)
Desktop/          FileOperationCommands (handlers), IFileClipboard/FileClipboard (+ OS clipboard),
                  ITransferQueue/TransferQueue, ConflictDialog, status-line progress
```

## PHASE01 — Create, rename, trash & delete engine

**Status:** DONE — see `docs/done/FEATURE-BF6B-PHASE01.md`
**Branch:** `feature/feature-bf6b-phase01-file-ops-engine`

### Steps

1. `NameValidator` (platform rules above, injectable platform for tests) with user-facing messages.
2. `UniqueNameGenerator`: `name (2).ext`, `name (3).ext`, … for files; `name (2)` for directories.
3. `FileOperations`: `CreateDirectory`, `CreateFile` (empty, `FileMode.CreateNew`), `Rename` (same
   directory; case-only rename on case-insensitive file systems goes through a temporary name),
   `DeletePermanently` (recursive, clears read-only attributes, does not follow directory links).
4. `ITrash`: Windows Recycle Bin (`SHFileOperation` with `FOF_ALLOWUNDO`, silent, no UI), freedesktop
   trash spec on Linux (`$XDG_DATA_HOME/Trash` → `files/` + `info/*.trashinfo`, unique names, URL-encoded
   path, ISO deletion date), macOS `~/.Trash` (move with unique name). Chosen by `TrashFactory` at
   runtime; the freedesktop implementation takes its root in the constructor so it is testable
   anywhere.
5. Tests: validator matrix, unique names, create/rename/delete on temp dirs, freedesktop trash layout
   and `.trashinfo` contents.

### Acceptance criteria

- Invalid names are rejected with a clear message; existing names are reported as conflicts.
- Freedesktop trash produces spec-conformant `files/` + `info/` entries (tested); Windows recycle bin
  verified manually.
- Build zero warnings, all tests green.

## PHASE02 — Copy/move transfer engine

**Status:** DONE — see `docs/done/FEATURE-BF6B-PHASE02.md`
**Branch:** `feature/feature-bf6b-phase02-transfer-engine`

### Steps

1. `TransferEngine.RunAsync(request, IConflictResolver, IProgress<TransferProgress>, ct)`:
   - plan: enumerate sources (files, recursive directories; directory links are recreated as links
     when possible, never followed), total item count and bytes;
   - refuse to copy/move a directory into itself or a descendant (whole request error);
   - copy files in chunks (progress per chunk, cancellation between chunks, timestamps preserved);
   - move: same-volume `File.Move`/`Directory.Move`; cross-volume falls back to copy then delete;
     a move into the directory the item already lives in is a no-op;
   - copy into the source's own directory auto-renames with `UniqueNameGenerator` (no conflict asked);
   - conflicts: ask the resolver (Overwrite / Skip / Keep both / Cancel, with apply-to-all); merging a
     directory into an existing directory recurses and asks per conflicting file;
   - cancellation leaves no half-written file (partial destination deleted);
   - per-item errors collected in `TransferResult` (with the created top-level destinations).
2. Tests on temp directories: copy file/tree, move same volume, move-into-self refused, same-dir copy
   auto-rename, each conflict resolution incl. apply-to-all, directory merge, cancellation cleanup,
   progress monotonic and complete, per-item error continues.

### Acceptance criteria

- All engine behaviours above covered by tests; build zero warnings, all tests green.

## PHASE03 — Create, rename & delete UI

**Status:** DONE — see `docs/done/FEATURE-BF6B-PHASE03.md`
**Branch:** `feature/feature-bf6b-phase03-create-rename-delete-ui`

### Steps

1. `a` / `:touch <name>` → "New file:" prompt (a trailing `/` creates a directory instead);
   `A`, `<C-S-n>` / `:mkdir <name>` → "New directory:" prompt. Validation errors keep the prompt open
   with the message; the cursor lands on the created item.
2. `r`, `cw`, `<F2>` / `:rename <name>` → "Rename:" prompt pre-filled with the name, the stem (name
   without extension; whole name for directories) selected; cursor follows the renamed item.
3. `dD`, `<Del>` / `:delete` → move the selection (or cursor item) to the trash; `<S-Del>` /
   `:delete!` → ContentDialog confirmation naming the count/first item, then permanent delete. The
   cursor moves to the next surviving item.
4. Errors go to the status line; partial failures list the first few names.
5. Tests: handlers with fake operations/prompt/dialogs — prompt flows, validation retry, cursor
   placement, confirmation cancel, trash vs permanent routing.

### Acceptance criteria

- Create file/directory, rename and both delete flavours work from keys and ex commands with
  validation and confirmation.
- Build zero warnings, all tests green.

## PHASE04 — Clipboard, transfer queue & conflicts

**Status:** TODO
**Branch:** `feature/feature-bf6b-phase04-clipboard-transfers`

### Steps

1. `FileClipboard` (app-level, authoritative): `yy`/`<C-c>` copy, `dd`/`<C-x>` cut (cut items rendered
   dimmed in every pane until pasted or replaced), `p`/`<C-v>` paste into the active pane's current
   directory (the Explorer convention). Copy/cut also publishes the files to the OS clipboard (best effort); paste with an empty app
   clipboard reads files from the OS clipboard (treated as copy). Cut + paste clears the clipboard.
2. `yp` copies the selected full paths as text (one per line).
3. `TransferQueue` (singleton): runs requests sequentially on a background task; exposes the current
   transfer (title, item, percent, bytes) for the status line, with a cancel button and `:cancel`;
   finishes with a summary message (and a dialog listing errors when any).
4. `ConflictDialog` (ContentDialog): source vs destination name, size and date; Overwrite / Skip /
   Keep both / Cancel + "apply to all"; implements `IConflictResolver` by marshalling to the UI thread.
5. Panes refresh after a transfer (the watcher covers most cases; an explicit refresh guarantees it).
6. Tests: clipboard state machine (copy/cut/paste/clear, cut dimming), queue sequencing and
   cancellation with a fake engine, conflict resolver marshalling.

### Acceptance criteria

- Copy/cut/paste between and within panes, with progress, cancel, conflicts and error summary.
- Files copied here paste into the OS file manager and vice versa (manual check, best effort).
- Build zero warnings, all tests green.

## Out of scope

- Undo; "Link here"/symlink creation; permissions/ownership editing; archive extraction; a properties
  dialog; parallel transfers; resuming interrupted transfers.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Key flavour | ranger-style `yy`/`dd`/`p`, `dD` trash; desktop aliases | Mirrors the draft's "copy/paste/cut" with vim muscle memory | vifm style (`dd` = delete) |
| Delete semantics | Trash by default, permanent behind Shift+Delete + confirmation | Recoverable by default; matches every desktop file manager | Permanent delete only |
| Windows trash | `SHFileOperation` P/Invoke with `FOF_ALLOWUNDO` | Real Recycle Bin, no UI, no extra dependency | `Microsoft.VisualBasic` FileSystem (may show UI); custom folder |
| Linux trash | freedesktop.org Trash spec implementation | What Dolphin/Nautilus read; restorable from them | `gio trash` process (external tool dependency) |
| Conflicts | Dialog with Overwrite / Skip / Keep both / Cancel + apply to all | The standard, safe set | Always overwrite / always skip |
| Same-folder copy | Auto-rename `name (2).ext` | Explorer/Dolphin behaviour; duplicating is the intent | Conflict dialog |
| Long operations | Sequential background queue with progress + cancel | No UI freeze; avoids disk thrash | Synchronous; parallel transfers |
| Clipboard | App clipboard authoritative + OS clipboard mirror/fallback | Cut semantics are not portable through the OS clipboard; interop still works for copy | App-only; OS-only |
| Paste target | The active pane's current directory | Explorer convention; predictable | The folder under the cursor |
| Name prompts | Bottom prompt (FEATURE-2E0F) with stem pre-selected | Keyboard-fast; vim-like | Modal dialogs |
