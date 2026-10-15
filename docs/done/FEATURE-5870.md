# FEATURE-5870 — Help dialog redesign

**Item:** FEATURE-5870 — Help dialog redesign
**Branch:** `feature/feature-5870-help-dialog`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

The key-binding help (`?`, `F1`, `:help`) was rebuilt.

**Why it scrolled badly.** The help had its own scroll area inside the dialog card, which has one of its
own, so the two nested scrollers fought over the wheel and the bar. Fluent's auto-hiding bar was thin,
and the dialog never took the keyboard, so only the wheel worked.

**Layout.**

- One `SettingsCardExpander` per category: Navigation, Marks, Split groups, View, Files, Search, General.
- Each card has its own icon, the category as header, and "N commands" as description.
- The Split groups card also names the leader key and where it is set (`leaderKey in settings.json`).
- The cards start expanded and can be collapsed with the mouse.

**One scroll area.** Its bar is always shown at full size, so it can be dragged; the wheel works too.

- The view sizes this area so the dialog card never scrolls: 440 px at most, less whatever the card
  would overflow by in a short window, and never below 120 px.
- The size is recomputed when the window is resized.

**Keyboard.** The help takes the keyboard as it opens.

| Keys | Action |
|---|---|
| `j` / `↓`, `k` / `↑` | scroll 40 px |
| `Ctrl+d` / `Ctrl+u` | half a page |
| `PageDown` / `Space`, `PageUp` | a page |
| `g` / `Home`, `G` / `End` | top, bottom |
| `q`, `Esc` | close, and the keyboard goes back to the listing |

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Views/IconGeometryConverter.cs` — a bound `PhosphorIcon` to the
  `Geometry` that `IconData` takes.
- `tests/…/ViewModels/HelpViewModelTests.cs` — cards, icons, counts, the leader in the description, and
  close.
- `tests/…/Views/HelpViewTests.cs` — two headless tests in the real window:
  - the help takes focus; there are 7 cards; the card does not scroll; `j j k G g` move as expected; `q`
    closes and the listing gets the keyboard back;
  - in a minimum-height window the area shrinks and the card still does not scroll.

**Modified**

- `Desktop/ViewModels/HelpViewModel.cs` — `HelpSection` gains `Icon` and `Description`; the constructor
  takes a close callback for `CloseCommand`.
- `Desktop/Views/HelpView.axaml` / `.axaml.cs` — the new layout, the help's own keys, focus, and fitting
  the card.
- `Desktop/Services/HelpDialogService.cs` — passes `HideAsync` as the close callback.
- `Desktop/Themes/Styles.axaml` — `helpSection` removed and `helpRow` added; `helpKeys` and `helpEx` kept.
- `docs/roadmap.md`, `docs/plan/FEATURE-5870.md` — status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Keeping the card from scrolling | Measure the card's own scroll area after layout, and shrink the help's by the overflow | No assumption about the Enigma card's padding or title size; also holds when the window is resized |
| Top of the help | `g` alone, plus `Home` | `gg` needs a pending state in the view for no gain: the help has no other `g` key |
| Icons in the cards | A small `IValueConverter` to `Geometry` | `IconData` is a `Geometry`, and `{ei:IconGeometry}` can only name a fixed icon; the ViewModel keeps a `PhosphorIcon`, as `EntryViewModel` does |

## Deviations & follow-ups

- No deviation from the plan.
- **Visual check.** I rendered two headless Skia frames in a temporary test, deleted afterwards and not
  committed. They show the cards with their icons, a single full-size scrollbar, and `Ctrl+d` scrolling
  (dark theme).
- **Icon sizes.** `ToGeometry` drops the Phosphor view box, as the icon library documents, so each card
  icon is scaled to its own ink bounds. In the frames the sizes look even.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 725  failed: 0  succeeded: 722  skipped: 3
```

- 5 tests were added: 3 view-model tests and 2 headless tests.
- The first run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones.
