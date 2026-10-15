# FEATURE-BF6B-PHASE02 — Copy/move transfer engine

**Item:** FEATURE-BF6B — File operations
**Branch:** `feature/feature-bf6b-phase02-transfer-engine`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

`TransferEngine` copies and moves files and directory trees on the thread pool. It takes a
`TransferRequest` (sources, target directory, copy/move) and returns a `TransferResult` (created
top-level destinations, per-item errors, skipped count, cancelled flag).

- **Refused up front, before anything happens.** A missing target directory, or a folder copied or
  moved into itself or a descendant.
- **Progress.** Totals are measured first (items and bytes, links counted without being followed).
  Files copy in 1 MB chunks with progress throttled to every 50 ms, and timestamps and attributes are
  preserved.
- **Moves.** Within one volume a move is a rename: by drive root on Windows, by mount point on Unix.
  Across volumes it is copy-then-delete, and the source is deleted only when its copy succeeded
  completely.
- **Same directory.** A copy into the source's own directory gets `name (2).ext` without asking; a
  move there is a no-op.
- **Conflicts.** `IConflictResolver` answers Overwrite / Skip / Keep both / Cancel, and "apply to all"
  remembers the answer for the rest of the transfer. A directory onto an existing directory merges
  and asks only about colliding files. After a move-merge the source directory is removed only when
  empty, so skipped files keep it.
- **Failures and cancel.** Per-item failures (missing source, locked file) are collected and the rest
  continues. Cancellation stops between chunks and deletes the half-written file.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Operations/` — `TransferMode`, `TransferRequest`, `TransferProgress`,
  `ConflictChoice`, `ConflictInfo`, `ConflictResolution`, `TransferError`, `TransferResult`,
  `IConflictResolver`, `ITransferEngine`, `TransferEngine`
- `tests/Enigma.FileExplorer.Core.UnitTests/Operations/TransferEngineTests.cs`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — the engine
- `docs/roadmap.md`, `docs/plan/FEATURE-BF6B.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Same-volume detection | Windows: drive root; Unix: longest matching mount point, with a failed rename falling back to copy | A cheap rename where possible, a correct copy where not, without native `stat` calls |
| Progress throttle | 50 ms, injectable (internal) for tests | Smooth enough for a progress bar without flooding the UI thread |
| Overwrite of a directory by a file (or the reverse) | Delete what is there, then transfer | What the user asked for; the conflict dialog shows both kinds |
| Access denied on a same-volume rename | That item fails and the transfer goes on | One locked entry must not abort a large move |
| Item counting | Files and links only; directories are containers | Matches what people mean by "12 of 340 items" |

## Deviations & follow-ups

- Destination free space is not checked up front; a full disk surfaces as per-item errors. This is a
  follow-up if wanted.
- Two platform-conditional tests are still skipped on this machine (see PHASE01's completion record).

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 524  failed: 0  skipped: 2
```

Fix cycles used: **1 of 3.** The first run had three failures:

- Two came from a real bug: after a move-by-rename, progress measured the source, which no longer
  existed. It is now measured before renaming.
- The third was the cancellation test. An 8 MB copy finished inside one 50 ms throttle window, so the
  cancel trigger never fired mid-copy. The engine now takes its report interval through an internal
  constructor, and the test uses zero.
