# Enigma.FileExplorer

A keyboard-first, cross-platform file explorer for the desktop, driven by **Vim's keys**, built with
[Avalonia 12](https://avaloniaui.net/),
[Enigma.Avalonia.Desktop](https://www.nuget.org/packages/Enigma.Avalonia.Desktop) and
[Enigma.Icons.Avalonia](https://www.nuget.org/packages/Enigma.Icons.Avalonia).

> **What's new in 0.2** — the first release: browsing in split panes with Vim's keys, file operations, name and
> content search, and bookmarks; a dialog that shows an unhandled error and keeps the explorer running; a new icon,
> also written beside the executable as `app.ico`; a Linux installer; and one publish script per platform. See
> [RELEASENOTES.md](RELEASENOTES.md).

## Features

- **Panes and splits.** Browse with `h`, `j`, `k`, `l`, and go back and forward with `H` and `L`. Split the pane with
  `Space v` or `Space s`, and move between panes with `gh`, `gj`, `gk`, `gl`. A places sidebar and a clickable path
  bar sit beside the listing.
- **Vim's way of working.** Counts, marks (`m`, `v` for a range), a `:` command line with completion and history
  (`:cd`, `:mkdir`, `:rename`, `:sort`, `:set`, …), quick-jump with `/`, `n` and `N`, and a leader key (Space).
  `?` lists every key.
- **Files.** Create with `a` / `A`, rename with `r`, copy, cut and paste with `yy`, `x` and `p` (with other
  applications too), move to the trash with `d`, or delete with `D`. Transfers run in the background, and ask when
  a name already exists. Right-click menus and drag and drop work as well.
- **Views.** Details, medium or large icons (`zd`, `zm`, `zl`); sort by name, date, size or type (`on`, `om`, `os`,
  `ot`, `or` to reverse); hidden files on or off (`zh`).
- **Search.** File names with `f`, file contents with `F`, from the pane's folder down. Results stream in and act like a
  listing.
- **Bookmarks.** `B` bookmarks a folder; `b` opens, adds or removes them.
- **Settings.** The theme (`:theme dark|light|system`), the hidden-files switch, the trash question, the bookmarks and
  the window's place are remembered between sessions.

## Your data

The explorer keeps one file of its own, `settings.json`, in a per-user folder. Beyond it, it reads the folders you
browse and the files you search, and changes only what you ask it to.

| Platform | Folder                                                    |
|----------|-----------------------------------------------------------|
| Windows  | `%APPDATA%\Enigma.FileExplorer`                           |
| Linux    | `$XDG_CONFIG_HOME/Enigma.FileExplorer` (`~/.config`)      |
| macOS    | `~/Library/Application Support/Enigma.FileExplorer`       |

`settings.json` is readable JSON, written atomically. A key it lacks keeps its default. A file that cannot be read is
kept as `settings.json.bad`, and the explorer starts with the defaults.

## Requirements

- .NET SDK 10.0.100 or later to build (pinned in `global.json`, rolls forward to newer feature bands). Running a
  published build needs the .NET 10 runtime. The Linux installer bundles it by default.
- Windows 10/11, Linux (X11) or macOS.

## Build, run, test

```bash
dotnet build Enigma.FileExplorer.slnx
dotnet run --project src/Enigma.FileExplorer.Desktop -- [directory]
dotnet test --solution Enigma.FileExplorer.slnx
```

To run it on a machine without the SDK, publish it for that platform:

- **Windows:** `packaging/windows/publish.ps1` (or `publish.bat` from a cmd prompt). The result lands in
  `src/Enigma.FileExplorer.Desktop/bin/Release/net10.0/win-x64/publish/`.
- **Linux:** `packaging/linux/publish.sh`. The result lands in `…/linux-x64/publish/`.

Both are Release builds compiled ReadyToRun, which run on an installed .NET 10 runtime. `app.ico` sits beside the
executable for shortcuts and installers.

## Install on Linux

`packaging/linux/install.sh` builds the explorer from this repository and installs it for you. It needs no root and no
`sudo`, and writes nothing outside your home directory. **Enigma File Explorer** is then in your application launcher:

```bash
./packaging/linux/install.sh
```

| What                                                      | Where (`XDG_DATA_HOME` and `XDG_BIN_HOME` are honoured)              |
|-----------------------------------------------------------|----------------------------------------------------------------------|
| The application                                           | `~/.local/share/enigma-file-explorer`                                |
| A launcher command, `enigma-file-explorer [<directory>]`  | `~/.local/bin/enigma-file-explorer`                                  |
| The desktop entry                                         | `~/.local/share/applications/enigma-file-explorer.desktop`           |
| The icon, in eight sizes                                  | `~/.local/share/icons/hicolor/<N>x<N>/apps/enigma-file-explorer.png` |

By default the .NET runtime is bundled with the application, so it runs whatever is installed on the machine. The
installer's options:

- `--framework-dependent` builds against an installed .NET 10 runtime instead (a much smaller install);
- `--from DIR` installs a directory you published yourself;
- `--rid` picks another runtime identifier.

Run the installer again to upgrade in place. `./packaging/linux/uninstall.sh` removes those four things and nothing
else. Your settings and bookmarks, in `~/.config/Enigma.FileExplorer`, are left as they are.

## Solution layout

| Path                                          | What it is                                                                   |
|-----------------------------------------------|------------------------------------------------------------------------------|
| `src/Enigma.FileExplorer.Core`                | Listings, file operations, search, keymaps, layout, settings — no UI         |
| `src/Enigma.FileExplorer.Desktop`             | The Avalonia app: composition root, commands, services, ViewModels, Views    |
| `tests/Enigma.FileExplorer.Core.UnitTests`    | xUnit v3 tests for the core library                                          |
| `tests/Enigma.FileExplorer.Desktop.UnitTests` | xUnit v3 tests for the app, including headless Avalonia tests                |
| `packaging/icon`                              | The application icon's SVG source                                            |
| `packaging/linux`                             | Linux install, uninstall and publish scripts, desktop entry, icons           |
| `packaging/windows`                           | The Windows publish scripts                                                  |
| `docs/`                                       | Roadmap, plans and completion records                                        |

## License

MIT — see [LICENSE.md](LICENSE.md).
