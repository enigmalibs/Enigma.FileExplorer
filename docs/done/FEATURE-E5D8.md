# FEATURE-E5D8 — Publish scripts for Windows and Linux

**Branch:** `feature/feature-e5d8-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

One-command publishing, as in Enigma.GitClient (FEATURE-0D4E). Each script resolves the project from its
own location, so it runs from any directory, and exits with `dotnet`'s exit code.

| Script | Runs | Output |
|---|---|---|
| `packaging/windows/publish.ps1` | `dotnet publish <src/Enigma.FileExplorer.Desktop> -c Release -r win-x64 -p:PublishReadyToRun=true` | `src/Enigma.FileExplorer.Desktop/bin/Release/net10.0/win-x64/publish/` |
| `packaging/windows/publish.bat` | the same, for a cmd prompt (no execution policy applies); CRLF | the same |
| `packaging/linux/publish.sh` | `… -c Release -r linux-x64 -p:PublishReadyToRun=true` (`exec`); mode `100755` | `…/linux-x64/publish/` |

The publish is framework-dependent, as the spec's command gives it. `install.sh` remains the
self-contained, one-step way on Linux.

## Files/modules touched

- Created:
  - `packaging/windows/publish.ps1`
  - `packaging/windows/publish.bat`
  - `packaging/linux/publish.sh`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/PackagingScriptsTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-E5D8.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Script text | GitClient's, with this app's names | Uniform across the Enigma apps; already reviewed there |
| Test scope | The publish scripts, plus `install.sh` / `uninstall.sh`'s `APP_EXE` and the desktop entry's `StartupWMClass` | GitClient's `AssemblyNameConsistencyTests` pins all of them. The same concern is "packaging names the assembly", and it costs three small cases |
| Test class name | `PackagingScriptsTests` | FE has no assembly-name test yet; the name says what it pins |

## Deviations & follow-ups

- **Test scope.** The test also covers FEATURE-6FDB's launcher name and window class (see above). No
  other deviation.
- Line endings: `publish.bat` is CRLF in the working tree, as `.gitattributes` asks (`*.bat eol=crlf`).
  Git stores it LF in the index.

## Documentation sweep

Nothing became wrong. FEATURE-26E7's README documents the scripts.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 824, failed 0, succeeded 821, skipped 3
From %TEMP%:  cmd /c publish.bat                 → exit 0, …\win-x64\publish\ (app.ico present)
              powershell -File publish.ps1       → exit 0, same folder
From /tmp:    bash publish.sh (Git Bash)         → exit 0, …/linux-x64/publish/ (launcher + app.ico)
With a stub `dotnet` exiting 3 first on PATH     → publish.sh 3, publish.bat 3, publish.ps1 3
```

- 6 tests added: 3 publish-script cases, 2 for the launcher name, 1 for the window class.
- No fix cycle was used.
