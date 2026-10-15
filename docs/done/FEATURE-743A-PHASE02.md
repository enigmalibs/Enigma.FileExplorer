# FEATURE-743A-PHASE02 — About dialog

**Item:** FEATURE-743A — App icon & About dialog
**Branch:** `feature/feature-743a-phase02-about-dialog`
**Run:** vibe/2026-10-15-app-icon-about

## Summary

`:about` (or `:ab`) opens an About dialog. It is Enigma.GitClient's dialog in its current, simplified
form.

**What it shows**, top to bottom:

- the 96 px application icon (PHASE01's `app-256.png`);
- "Enigma File Explorer";
- "Version 0.1.0";
- a quieter, selectable "Build `<sha>`" line, absent when the build recorded no revision;
- the copyright, absent when the assembly carries none;
- one *Close* button at the bottom right.

The card has no title and no icon column.

**Keyboard.**

- The view takes the keyboard as it opens.
- `q` and Enter close it, like the explorer's other dialogs. `Esc` is the dialog's own key.
- The listing gets the keyboard back after closing.

**Help.** The help lists the command in its *General* card as "About Enigma File Explorer —
`:ab[out]`".

**Product identity.** It now lives in one place, `Core/Diagnostics`, as in GitClient:

- `ProductVersion.From(informational, fallback)` is a pure parser. It splits `X.Y.Z+<sha>`, shortens
  the sha to 7 characters, and falls back to the assembly version, then `0.0.0`, instead of throwing.
- `ProductInformation` exposes `Name`, `DisplayName`, `Version`, `BuildSha` and `Copyright`, read once
  from the Core assembly. Core and Desktop share one version through `Directory.Build.props`.
- `MainWindowViewModel.ProductName` is now `ProductInformation.Name`. The window title is unchanged.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Diagnostics/ProductVersion.cs`
- `src/Enigma.FileExplorer.Core/Diagnostics/ProductInformation.cs`
- `src/Enigma.FileExplorer.Desktop/ViewModels/AboutViewModel.cs`
- `src/Enigma.FileExplorer.Desktop/Views/AboutView.axaml` / `.axaml.cs`
- `src/Enigma.FileExplorer.Desktop/Services/IAboutDialogService.cs` / `AboutDialogService.cs`
- `tests/Enigma.FileExplorer.Core.UnitTests/Diagnostics/ProductVersionTests.cs` — 10 tests.
- `tests/Enigma.FileExplorer.Core.UnitTests/Diagnostics/ProductInformationTests.cs` — 5 tests.
- `tests/Enigma.FileExplorer.Desktop.UnitTests/ViewModels/AboutViewModelTests.cs` — 7 tests.
- `tests/Enigma.FileExplorer.Desktop.UnitTests/Views/AboutViewTests.cs` — 4 headless tests in the real
  window.
- `tests/Enigma.FileExplorer.Desktop.UnitTests/Infrastructure/FakeAboutDialog.cs`

**Modified**

- `Core/Commands/CommandIds.cs` — `About = "app.about"`.
- `Core/Commands/CommandCatalog.cs` — the General command "About Enigma File Explorer" (no key) and
  the ex command `about` (minimum `ab`).
- `Desktop/Commands/CommandLineCommands.cs` — runs `app.about`, beside the help.
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — registers `IAboutDialogService`.
- `Desktop/ViewModels/MainWindowViewModel.cs` — `ProductName = ProductInformation.Name`.
- `tests/…/Infrastructure/ExplorerHarness.cs` — the harness's `About` fake, passed to
  `CommandLineCommands`.
- `tests/…/ViewModels/CommandLineViewModelTests.cs` — two tests: `:about` / `:ab` show the dialog, and
  the help's General card lists it.
- `docs/roadmap.md`, `docs/plan/FEATURE-743A.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Enter | The view closes on Enter itself | The library's `DefaultButton` only declares intent. With the keyboard in the view, Enter would otherwise do nothing |
| Where focus goes | The view itself (`Focusable`), posted at `Loaded` priority | It has nothing else to focus; the help and the bookmarks post their focus the same way |
| Key handling | A tunnel `KeyDown` handler over `KeyStrokeMapper` and `KeyNotation` | The help's own pattern. `q` still closes while the selectable build text has the focus |
| ViewModel shape | A plain sealed class with a `CloseCommand` over a close callback | `HelpViewModel`'s shape; nothing changes while the dialog is open |
| Where `app.about` is handled | `CommandLineCommands`, beside `app.help` | Both are application dialogs reached from the command line; no module for one line |
| Command title | `"About " + ProductInformation.DisplayName` | The help can never disagree with the dialog about the product's name |
| Image accessibility | `AutomationProperties.Name` bound to the display name | A screen reader names the icon |

## Deviations & follow-ups

- **No deviation from the plan.**
- **Visual check.** I rendered two headless Skia frames of the dialog in the real window, dark and
  light, with a temporary test that was deleted afterwards and not committed. Both show everything in
  place. In the dark theme the build line is faint (tertiary foreground), exactly as in GitClient: it
  is meant for bug reports.
- **Follow-up (optional):** a key binding for About, if users ask. Today it has an ex command only, as
  vim has no key for its `:version`.
- **Follow-up (optional):** `:version` as a vim-familiar alias. I rejected it in planning as a second
  name for one command.
- Line endings: the new files are LF; no CRLF churn.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, there is no `CLAUDE.md`
or `AGENTS.md`, and no prose document lists the ex commands: the in-app help, generated from the
catalog, already shows `:about`.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 775  failed: 0  succeeded: 772  skipped: 3
```

- 28 tests added, all passing: 15 in Core and 13 in Desktop, including the 4 headless real-window
  tests.
- The headless tests check:
  - no title or card icon, one *Close* button that is the default;
  - the name, version, copyright and icon shown;
  - the view takes focus;
  - the card does not scroll at the window's minimum height (400);
  - `q`, Enter and `Esc` each close it and give the keyboard back to the listing.
- The first run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones: symbolic links need a privilege on this
  machine.
