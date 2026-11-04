# FEATURE-3EE0 — Release 0.4.0

**Item:** FEATURE-3EE0 — Release 0.4.0
**Branch:** `feature/feature-3ee0-release-0-4-0`
**Run:** feature/2026-11-04-final3-icons-release

## Summary

0.4.0 is cut, in 0.3.0's shape for a minor release:

- `Directory.Build.props`: `<Version>0.4.0</Version>`, by the user's rule (0.3.0 → 0.(3+1).0, with z already 0).
  This is the version `buildmsi` builds: `buildmsi.ps1` has no version literal and reads this element, so the
  script itself did not change;
- a dated `## 0.4.0 — 2026-11-04` section on top of `RELEASENOTES.md`:
  - a summary;
  - *The icon fills its frame*;
  - *Upgrading from 0.3*;
  - *Dependencies*;
  - *Version*;
- the README callout names 0.4, then sums up 0.3 and 0.2 after it;
- `SECURITY.md` supports `0.4.x`; `< 0.4` is not supported.

The release carries the run's one other item, which is `DONE`: FEATURE-16CA, the icon set that fills its
frame. No item was quarantined.

## Files/modules touched

- Modified:
  - `Directory.Build.props`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-3EE0.md`: statuses.
- Created: `docs/done/FEATURE-3EE0.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.5 | Held at 12.1.4, logged in *Dependencies* | Enigma.Avalonia.Desktop 1.3.0, still the latest, depends on Avalonia 12.1.4 (its nuspec), and the set moves only with it |
| The summary's form | One sentence, not a list | The release has a single change |
| The icon's size in the notes | "224 of 256 pixels, against 188", from the SVG master | The export scales 0.3.0's drawing (x 36 to 224) by 1.1915 to span 16 to 240 |
| The MSI in *Upgrading from 0.3* | "It replaces 0.3.0", with the icon-cache caveat | The upgrade code is unchanged. The cache note is the one FEATURE-16CA recorded |

## Deviations & follow-ups

- **The draft asked to update the version in the buildmsi script.** The script has no version to update. It reads
  `<Version>` from `Directory.Build.props`, as every sibling Enigma app's `buildmsi.ps1` does, and the 0.3.0 notes
  promise that nothing about the installer is edited for a release. Bumping `Directory.Build.props` is therefore the
  update. The script's own version-read lines, run in isolation in Windows PowerShell 5.1, give `0.4.0` and the name
  `Enigma.FileExplorer.0.4.0`. A hard-coded literal was rejected: it would be a second source of truth.
- **After merging:** run `packaging\windows\buildmsi.ps1` on `main` to build
  `artifacts\Enigma.FileExplorer.0.4.0.msi`, then install it. Nothing in this run built or installed an MSI.
- Merging into `develop` and `main`, tagging `0.4.0` and pushing are the user's (runbook below). Nothing here ran
  them.
- Pre-existing: line 209 of `RELEASENOTES.md` (0.2.0's section, line 173 before this dev) is 121 characters, one
  more than the file's width. It was left as is.

## Build/test evidence

- `git tag --list 0.4.0`: empty (`0.2.0` and `0.3.0` exist).
- `buildmsi.ps1`'s version read (its XPath and `X.Y.Z` check), run on its own: `version=0.4.0`,
  `name=Enigma.FileExplorer.0.4.0`.
- `dotnet list Enigma.FileExplorer.slnx package --outdated`, 2026-11-04:
  - only the Avalonia set has updates: Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent,
    Avalonia.Headless and Avalonia.Skia, 12.1.4 → 12.1.5. Held, as above;
  - "no updates" for Core and its tests. Enigma.Avalonia.Desktop 1.3.0 is not listed, so it is the latest.
- `AppSettings.CurrentSchemaVersion` is still 1, as the notes say. Under `src/`, the run changed only
  `Assets/app.ico` and `Assets/app-256.png`.
- Every path the notes quote exists: `buildmsi.ps1`, `packaging/linux/icons` and `install.sh`. The three colours
  are the SVG master's. The new notes' lines and the README callout are at most 120 characters.
- Baseline: see `docs/done/FEATURE-16CA.md`.
- **Release:**
  - `dotnet build Enigma.FileExplorer.slnx -c Release`: **0 warnings, 0 errors**;
  - `dotnet test --solution Enigma.FileExplorer.slnx -c Release`: 827 total, 824 passed, 3 skipped, **0 failed**.
- **Debug:**
  - `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**;
  - `dotnet test --solution Enigma.FileExplorer.slnx`: 827 total, 824 passed, 3 skipped, **0 failed**.
- The Release `Enigma.FileExplorer.Core.dll` and `Enigma.FileExplorer.Desktop.dll` carry file version `0.4.0.0`,
  product version `0.4.0+5b3988bc0718a515f2668e8faf19f514d9846e4b`.
- Nothing in this dev ran `git tag`, `git push`, `dotnet pack`, `dotnet nuget push` or `buildmsi`, or installed an
  MSI.
- Fix budget: 0 cycles used.

## Documentation sweep

- The release documents are this dev's own edits (above). Nothing else became stale. No other file names 0.3. The
  README's *Windows installer* section gives the MSI's name pattern (`X.Y.Z`), not a version.

## Runbook — printed, not run

```bash
# 1. Pre-flight (done in this dev; repeat after merging)
dotnet build Enigma.FileExplorer.slnx -c Release
dotnet test --solution Enigma.FileExplorer.slnx -c Release

# 2. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-11-04-final3-icons-release
git switch main && git merge --no-ff develop

# 3. Tag: bare X.Y.Z, as 0.2.0 and 0.3.0
git tag 0.4.0
git push origin main develop 0.4.0

# 4. Build the Windows installer on main (runs git clean -dxf: commit or move new files first)
.\packaging\windows\buildmsi.ps1        # → artifacts\Enigma.FileExplorer.0.4.0.msi
```
