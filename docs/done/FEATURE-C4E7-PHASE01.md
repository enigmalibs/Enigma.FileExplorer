# FEATURE-C4E7-PHASE01 — Solution scaffold & app shell

**Item:** FEATURE-C4E7 — Foundation & directory browsing
**Branch:** `feature/feature-c4e7-phase01-scaffold`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

Bootstrapped the solution from the house templates and the sibling apps' proven wiring: git
hygiene files, the full C# `.editorconfig`, solution-wide build defaults, Central Package Management
with the Avalonia 12.1.1 / Enigma.Avalonia.Desktop 1.2.0 coupled set, the SDK pin with the Microsoft
Testing Platform runner, the MIT licence, the `.slnx`, and four projects. The Desktop app starts an
IHost (started, never run), merges the Enigma theme, registers the six Enigma services and hands the
three host controls over before the window is shown. Each test project carries its first tests: a
Core container check, a Desktop composition-root validation and a headless open of the real main
window.

## Files / modules touched

**Created**

- `.gitignore`, `.gitattributes` (git-repo-hygiene), `.editorconfig` (dotnet-solution-config)
- `Directory.Build.props` (Josué Clément, 2026, version 0.1.0), `Directory.Packages.props`, `global.json`
- `LICENSE.md` (MIT), `RELEASENOTES.md` (empty placeholder)
- `Enigma.FileExplorer.slnx`
- `src/Enigma.FileExplorer.Core/` — csproj, `DependencyInjection/ServiceCollectionExtensions.cs`
- `src/Enigma.FileExplorer.Desktop/` — csproj, `app.manifest`, `Program.cs`, `App.axaml(.cs)`,
  `DependencyInjection/ServiceCollectionExtensions.cs`, `ViewModels/MainWindowViewModel.cs`,
  `Views/MainWindow.axaml(.cs)`, `Themes/Styles.axaml`
- `tests/Enigma.FileExplorer.Core.UnitTests/` — csproj, `DependencyInjection/ServiceCollectionExtensionsTests.cs`
- `tests/Enigma.FileExplorer.Desktop.UnitTests/` — csproj, `Infrastructure/HeadlessAvaloniaFixture.cs`,
  `CompositionRootTests.cs`, `MainWindowTests.cs`

**Modified**

- `docs/roadmap.md`, `docs/plan/FEATURE-C4E7.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Core's package references | `Microsoft.Extensions.DependencyInjection.Abstractions` + `Logging.Abstractions` only | A library needs the abstractions, not the host (Launchpad's choice); the Desktop app brings `Microsoft.Extensions.Hosting` |
| `Microsoft.Extensions.DependencyInjection` in CPM | Declared for the Core test project | The Core tests compose a real container with `ValidateOnBuild` |
| Headless fixture | Copied from Enigma.Launchpad (dedicated UI thread, dispatcher pumping) | Proven on this machine and on Linux; Avalonia initialises once per process |
| `MainWindow` lifetime | Singleton | One main window per process; resolved once at startup (Launchpad precedent) |
| Fix-budget accounting | Cycles counted from the first full build + test run of the dev | Compile iterations while writing the code are implementation, not quality-gate fixes |

## Deviations & follow-ups

- **No application icon** (`<ApplicationIcon>`, `Icon=`): icon art is user-owned per the house
  convention, and a reference to a missing `.ico` fails the build. Follow-up: add `Assets/app.ico`
  and wire both places.
- `README.md` kept as is (title only); filling it is release-time work (dotnet-release).
- **Line endings (recommendation only):** the repository's global `core.autocrlf` converted the
  planning commit's markdown on checkout; `.gitattributes` now declares `* text=auto eol=lf`. If CRLF↔LF
  churn shows up, `git add --renormalize .` in a dedicated commit is the fix. No action taken.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  Test run summary: Passed!
  total: 12  failed: 0  succeeded: 12  skipped: 0
```

The built `Enigma.FileExplorer.Desktop.exe` was launched and showed its window titled
"Enigma.FileExplorer" (then closed). Warning count read explicitly (0), covering `AVLN*` warnings.
