# FEATURE-3EE0 — Release 0.4.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-3ee0-release-0-4-0`
**Run:** feature/2026-11-04-final3-icons-release

## Objective

Cut **0.4.0**, using FEATURE-27BF (0.3.0) as the template and following the `dotnet-release` skill's app
path (no NuGet package):

- the version, including the one the Windows installer build (`buildmsi`) uses;
- the release notes for what 0.4.0 adds beyond 0.3.0;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a pre-flight.

## Context & constraints

- **An app, not a package.** No project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not
  apply. The version lives in `Directory.Build.props` (`0.3.0`), shared by Core and Desktop. Tags are bare
  `X.Y.Z` (`0.2.0` and `0.3.0` exist).
- **The version rule is the user's:** `x.y.z` → `x.(y+1).z`. Here `z` is 0, so 0.3.0 → **0.4.0**.
- **`buildmsi` holds no version literal.** `packaging/windows/buildmsi.ps1` reads `<Version>` from
  `Directory.Build.props` and names the MSI, its profile and the Windows Installer `version` from it.
  `buildmsi.bat` only runs the `.ps1`. Every sibling Enigma app's `buildmsi.ps1` works the same way, and
  the 0.3.0 notes say "nothing about the installer is edited for a release". Updating
  `Directory.Build.props` is therefore what updates the version that `buildmsi` builds.
- **0.4.0 carries** this run's items that are `DONE` on the run branch before this dev starts:
  - FEATURE-16CA: the icon set that fills the frame.

  An item that ended quarantined is left out of the notes and named in the completion doc.
- `RELEASENOTES.md` opens with `## 0.3.0 — 2026-11-03`. The new section goes on top, in the same shape:
  - a summary list;
  - one `###` section per change;
  - *Upgrading from 0.3*;
  - *Dependencies*;
  - *Version*.
- `SECURITY.md` supports only the latest minor line (`0.3.x` today; `0.2.x` is listed as unsupported).
- The Avalonia set moves only as a whole, and only together with what Enigma.Avalonia.Desktop is built
  against (house rule from FEATURE-CC7E and FEATURE-27BF).

## Steps

1. Check that `git tag --list 0.4.0` is empty.
2. `Directory.Build.props`: `<Version>0.4.0</Version>`.
3. Check what `buildmsi.ps1` would build: run its version read (the `Directory.Build.props` XPath and the
   `X.Y.Z` check) on its own, outside the script. It must give `0.4.0` and the name
   `Enigma.FileExplorer.0.4.0`. The script itself is never run: it starts with `git clean -dxf`.
4. `RELEASENOTES.md`: a dated `## 0.4.0 — <date>` section on top, covering the icon. Every path the notes
   quote is checked against the files.
5. `README.md`: the callout names 0.4 first, then sums up the earlier releases after it.
6. `SECURITY.md`: `0.4.x` is supported; every earlier line is not.
7. Dependency check (`dotnet list package --outdated`), logged. Non-coupled patch and minor bumps are
   applied. The Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it.
8. Pre-flight in Release and Debug: a clean build with zero warnings, the whole suite green against the
   baseline, and the assemblies carrying `0.4.0`.
9. The completion doc prints the merge/tag/MSI runbook for the user. Nothing in it is run.

## Acceptance criteria

- In Release and Debug, `dotnet build Enigma.FileExplorer.slnx` has zero warnings, and
  `dotnet test --solution Enigma.FileExplorer.slnx` shows no new failure.
- The built assemblies carry 0.4.0: file version `0.4.0.0`, informational `0.4.0+<commit>`.
- `buildmsi.ps1`'s version read gives `0.4.0`, so it would build `artifacts\Enigma.FileExplorer.0.4.0.msi`.
- `RELEASENOTES.md` covers every `DONE` item of this run, and its quoted paths match the files.
- The README callout names 0.4. `SECURITY.md` supports `0.4.x` only.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack`, `dotnet nuget push` or `buildmsi`, or
  installs an MSI.

## Out of scope

- NuGet packaging.
- Building the 0.4.0 MSI for distribution (the user runs `buildmsi` after merging).
- Moving the Avalonia set on its own.
- `docs/RELEASE.md`.
- Merging into `develop` or `main`, and tagging.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 0.4.0 | The user's rule (y+1; z is 0) | 0.3.1; 1.0.0 |
| "Update the new version number in the buildmsi script" | Update `Directory.Build.props`, which `buildmsi.ps1` reads; leave the script unchanged; check its version read gives 0.4.0 | The script has no version literal by design. A literal would be a second source of truth that drifts from the assemblies | Hard-coding `0.4.0` in `buildmsi.ps1`; a `-Version` parameter |
| Checking `buildmsi` | Its version read only, run on its own | The script starts with `git clean -dxf`, which would delete untracked files | Running the whole script in a scratch clone (done for FEATURE-5447; the read is all a release changes) |
| Order in the run | Last | The notes describe the icon, built first | First, as in the draft's order |
| Shape of the notes | 0.3.0's minor-release shape | The Enigma apps' notes share one shape | A single *Changes* list |
| `SECURITY.md` rows | `0.4.x` supported, `< 0.4` not | One row covers every older line, so the table stops growing | Keeping one `:x:` row per old minor |
| A committed MSI profile | None | `buildmsi.ps1` generates the profile per build from the version | `msiProfiles/<App>.0.4.0.msipkg.json` |
| Breakdown | One phase | A routine release (`dotnet-release`) | Splitting the docs from the version |
| Dependencies | Non-coupled patch and minor bumps applied; the Avalonia set held with the library | House rule from earlier releases | Bumping everything |
