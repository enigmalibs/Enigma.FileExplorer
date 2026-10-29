# FEATURE-6FDB — Linux install and uninstall scripts

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-6fdb-linux-install-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Install and uninstall Enigma File Explorer on Linux for the current user, as Enigma.GitClient's
`packaging/linux/install.sh` and `uninstall.sh` do: the application under `$XDG_DATA_HOME`, a launcher
symlink under `$XDG_BIN_HOME`, a desktop entry and the hicolor icons — no root, nothing outside `$HOME`.

## Context & constraints

- **The reference** is `Enigma.GitClient/packaging/linux/`: `install.sh` (options `--from`,
  `--framework-dependent`, `--rid`, `-h`; refuses root; validates XDG paths; publishes into a staging
  sibling and swaps it in; symlink; icons; desktop entry from `enigma-git-client.desktop.in` with `@EXEC@`
  substituted in bash; `DOTNET_ROOT` pinned when a framework-dependent runtime is not where a launcher looks;
  desktop/icon caches refreshed), `uninstall.sh` (removes exactly that, symlink only if it points into the
  install), the `.desktop.in`, and `icons/enigma-git-client-N.png` for 8 sizes. Scripts are mode `100755`.
- **Here:** no `packaging/linux/` yet. The app id is `enigma-file-explorer`, the launcher
  `Enigma.FileExplorer.Desktop`, the display name "Enigma File Explorer"; settings live in
  `$XDG_CONFIG_HOME/Enigma.FileExplorer` (`AppPaths`). The app takes an optional directory argument
  (`Session.DirectoryArgument`).
- The icon PNGs come from `Branding/Apps/final/file-explorer/png/` (FEATURE-8929's set), byte for byte the
  `.ico` frames.
- `.gitattributes` keeps `*.sh` LF.

## Steps

1. `packaging/linux/install.sh`, `uninstall.sh`: GitClient's scripts with this app's id, launcher, name,
   help text and settings folder. Drop the git-specific note; the launch hint says
   `enigma-file-explorer [<directory>]`. Neither script has a former launcher name to handle.
2. `packaging/linux/enigma-file-explorer.desktop.in`: `Name` "Enigma File Explorer", `GenericName`
   "File Manager", a comment, `Exec=@EXEC@` (no field code: the launcher opens the explorer, not files),
   `Icon=enigma-file-explorer`, `Categories=System;FileTools;FileManager;`, keywords,
   `StartupWMClass=Enigma.FileExplorer.Desktop`.
3. `packaging/linux/icons/enigma-file-explorer-N.png` for 16 24 32 48 64 96 128 256, copied byte for byte.
4. Stage both scripts with mode `100755` (`git add --chmod=+x`).
5. Tests (Desktop, `AppIconTests` or a new `LinuxPackagingTests`), finding the repository root by walking
   up to `Enigma.FileExplorer.slnx`: every size `install.sh`'s `ICON_SIZES` lists has its PNG, byte for byte
   the `.ico`'s frame of that size; `uninstall.sh` lists the same sizes.
6. Verify: `bash -n` on both scripts; through Git Bash, `install.sh --help` and `uninstall.sh --help` run;
   if WSL is available, an install/uninstall round trip into a temporary `XDG_DATA_HOME`/`XDG_BIN_HOME`.

## Acceptance criteria

- Both scripts parse (`bash -n`), print their help, and are `100755` in the index, LF in the tree.
- The icon copies and the two `ICON_SIZES` lists are pinned by a test.
- The build has zero warnings; the whole suite is green.

## Out of scope

- A system-wide install, `.deb`/`.rpm`/Flatpak/AppImage packages, a macOS bundle.
- Registering as the desktop's default file manager (`MimeType=inode/directory`).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Shape | GitClient's scripts, renamed | "Like done by Enigma.GitClient"; reviewed and hardened there | Writing new ones |
| `MimeType=inode/directory` | Not declared | It could make the explorer the desktop's folder handler without asking; not requested | Declaring it |
| Desktop categories | `System;FileTools;FileManager;` | freedesktop's registered categories for a file manager | `Utility;` |
| Icon sizes | 8 (16 24 32 48 64 96 128 256) | hicolor has these directories, as in GitClient; 20/40 have none | All ten |
| Test | Pin icon copies and size lists | The lists are hand-edited in two scripts | No test |
