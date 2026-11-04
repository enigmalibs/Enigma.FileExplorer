# FEATURE-16CA — Icon set filling the frame

**Item:** FEATURE-16CA — Icon set filling the frame
**Branch:** `feature/feature-16ca-icon-fills-frame`
**Run:** feature/2026-11-04-final3-icons-release

## Summary

The new set from `Branding/Apps/final3/file-explorer` (`export.py final3`) replaces the application icon.
It is the same open folder in the *Soft* palette, without a tile, **scaled by 1.1915 so that it fills the
frame**: its longest side now spans 16 to 240 of 256 units. Every copy is the delivered file, byte for byte:

- `Assets/app.ico`: the new ten-frame icon (16, 20, 24, 32, 40, 48, 64, 96, 128, 256; 32-bit RGBA PNG
  frames). It is:
  - the executable's and the taskbar's icon;
  - the window's icon;
  - the `app.ico` written beside the executable, which `buildmsi.ps1` gives the MSI for its Control Panel
    entry and its two shortcuts;
- `Assets/app-256.png`: the new 256 frame, which the About dialog shows;
- `packaging/icon/enigma-file-explorer.svg`: the new master, with the header comment the export writes;
- `packaging/linux/icons/`: the eight sizes `install.sh` installs.

The icon's shape is unchanged (same sizes, same encoding, same file names), so no code, script or test
changed.

## Files/modules touched

- Modified:
  - `src/Enigma.FileExplorer.Desktop/Assets/app.ico`
  - `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-file-explorer.svg`
  - `packaging/linux/icons/enigma-file-explorer-{16,24,32,48,64,96,128,256}.png`
  - `docs/roadmap.md`, `docs/plan/FEATURE-16CA.md`: statuses.
- Created: `docs/done/FEATURE-16CA.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Checking the frames beyond `AppIconTests` | A Python check that each `.ico` frame is the delivered PNG of its size | `AppIconTests` compares the Linux copies with the frames, not the 20/40 frames with the Branding PNGs |
| Prose about the icon's margin | Nothing to change | No README, script, comment or test describes how much of the frame the drawing fills. The README's 0.3 callout is history, and FEATURE-3EE0 rewrites it |

## Deviations & follow-ups

- No deviation from the plan.
- Windows caches executable and shortcut icons. Explorer may show the old icon on an existing shortcut
  until its cache refreshes. That is not a defect.
- The source directory `Branding/Apps/final3/file-explorer` is outside this repository and was left alone.
- Line endings: the delivered SVG is CRLF. `.gitattributes` stores it as LF, so the committed SVG differs
  from the source only in line endings. The binaries (`.ico`, `.png`) are byte for byte. No action taken.
  Recommendation only: have `export.py` write LF.

## Build/test evidence

**The run's baseline** was taken once on `feature/2026-11-04-final3-icons-release` at `070a83e` (`main`
@ `b7c9a90` plus the planning commit), before this first dev. Later devs of the run cite it:

- `dotnet build Enigma.FileExplorer.slnx`: succeeded, **0 warnings, 0 errors**;
- `dotnet test --solution Enigma.FileExplorer.slnx`: 827 total, 824 passed, 3 skipped (Linux/macOS-only
  and symbolic-link-privilege tests), **0 failed**.

The baseline is clean, so the strict rule applies: zero warnings, every test passing.

**This dev:**

- `cmp`: each of the 11 destination files is identical to its source in the working tree.
- Python frame check: `app.ico` is type 1 with 10 frames. Each frame is an 8-bit RGBA PNG (32 bpp) whose
  IHDR matches its size, and is byte for byte `png/enigma-file-explorer-<N>.png`, for all ten sizes.
- `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.FileExplorer.slnx`: 827 total, 824 passed, 3 skipped, **0 failed**.
- `AppIconTests` alone: 5 passed, 0 failed.
- The Debug `Enigma.FileExplorer.Desktop.dll` embeds the new `app.ico` and `app-256.png`. The `app.ico`
  in the Desktop output and in the Desktop test output is the new file. `publish.ps1`, and with it
  `buildmsi.ps1`, takes it from the same `None` item. No publish or MSI was built to check this.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale. The README describes where the icon goes and its eight Linux sizes, and neither changed.
  The new look goes into the 0.4.0 release notes (FEATURE-3EE0).
