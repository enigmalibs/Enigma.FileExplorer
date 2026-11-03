# FEATURE-5447-PHASE01 — Vendor the Enigma.Msi worker CLI

**Item:** FEATURE-5447 — Windows MSI build script
**Branch:** `feature/feature-5447-phase01-vendor-msi-worker`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The repository now carries Enigma.Msi's headless worker CLI in `packaging/windows/Enigma.Msi.Worker/`.
PHASE02's `buildmsi.ps1` runs it as `Enigma.Msi.Worker.exe build <profile>`.

- **What it is:** the same copy Enigma.GitClient vendors (FEATURE-EAB7-PHASE01), Enigma.Msi `1.7.0`
  (`a7a1bfb`) built against WixSharp_wix4 2.14.2. That is the 20 files of Enigma.Msi's
  `bin/Release/net472/` output, without the PDBs, the API XML and `.wixsharp/` (3.4 MB).
- **`README.md` in the folder:** GitClient's, with the MSI's name changed to
  `artifacts/Enigma.FileExplorer.X.Y.Z.msi`. It gives:
  - the source;
  - the exit codes;
  - the requirements (.NET Framework 4.7.2, the WiX CLI, WiX 7's EULA);
  - the licences (Enigma.Msi, WixSharp, .NET: MIT; WiX 4.0.1 assemblies: MS-RL);
  - how to refresh it.
- **`.gitignore`** ignores `packaging/windows/Enigma.Msi.Worker/.wixsharp/`, with GitClient's comment.
  WixSharp writes that folder beside the worker on its first build.

## Files/modules touched

- Created:
  - `packaging/windows/Enigma.Msi.Worker/` (20 binaries: `Enigma.Msi.Worker.exe`,
    `Enigma.Msi.Worker.exe.config`, `Enigma.Msi.dll`, the `WixSharp*`, `WixToolset.*` and `System.*`
    files, `Microsoft.Bcl.AsyncInterfaces.dll`, `mbanative.dll`)
  - `packaging/windows/Enigma.Msi.Worker/README.md`
  - `docs/done/FEATURE-5447-PHASE01.md`
- Modified:
  - `.gitignore`
  - `docs/roadmap.md`, `docs/plan/FEATURE-5447.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which files to copy from GitClient | The 20 files git tracks there, not its `.wixsharp/` | GitClient's `.wixsharp/` is the ignored output of its own builds |
| README wording | Only the MSI's name changed | Every other line (source, licences, refresh steps) is as true here |

## Deviations & follow-ups

- No deviation from the plan.
- `Enigma.Msi.Worker.exe.config` matches GitClient's working copy (CRLF) byte for byte. It differs from
  Enigma.Msi's build output only in line endings. `.gitattributes` (`* text=auto eol=lf`) stores it as LF,
  as in GitClient, and .NET Framework reads either. Every `.exe`/`.dll` is byte for byte
  (`.gitattributes` marks them binary).
- The `.gitignore` had no newline at its end. The new entry adds one, as in GitClient.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-4ACE.md` (0 warnings; 824 tests, 0 failed).
- `cmp` of each copied file:
  - all 20 identical to `Enigma.GitClient/packaging/windows/Enigma.Msi.Worker/`;
  - the 19 binaries identical to `Enigma.Msi/src/Enigma.Msi.Worker/bin/Release/net472/`;
  - the `.config` identical to that output after removing CRs.
- `git status --porcelain --ignored -uall packaging/windows/`: the 21 files (20 binaries and the README)
  are untracked, none ignored. `git check-ignore -v` matches `.wixsharp/…` to `.gitignore:369`.
- `Enigma.Msi.Worker.exe` with no arguments printed "No arguments were given." and the two usage forms,
  exit code **2**.
- `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.FileExplorer.slnx`: 824 total, 821 passed, 3 skipped, **0 failed**.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale yet. The README changes with the script itself (PHASE02).
