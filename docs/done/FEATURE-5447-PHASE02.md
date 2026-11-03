# FEATURE-5447-PHASE02 — The buildmsi script

**Item:** FEATURE-5447 — Windows MSI build script
**Branch:** `feature/feature-5447-phase02-buildmsi-script`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

`packaging/windows/buildmsi.ps1` builds the Windows installer in one command, from any directory. It is
Enigma.GitClient's script, with only the names, the constants and the help text changed:

1. It checks that `git`, `dotnet`, `wix` and the vendored worker are there, **before** anything is
   cleaned.
2. It reads the version from `Directory.Build.props` and checks it is `X.Y.Z`.
3. `git -C <root> clean -dxf`.
4. `publish.ps1` (`-c Release -r win-x64 -p:PublishReadyToRun=true`). Then it checks that the exe and
   `app.ico` are in the publish folder.
5. It writes `artifacts/Enigma.FileExplorer.X.Y.Z.msipkg.json`, an Enigma.Msi profile:
   - absolute paths, a new `productId` each build;
   - upgrade code **`6b9404a4-3818-4205-b6c2-bc3befe8c3eb`**, the product line's own;
   - "Enigma File Explorer", "Josué Clément", `PerMachine`, `%ProgramFiles%\Enigma File Explorer`;
   - `compression` `None`;
   - `app.ico` as the Control Panel icon and both shortcuts' icon;
   - shortcuts in `%Desktop%` and `%ProgramMenu%`.
6. It runs `Enigma.Msi.Worker.exe build` on that profile into `artifacts/Enigma.FileExplorer.X.Y.Z.msi`.

Every failure prints `buildmsi: <reason>` and exits non-zero, with the failing tool's exit code when there
is one. `buildmsi.bat` runs it from `cmd` with `-ExecutionPolicy Bypass` and passes the exit code on.

Two tests tie the script to the product:
- the new `WindowsInstallerIdentityTests` pins the upgrade code and `$ProductName` =
  `ProductInformation.DisplayName`;
- `PackagingScriptsTests` pins `$Assembly` = the assembly's name.

## Files/modules touched

- Created:
  - `packaging/windows/buildmsi.ps1`
  - `packaging/windows/buildmsi.bat`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/WindowsInstallerIdentityTests.cs`
  - `docs/done/FEATURE-5447-PHASE02.md`
- Modified:
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/PackagingScriptsTests.cs`
  - `README.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-5447.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Deriving the scripts | `sed` on GitClient's files: upgrade code, `Enigma Git Client` → `Enigma File Explorer`, `Enigma.GitClient` → `Enigma.FileExplorer` | Keeps every other line identical, so the two can be diffed |
| `buildmsi.bat`'s first comment | Rewrapped by one word ("cmd") | The longer name pushed it past the file's width |
| Where the tests live | Desktop tests, a new class plus one fact in `PackagingScriptsTests` | FE runs the Desktop suite here, and only that project has `RepositoryRoot` |
| Test names | `Subject_Behaviour`, FE's convention | GitClient's names follow its own convention |
| Reading the MSI | The vendored `WixToolset.Dtf.WindowsInstaller.dll` from a scratch script | The COM automation's late binding refused the record index |

## Deviations & follow-ups

- **Follow-up: the MSI is 174 MB because of native PDBs.** The publish folder carries `libSkiaSharp.pdb`
  (84 MB) and `libHarfBuzzSharp.pdb` (21 MB) from the SkiaSharp/HarfBuzz packages, as in GitClient.
  Leaving them out would roughly halve the MSI, but it changes the publish (FEATURE-E5D8), so it is not
  done here.
- **A 32-bit package.** Enigma.Msi has no platform setting, so the template is `Intel;1033`, and
  `%ProgramFiles%` is `Program Files (x86)` on 64-bit Windows. GitClient's installs land there too.
- **WixSharp's "source base directory"** is the current directory: it printed this repository's path. It
  resolves relative paths only, and the profile's are absolute. `git status --ignored` in this
  repository showed no new file after the build.
- **The first MSI has nothing to upgrade.** No Enigma File Explorer MSI was ever installed here. The
  `Upgrade` table still lists the code, so 0.3.0 and later replace whatever an earlier MSI installed.
- **Not run, by design:** installing, launching or uninstalling the MSI.
- Line endings: `buildmsi.bat` is CRLF, as `.gitattributes` asks; the `.ps1` is LF and ASCII.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-4ACE.md`.
- `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.FileExplorer.slnx`: **827** total (the baseline's 824 plus the 3 new
  facts), 824 passed, 3 skipped, **0 failed**.
  - `WindowsInstallerIdentityTests` on its own: 2/2 passed.
  - `PackagingScriptsTests` on its own: 7/7 passed.
- `diff` against GitClient's `buildmsi.ps1`: only the help text's names, the comment's product name,
  `$UpgradeCode`, `$ProductName`, `$Assembly` and `$name`. `buildmsi.bat`: only lines 2–3.
- **End to end, in a scratch clone** of this branch with the dev's tree committed there (the run may not
  `git clean` the user's repository):
  - **No `wix` on the `PATH`:** `buildmsi: wix is not on the PATH: dotnet tool install --global wix.`,
    exit code 1. An untracked canary file was still there, so nothing was cleaned.
  - **`buildmsi.bat`:**
    - git removed the canary; the publish ran; the worker built the MSI;
    - exit code **0**, in **20 s**;
    - `artifacts/` holds `Enigma.FileExplorer.0.2.0.msi` (174,289,211 bytes, 77 files) and its
      `.msipkg.json` (`"compression": "None"`);
    - `git status --porcelain` was empty afterwards. Only `artifacts/`, `bin/`, `obj/` and
      `Enigma.Msi.Worker/.wixsharp/` appear, as ignored;
    - the publish folder's `app.ico` is FEATURE-4ACE's new icon.
  - **The MSI's tables**, read through `WixToolset.Dtf.WindowsInstaller`:
    - `ProductName` Enigma File Explorer, `Manufacturer` Josué Clément, `ProductVersion` 0.2.0 (the
      `Directory.Build.props` version until FEATURE-27BF);
    - `UpgradeCode` `{6B9404A4-3818-4205-B6C2-BC3BEFE8C3EB}`, `ALLUSERS` 1, `ARPPRODUCTICON`
      `app_icon.ico`;
    - `INSTALLDIR` "Enigma File Explorer" under `ProgramFilesFolder`;
    - two "Enigma File Explorer.lnk" shortcuts (`DesktopFolder`, `ProgramMenuFolder`) on
      `[INSTALLDIR]\Enigma.FileExplorer.Desktop.exe`, icon `IconFile1_app.ico`;
    - `Upgrade`: `VersionMax 0.2.0` (attributes 1) and `VersionMin 0.2.0` (attributes 2);
    - template `Intel;1033`.
- Fix budget: 0 cycles used.

## Documentation sweep

- `README.md`:
  - a new *Windows installer* section after *Build, run, test*: what `buildmsi` does (with the
    `git clean -dxf` warning), what the MSI installs, and the prerequisites;
  - the *Solution layout* row for `packaging/windows` now names the installer scripts and the worker.
