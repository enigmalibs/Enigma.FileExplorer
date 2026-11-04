# FEATURE-16CA — Icon set filling the frame

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-16ca-icon-fills-frame`
**Run:** feature/2026-11-04-final3-icons-release

## Objective

Replace the application icon with the new set in `C:\Dev\EnigmaLibs\Branding\Apps\final3\file-explorer`
(`.ico`, ten PNG frames, the SVG master). Update every place the repository carries it:

- the executable, taskbar and window icon;
- the About dialog's image;
- the icon of the Windows installer (Control Panel entry and shortcuts);
- the Linux installer's icons;
- the SVG master.

## Context & constraints

- **The new artwork** is FEATURE-4ACE's drawing (FE-E2, an open folder in the *Soft* palette: `#4F7FDB`,
  `#63C77D`, `#6A9BF4`, no tile, transparent background), **scaled by 1.1915 and centred so that it fills the
  frame**: its longest side spans 16 to 240 of the 256-unit view box. `Branding/Apps/export.py final3`
  generated it, as the SVG's header comment says.
- **The new `.ico`** holds ten 32-bit PNG frames (16, 20, 24, 32, 40, 48, 64, 96, 128, 256), the same sizes
  as today. Each `png/enigma-file-explorer-N.png` is meant to be the `.ico`'s frame of that size, byte for
  byte.
- **Where the icon lives** (since FEATURE-8929 / FEATURE-5050 / FEATURE-6FDB / FEATURE-5447):
  - `src/Enigma.FileExplorer.Desktop/Assets/app.ico`, which is:
    - the `ApplicationIcon` (the executable's and the taskbar's icon);
    - `MainWindow.Icon`;
    - the `app.ico` copied beside the executable. `packaging/windows/buildmsi.ps1` hands that copy to
      Enigma.Msi as the Control Panel icon and the shortcuts' icon;
  - `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`: the About dialog's image, the `.ico`'s 256 frame,
    byte for byte;
  - `packaging/icon/enigma-file-explorer.svg`: the master;
  - `packaging/linux/icons/enigma-file-explorer-{16,24,32,48,64,96,128,256}.png`: what `install.sh`
    installs (`ICON_SIZES`).
- `AppIconTests` (Desktop tests) pins the frames, the About copy, the Linux copies and the output copy.
- The source directory is outside the repository and is left in place.

## Steps

1. Copy, byte for byte:
   - `enigma-file-explorer.ico` → `src/Enigma.FileExplorer.Desktop/Assets/app.ico`;
   - `png/enigma-file-explorer-256.png` → `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`;
   - `enigma-file-explorer.svg` → `packaging/icon/enigma-file-explorer.svg`;
   - the 16, 24, 32, 48, 64, 96, 128 and 256 PNGs → `packaging/linux/icons/`.
2. Check that each `.ico` frame is the delivered PNG of its size (a Python check).
3. Check that no prose describes the icon's size or margin in a way the new set makes wrong (README,
   comments, tests). Fix any that does.
4. Build, then run the whole suite.

## Acceptance criteria

- The four destinations hold the new files (`cmp`; the SVG may differ only in line endings), and no old
  file is left.
- `AppIconTests` passes: ten 32-bit RGBA PNG frames, `app-256.png` is the 256 frame, each Linux PNG is
  the frame of its size, and the output copy matches.
- The Desktop build output's `app.ico` (the file `buildmsi.ps1` packages) is the new icon.
- The build has zero warnings. The whole suite shows no new failure against the run's baseline.

## Out of scope

- The application's accent colours.
- Release notes and version (FEATURE-3EE0).
- Changing the sizes the Linux installer installs (still eight).
- Building an MSI.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which files | All four destinations, from the delivered set unchanged | "Taskbar/window, about, installers": the `.ico` covers the exe, window and MSI; the PNGs cover About and Linux | Re-packing the `.ico` |
| The Windows installer's icon | No change to `buildmsi.ps1` | It packages the published `app.ico`, which this dev replaces | A separate MSI icon file |
| The SVG's header comment | Copied verbatim | `export.py` writes it; an edit would be lost on the next export | Editing it |
| Tests | No test change | The shape is unchanged, so `AppIconTests` still pins what matters | A pixel comparison |
| The 20 and 40 PNGs | Not copied | They are only `.ico` frames; `install.sh` installs eight sizes | Adding them to the Linux icons |
| The source directory | Left in place | It is the Branding repository's output | Deleting it |
| Breakdown | One phase | A file swap of about ten binaries, reviewed in minutes | Splitting Windows from Linux |
