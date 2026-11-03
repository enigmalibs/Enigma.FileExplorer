# FEATURE-27BF — Release 0.3.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-27bf-release-0-3-0`
**Run:** feature/2026-11-03-icons-msi-release

## Objective

Cut **0.3.0**, using FEATURE-26E7 (0.2.0) as the template and following the `dotnet-release` skill's app
path (no NuGet package):

- the version;
- the release notes for what 0.3.0 adds beyond 0.2.0;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a pre-flight.

## Context & constraints

- **An app, not a package.** No project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not
  apply. The version lives in `Directory.Build.props` (`0.2.0`), shared by Core and Desktop. Tags are bare
  `X.Y.Z` (`0.2.0` exists).
- **The version rule is the user's:** `x.y.z` → `x.(y+1).z`, and `z` resets to 0 when it is above 0. Here
  `z` is already 0, so 0.2.0 → **0.3.0**.
- **0.3.0 carries** this run's items that are `DONE` on the run branch before this dev starts:
  - FEATURE-4ACE: the new icon set without the tile;
  - FEATURE-5447: `buildmsi.ps1` / `buildmsi.bat` and the vendored Enigma.Msi worker.
  An item that ended quarantined is left out of the notes and named in the completion doc.
- `RELEASENOTES.md` has one section, `## 0.2.0 — 2026-10-29`. The new section goes on top, in GitClient
  6.14.0's shape for a minor release: a summary list, one `###` section per change, *Upgrading from 0.2*,
  *Dependencies*, *Version*.
- `SECURITY.md` supports only the latest minor line (`0.2.x` today).
- The Avalonia set moves only as a whole, and only together with what Enigma.Avalonia.Desktop is built
  against (house rule from FEATURE-CC7E and GitClient's 6.14.0).

## Steps

1. Check that `git tag --list 0.3.0` is empty.
2. `Directory.Build.props`: `<Version>0.3.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 0.3.0 — <date>` section on top, covering the icon and the Windows
   installer. Every path the notes quote is checked against the files.
4. `README.md`: the callout names 0.3 first, then sums up 0.2 after it.
5. `SECURITY.md`: `0.3.x` is supported; `0.2.x` no longer is.
6. Dependency check (`dotnet list package --outdated`), logged. Non-coupled patch and minor bumps are
   applied. The Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it.
7. Pre-flight in Release and Debug: a clean build with zero warnings, the whole suite green against the
   baseline, and the assemblies carrying `0.3.0`.
8. The completion doc prints the merge/tag/MSI runbook for the user. Nothing in it is run.

## Acceptance criteria

- In Release and Debug, `dotnet build Enigma.FileExplorer.slnx` has zero warnings, and
  `dotnet test --solution Enigma.FileExplorer.slnx` shows no new failure.
- The built assemblies carry 0.3.0: file version `0.3.0.0`, informational `0.3.0+<commit>`.
- `RELEASENOTES.md` covers every `DONE` item of this run, and its quoted paths match the files.
- The README callout names 0.3. `SECURITY.md` supports `0.3.x`.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or installs the
  MSI.

## Out of scope

- NuGet packaging.
- Building the 0.3.0 MSI for distribution (the user runs `buildmsi` after merging).
- Moving the Avalonia set on its own.
- `docs/RELEASE.md`.
- Merging into `develop` or `main`, and tagging.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 0.3.0 | The user's rule (y+1; z is already 0) | 0.2.1; 1.0.0 |
| Order in the run | Last | The notes describe the icon and the installer, built first | First, as in the draft's order |
| Shape of the notes | GitClient 6.14.0's minor-release shape | The Enigma apps' notes share one shape (FEATURE-26E7) | A single *Changes* list |
| A committed MSI profile | None | FEATURE-5447's script generates the profile per build from the version | `msiProfiles/<App>.0.3.0.msipkg.json` |
| Breakdown | One phase | A routine release (`dotnet-release`) | Splitting the docs from the version |
| Dependencies | Non-coupled patch and minor bumps applied; the Avalonia set held with the library | House rule from earlier releases | Bumping everything |
