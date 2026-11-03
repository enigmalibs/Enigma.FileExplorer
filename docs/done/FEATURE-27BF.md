# FEATURE-27BF — Release 0.3.0

**Item:** FEATURE-27BF — Release 0.3.0
**Branch:** `feature/feature-27bf-release-0-3-0`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

0.3.0 is cut, in GitClient 6.14.0's shape for a minor release:

- `Directory.Build.props`: `<Version>0.3.0</Version>`. The user's rule: 0.2.0 → 0.(2+1).0, with z already 0;
- a dated `## 0.3.0 — 2026-11-03` section on top of `RELEASENOTES.md`:
  - a summary list;
  - *The icon without its tile*;
  - *The Windows installer*;
  - *Upgrading from 0.2*;
  - *Dependencies*;
  - *Version*;
- the README callout names 0.3, then sums up 0.2 after it;
- `SECURITY.md`:
  - supports `0.3.x`; `0.2.x` is no longer supported;
  - its scope now includes the Windows installer scripts and what the MSI installs.

The release carries the run's two `DONE` items:

- FEATURE-4ACE: the icon without its dark tile;
- FEATURE-5447: `buildmsi.ps1` / `buildmsi.bat` and the vendored Enigma.Msi worker.

No item was quarantined.

## Files/modules touched

- Modified:
  - `Directory.Build.props`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-27BF.md`: statuses.
- Created: `docs/done/FEATURE-27BF.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.5 | Held at 12.1.4, logged in *Dependencies* | Enigma.Avalonia.Desktop 1.3.0, still the latest, is built against 12.1.4, and the set moves only with it |
| The Enigma.Msi worker in *Dependencies* | Named, as a build tool outside the application | New in the repository, but nothing in the application depends on it |
| `0.2.x` in `SECURITY.md` | Listed as unsupported (`:x:`) | Says plainly that 0.2 gets no fixes, rather than dropping the row |
| `SECURITY.md` scope | One clause added for the Windows installer | 0.3.0 ships a script that runs `git clean -dxf` and an MSI that installs per machine. Both are new surface |
| *Upgrading from 0.2* on Windows | Says this is the first MSI, so a `publish.ps1` folder is not replaced | No earlier MSI exists to upgrade |
| The notes' step table | Shorter cells than GitClient's, padded | The house rule for tables read raw |

## Deviations & follow-ups

- No deviation from the plan.
- **After merging:** run `packaging\windows\buildmsi.ps1` on `main` to build
  `artifacts\Enigma.FileExplorer.0.3.0.msi`, then install it. The run built a 0.2.0 MSI in a scratch clone
  only (FEATURE-5447-PHASE02) and installed nothing.
- Merging into `develop` and `main`, tagging `0.3.0` and pushing are the user's (runbook below). Nothing
  here ran them.
- Pre-existing: line 173 of `RELEASENOTES.md` (0.2.0's section) is 121 characters, one more than the
  file's width. It was left as is.

## Build/test evidence

- `git tag --list 0.3.0`: empty (`0.2.0` is the only tag).
- `dotnet list Enigma.FileExplorer.slnx package --outdated`, 2026-11-03:
  - only the Avalonia set has updates: Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter,
    Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia, 12.1.4 → 12.1.5. Held, as above;
  - "no updates" for Core and its tests; Enigma.Avalonia.Desktop 1.3.0 is the latest.
- `git diff d8ed05b HEAD -G CurrentSchemaVersion`: empty. `AppSettings.CurrentSchemaVersion` is still 1,
  as the notes say. Under `src/`, the run changed only `Assets/app.ico` and `Assets/app-256.png`.
- Every path the notes quote exists: `buildmsi.ps1`, `buildmsi.bat`, `publish.ps1`, `Enigma.Msi.Worker/`,
  `packaging/linux/icons`, `install.sh` and `WindowsInstallerIdentityTests`. The three colours are the
  SVG master's.
- Baseline: see `docs/done/FEATURE-4ACE.md`.
- **Release:**
  - `dotnet build Enigma.FileExplorer.slnx -c Release`: **0 warnings, 0 errors**;
  - `dotnet test --solution … -c Release`: 827 total, 824 passed, 3 skipped, **0 failed**.
- **Debug:**
  - `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**;
  - `dotnet test --solution Enigma.FileExplorer.slnx`: 827 total, 824 passed, 3 skipped, **0 failed**.
- The Release `Enigma.FileExplorer.Core.dll` and `Enigma.FileExplorer.Desktop.dll` carry file version
  `0.3.0.0`, product version `0.3.0+eac0eb621098c62d58a719000953697be2c62e49`.
- Nothing in this dev ran `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or installed an MSI.
- Fix budget: 0 cycles used.

## Documentation sweep

- The release documents are this dev's own edits (above). Nothing else became stale: the README's
  *Windows installer* section (FEATURE-5447) gives the MSI's name pattern, not its version.

## Runbook — printed, not run

```bash
# 1. Pre-flight (done in this dev; repeat after merging)
dotnet build Enigma.FileExplorer.slnx -c Release
dotnet test --solution Enigma.FileExplorer.slnx -c Release

# 2. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-11-03-icons-msi-release
git switch main && git merge --no-ff develop

# 3. Tag: bare X.Y.Z, as 0.2.0
git tag 0.3.0
git push origin main develop 0.3.0

# 4. Build the Windows installer on main (runs git clean -dxf: commit or move new files first)
.\packaging\windows\buildmsi.ps1        # → artifacts\Enigma.FileExplorer.0.3.0.msi
```
