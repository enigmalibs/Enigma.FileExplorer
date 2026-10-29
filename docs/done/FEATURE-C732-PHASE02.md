# FEATURE-C732-PHASE02 — Error dialog and wiring

**Item:** FEATURE-C732 — Catch unhandled exceptions
**Branch:** `feature/feature-c732-phase02-error-dialog`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

The safety net now shows what it catches. An exception that nothing handled is logged at Error with its
source. Then the "Something went wrong" dialog opens in the window's content dialog, with Enigma.GitClient's
layout, warning glyph and `#E8A33D` brush. The dialog shows:

- the lead sentence ("…the application is still running…");
- the message;
- "N more errors occurred." when more errors came;
- collapsed *Details*: a monospace, read-only box at most 220 high, and *Copy details*;
- one *Close* button.

**How it behaves:**

- **One at a time.** A burst of errors opens one dialog. It never replaces a question on screen: it waits
  for that question's `Closed`, then shows.
- **Before the window.** Errors caught before the window hands over its host are shown once it does.
- **Not shown:** cancellations someone asked for, and AppDomain errors (the process ends; they are logged
  only).
- **Keys.** The view takes the keyboard as it opens. `q` and Enter close it, like the explorer's other
  dialogs, and `Esc` is the dialog's own. Enter on a focused button (the expander header, *Copy details*)
  stays that button's.
- **Copy details** goes through the app's `ISystemClipboard`.

**Wiring:**

- `App` installs the dispatcher hook first.
- `RegisterHosts` hands the reporter the dialog host.
- The hooks are attached to the reporter right after.
- `ShutdownRequested` and `OnExit` tell it the app is closing. `OnExit` also detaches it before the host is
  disposed.

## Files/modules touched

- Created:
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.FileExplorer.Desktop/ViewModels/UnhandledErrorViewModel.cs`
  - `src/Enigma.FileExplorer.Desktop/Views/UnhandledErrorView.axaml` / `.axaml.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Views/UnhandledErrorDialogTests.cs`
- Modified:
  - `src/Enigma.FileExplorer.Desktop/App.axaml.cs`
  - `src/Enigma.FileExplorer.Desktop/DependencyInjection/ServiceCollectionExtensions.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/CompositionRootTests.cs`
  - `docs/roadmap.md`, `docs/plan/FEATURE-C732.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Copying | Through `ISystemClipboard` (injected), not `TopLevel.Clipboard` | The app already has the service, attached to the window; tests use its fake |
| AppDomain errors | Logged, no flush | The host's default logging writes synchronously; no NLog buffer here (TextEditor flushes NLog) |
| Monospace family | The app-wide `MonospaceFontFamily` | FE already defines it in `App.axaml`; the toolbar button style stays the view's own |
| Enter on a button | Left to the button | Otherwise Enter on *Copy details* or the *Details* header would close the dialog instead |
| `_ = PresentAsync()` / `_ = HideAsync()` | Discards for now | `Forget()` comes in PHASE03, which replaces every discard in `src/` |
| Dialog tests | Through the app's own container and `App.RegisterHosts` | The real wiring, as `AboutViewTests` |

## Deviations & follow-ups

- **Plan step 6, "a reported error from a posted callback".** Done end to end: a test installs the
  dispatcher hook, attaches the app's reporter, posts a throwing callback, and the dialog opens while the
  dispatcher carries on.
- No other deviation.

## Documentation sweep

Nothing became wrong. No prose document describes error handling.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 812, failed 0, succeeded 809, skipped 3
  (UnhandledErrorDialogTests + CompositionRootTests: 24 passed)
Enigma.FileExplorer.Desktop.exe started for 6 s  → window up ("jcl — Enigma.FileExplorer"), no crash
```

- 13 tests added: 11 dialog tests (the `q`/Enter/`Esc` theory counts 3) and 2 composition-root tests.
- The dialog tests cover: opened with title, icon and *Close*; a dispatcher callback that throws shows it;
  a burst is counted; it waits for an open question; an error before the host; cancellation not shown;
  AppDomain not shown; *Copy details*; each closing key gives the keyboard back to the listing.
- No fix cycle was used.
