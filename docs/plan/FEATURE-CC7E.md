# FEATURE-CC7E — Update NuGet packages

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-cc7e-update-nuget-packages`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every package the solution references is on its latest stable version, part of the uniform baseline for
every Enigma app. This dev goes first, so the rest of the run builds and tests against the final libraries.

## Context & constraints

- Versions live in `Directory.Packages.props` (Central Package Management); projects carry no `Version`.
- `dotnet list Enigma.FileExplorer.slnx package --outdated` on 2026-10-29 (nuget.org):

  | Package | Pinned | Latest |
  |---|---|---|
  | Microsoft.Extensions.DependencyInjection(.Abstractions), Logging.Abstractions, Hosting | 10.0.13 | 10.0.14 |
  | Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter | 12.1.1 | 12.1.4 |
  | Avalonia.Headless, Avalonia.Skia (tests) | 12.1.1 | 12.1.4 |
  | Enigma.Avalonia.Desktop | 1.2.0 | 1.3.0 |

  Already latest: AvaloniaUI.DiagnosticsSupport 2.2.3, CommunityToolkit.Mvvm 8.4.2, Enigma.Icons.Avalonia
  1.0.0, xunit.v3 4.0.2, Microsoft.Testing.Extensions.CodeCoverage 18.12.0.
- **The coupling rule** (comment in `Directory.Packages.props`): Avalonia, its companions and the Enigma
  libraries move together. Enigma.Avalonia.Desktop 1.3.0's nuspec pins Avalonia 12.1.4 and
  CommunityToolkit.Mvvm 8.4.2; its release notes say 1.3.0 is backward compatible with 1.2.0 (no API, key or
  value change, only the Avalonia floor).
- Enigma.GitClient and Enigma.TextEditor already pin exactly this set.

## Steps

1. `Directory.Packages.props`:
   - the four Microsoft.Extensions pins → 10.0.14;
   - the six Avalonia pins (app and tests) → 12.1.4;
   - Enigma.Avalonia.Desktop → 1.3.0;
   - the coupling comment names 1.3.0 / 12.1.4.
2. Re-run `dotnet list package --outdated` and record the result: nothing left, or the reason anything was
   held back (prereleases are never taken).
3. Clean build (zero warnings) and the whole suite.

## Acceptance criteria

- `dotnet list Enigma.FileExplorer.slnx package --outdated` reports no stable update.
- The build has zero warnings; the whole suite is green.

## Out of scope

- Prerelease packages (e.g. .NET 11 previews/RCs).
- The SDK pin in `global.json`.
- New packages.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What to bump | Every outdated stable package | "Latest versions"; all are patch or minor | Only the non-coupled ones |
| The Avalonia set | All of it to 12.1.4 with Enigma.Avalonia.Desktop 1.3.0, together | The house coupling rule; the library pins 12.1.4 | Bumping Avalonia without the library (mismatch) |
| Position in the run | First | Later devs are built and tested on the final dependencies | Folding it into the release dev |
| Prereleases | Never | A release ships on stable dependencies | Taking .NET 11 RCs |
