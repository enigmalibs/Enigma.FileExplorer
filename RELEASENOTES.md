# Release notes

## 0.3.0 — 2026-11-03

A minor release:

- the **application icon drops its dark tile**: the open folder stands on a transparent background, in the taskbar,
  the window, the About dialog and the Linux launcher;
- a **Windows installer in one command**: `packaging\windows\buildmsi.ps1`, or `buildmsi.bat`, cleans the repository,
  publishes, and builds `artifacts\Enigma.FileExplorer.0.3.0.msi`.

### The icon without its tile

- **The same drawing, without the dark rounded square.** The open folder keeps its blues (`#4F7FDB`, `#6A9BF4`) and
  the green (`#63C77D`) of the page inside it, and stands on whatever is behind it.
- **The same files and sizes.** `app.ico` still holds ten frames, from 16 to 256 pixels. It is in the executable, on
  the window, and beside the executable as `app.ico`. The About dialog shows its 256-pixel frame, and the Linux
  installer installs eight sizes from `packaging/linux/icons`.

### The Windows installer

- **One command, from any directory**: `packaging\windows\buildmsi.ps1`, or `buildmsi.bat` from a `cmd` prompt.

  | Step                | What it does                                                                              |
  |---------------------|-------------------------------------------------------------------------------------------|
  | 1. `git clean -dxf` | Deletes every file git does not track: build output, `artifacts\`, IDE folders, new files |
  | 2. `publish.ps1`    | The usual publish: `-c Release -r win-x64 -p:PublishReadyToRun=true`                      |
  | 3. Enigma.Msi       | Packages the publish folder, uncompressed, into `artifacts\Enigma.FileExplorer.X.Y.Z.msi` |

- **What the MSI installs**: **Enigma File Explorer** for all users, in `%ProgramFiles%\Enigma File Explorer`, with a
  shortcut on the desktop and one in the program menu, both with the application's icon. In *Installed apps* it shows
  as Enigma File Explorer by Josué Clément.
- **Later MSIs replace it.** Every MSI carries the same upgrade code, the explorer's own. The code is written once in
  the script, and `WindowsInstallerIdentityTests` pins it.
- **The version is the one in `Directory.Build.props`.** The profile the MSI was built from is kept beside it, as
  `Enigma.FileExplorer.X.Y.Z.msipkg.json`. Nothing about the installer is edited for a release.
- **What it needs:** Windows, git, the .NET 10 SDK and the WiX CLI (`dotnet tool install --global wix`). The script
  checks them before it cleans anything. A copy of Enigma.Msi's worker (1.7.0) is in
  `packaging\windows\Enigma.Msi.Worker\`, so there is nothing else to install.
- **Commit or move new files first:** `git clean -dxf` deletes them.

### Upgrading from 0.2

- **On Windows, install the 0.3.0 MSI.** It is the first MSI of the explorer, so a folder you published with
  `publish.ps1` is not replaced. Delete it yourself if you no longer want it.
- **On Linux, run `packaging/linux/install.sh` again** to get the new icon in the launcher.
- **Nothing you saved changes.** `settings.json` keeps its format (schema version 1), so going back to 0.2 keeps
  everything.

### Dependencies

- **The Avalonia set stays at 12.1.4**, with Enigma.Avalonia.Desktop **1.3.0**. Avalonia 12.1.5 is out, but
  Enigma.Avalonia.Desktop 1.3.0, still the latest, is built against 12.1.4, and the set moves only with it. Nothing
  else is outdated (`dotnet list package --outdated`, 2026-11-03).
- The installer build uses Enigma.Msi 1.7.0's worker, on WixSharp_wix4 2.14.2. It is a build tool and is not part of
  the application.

### Version

- **0.3.0** is a minor release under Semantic Versioning. It changes the icon's artwork and adds the Windows
  installer build. It changes no behaviour, keeps the settings format and moves no dependency of the application.

## 0.2.0 — 2026-10-29

The first release: a keyboard-first file explorer for the desktop, driven by Vim's keys. It browses folders in panes
that split any way you like, copies, moves, renames and trashes files, searches names and contents, and remembers your
bookmarks and preferences. It runs on Windows, Linux and macOS from the same code. 0.1.0 was the development version
and was never released.

This release also brings the Enigma applications' common ground:

- an error that nothing else handles is shown in a dialog, with details to copy for a bug report, and the explorer
  keeps running;
- a new application icon;
- the icon is written beside the executable, as `app.ico`;
- a Linux installer;
- one publish script per platform;
- up-to-date libraries.

### Browsing

- **One pane at start**, in your home folder or in the folder named on the command line. The window title names the
  folder.
- `j` / `k` move, `l` or Enter opens (enters a folder, opens a file with its application), `h` or Backspace goes to the
  parent. `gg` and `G` go to the first and last entry; a count picks the n-th. `Ctrl+D` / `Ctrl+U` move half a page.
- `H` / `L` (or Alt+Left / Alt+Right) go back and forward. `gH` goes home, `g/` to the root of the volume.
- **The path bar** shows the folder as clickable segments. `Ctrl+L` edits it as text, and `:cd path` goes anywhere.
- **The places sidebar** lists your home and well-known folders, then the drives (Windows) or mounts (Linux, macOS).
  `g1` to `g9` jump to them.
- **File-type icons** for about 120 extensions. A link to a folder shows a dashed folder.
- **Auto-refresh.** A pane follows changes on disk and keeps the cursor and the marks. If its folder disappears, it
  moves to the nearest folder that still exists. `R` or F5 refreshes by hand.

### Vim keys and the command line

- **Marks.** `m` or Insert marks the entry and moves down, `v` marks a range, `Ctrl+A` marks everything, and Esc
  leaves visual mode, then clears the highlight, then the marks. Commands act on the marks, or else on the entry
  under the cursor.
- **The command line**, `:`, takes Vim-style commands with their abbreviations and completion:
  - `:cd`, `:split`, `:vsplit`, `:close`, `:only`;
  - `:mkdir`, `:touch`, `:rename`, `:delete` (`:delete!` deletes permanently);
  - `:find`, `:grep`;
  - `:view`, `:sort[!]`, `:set`, `:theme`;
  - `:refresh`, `:nohlsearch`, `:bookmarks`, `:cancel`;
  - `:help`, `:about`, `:quit`, `:qall`.

  The commands remember their history.
- **Quick-jump.** `/` finds an entry in the folder as you type and highlights every match; `n` / `N` go to the next
  and previous one, and `:noh` clears the highlight.
- **The leader key** is Space by default, and can be set as `leaderKey` in `settings.json`.
- **Help.** `?` or F1 lists every key and command, by category.
- **The status line** shows the mode, the pending keys, messages, and the running transfer.

### Split groups

- `Space v` and `Space s` split the pane side by side or top and bottom. `:vsplit` and `:split` do the same, with an
  optional folder.
- `gh`, `gj`, `gk`, `gl` move to the pane on that side; `Space w` / `Space W` to the next and previous one.
- `Space c` (or `Space q`) closes the pane and `Space o` keeps only this one. `Space =` makes them all equal, and
  `Space +`, `-`, `>`, `<` resize.

### View, sorting and hidden files

- Three views: **details**, **medium icons** and **large icons**, with `zd`, `zm`, `zl` or `:view`.
- Sort by **name** (natural order: `file2` before `file10`), **date modified**, **size** or **type**, with `on`, `om`,
  `os`, `ot`; `or` reverses. `:sort` does the same, and `:sort!` sorts in reverse. Folders always come first.
- `zh` shows or hides hidden files: names starting with a dot, and files with the hidden attribute.

### File operations

- **Create.** `a` creates a file (end the name with `/` for a folder), `A` or Ctrl+Shift+N a folder. **Rename** with
  `r`, `cw` or F2.
- **Copy, cut and paste.** `yy` / Ctrl+C copies, `x` / Ctrl+X cuts, `p` / Ctrl+V pastes into the pane's folder. Files
  are exchanged with other applications through the system clipboard both ways. Cut entries are faded until they are
  pasted. `yp` copies the paths as text.
- **Trash and delete.** `d` or Del moves to the trash, after a question you can switch off (`:set noconfirmtrash`).
  `D` or Shift+Del deletes permanently, and always asks.
- **Transfers** run one at a time. The status line shows the progress, with a button (or `:cancel`) to stop. An
  "already exists" question offers *Overwrite*, *Keep both* or *Skip*, for this item or every remaining one. Failures
  are listed in a dialog at the end.
- **Context menus.** A right-click, the Menu key or Shift+F10 opens a menu of the commands that apply, each with its
  key.
- **Drag and drop** within a pane, between panes, onto a place in the sidebar, and to or from other applications.

### Search

- `f` / Ctrl+F searches file **names**, and `F` / Ctrl+Shift+F file **contents**, from the pane's folder down. The
  search is smart-case, and `*` and `?` work in names.
- Results stream into the pane, nearest first, and act like a listing. Open a hit, reveal it in its folder with `gf`,
  or copy, move or trash it. `H` returns to the folder and `L` brings the results back.

### Bookmarks

- `B` bookmarks the pane's folder. `b` opens the bookmarks dialog: open one in this pane, add the current folder, or
  remove one.

### Settings and theme

- **Remembered between sessions:** the theme (`:theme dark`, `light` or `system`), the hidden-files switch, the trash
  question, the bookmarks, the window's place and size, and the view and sort of the pane you last used.
- They live in `settings.json`, written atomically:

  | Platform | Folder                                                    |
  |----------|-----------------------------------------------------------|
  | Windows  | `%APPDATA%\Enigma.FileExplorer`                           |
  | Linux    | `$XDG_CONFIG_HOME/Enigma.FileExplorer` (`~/.config`)      |
  | macOS    | `~/Library/Application Support/Enigma.FileExplorer`       |

- A file that cannot be read is kept as `settings.json.bad`, and the explorer starts with the defaults.

### When something goes wrong

- An error that nothing else handles — in a key, a command, a posted callback, or work running in the background — is
  logged and shown in a **"Something went wrong"** dialog. The dialog says the explorer is still running, gives the
  message, and counts any further errors. Its collapsed *Details* hold the type, message and stack of every error,
  with *Copy details* for a bug report.
- **One dialog at a time.** A burst of errors opens one dialog. An error that comes while another question is open
  waits for it to be answered. The same error is not shown again for a minute after you close it.
- `q`, Enter and Esc close it. A cancellation you asked for is never shown.

### Icon, About and packaging

- **A new application icon.** An open folder in the Enigma Soft palette, in ten sizes from 16 to 256 pixels for
  Windows.
- **`app.ico`** is written beside `Enigma.FileExplorer.Desktop(.exe)`, in the build output and in every published
  folder, for shortcuts and installers.
- **About.** `:about` shows the icon, the name, the version, the build it was cut from, and the copyright, in the same
  dialog as the other Enigma applications.
- **Linux installer.** `packaging/linux/install.sh` installs the explorer for you alone, with no root:
  - the application;
  - an `enigma-file-explorer [<directory>]` command;
  - a launcher entry;
  - the icon in eight sizes.

  `uninstall.sh` removes exactly that and leaves your settings.
- **Publish scripts.** `packaging/windows/publish.ps1` and `publish.bat` publish a Release build for `win-x64`, and
  `packaging/linux/publish.sh` one for `linux-x64`, both compiled ReadyToRun.

### Dependencies

- Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter **12.1.1 → 12.1.4**, together with
  Avalonia.Headless and Avalonia.Skia (tests).
- Enigma.Avalonia.Desktop **1.2.0 → 1.3.0**, which is built against Avalonia 12.1.4.
- Microsoft.Extensions.DependencyInjection(.Abstractions), Logging.Abstractions and Hosting **10.0.13 → 10.0.14**.
- Unchanged, already at their latest:
  - CommunityToolkit.Mvvm 8.4.2;
  - Enigma.Icons.Avalonia 1.0.0;
  - AvaloniaUI.DiagnosticsSupport 2.2.3;
  - xunit.v3 4.0.2;
  - Microsoft.Testing.Extensions.CodeCoverage 18.12.0.

### Compatibility

- Targets **.NET 10** (`net10.0`). Running it needs the .NET 10 runtime, unless it is installed with the Linux
  installer's default, which bundles the runtime.
- Windows 10/11, Linux (X11) and macOS.

### Version

- Released: **0.2.0**.
