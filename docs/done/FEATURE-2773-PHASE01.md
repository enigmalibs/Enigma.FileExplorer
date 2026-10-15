# FEATURE-2773-PHASE01 — Bookmark store & add key

**Item:** FEATURE-2773 — Bookmarks, PHASE01 — Bookmark store & add key
**Branch:** `feature/feature-2773-phase01-bookmark-store`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

Folders can now be bookmarked.

**Adding a bookmark.** `B` bookmarks the active pane's folder and says so ("Bookmarked 'docs'"). A second
`B` on the same folder says it is already bookmarked. Search results are not a folder, so `B` refuses
there ("Only a folder can be bookmarked").

**The list.**

- Bookmarks are kept in the order they were added.
- Each one is listed under the folder's name, or under its full path for a volume root.
- Paths are compared with the platform's rules, so on Windows `C:\Docs` and `c:\docs\` are the same
  bookmark.

**Saving.** The list is saved at once, as `bookmarks` (`name` and `path`) in `settings.json`, whenever it
changes, and it is loaded at start. Loading skips blank and repeated folders, and gives a missing name the
folder's name.

The dialog that opens bookmarks comes in PHASE02. The help already shows a "Bookmarks" card for `B`.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Settings/BookmarkSettings.cs` — `Name`, `Path`.
- `src/Enigma.FileExplorer.Desktop/Services/Bookmarks.cs` — `Bookmark` record; `Bookmarks` with `Items`,
  `Add`, `Remove`, `Contains`, `Load`, `NameOf` and `Changed`.
- `src/Enigma.FileExplorer.Desktop/Commands/BookmarkCommands.cs` — `B`.
- `tests/…/Services/BookmarksTests.cs` — the list's rules, and `B` in a folder and in search results.

**Modified**

- `Core/Settings/AppSettings.cs` — `Bookmarks` (default empty).
- `Core/Commands/CommandIds.cs`, `CommandCatalog.cs` — `BookmarkAdd` on `B`, in a new `Bookmarks` help
  category.
- `Desktop/Services/Session.cs` — loads, saves and save-on-change for the bookmarks.
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — `Bookmarks` and `BookmarkCommands`.
- `Desktop/ViewModels/HelpViewModel.cs` — the Bookmarks card's icon.
- Tests:
  - `SessionTests` — bookmarks load and save at once;
  - `JsonSettingsStoreTests` — bookmarks round-trip in order; the full round trip compares the list
    apart;
  - `HelpViewModelTests`, `HelpViewTests` — the 8th card;
  - `ExplorerHarness` — `Bookmarks` and the module.
- `docs/roadmap.md`, `docs/plan/FEATURE-2773.md` — status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The bookmark model | A Desktop `Bookmark(Name, Path)` record; `BookmarkSettings` stays the file's shape | The same split as the other settings: the UI never binds to the persisted DTO |
| Add on a duplicate | Returns `null`; the command says "already bookmarked" as information, not an error | Pressing `B` twice is harmless, not a mistake |
| `AppSettings` equality in the store test | Compare the bookmark list apart | A record compares an `IReadOnlyList` member by reference; found by the first test run |

## Deviations & follow-ups

- No deviation from the plan.
- **Latent trap.** `AppSettings` record equality now treats two lists with equal contents as different.
  Nothing in the product compares settings, so this was noted rather than changed.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 733  failed: 0  succeeded: 730  skipped: 3
```

- 8 tests were added: 6 bookmark tests, 1 session test and 1 store test.
- One fix cycle was used:
  - a nullable warning (treated as an error) in a new test;
  - the round-trip test's whole-record comparison, which the new list member broke.
- The three skips are the known platform-specific ones.
