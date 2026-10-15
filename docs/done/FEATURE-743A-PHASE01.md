# FEATURE-743A-PHASE01 — Application icon

**Item:** FEATURE-743A — App icon & About dialog
**Branch:** `feature/feature-743a-phase01-app-icon`
**Run:** vibe/2026-10-15-app-icon-about

## Summary

The explorer now has an application icon, built the same way as Enigma.GitClient's.

**The drawing.** It is a folder holding a listing, with its cursor row in green: what the explorer
shows. It sits on the tile the other Enigma applications use.

- `#1E1F22` tile, 240 units, corner 48: the same as Enigma Git Client.
- Folder back and tab in `#2558C4`; folder front in `#3574F0`, Git Client's blue.
- Three listing rows on the front. The middle one, the cursor row, is `#3FB950`, Git Client's green.
  The two others are `#2D56A8`: the tile colour at 35 % over the front, written as a solid colour so
  every rasterizer agrees.

**Small frames hinted.** At 16 and 32 px, three 18-unit rows six units apart blur into one stripe. The
small master keeps only the cursor row, twice as tall (32 units), and uses a plain tab. At 16 px it is
a crisp 2-pixel green bar on the blue folder.

**Wiring.** The icon is wired the two ways the Avalonia convention asks for:

- `<ApplicationIcon>` embeds it in the Windows `.exe`;
- `Icon="/Assets/app.ico"` puts it on the running window on every platform.

## Files / modules touched

**Created**

- `packaging/icon/enigma-file-explorer.svg` — the master for 48, 64, 128 and 256, with the
  regeneration recipe in its header comment.
- `packaging/icon/enigma-file-explorer-small.svg` — the hinted master for 16 and 32.
- `src/Enigma.FileExplorer.Desktop/Assets/app.ico` — six 32-bit RGBA PNG frames, 16 → 256, smallest
  first, 8 934 bytes.
- `src/Enigma.FileExplorer.Desktop/Assets/app-256.png` — the 256 frame, byte for byte (the About
  dialog of PHASE02 shows it).
- `tests/Enigma.FileExplorer.Desktop.UnitTests/AppIconTests.cs` — three tests.

**Modified**

- `src/Enigma.FileExplorer.Desktop/Enigma.FileExplorer.Desktop.csproj` — `ApplicationIcon` and
  `<AvaloniaResource Include="Assets/**" />`.
- `src/Enigma.FileExplorer.Desktop/Views/MainWindow.axaml` — `Icon="/Assets/app.ico"`.
- `docs/roadmap.md`, `docs/plan/FEATURE-743A.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which drawing | A folder with three listing rows, the middle one green (the cursor row) | Of four drafts, it reads most clearly as a file explorer. The others were a block cursor alone, a block cursor with a text line, and a split folder. It keeps Git Client's palette |
| Row colour | `#2D56A8`, solid | The tile colour at 35 % over the front. An `opacity` attribute could blend differently from one rasterizer to another |
| Small-frame hinting | Only the cursor row, 32 units tall; a straight tab corner | At 16 px the full drawing's rows are about one pixel apart. Compared side by side, the hinted one is the legible one |
| Rasterizer | Svg.Skia 3.7.0 (SkiaSharp 2.88.9), already in the local NuGet cache, run from a throwaway file-based C# script in the session scratchpad | `rsvg-convert`, the tool Git Client's recipe names, is not installed here. No renderer is committed |
| ICO packing | The rendered PNGs unchanged, smallest first, 32-bit, PNG entries | Git Client's exact layout. Nothing re-encodes the frames |
| Where the tests read from | `avares://Enigma.FileExplorer.Desktop/Assets/…` | What the application actually loads |
| Window-icon test | The DI-resolved `MainWindow` has a non-null `Icon` | The headless platform's icon loader is a stub, so comparing the pixels proves nothing more. The asset tests pin the content |

## Deviations & follow-ups

- **Recipe wording.** The SVG header names `rsvg-convert` as an example and says any SVG rasterizer
  will do. Unlike Git Client's recipe, it does not promise a byte-for-byte reproduction. The committed
  frames came from Svg.Skia, and another rasterizer's anti-aliasing can differ by a few edge pixels.
  The tests pin what matters: sizes, the encoding, and the 256 copy.
- **No Linux icon copies.** There is no Linux packaging in this repository. When a launcher is added,
  copy the six frames to its icon folder, as Git Client's `packaging/linux/icons/` does.
- **Follow-up (optional):** the PNGs could be losslessly recompressed (e.g. `oxipng`). They are already
  small (8.9 KB for all six).
- Line endings: the new text files are LF, as `.gitattributes` asks; no CRLF churn.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, and there is no
`CLAUDE.md` or `AGENTS.md`.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 747  failed: 0  succeeded: 744  skipped: 3
```

- Three tests added, all passing:
  - `TheIcon_HoldsSixSquare32BitPngFramesFrom16To256`;
  - `The256Image_IsTheIconsLargestFrameByteForByte`;
  - `TheMainWindow_CarriesTheIcon`.
- The first run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones: symbolic links need a privilege on this
  machine.
- The built `Enigma.FileExplorer.Desktop.exe` contains the 256 frame's bytes, so the icon is embedded
  in the apphost.
