# FEATURE-4ACE — New icon set without the tile

**Item:** FEATURE-4ACE — New icon set without the tile
**Branch:** `feature/feature-4ace-new-icon-set`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The new set from `Branding/Apps/final2/file-explorer` (`export.py final2`) replaces the application icon.
It is the same open folder in the *Soft* palette, **without the dark rounded tile**, so the background is
transparent. Every copy is the delivered file byte for byte:

- `Assets/app.ico`: the new ten-frame icon (16, 20, 24, 32, 40, 48, 64, 96, 128, 256; 32-bit RGBA PNG
  frames). It is the executable's icon, the window's icon, and the `app.ico` written beside the
  executable;
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
  - `docs/roadmap.md`, `docs/plan/FEATURE-4ACE.md`: statuses.
- Created: `docs/done/FEATURE-4ACE.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Checking the frames beyond `AppIconTests` | A Python check that each `.ico` frame is the delivered PNG of its size | `AppIconTests` compares the Linux copies with the frames, not the 20/40 frames with the Branding PNGs |
| Prose about the tile | Nothing to change | No README, release note, script, comment or test describes the icon's background |

## Deviations & follow-ups

- No deviation from the plan.
- Windows caches executable icons. Explorer may show the old icon on an existing shortcut until its cache
  refreshes, which is not a defect.
- The source directory `Branding/Apps/final2/file-explorer` is outside this repository and was left alone.
- Line endings: the delivered SVG is CRLF. `.gitattributes` (`* text=auto eol=lf`) stores it as LF, so
  the committed SVG differs from the source only in line endings. The binaries (`.ico`, `.png`) are
  byte for byte. No action taken (recommendation only: have `export.py` write LF).

## Build/test evidence

**The run's baseline**, taken once on `feature/2026-11-03-icons-msi-release` at `9821709` (`develop`
@ `d8ed05b` plus the planning commit), before this first dev. Later devs of the run cite it:

- `dotnet build Enigma.FileExplorer.slnx`: succeeded, **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.FileExplorer.slnx`: 824 total, 821 passed, 3 skipped (Linux/macOS-only
  and symbolic-link-privilege tests), **0 failed**.

A clean baseline, so the strict rule applies: zero warnings, every test passing.

**This dev:**

- `cmp`: each of the 11 destination files is identical to its source in the working tree.
- Python frame check: `app.ico` is type 1 with 10 frames. Each frame is a 32-bit PNG and is byte for byte
  `png/enigma-file-explorer-<N>.png`, for all ten sizes.
- `dotnet build Enigma.FileExplorer.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.FileExplorer.slnx`: 824 total, 821 passed, 3 skipped, **0 failed**.
  `AppIconTests` (5 facts) ran against the new files.
- The Debug `Enigma.FileExplorer.Desktop.dll` embeds the new `app.ico` and `app-256.png`. The `app.ico`
  in the app's output and in the Desktop test output is the new file.
- Fix budget: 0 cycles used.

## Documentation sweep

- Nothing stale. The README describes the icon's places and its eight Linux sizes, and neither changed.
  The new look goes into the 0.3.0 release notes (FEATURE-27BF).
