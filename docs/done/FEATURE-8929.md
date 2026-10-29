# FEATURE-8929 — New application icon

**Branch:** `feature/feature-8929-new-app-icon`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

The application icon is the new Branding set, `Branding/Apps/final/file-explorer`. It is variant FE-E2,
an open folder in the Soft palette, exported by `Branding/Apps/export.py`. Each destination is a
byte-for-byte copy:

| Source (Branding) | Destination |
|---|---|
| `enigma-file-explorer.ico` (ten 32-bit PNG frames: 16, 20, 24, 32, 40, 48, 64, 96, 128, 256) | `src/Enigma.FileExplorer.Desktop/Assets/app.ico` |
| `png/enigma-file-explorer-256.png` | `src/Enigma.FileExplorer.Desktop/Assets/app-256.png` (About dialog) |
| `enigma-file-explorer.svg` (header comment from `export.py`, verbatim) | `packaging/icon/enigma-file-explorer.svg` |

The old hinted `packaging/icon/enigma-file-explorer-small.svg` is deleted. The new set has none:
`export.py` renders every frame from the master when an application has no small drawing.

## Files/modules touched

- Modified:
  - `src/Enigma.FileExplorer.Desktop/Assets/app.ico`
  - `src/Enigma.FileExplorer.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-file-explorer.svg`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/AppIconTests.cs`
- Deleted: `packaging/icon/enigma-file-explorer-small.svg`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-8929.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Test name | `TheIcon_HoldsTenSquare32BitPngFramesFrom16To256` | The old name stated six frames |
| A why-comment on the sizes | Added (20/24/40 are the 125 %/150 % frames) | The odd sizes would otherwise look arbitrary |

## Deviations & follow-ups

- No deviation from the plan.
- Windows caches executable icons. Explorer may show the old icon on an existing shortcut until its icon
  cache refreshes, which is not a defect.
- The Linux hicolor copies come with FEATURE-6FDB, taken from the same Branding set.
- Line endings: the Branding SVG is CRLF. `.gitattributes` (`* text=auto eol=lf`) stores it LF, so the
  committed SVG matches the source in content, not in bytes; the binary `.ico` and `.png` are exact.
  Recommendation only: export the Branding SVGs with LF so the copies stay byte-identical.

## Documentation sweep

Nothing became wrong. Only the plan files and the old done records mention the small SVG, and those
are history.

## Build/test evidence

```
cmp Branding/…/enigma-file-explorer.ico            Assets/app.ico      → identical
cmp Branding/…/png/enigma-file-explorer-256.png    Assets/app-256.png  → identical
cmp Branding/…/enigma-file-explorer.svg            packaging/icon/…    → identical
dotnet build Enigma.FileExplorer.slnx              → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx    → total 775, failed 0, succeeded 772, skipped 3
```

- `AppIconTests` now pins the ten frames through the embedded resource. `app-256.png` is still the largest
  frame byte for byte, and the window still carries the icon.
- No fix cycle was used.
