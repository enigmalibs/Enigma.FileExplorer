# FEATURE-6FDB — Linux install and uninstall scripts

**Branch:** `feature/feature-6fdb-linux-install-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

Enigma File Explorer installs on Linux for the current user, without root, as Enigma.GitClient does.

**`install.sh` puts the application in four places:**

| What | Where |
|---|---|
| The application | `$XDG_DATA_HOME/enigma-file-explorer` |
| The `enigma-file-explorer` launcher symlink | `$XDG_BIN_HOME` |
| The desktop entry | `$XDG_DATA_HOME/applications/enigma-file-explorer.desktop` |
| Eight hicolor icons (16 24 32 48 64 96 128 256) | `$XDG_DATA_HOME/icons/hicolor` |

Options:

- **Default:** builds from the repository, bundling the .NET runtime.
- **`--framework-dependent`:** builds without the runtime.
- **`--from DIR`:** takes an already-published directory.
- **`--rid`:** the runtime identifier.

It also:

- refuses root;
- validates the XDG paths;
- swaps the new install into place, so a failed build leaves the old install untouched;
- pins `DOTNET_ROOT` in the entry when a framework-dependent runtime is not where a launcher looks.

**`uninstall.sh`** removes exactly that. It removes the symlink only when it points into the
installation, and leaves `~/.config/Enigma.FileExplorer` (settings and bookmarks) untouched.

**The scripts** are GitClient's, with this app's id, launcher, name and settings folder. The git
requirement, the accounts and tokens wording, the former-launcher clause (FE never had one) and the
GitClient-specific size figures are gone. The launch hint reads `enigma-file-explorer <directory>`, which
`Session.DirectoryArgument` honours.

**The desktop entry:**

- `GenericName=File Manager`;
- `Categories=System;FileTools;FileManager;`;
- `StartupWMClass=Enigma.FileExplorer.Desktop`;
- no `MimeType`, so the explorer never becomes the folder handler unasked.

## Files/modules touched

- Created:
  - `packaging/linux/install.sh`, `packaging/linux/uninstall.sh` (mode `100755`)
  - `packaging/linux/enigma-file-explorer.desktop.in`
  - `packaging/linux/icons/enigma-file-explorer-{16,24,32,48,64,96,128,256}.png`
  - `tests/Enigma.FileExplorer.Desktop.UnitTests/Infrastructure/RepositoryRoot.cs`
- Modified: `tests/Enigma.FileExplorer.Desktop.UnitTests/AppIconTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-6FDB.md` (statuses)

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Size figures in `--help` ("about 37 MB rather than 116 MB") | Replaced by "a much smaller install" | GitClient's numbers; FE's differ and would go stale |
| Repository-root lookup in tests | A shared `Infrastructure/RepositoryRoot` | FEATURE-E5D8's test needs it too |
| Where the icon test lives | `AppIconTests`, as GitClient | The other icon copies are pinned there |
| Icon source | `Branding/Apps/final/file-explorer/png/` | The same export as `app.ico`; the test proves byte equality with its frames |

## Deviations & follow-ups

- No deviation from the plan.
- Like GitClient's, `uninstall.sh` leaves empty `hicolor/<N>x<N>/apps` directories behind. These are
  shared theme directories, not this application's.

## Documentation sweep

Nothing became wrong. The README is a title only; FEATURE-26E7 documents the installer.

## Build/test evidence

```
bash -n install.sh / uninstall.sh                → syntax ok; --help of both prints the FE text
dotnet build Enigma.FileExplorer.slnx            → 0 Warning(s), 0 Error(s)
dotnet test --solution Enigma.FileExplorer.slnx  → total 818, failed 0, succeeded 815, skipped 3
```

**WSL (archlinux, non-root) round trip.** The input was a linux-x64 publish made on Windows. All XDG
directories pointed into a `mktemp -d /tmp/fe-install.*` folder.

1. `install.sh --from <publish>` exits 0:
   - the symlink points at the launcher;
   - 73 files installed, the launcher executable, `app.ico` among them;
   - 8 icons under `hicolor`;
   - the entry's `Exec` holds the quoted launcher path.

   It also warns as designed, because WSL has no .NET runtime.
2. A re-install upgrades in place and leaves no staging directory behind.
3. `uninstall.sh` removes all four parts and leaves only the empty hicolor directories.
4. A second `uninstall.sh` says "Nothing to remove".
5. `install.sh --bogus` exits 1.

- One test added: `AppIconTests.TheLinuxIcons_AreTheIconsFramesByteForByte_AndBothScriptsListTheSameSizes`.
- No fix cycle was used.
