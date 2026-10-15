# FEATURE-6964-PHASE01 — Core name & content search

**Item:** FEATURE-6964 — File search
**Branch:** `feature/feature-6964-phase01-search-engine`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

`FileSearch.SearchAsync(query)` walks the tree below a root breadth first on the thread pool and
streams `SearchHit`s through a bounded channel. Nearer folders come first, entries within a folder
come in natural name order, and stopping the enumeration stops the walk.

- **Names:** `NamePattern` matches whole names as a glob when the pattern has `*` or `?`, and as a
  substring otherwise; smart-case either way. Folders match too.
- **Contents:** literal, smart-case, line by line. Each matching line gives its number and a trimmed
  snippet, shortened to 200 characters around the match with ellipses. At most 100 lines per file.
- **Skipped:** hidden entries and the insides of hidden folders, unless asked for; links (listed,
  never entered); unreadable folders; binary files (a NUL byte in the first 8 KB, with UTF-16 text
  recognised by its byte order mark); files over 10 MB.
- **Capped and cancellable:** at most 10,000 hits by default.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Search/` — `SearchKind`, `SearchQuery`, `SearchHit`, `NamePattern`,
  `IFileSearch`, `FileSearch`
- `tests/Enigma.FileExplorer.Core.UnitTests/Search/FileSearchTests.cs`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — `IFileSearch`
- `docs/roadmap.md`, `docs/plan/FEATURE-6964.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Streaming | A bounded channel fed by a thread-pool walk, read as `IAsyncEnumerable` | The UI consumes hits without ever running the walk on its thread; the bound applies back-pressure |
| Order within a folder | Natural name order | Results are identical on NTFS and ext4 (whose native order is a hash), and read better |
| Glob anchoring | Whole name (`*.md`, not "contains .md") | What shell users expect from a wildcard |
| UTF-16 files | Recognised by their BOM, not mistaken for binaries | Windows tools write UTF-16 logs and `.reg` files |
| Regex safety | 100 ms match timeout for globs | A pathological pattern cannot freeze a search |

## Deviations & follow-ups

- Regex search, an index, and searching inside archives remain out of scope, as planned.
- A third platform-conditional test is now skipped on this machine: the link-loop search test needs
  the privilege to create links.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 572  failed: 0  skipped: 3
```

The first full build and test run was green; no fix cycle was used.
