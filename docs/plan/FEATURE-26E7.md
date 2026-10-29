# FEATURE-26E7 — Release 0.2.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-26e7-release-0-2-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Cut **0.2.0**, the first tagged release of Enigma.FileExplorer, following the `dotnet-release` skill on its
app path (no NuGet package): the version, the first release notes, a real README, `SECURITY.md`, a
dependency re-check and a pre-flight. Merges, the tag and any push are left to the user.

## Context & constraints

- **An app, not a package:** no project has a `PackageId`; pack, push and `PackageReleaseNotes` do not
  apply. The version lives in `Directory.Build.props` (`0.1.0`), shared by Core and Desktop.
- **The version rule is the user's:** `x.y.z` → `x.(y+1).0` — 0.1.0 → **0.2.0**.
- **No tag exists** (`git tag --list` is empty); `RELEASENOTES.md` is empty; `README.md` is a title only;
  there is no `SECURITY.md`. So the notes describe the application as it ships (the items already `DONE` in
  `docs/roadmap.md`), then what this run added.
- **0.2.0 carries** this run's items `DONE` on the run branch before this dev: FEATURE-CC7E, FEATURE-8929,
  FEATURE-5050, FEATURE-C732, FEATURE-6FDB, FEATURE-E5D8. A quarantined item is left out and named.
- **Spec items that needed no change** (recorded here, not written up as changes):
  - splash screen — the app never had one;
  - About dialog — already Enigma.GitClient's (FEATURE-743A-PHASE02); layout, sizes, styles and brushes are
    identical; the explorer adds only its `q`/Enter keys and an automation name;
  - .NET 8 targets — none; every project targets `net10.0` only.
- The Avalonia set moves only as a whole, with Enigma.Avalonia.Desktop.

## Steps

1. Check `git tag --list 0.2.0` is empty.
2. `Directory.Build.props`: `<Version>0.2.0</Version>`.
3. `RELEASENOTES.md`: a dated `0.2.0` section in the `dotnet-release` template's shape, adapted to an app:
   a summary, the features (browsing, vim keys and commands, splits, view modes, file operations, search,
   context menus and drag-drop, session, bookmarks, About), this run's additions (error safety net, new
   icon, `app.ico` in the output, Linux installer, publish scripts, updated libraries), *Dependencies*,
   *Compatibility*, *Version*. Every path quoted is checked against the files.
4. `README.md`: what the app is, a release callout, requirements (.NET 10 runtime), install (Linux
   `install.sh`; Windows `publish.ps1`/`publish.bat`), build and test, licence.
5. `SECURITY.md` from the `dotnet-release` template: `0.2.x` supported; scope fitted to this app (local file
   operations, no network, no credentials).
6. Dependency check (`dotnet list package --outdated`), logged; anything published since FEATURE-CC7E is
   bumped (stable only, coupled set together).
7. Pre-flight in Release and Debug: a clean build with zero warnings; the whole suite green; the assemblies
   carry `0.2.0`.
8. The completion doc prints the tag/merge runbook for the user; nothing is run.

## Acceptance criteria

- In Release and Debug, `dotnet build Enigma.FileExplorer.slnx` has zero warnings and
  `dotnet test --solution Enigma.FileExplorer.slnx` is green.
- The built assemblies carry `0.2.0` (file version `0.2.0.0`, informational `0.2.0+<commit>`).
- `RELEASENOTES.md` has one dated `0.2.0` section covering every merged run item and naming any quarantined
  one; its quoted paths match the files.
- `README.md` documents install, publish and build; `SECURITY.md` supports `0.2.x`.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and none writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour.
- `docs/RELEASE.md` (a runbook file): the runbook is printed in the completion doc.
- The merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 0.2.0 | The user's rule (y+1, z reset to 0) | 0.1.1; 1.0.0 |
| Notes scope | The app as it ships, then this run's additions | First release: 0.1.0 was never tagged, so there is no previous release to diff against | Only this run's items |
| No-op spec items | Recorded in this plan, not as notes entries | Nothing changed for them; notes describe changes | "No change" sections |
| README | A real README (what, install, publish, build) | A release whose README is a title says nothing to a user; `dotnet-release` authors it | Leaving it a title |
| `SECURITY.md` | Added, `0.2.x` only | `dotnet-release` template; latest-minor-only, as the other Enigma apps | Skipping it |
| `docs/RELEASE.md` | Not added | Not asked; the runbook goes in the completion doc | A runbook file like GitClient's |
| MSI profile | None | Not asked; the packaging is the scripts | Generating one |
| Breakdown | One phase, last in the run | A routine release describes what the run merged | Releasing first |
