# FEATURE-C4E7-PHASE02 — Core directory listing & sorting

**Item:** FEATURE-C4E7 — Foundation & directory browsing
**Branch:** `feature/feature-c4e7-phase02-directory-listing`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The UI-free foundation every pane reads through. `DirectoryReader` lists a directory off the calling
thread into immutable `FileSystemEntry` records: kind, size, local last-write time, hidden and link
flags. A missing directory is reported, and so is an unreadable one: it is never shown as empty.
Entries that vanish mid-read are skipped. `NaturalStringComparer` orders names the way people read
them. `EntrySorter` keeps directories first under every key and direction and breaks ties by name.
`FileTypes` names types for both the Type column and the Type sort. `PathUtilities` does the path
arithmetic navigation and the command line need: normalize, resolve typed input (`~`, relative,
bare drive), parent, volume root, nearest existing ancestor, same/descendant tests.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/FileSystem/` — `EntryKind`, `FileSystemEntry`, `HiddenRules`,
  `DirectoryListing`, `IDirectoryReader`, `DirectoryReader`, `NaturalStringComparer`, `SortKey`,
  `SortDirection`, `EntrySort`, `FileTypes`, `EntrySorter`, `PathUtilities`
- `tests/Enigma.FileExplorer.Core.UnitTests/Infrastructure/TempDirectory.cs`
- `tests/Enigma.FileExplorer.Core.UnitTests/FileSystem/` — `DirectoryReaderTests`,
  `NaturalStringComparerTests`, `EntrySorterTests`, `FileSystemEntryTests`, `PathUtilitiesTests`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — registers
  `IDirectoryReader`
- `docs/roadmap.md`, `docs/plan/FEATURE-C4E7.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Enumeration API | `FileSystemEnumerable` with a transform | No `FileSystemInfo` allocation per entry, and the attributes come from the directory read itself |
| `IgnoreInaccessible` | `false` | The read is not recursive, so the option only concerns the directory itself, and `true` would silently show an access-denied directory as empty |
| Vanished entries | Transform returns `null` on `IOException`/`UnauthorizedAccessException`, filtered out | One racing entry must not fail the whole listing |
| Text comparison in natural sort | Invariant culture, ignore case, with an ordinal tie-break | Accents sort next to their base letter; the order is total and the same on every machine |
| Ties under a descending key | Broken by name ascending | Equal sizes/dates stay findable alphabetically |
| Type sort | By the `FileTypes.Describe` text | The Type column and the Type sort always agree |
| Dot-file extension | None (`.gitignore` has no extension) | How every file manager treats it |
| Extra `PathUtilities` members (`AreSame`, `IsSameOrDescendant`, `Comparison`) | Added now | Navigation needs them in PHASE03, and file operations need them for into-self checks; they belong with the rest of the path arithmetic |

## Deviations & follow-ups

- None against the plan.
- Symbolic-link detection is covered only by the attribute mapping. Creating links needs a privilege
  on Windows, so no test creates one.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  Test run summary: Passed!
  total: 87  failed: 0  succeeded: 87  skipped: 0
```

The first build and test run was green; no fix cycle was used.
