# FEATURE-5050 — App icon in the output directory

**Branch:** `feature/feature-5050-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

`Assets/app.ico` is now also written as `app.ico` at the root of the build output, and so of the publish
folder, next to `Enigma.FileExplorer.Desktop(.exe)`. Shortcuts, launchers and installers can point at a
file. The same line as Enigma.GitClient's FEATURE-5096:
`<None Update="Assets/app.ico" CopyToOutputDirectory="PreserveNewest" TargetPath="app.ico" />`. The icon
stays an `AvaloniaResource` and the `ApplicationIcon`.

## Files/modules touched

- Modified: `src/Enigma.FileExplorer.Desktop/Enigma.FileExplorer.Desktop.csproj`
- Modified: `tests/Enigma.FileExplorer.Desktop.UnitTests/AppIconTests.cs` (one test; the class summary)
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-5050.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Item kind | `None Update` | `Assets/app.ico` is already a `None` item by default glob; `Update` sets its copy without a second item |
| Test | `TheIconFile_SitsInTheOutputDirectoryByteForByte` in `AppIconTests` | GitClient's test, beside the other icon pins |

## Deviations & follow-ups

- No deviation from the plan.

## Documentation sweep

Nothing became wrong. The README is a title only, and FEATURE-26E7 writes it.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 776, failed 0, succeeded 773, skipped 3
bin/Debug/net10.0/app.ico                        → 14441 bytes, cmp-identical to Assets/app.ico
tests/…Desktop.UnitTests/bin/Debug/net10.0/app.ico → present (flows from the referenced project)
dotnet publish src/Enigma.FileExplorer.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true
  publish/app.ico                                → cmp-identical to Assets/app.ico
```

- 1 test added; it passes.
- The embedded asset still resolves: the frame and About-image tests read it through `avares://`.
- No fix cycle was used.
