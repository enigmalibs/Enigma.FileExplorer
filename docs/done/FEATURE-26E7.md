# FEATURE-26E7 — Release 0.2.0

**Branch:** `feature/feature-26e7-release-0-2-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

0.2.0 is cut: the first release of Enigma.FileExplorer. 0.1.0 was never tagged. It follows the
`dotnet-release` skill on its app path.

- **`Directory.Build.props`:** `<Version>0.2.0</Version>`. The user's rule, 0.1.0 → 0.(1+1).0.
- **`RELEASENOTES.md`** (was empty) has a dated `## 0.2.0 — 2026-10-29` section in the sibling apps' shape
  (`# Release notes`, `###` per area). It describes the explorer as it ships:
  - browsing, Vim keys and the command line, split groups;
  - view and sort, file operations, search, bookmarks, settings and theme.

  It then covers this run's items:
  - the error safety net;
  - the new icon, `app.ico`, About and packaging;
  - *Dependencies* (every old → new);
  - *Compatibility*;
  - *Version*.
- **`README.md`** (was a title) follows the sibling apps:
  - intro and the what's-new callout;
  - *Features*, *Your data*, *Requirements*;
  - *Build, run, test* with the publish scripts;
  - *Install on Linux*, *Solution layout*, *License*.
- **`SECURITY.md`** (new, from the template) supports `0.2.x`. Its scope covers file operations, search,
  shell open, drag and drop and the clipboard, settings, the error dialog and the packaging scripts.

The spec items that needed no change are recorded in the plan, not in the notes: no splash screen ever,
an About dialog already GitClient's, and no .NET 8 target.

## Files/modules touched

- Modified: `Directory.Build.props`, `README.md`, `RELEASENOTES.md`
- Created: `SECURITY.md`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-26E7.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Notes heading style | `# Release notes` / `## 0.2.0 — date`, as GitClient and TextEditor | The skill's template defers to existing conventions; the sibling apps have one, and this run is about uniformity |
| First-release four-phase shape (skill) | Kept as one phase | It is for a NuGet library: package metadata, guides and pack-verify do not apply to an app with no `PackageId` |
| Third-party license audit | Done, of the runtime packages a publish ships | First-release step 2; installers and publish folders redistribute them |
| MSI profile | Not offered, not generated | Unattended run; not asked; the packaging is the scripts (as in the sibling apps' releases) |
| `docs/RELEASE.md` | Not added, as planned | The runbook is printed below; a runbook file is a follow-up |
| Claims in the notes and README | Checked against the code | e.g. `:sort!` reverses the key's direction (`EntrySort.Reversed`), it does not force descending |

## Third-party license audit (runtime packages in `…/win-x64/publish/*.deps.json`)

| Package(s) | License | Redistribution |
|---|---|---|
| Avalonia 12.1.4 (+ Desktop, Themes.Fluent, Fonts.Inter, Skia, HarfBuzz, Win32, X11, Native, FreeDesktop, AtSpi, Remote.Protocol) | MIT | yes |
| Avalonia.Angle.Windows.Natives 2.1.27548 | BSD-3-Clause (ANGLE) | yes, with notice |
| SkiaSharp 3.119.4, HarfBuzzSharp 8.3.1.3 (+ native assets) | MIT | yes |
| CommunityToolkit.Mvvm 8.4.2, MicroCom.Runtime 0.11.6, Tmds.DBus.Protocol 0.94.1, BouncyCastle.Cryptography 2.6.2 | MIT | yes |
| Microsoft.Extensions.* 10.0.14, System.Diagnostics.EventLog 10.0.14 | MIT | yes |
| Enigma.Avalonia.Desktop 1.3.0, Enigma.Core 1.0.0, Enigma.Icons(.Avalonia, .Phosphor) 1.0.0 | MIT (own) | yes |

Test-only packages (xunit.v3, CodeCoverage, Avalonia.Headless) and AvaloniaUI.DiagnosticsSupport (Debug
only, `PrivateAssets=All` in Release) do not ship. `LICENSE.md` (MIT) is present.

## Runbook — printed, not run

```bash
# 1. Pre-flight (done in this dev; repeat after merging)
dotnet build Enigma.FileExplorer.slnx -c Release
dotnet test --solution Enigma.FileExplorer.slnx -c Release

# 2. Merge the run into develop, then develop into main
git switch develop && git merge --no-ff feature/2026-10-29-uniform-enigma-apps
git switch main && git merge --no-ff develop

# 3. Tag — no tag exists yet, so bare X.Y.Z, as the sibling apps
git tag 0.2.0
git push origin main develop 0.2.0

# 4. Publish binaries for the release
packaging/windows/publish.ps1        # or publish.bat
packaging/linux/publish.sh           # or install.sh to install locally
```

No pack or NuGet push: the explorer is not a package.

## Deviations & follow-ups

- **Follow-up:** `docs/RELEASE.md`, a release runbook file like the sibling apps', if wanted.
- **Follow-up:** enable GitHub private vulnerability reporting, which `SECURITY.md` points to.

## Documentation sweep

This dev is the documentation. No other prose document exists: no `CLAUDE.md`, no `AGENTS.md`, and no
`CHANGELOG.md` (rightly).

## Build/test evidence

```
git tag --list 0.2.0                                    → empty
dotnet list Enigma.FileExplorer.slnx package --outdated → no updates (4 projects)
Release: dotnet build --no-incremental → 0 Warning(s), 0 Error(s); dotnet test → 824 total, 0 failed, 821 passed, 3 skipped
Debug:   dotnet build --no-incremental → 0 Warning(s), 0 Error(s); dotnet test → 824 total, 0 failed, 821 passed, 3 skipped
Enigma.FileExplorer.Desktop.dll / Core.dll → FileVersion 0.2.0.0, ProductVersion 0.2.0+940832d…
```

- Every path the notes and README quote exists (checked).
- The tables are re-padded.
- No fix cycle was used.
