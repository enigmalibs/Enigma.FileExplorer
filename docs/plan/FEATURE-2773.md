# FEATURE-2773 — Bookmarks

**Status:** DONE — see `docs/done/FEATURE-2773-PHASE01.md`, `docs/done/FEATURE-2773-PHASE02.md`
**Type:** FEATURE
**Branch:** `feature/feature-2773-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

Bookmark folders and jump to them fast:

- one key adds the current folder;
- one key opens a bookmarks dialog, where Enter opens the selected folder in the current pane;
- the dialog also manages the list: add, remove.

## Context & constraints

- Persistence goes into the existing `settings.json` (`AppSettings`, `Session`), saved at once on
  change, like the theme and the hidden-files switch.
- Dialogs use the window's `ContentDialog` through `IContentDialogService`. While a dialog is open,
  `MainWindow` routes no keys to the keymap, so the dialog view handles its own keys. The help dialog
  (FEATURE-5870) sets the pattern: the view takes focus, and `q` closes through a VM callback.
- Keys come after FEATURE-6526 and FEATURE-5ACD:
  - `b` and `B` are free;
  - `d` is trash in the listing, but inside the dialog it removes a bookmark.

## PHASE01 — Bookmark store & add key

**Status:** DONE — see `docs/done/FEATURE-2773-PHASE01.md`
**Branch:** `feature/feature-2773-phase01-bookmark-store`

### Steps

1. Core:
   - `Settings/BookmarkSettings` record (`Name`, `Path`);
   - `AppSettings.Bookmarks` (list, default empty).
2. Desktop:
   - `Services/Bookmarks` (observable, singleton) with an ordered `Items` list, `Add(path)` that returns
     false for a duplicate (same path, platform comparison), `Remove(bookmark)`, and a `Changed` event;
   - the name is the folder's name, or the full path for a volume root;
   - `Session` loads the bookmarks, applies them, and saves at once on `Changed`.
3. Commands:
   - `CommandIds.BookmarkAdd` ("Bookmark this folder", `B`, in a new "Bookmarks" help category) adds
     the active pane's folder;
   - status messages "Bookmarked 'name'" and "Already bookmarked".
4. Tests:
   - add, duplicate and remove;
   - the session round-trips the list and saves when it changes;
   - `B` adds the current folder;
   - search results (no folder) report nothing to bookmark.

### Acceptance criteria

- `B` bookmarks the current folder once; a second `B` says it is already bookmarked.
- Bookmarks survive a restart (`bookmarks` in settings.json).
- Build has zero warnings and all tests are green.

## PHASE02 — Bookmarks dialog

**Status:** DONE — see `docs/done/FEATURE-2773-PHASE02.md`
**Branch:** `feature/feature-2773-phase02-bookmarks-dialog`

### Steps

1. `CommandIds.Bookmarks` (`b`, `:bookmarks`) shows the dialog through `IBookmarksDialogService`.
2. `BookmarksViewModel`:
   - the items, with the selection on the first;
   - `Open` puts the selected folder in the current pane and closes;
   - `Remove`, and `AddCurrent` for the active pane's folder;
   - a placeholder when the list is empty.
   The view is a `ListBox` with the name and the full path.
3. Keys in the dialog:
   - `j`/`↓` and `k`/`↑` move; `g`/`G` go to the first or last;
   - `Enter`/`l` open;
   - `d`/`Del` remove;
   - `a` adds the current folder;
   - `q`/`Esc` close.
   A double-click opens. Buttons: Open (primary) and Close.
4. Opening a bookmark whose folder no longer exists shows a status error and keeps the dialog's choice
   harmless; it goes through the pane's normal navigation.
5. Tests:
   - VM: open, remove, add and selection rules;
   - headless: `b` opens the dialog with the list focused; `j` + Enter opens the second bookmark in the
     active pane; `d` removes; `q` closes.

### Acceptance criteria

- `b` (or `:bookmarks`) opens the bookmarks dialog.
- `j`/`k` move, Enter opens the folder in the current pane, `d` removes, `a` adds the current folder,
  and `q`/Esc close.
- Build has zero warnings and all tests are green.

## Out of scope

- Renaming or reordering bookmarks, showing them in the places sidebar, and lettered vim marks.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Storage | `bookmarks` in settings.json, saved at once | Existing settings store and pattern; survives crashes | A separate bookmarks file |
| Keys | `B` adds, `b` opens the dialog, plus `:bookmarks` | Single keystrokes for frequent actions; both are free | Putting them under the leader |
| Order | Insertion order | Predictable; the newest is last | Alphabetical |
| Name | The folder's name (the full path for a root) | Readable; the full path is shown beside it | Asking for a name on every add |
| Dialog keys | `j/k`, `Enter`/`l`, `d`/`Del`, `a`, `q`/`Esc` | Mirrors the listing's vim keys | Mouse-only management |
| Phasing | Store and add key first, then the dialog | Two reviewable commits | One large dev |
