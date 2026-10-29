# FEATURE-CC7E — Update NuGet packages

**Branch:** `feature/feature-cc7e-update-nuget-packages`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

Every package in `Directory.Packages.props` is on its latest stable version. This dev comes first in the
run, so the later devs build and test against these versions.

| Package | Before | After |
|---|---|---|
| Microsoft.Extensions.DependencyInjection(.Abstractions), Logging.Abstractions, Hosting | 10.0.13 | 10.0.14 |
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter | 12.1.1 | 12.1.4 |
| Avalonia.Headless, Avalonia.Skia (tests) | 12.1.1 | 12.1.4 |
| Enigma.Avalonia.Desktop | 1.2.0 | 1.3.0 |

The Avalonia set moved as a whole, together with Enigma.Avalonia.Desktop. The 1.3.0 nuspec pins
Avalonia 12.1.4 and CommunityToolkit.Mvvm 8.4.2. Its release notes say it is backward compatible with
1.2.0: no API, key or value changes. The coupling comment now names 1.3.0 / 12.1.4.

## Files/modules touched

- Modified: `Directory.Packages.props`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-CC7E.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Release build as well as Debug | Both built | `AvaloniaUI.DiagnosticsSupport` is conditioned on the configuration |

## Deviations & follow-ups

- No deviation from the plan.
- Already at their latest version and left unchanged:
  - AvaloniaUI.DiagnosticsSupport 2.2.3
  - CommunityToolkit.Mvvm 8.4.2
  - Enigma.Icons.Avalonia 1.0.0
  - xunit.v3 4.0.2
  - Microsoft.Testing.Extensions.CodeCoverage 18.12.0
- `set-status` re-padded the roadmap table: the Status column widened to fit `IN PROGRESS`.

## Documentation sweep

Nothing became wrong: no prose document names a package version.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx                → 0 Warning(s), 0 Error(s)
dotnet build Enigma.FileExplorer.slnx -c Release     → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx      → total 775, failed 0, succeeded 772, skipped 3
dotnet list Enigma.FileExplorer.slnx package --outdated → no updates for any of the four projects
```

- `obj/project.assets.json` resolves Avalonia 12.1.4, Enigma.Avalonia.Desktop 1.3.0 and
  Microsoft.Extensions.Hosting 10.0.14.
- The 3 skips are the known symbolic-link tests, which need a privilege on this machine.
- No fix cycle was used.
