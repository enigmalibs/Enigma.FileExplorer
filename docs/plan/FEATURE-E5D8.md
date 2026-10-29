# FEATURE-E5D8 — Publish scripts for Windows and Linux

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-e5d8-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

One-command publish scripts on both platforms, part of the uniform baseline for every Enigma app:

- Windows — `packaging/windows/publish.ps1` and `publish.bat`, both running
  `dotnet publish <Desktop project> -c Release -r win-x64 -p:PublishReadyToRun=true`;
- Linux — `packaging/linux/publish.sh`, next to `install.sh` / `uninstall.sh`, running
  `dotnet publish <Desktop project> -c Release -r linux-x64 -p:PublishReadyToRun=true`.

## Context & constraints

- **The reference** is Enigma.GitClient's FEATURE-0D4E: `packaging/windows/publish.ps1`, `publish.bat`,
  `packaging/linux/publish.sh`. Each resolves the project from its own location, so it runs from anywhere,
  and exits with `dotnet`'s exit code; each has a header comment naming the command and the output folder.
- The Desktop project is `src/Enigma.FileExplorer.Desktop`; output lands in
  `src/Enigma.FileExplorer.Desktop/bin/Release/net10.0/<rid>/publish/`.
- `.gitattributes` keeps `*.bat` CRLF and `*.sh` LF. `publish.sh` is staged `100755`.
- FEATURE-6FDB creates `packaging/linux/` and the repository-root test helper.

## Steps

1. `packaging/windows/publish.ps1`: GitClient's script for this project.
2. `packaging/windows/publish.bat`: `@echo off`, `setlocal`, the project from `%~dp0`, the command,
   `exit /b %ERRORLEVEL%`; CRLF.
3. `packaging/linux/publish.sh`: `#!/usr/bin/env bash`, `set -euo pipefail`, the project from the
   script's directory, `exec dotnet publish …`; staged `100755`.
4. Test: every publish script names `src/Enigma.FileExplorer.Desktop` and its runtime identifier with
   `-c Release` and `-p:PublishReadyToRun=true`.
5. Verify: run `publish.bat` and `publish.ps1` (win-x64), and `publish.sh` through Git Bash (linux-x64);
   each exits 0 and leaves its `publish/` folder, which holds `app.ico` (FEATURE-5050).

## Acceptance criteria

- The three scripts run from any directory and exit with `dotnet`'s exit code.
- `publish.sh` is `100755` in the index; `publish.bat` is CRLF in the working tree.
- The build has zero warnings; the whole suite is green.

## Out of scope

- Self-contained publishing, other runtime identifiers, archives (`install.sh` covers those on Linux).
- An MSI.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where | `packaging/windows/`, `packaging/linux/` | GitClient's layout; next to each platform's other scripts | A root `publish/` folder |
| `publish.bat` | Calls `dotnet` itself | No dependency on PowerShell's execution policy | Wrapping `publish.ps1` |
| Arguments | None accepted | The spec fixes the command | Passing `%*` / `"$@"` through |
| Self-contained | Not set (framework-dependent, the default for a RID publish in .NET 10) | The spec's command, verbatim | Adding `--self-contained` |
| Tests | One text test + running all three scripts | Pins the command; running is the real proof | No test |
