# FEATURE-C732 — Catch unhandled exceptions

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-c732-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every exception that nothing else handles reaches one safety net. The net logs the exception and shows an
error content dialog that says the application is still running, with the error, a count of further errors,
collapsed details and *Copy details*. It is Enigma.GitClient's net, ported as Enigma.TextEditor's
FEATURE-452D ported it, so the Enigma apps fail alike.

## Context & constraints

- **The reference** is Enigma.GitClient, `src/Enigma.GitClient.Desktop/`:
  - `Services/UnhandledErrorPolicy.cs` (source and decision enums, `UnhandledErrorBatch`, the policy);
  - `Services/UnhandledExceptionHooks.cs`, `Services/UnhandledErrorReporter.cs`;
  - `ViewModels/Dialogs/UnhandledErrorViewModel.cs`, `Views/Dialogs/UnhandledErrorView.axaml`;
  - `Services/TaskExtensions.cs` (`Forget`).

  The single-window port to follow is Enigma.TextEditor's, on its branch
  `feature/2026-10-29-uniform-enigma-apps` (`git -C ../Enigma.TextEditor show <branch>:<path>`).
- **What does not carry over:** several windows (`IAppWindows`, `CurrentWindowChanged`), the token reset
  (`ITokenRecovery`), `ArgumentRedactor`, NLog flushing. This app has one `MainWindow`, whose `HostDialog`
  every question shares through `IContentDialogService`, no credentials, and the host's default logging.
- **Today nothing is caught:** `Program.Main` starts the lifetime; `App.OnFrameworkInitializationCompleted`
  posts `_ = viewModel.StartAsync(...)`; 14 `_ =` discards in `src/` lose their faults.
  `CommandDispatcher.ExecuteAsync` already catches command failures and says them where they happen.
- **Conventions:** flat `Services/`, `ViewModels/`, `Views/`; primary constructors; ViewModels are plain
  classes or `ObservableObject`; dialogs close with `q` and Enter as well as `Esc` (the About view's
  tunnel key handler over `KeyStrokeMapper`/`KeyNotation`); headless tests through `HeadlessAvaloniaFixture`
  and `[Collection(HeadlessCollection.Name)]`; each service has a fake in `tests/…/Infrastructure/`.
- The copy goes through `SystemClipboard`/`ISystemClipboard` if it can set text; otherwise through
  `TopLevel.Clipboard` behind a delegate.

## PHASE01 — Error policy and process hooks

**Status:** DONE
**Branch:** `feature/feature-c732-phase01-error-policy-hooks`

### Steps

1. `Services/UnhandledErrorPolicy.cs`: `UnhandledErrorSource` (`Dispatcher`, `UnobservedTask`, `AppDomain`,
   `FireAndForget`, `Startup`), `UnhandledErrorDecision`, `UnhandledErrorBatch`, `UnhandledErrorPolicy`:
   - one batch at a time, later errors merged into it;
   - the same error suppressed for `RepeatWindow` (1 min) after it was closed;
   - a requested cancellation, or any cancellation while shutting down, ignored;
   - `Summarise` (first meaningful message, unwrapping a single-inner `AggregateException` and
     `TargetInvocationException`); `BuildDetails` (type, message, stack, inner exceptions to depth 8; never
     `Data`, never `ToString()`).
2. `Services/IUnhandledErrorReporter.cs`: `Report(Exception, UnhandledErrorSource)`, `ShutdownStarted()`.
3. `Services/UnhandledExceptionHooks.cs` (static): `Install` (AppDomain + `TaskScheduler.UnobservedTaskException`
   with `SetObserved`), `InstallDispatcher` (`Dispatcher.UIThread.UnhandledException`, `Handled = true`),
   `Attach` (replays at most 32 early reports), `Detach`, `Report` (traces while nothing is attached), and an
   `internal` reset seam for tests.
4. `Program.Main`: `UnhandledExceptionHooks.Install()` first.
5. Tests (no platform needed): the policy (ignore, queue, merge, suppress then show again with a fake
   `TimeProvider`, `Summarise`, details with inner exceptions); the batch (`More`, `Details`, `Changed`); the
   hooks (bounded replay on `Attach`, delivery after `Attach`, nothing after `Detach`).

### Acceptance criteria

- The policy and the hooks are covered by unit tests; build with zero warnings; whole suite green.
- `Program.Main` installs the process hooks before anything else.

## PHASE02 — Error dialog and wiring

**Status:** DONE
**Branch:** `feature/feature-c732-phase02-error-dialog`

### Steps

1. `ViewModels/UnhandledErrorViewModel.cs`: `Lead`, `Message`, `HasMore`, `MoreText`, `Details`,
   `CopyDetailsCommand` over an injected copy delegate; follows `batch.Changed`; a close callback for the
   explorer's dialog keys.
2. `Views/UnhandledErrorView.axaml(.cs)`: GitClient's layout (lead, semi-bold message, "N more errors" in
   the secondary brush, a collapsed *Details* expander with a read-only monospace `TextBox` at most 220 high,
   *Copy details*; `MinWidth` 480, `MaxWidth` 560 on the texts; the same Enigma brushes and automation
   names). It takes the keyboard as it opens; `q` and Enter close it, like the About view.
3. `Services/UnhandledErrorReporter.cs`: logs on the reporting thread at Error with the source; decides on
   the UI thread through the policy; shows "Something went wrong" on `IContentDialogService` with *Close*
   (default) and GitClient's warning glyph and `#E8A33D` brush; waits for an open question's `Closed`, then
   retries at Background priority; before the host is registered, the hooks hold the reports; a failing
   dialog is logged and only logging is done from then on.
4. DI: `AddFileExplorerApp` registers `TimeProvider.System` (`TryAdd`) and the reporter (singleton).
5. `App.OnFrameworkInitializationCompleted`: `InstallDispatcher()` first; `Attach(reporter)` once the hosts
   are registered; `desktop.ShutdownRequested` → `ShutdownStarted()`. `OnExit` calls `ShutdownStarted()` and
   `Detach()` before the host is disposed.
6. Tests (headless, real `MainWindow`): a reported error opens the dialog with its title, message and
   *Close*; a second error while open shows "1 more error occurred."; an error raised while another question
   is open waits for it; a cancellation is not shown; *Copy details* copies the details; `q` closes it; the
   composition root resolves the reporter.

### Acceptance criteria

- An exception thrown by a posted callback on the UI thread shows the dialog, and the app keeps running.
- The dialog never replaces a question on screen; a burst of errors opens one dialog.
- Build with zero warnings; whole suite green.

## PHASE03 — Fire-and-forget tasks reach the net

**Status:** DONE
**Branch:** `feature/feature-c732-phase03-fire-and-forget`

### Steps

1. `Services/TaskExtensions.cs` (or the folder the codebase's extension classes use): `Forget(this Task)`;
   observes the task; a fault that is not an ignored cancellation goes to
   `UnhandledExceptionHooks.Report(…, FireAndForget)`; a completed task costs nothing. Plus a way for `App`'s
   posted `StartAsync` to report as `Startup`.
2. Replace every `_ = …Async(…)` / `_ = <task>` discard in `src/` with `.Forget()`.
3. Tests: a faulted task reaches the reporter as `FireAndForget`; completed and cancelled tasks report
   nothing; a task that faults later still reports.

### Acceptance criteria

- `grep -rn "_ = .*Async(" src` finds nothing.
- Build with zero warnings; whole suite green.

## Out of scope

- A dialog for `AppDomain.UnhandledException`: .NET ends the process after the event; it is only logged.
- Restart or "send report" buttons; any change to the existing error messages.
- A file logger.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Design | Port GitClient's net (policy, hooks, reporter, dialog), as TextEditor did | "Uniform" Enigma apps; tested; handles bursts and open questions | A bare `try/catch` in `Main`; a message per hook |
| Multi-window logic, token reset, redaction | Dropped | One window; no credentials; details never read `Data`/`ToString()` | Porting them |
| Dialog keys | `q` and Enter close it too, besides *Close* and `Esc` | Every explorer dialog closes with `q`; keyboard-first app | The library's keys only |
| Discarded tasks | `Forget()` replacing all `_ =` discards (PHASE03) | "Catch **all** unhandled exceptions": a discarded fault is otherwise seen at best when finalised | Leaving them to `UnobservedTaskException` |
| Phasing | 3 phases: logic, dialog, discards | One reviewable commit each; the logic is reviewable without UI | One large dev |
| Folders | Flat `Services/`, `ViewModels/`, `Views/` | This codebase has no `Dialogs/` folders | GitClient's `Dialogs/` folders |
