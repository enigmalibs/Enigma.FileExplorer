# FEATURE-2773-PHASE02 — Bookmarks dialog

**Item:** FEATURE-2773 — Bookmarks, PHASE02 — Bookmarks dialog
**Branch:** `feature/feature-2773-phase02-bookmarks-dialog`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

`b` (or `:bookmarks`) opens the bookmarks in the window's content dialog. Each bookmark shows its name over
its full path, the first one is selected, and the dialog takes the keyboard.

| Keys | Action |
|---|---|
| `j` / `↓`, `k` / `↑` | move |
| `g` / `Home`, `G` / `End` | first, last |
| `Enter` / `l` (or a double-click, or the **Open** button) | close the dialog and open the folder in the **current pane** |
| `d` / `Del` | remove the selected bookmark (the selection stays in place) |
| `a` | bookmark the current folder and select it |
| `q` / `Esc` (or **Close**) | close without opening anything |

**Edge cases.**

- An empty list shows a hint, and the dialog still takes the keys, so `a`, `q` and `Esc` work.
- A bookmark whose folder no longer exists gets the pane's usual error ("'…' does not exist"), and the
  pane stays where it was.
- The list never makes the dialog card scroll. The help's sizing logic was moved into a shared `DialogFit`
  helper that both dialogs use.

FEATURE-2773 is complete with this phase.

## Files / modules touched

**Created**

- `Desktop/ViewModels/BookmarksViewModel.cs` — items, selection, `Open`/`Remove`/`AddCurrent`/`Close`,
  `Chosen`.
- `Desktop/Views/BookmarksView.axaml` / `.axaml.cs` — the list, the hint, the keys, focus, and fitting the
  card.
- `Desktop/Views/DialogFit.cs` — shared: caps a scroll area so the Enigma dialog card does not overflow.
- `Desktop/Services/IBookmarksDialogService.cs`, `BookmarksDialogService.cs` — shows the dialog and returns
  the chosen bookmark.
- Tests:
  - `ViewModels/BookmarksViewModelTests` — 6 tests;
  - `Views/BookmarksViewTests` — 2 headless tests in the real window: `b`, `j`, Enter, `d`, `q`; and an
    empty list with `a`;
  - `Infrastructure/FakeBookmarksDialog`.

**Modified**

- `Core/Commands/CommandIds.cs`, `CommandCatalog.cs` — `Bookmarks` on `b` and `:bookmarks` (`:bo`).
- `Desktop/Commands/BookmarkCommands.cs` — shows the dialog and opens the chosen folder in the active
  pane.
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — the dialog service.
- `Desktop/Themes/Styles.axaml` — `bookmarkName`, `bookmarkPath`, `bookmarksHint`.
- `Desktop/Views/HelpView.axaml` / `.axaml.cs` — use `DialogFit`; `MaxHeight` instead of `Height`.
- Tests:
  - `Services/BookmarksTests` — `b` opens the pick, closing without a pick does nothing, a bookmark whose
    folder is gone;
  - `Views/HelpViewTests` — opens the help with the real `?` key, and never blocks on a dialog left open;
  - `ExplorerHarness` — the fake dialog.
- `docs/roadmap.md`, `docs/plan/FEATURE-2773.md` — the phase and the item are `DONE`.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who navigates | The command, after the dialog returns the chosen bookmark | The dialog stays a picker; navigation reuses the pane's own error reporting, and the command is testable with a fake dialog |
| Enter vs. the Open button | Enter (`OpenCommand`) records the choice and closes. The button's `Primary` result chooses the selection afterwards (`ChooseSelected`) | The dialog closes itself on a button click; the two paths never close twice |
| Sharing the card-fitting logic | Extracted from the help into `DialogFit` | Two dialogs need the same guard against nested scrolling |

## Deviations & follow-ups

- **Bug found and fixed in this phase.** An Avalonia `ListBox` is **not focusable by default**. The
  first run showed the dialog open with the pane still holding the keyboard: `j` and Enter did nothing,
  and the test awaiting the dialog hung until it was killed. Fixed with `Focusable="True"` on the list
  (and on the view, for the empty list). The help was not affected (its `ScrollViewer` was already
  focusable), but its test now opens it with the real `?` key.
- **Tests no longer hang.** The headless dialog tests close any dialog a key failed to close before they
  await the command, so a regression fails instead of hanging.
- **Visual check.** I rendered one headless frame in a temporary test, deleted afterwards. The selection
  follows `j`, and names and paths read well (dark theme).
- **Follow-ups.** Renaming and reordering bookmarks, and showing them in the places sidebar (out of
  scope, as planned).

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 744  failed: 0  succeeded: 741  skipped: 3
```

- 11 tests were added: 6 view-model, 3 command and 2 headless tests.
- First run: hung on the focus bug; the test process was killed.
- Fix cycle 1: the focus fix. The full suite was green: 743.
- Fix cycle 2: the hang-proof tests and the empty-list test. Green: 744.
- The three skips are the known platform-specific ones.
