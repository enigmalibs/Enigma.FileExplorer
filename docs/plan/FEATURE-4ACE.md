# FEATURE-4ACE — New icon set without the tile

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-4ace-new-icon-set`
**Run:** feature/2026-11-03-icons-msi-release

## Objective

Replace the application icon with the new set in `C:\Dev\EnigmaLibs\Branding\Apps\final2\file-explorer`
(`.ico`, ten PNG frames, the SVG master). Update every place the repository carries it: the executable and
window icon, the About dialog's image, the Linux installer's icons and the SVG master.

## Context & constraints

- **The new artwork** is FEATURE-8929's drawing (FE-E2, an open folder in the *Soft* palette:
  `#4F7FDB`, `#63C77D`, `#6A9BF4`), **without the dark rounded tile** (`#1E1F22`), so the background is
  transparent. `Branding/Apps/export.py final2` generated it, as the SVG's header comment says.
- **The new `.ico`** holds ten 32-bit PNG frames (16, 20, 24, 32, 40, 48, 64, 96, 128, 256), the same sizes
  as today. Each `png/enigma-file-explorer-N.png` is meant to be the `.ico`'s frame of that size byte for
  byte. The build checks this.
- **Where the icon lives** (since FEATURE-8929 / FEATURE-5050 / FEATURE-6FDB):
  - `src/Enigma.FileExplorer.Desktop/Assets/app.ico`: the `ApplicationIcon`, `MainWindow.Icon`, and the
    `app.ico` copied beside the executable;
  - `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`: the About dialog's image, the `.ico`'s 256
    frame byte for byte;
  - `packaging/icon/enigma-file-explorer.svg`: the master;
  - `packaging/linux/icons/enigma-file-explorer-{16,24,32,48,64,96,128,256}.png`: what `install.sh`
    installs (`ICON_SIZES`).
- `AppIconTests` (Desktop tests) pins the frames, the About copy, the Linux copies and the output copy.
  Earlier runs ran the whole suite, Desktop tests included, on this machine.
- The source directory is outside the repository and is left in place.

## Steps

1. Copy, byte for byte:
   - `enigma-file-explorer.ico` → `src/Enigma.FileExplorer.Desktop/Assets/app.ico`;
   - `png/enigma-file-explorer-256.png` → `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`;
   - `enigma-file-explorer.svg` → `packaging/icon/enigma-file-explorer.svg`;
   - the 16, 24, 32, 48, 64, 96, 128 and 256 PNGs → `packaging/linux/icons/`.
2. Check that no prose describes the icon's dark tile (README, comments, tests). Fix any that does.
3. Build, then run the whole suite.

## Acceptance criteria

- The four destinations hold the new files (`cmp`; the SVG may differ only in line endings), and no old
  file is left.
- `AppIconTests` passes: ten 32-bit RGBA PNG frames, `app-256.png` is the 256 frame, each Linux PNG is
  the frame of its size, and the output copy matches.
- The build has zero warnings. The whole suite shows no new failure against the run's baseline.

## Out of scope

- The application's accent colours.
- Release notes and version (FEATURE-27BF).
- Changing the sizes the Linux installer installs (still eight).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which files | All four destinations, from the delivered set unchanged | "Update the icons currently used in the app"; the set has the same shape as today's | Re-packing the `.ico` |
| The SVG's header comment | Copied verbatim | `export.py` writes it; an edit would be lost on the next export (FEATURE-8929's rule) | Editing it |
| Tests | No test change | The shape is unchanged, so `AppIconTests` still pins what matters | A pixel comparison |
| The 20 and 40 PNGs | Not copied | They are only `.ico` frames; `install.sh` installs eight sizes | Adding them to the Linux icons |
| The source directory | Left in place | It is the Branding repository's output | Deleting it |
