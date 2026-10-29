# FEATURE-5050 — App icon in the output directory

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-5050-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The application icon is written to the build output directory — and so to the publish directory — next to
`Enigma.FileExplorer.Desktop(.exe)`, as `app.ico`. Shortcuts, launchers and installers can then point at a
file instead of extracting it from the executable. Same as Enigma.GitClient's FEATURE-5096.

## Context & constraints

- `src/Enigma.FileExplorer.Desktop/Assets/app.ico` is the `ApplicationIcon` and the window's `Icon`.
  `Assets/**` is an `AvaloniaResource` (embedded), so nothing lands in `bin/` today.
- Items with `CopyToOutputDirectory` are also published, and flow to referencing projects' output (the
  Desktop test project), so `AppContext.BaseDirectory` of the test run holds the copy too.
- `AppIconTests` (Desktop tests) pins the icon and its copies.

## Steps

1. `Enigma.FileExplorer.Desktop.csproj`: `Assets/app.ico` copied to the output root as `app.ico`
   (`None Update` … `CopyToOutputDirectory="PreserveNewest"`, `Link`/`TargetPath="app.ico"`), with a one-line
   comment saying why. It stays an `AvaloniaResource`.
2. `AppIconTests`: a test that `app.ico` sits in `AppContext.BaseDirectory`, byte for byte the embedded
   asset.
3. Verify by hand: a build puts `app.ico` in `bin/<Configuration>/net10.0/`; a `win-x64` publish puts it in
   `publish/`; both byte-identical to `Assets/app.ico`.

## Acceptance criteria

- `bin/<Configuration>/net10.0/app.ico` and the publish folder's `app.ico` exist, byte for byte
  `Assets/app.ico`.
- The embedded resource still resolves (`avares://…/Assets/app.ico` unchanged).
- The build has zero warnings; the whole suite is green.

## Out of scope

- The PNG copy (`app-256.png`) and the Linux hicolor icons (installed by FEATURE-6FDB's `install.sh`).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which file | `app.ico` only | "The app icon" is the `ApplicationIcon`; the narrowest reading, as in GitClient | Also copying `app-256.png` |
| Name and place | `app.ico` at the output root | One predictable name across the Enigma apps | `Assets/app.ico` sub-folder; `Enigma.FileExplorer.Desktop.ico` |
| Mechanism | An item with `CopyToOutputDirectory` + `TargetPath` | Standard MSBuild; flows to publish with no custom target | A custom `Copy` target after build and publish |
| Test | One Desktop test + a manual build/publish check | Pins the copy where the other icon copies are pinned | No test |
