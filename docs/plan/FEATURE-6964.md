# FEATURE-6964 — File search

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-6964-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Search recursively from the active pane's directory, with two separate commands: **by name**
(`f`, `<C-f>`, `:find <pattern>`) and **by content** (`F`, `<C-S-f>`, `:grep <text>`). Results stream
into the pane as a virtual listing that can be navigated and acted on like a directory.

## Context & constraints

- Builds on the pane and views (FEATURE-C4E7, FEATURE-2A19), the command catalog/prompt
  (FEATURE-2E0F); the catalog ids already exist.
- Smart-case everywhere (vim `smartcase`): case-insensitive unless the pattern contains an uppercase
  letter.
- Name patterns: `*` and `?` wildcards when present, otherwise substring.
- Content search: literal text (no regex), line-based, reports file, line number and a trimmed line
  snippet; skips binary files (NUL byte in the first 8 KB) and files larger than 10 MB; reads with BOM
  detection, UTF-8 default.
- Hidden files/directories follow the global hidden toggle; directory links/junctions are not
  recursed (no cycles); inaccessible directories are skipped silently; at most 10,000 results (a
  "results truncated" notice beyond).
- Search runs off the UI thread, streams results in batches (~100 ms), and is cancelled by Esc,
  navigation, or a new search in the same pane.

## PHASE01 — Core name & content search

**Status:** TODO
**Branch:** `feature/feature-6964-phase01-search-engine`

### Steps

1. `SearchQuery` (root, pattern, kind Name|Content, include hidden, max results), `SearchHit` (entry,
   relative directory, line number + snippet for content hits), `NamePattern` (glob/substring,
   smart-case), `IFileSearch.SearchAsync(query, ct)` → `IAsyncEnumerable<SearchHit>`.
2. Breadth-first walk with the skip rules above; content scan streams lines, stops at the first hit
   per line, reports every matching line per file (capped per file at 100 lines).
3. Tests on temp trees: glob vs substring, smart-case, hidden toggle, link not recursed, binary and
   size skipping, line numbers/snippets, result cap, cancellation.

### Acceptance criteria

- Behaviours above covered by tests; build zero warnings, all tests green.

## PHASE02 — Search results UI

**Status:** TODO
**Branch:** `feature/feature-6964-phase02-search-ui`

### Steps

1. `f`/`<C-f>` → "Find name:" prompt, `F`/`<C-S-f>` → "Find in files:" prompt; `:find`/`:grep` take
   the pattern directly.
2. The pane enters results mode: header shows `Search "<pattern>" in <root>` with a progress indicator
   and the hit count; results listed in the pane's view (details shows extra columns: Folder, and for
   content hits Line + Text).
3. In results mode: `j`/`k` etc. move; `l`/`<CR>` opens (directory → navigate there, file → default
   app); `gl` reveals the hit (navigate to its folder with the cursor on it); `h`/`H`/`<Esc>` leave
   results and return to the directory (Esc first cancels a running search); results support
   selection so copy/cut/trash/drag act on them.
4. Back history treats a results listing as a location (so `L` can return to it within the session).
5. Tests: results-mode state machine, batching, cancel on navigation, reveal, opening.

### Acceptance criteria

- Name and content search are separate commands; results stream, can be cancelled and acted on.
- Build zero warnings, all tests green.

## Out of scope

- Regex search, an index/daemon, search inside archives, replace, opening a file at a line in an
  editor, saved searches.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Results presentation | Virtual listing inside the pane | Results become actionable with every existing key and view; how Explorer/Dolphin do it | Separate results panel; filter-only |
| Matching | Smart-case; glob when wildcards present else substring; content literal | Predictable for vim users without regex pitfalls | Regex by default |
| Limits | 10,000 results, 10 MB per file, binary skipped, links not recursed | Keeps memory and time bounded on huge trees | Unbounded |
| Keys | `f`/`F` + `<C-f>`/`<C-S-f>` + `:find`/`:grep` | Separate commands as the draft requires; desktop aliases | One combined search |
| Reveal key | `gl` ("go to location") | `g`-prefix family for jumps | Overloading `l` |
