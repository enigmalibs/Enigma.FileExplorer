# FEATURE-C732-PHASE01 — Error policy and process hooks

**Item:** FEATURE-C732 — Catch unhandled exceptions
**Branch:** `feature/feature-c732-phase01-error-policy-hooks`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

The logic of the unhandled-exception safety net. It is Enigma.GitClient's, taken from Enigma.TextEditor's
single-window port (FEATURE-452D) so the Enigma apps fail alike. Nothing is shown yet; PHASE02 adds the
dialog and the reporter.

- **`UnhandledErrorPolicy`** decides what each exception does:
  - one batch at a time, with later errors merged into it;
  - the same failure is not shown again for `RepeatWindow` (1 min) after it was closed;
  - a requested cancellation, or any cancellation while closing, is ignored; a timeout is not;
  - the same exception instance offered twice counts once.

  `Summarise` unwraps single-inner aggregates and invocation wrappers. `BuildDetails` writes the type,
  message and stack of every inner exception to depth 8, never `Data` and never `ToString()`.
- **`UnhandledErrorBatch`** holds the batch: `First`, `More`, `Details` ("Error n of m") and `Changed`.
- **`UnhandledExceptionHooks`** (static):
  - `Install` hooks AppDomain and unobserved tasks, the latter with `SetObserved`;
  - `InstallDispatcher` hooks the UI dispatcher (`Handled = true`);
  - `Attach` replays at most 32 early reports, and `Detach` takes the reporter away again;
  - `Report` is the one way in for code outside the container;
  - `ResetForTests` is internal, for the tests only.
- **`Program.Main`** calls `UnhandledExceptionHooks.Install()` before the lifetime starts.

## Files/modules touched

- Created:
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorSource.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorDecision.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorBatch.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledErrorPolicy.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/IUnhandledErrorReporter.cs`
  - `src/Enigma.FileExplorer.Desktop/Services/UnhandledExceptionHooks.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Infrastructure/ManualTimeProvider.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Infrastructure/RecordingErrorReporter.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Services/UnhandledErrorPolicyTests.cs`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Services/UnhandledExceptionHooksTests.cs`
- Modified: `src/Enigma.FileExplorer.Desktop/Program.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-C732.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Port source | TextEditor's files, verbatim except namespace, doc wording ("Initialises", as FE) and the reporter's remark | Already adapted to one window; keeps the apps identical where they can be |
| Enum and batch files | One type per file (`UnhandledErrorSource`, `UnhandledErrorDecision`, `UnhandledErrorBatch`) | FE's one-type-per-file layout |
| Hooks tests | In the headless collection | The hooks' state is the process's; the dispatcher hook must never be live during another UI test |

## Deviations & follow-ups

- No deviation from the plan.
- The process events (`AppDomain`, `UnobservedTaskException`) are not raised in tests: an AppDomain
  exception ends the test host, and the host's own unobserved tasks would pollute the reports. Their
  handlers are three lines each over `Report`, which is tested.

## Documentation sweep

Nothing became wrong. No prose document describes error handling.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 799, failed 0, succeeded 796, skipped 3
  (UnhandledErrorPolicyTests + UnhandledExceptionHooksTests: 23 passed)
```

- 23 tests added: 18 for the policy and batch, 5 for the hooks.
- The 3 skips are the known symbolic-link tests.
- No fix cycle was used.
