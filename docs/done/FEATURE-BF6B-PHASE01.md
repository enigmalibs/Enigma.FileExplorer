# FEATURE-BF6B-PHASE01 — Create, rename, trash & delete engine

**Item:** FEATURE-BF6B — File operations
**Branch:** `feature/feature-bf6b-phase01-file-ops-engine`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The Core engine for single-entry file operations. Every failure is a `FileOperationException` carrying
a message fit for the status line.

- **Name validation.** `NameValidator` checks a name before it touches the disk, by platform.
  - Every platform rejects: empty names, `.`/`..`, path separators, NUL, and more than 255 characters.
  - Windows also rejects `<>:"|?*`, control characters, a trailing dot or space, and the reserved
    device names (with any extension).
- **Free names.** `UniqueNameGenerator.FirstFree` produces `report (2).pdf`, continues `report (2)` as
  `(3)`, and treats directory names and dot-files as extension-less.
- **Create and rename.** `FileOperations` creates directories and empty files (never over an existing
  entry) and renames within a directory, refusing a taken name. A change of case alone goes through a
  temporary name on case-insensitive file systems.
- **Permanent delete.** Deletes recursively, clears read-only attributes, removes links without
  following them, and is cancellable between entries.
- **Trash.** `ITrash` has three implementations, picked at start by `TrashFactory`:
  - **Windows Recycle Bin**, through `SHFileOperationW` with undo allowed and all UI suppressed. It is
    offered only on fixed local drives, because elsewhere the shell would silently delete for good.
  - **freedesktop.org trash** (Linux): the `.trashinfo` is reserved exclusively first, with a
    percent-encoded original path and the local deletion date. The entry then moves to `files/` under
    a free name, and the info is rolled back if the move fails.
  - **macOS `~/.Trash`**, under a free name.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Operations/` — `FileOperationException`, `NamePlatform`, `NamePlatforms`,
  `NameValidator`, `UniqueNameGenerator`, `IFileOperations`, `FileOperations`, `ITrash`,
  `WindowsRecycleBin`, `FreedesktopTrash`, `MacTrash`, `TrashFactory`
- `tests/Enigma.FileExplorer.Core.UnitTests/Operations/` — `NameRulesTests`, `FileOperationsTests`, `TrashTests`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — file operations,
  `TimeProvider`, the platform's trash
- `docs/roadmap.md`, `docs/plan/FEATURE-BF6B.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Recycle Bin on network/removable drives | Refused (`CanTrash` false), so the UI asks before deleting for good | `FOF_ALLOWUNDO` there deletes permanently without a word: a silent data-loss trap |
| `SHFILEOPSTRUCTW` layout | Two struct declarations, natural alignment in 64-bit and `Pack = 1` in 32-bit, chosen at run time | The header packs the struct to one byte on 32-bit Windows only |
| P/Invoke style | `DllImport` with `CharSet.Unicode` | The struct's string fields need runtime marshalling; `LibraryImport` would require unsafe code for no gain |
| freedesktop name reservation | Exclusive create of the `.trashinfo` before moving | The specification's own locking rule; concurrent trashing cannot collide |
| Cross-device trash on Linux | Not attempted; the move fails and the user is told | Copying a large tree into the home trash is not what "trash" should silently do; topdir trashes are a follow-up |
| `"CON.txt"` and friends | Reserved with any extension | Windows treats them as devices whatever follows the dot |
| Sync API | Synchronous Core methods; the UI runs them off the UI thread | Single-entry operations; a cancellation token where work can be long (recursive delete) |

## Deviations & follow-ups

- **Follow-up:** freedesktop "topdir" trashes (`$topdir/.Trash-$uid`) for removable drives on Linux.
  Today those entries are refused for the trash and offered for permanent deletion instead.
- **Manual check:** a one-off test (not committed) moved a temp file named
  `enigma-fileexplorer-recycle-check-<guid>.txt` to this machine's Recycle Bin through `WindowsRecycleBin`,
  and the file left its folder with no error. That ~60-byte file is in the Recycle Bin and can be
  emptied.
- Two tests are platform-conditional and were skipped on this machine: the Linux/macOS launcher test
  (since FEATURE-C4E7-PHASE03), and the symbolic-link delete test (creating links needs a privilege
  here).

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 505  failed: 0  skipped: 2
```

Fix cycles used: **3 of 3.**

1. The first full build failed on test code only: `DeletePermanently` calls missing the test
   cancellation token (xUnit1051), and the Windows-only Recycle Bin test lacking a platform guard the
   analyzer recognises (CA1416).
2. My scripted edit for the second issue did not apply (a string-escaping slip), so the rebuild still
   failed on CA1416.
3. The guard was applied with a direct edit, and the build and the whole suite went green.
