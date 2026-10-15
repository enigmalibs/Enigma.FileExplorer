# FEATURE-743A — App icon & About dialog

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-743a-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-app-icon-about

## Objective

Give the explorer the two product surfaces Enigma.GitClient has, in the same style:

1. **An application icon**: on the Windows executable, and on the running window on every platform.
2. **An About dialog** as GitClient shows it today (FEATURE-62B7 there):
   - the application's icon;
   - its name;
   - the version;
   - the build it was cut from, when there is one;
   - the copyright;
   - one **Close** button at the bottom right.

## Context & constraints

- **The reference is Enigma.GitClient:**
  - `packaging/icon/enigma-git-client.svg` (48–256 px) and `-small.svg` (hinted for 16 and 32 px),
    each with its regeneration recipe in a header comment;
  - `Assets/app.ico`: six 32-bit PNG frames, 16, 32, 48, 64, 128, 256, smallest first;
  - `Assets/app-256.png`: the 256 frame, byte for byte, which the About view shows;
  - `<ApplicationIcon>Assets/app.ico</ApplicationIcon>`, `<AvaloniaResource Include="Assets/**" />`
    and `Icon="/Assets/app.ico"` on the windows;
  - `Core/Diagnostics/ProductInformation` (`Name`, `DisplayName`, `Version`, `BuildSha`, `Copyright`)
    over `ProductVersion.From(informational, fallback)`, a pure parser of `X.Y.Z+<sha>`;
  - `AboutViewModel`, `AboutView` (380 wide, 96 px icon, name 18 SemiBold, "Version X", a selectable
    build line hidden when there is no revision, the copyright) and `AboutDialogService` (no title, no
    icon, one *Close* button that is the default);
  - `AppIconTests` and `AboutDialogTests`.
- **The explorer today:**
  - no icon and no `Assets/` folder;
  - the product name is a literal in two places: `MainWindowViewModel.ProductName` (window title)
    and `AppPaths.ApplicationName` (settings folder);
  - `Directory.Build.props` stamps `Version` 0.1.0 and the copyright; the SDK appends `+<full sha>`
    to the informational version in a git checkout.
- The explorer is keyboard-first: no toolbar, no menu bar. Its dialogs open from keys or ex commands,
  every command is described once in `CommandCatalog` (keys, ex commands, help), and its dialogs
  (help, bookmarks) close with `q` and `Esc`.
- `HostDialog` is 360–900 wide and at most 640 high: room for a 380-wide About view.
- No Linux or macOS packaging exists in this repository.

## Decisions

| Decision | Rationale |
|---|---|
| New artwork in the GitClient family: the same `#1E1F22` tile (240 units, corner 48), a folder in `#3574F0`, a `#3FB950` accent | The Enigma apps read as siblings; the folder says what this one is |
| SVG masters under `packaging/icon/`, frames packed into `Assets/app.ico`, the 256 frame copied to `Assets/app-256.png` | GitClient's layout, so regenerating works the same way in both repositories |
| `Core/Diagnostics/ProductInformation` + `ProductVersion` | GitClient's shape; the parsing is pure and testable in Core |
| Display name "Enigma File Explorer" | As "Enigma Git Client"; `Name` stays "Enigma.FileExplorer" (window title, settings folder) |
| `:about` opens it; listed in the help's General card; no key | No toolbar to hold a button; an ex command is this app's idiom, and keys are scarce |
| Closes with *Close* (Enter), `Esc` and `q` | `q` is how the explorer's other dialogs close |

## PHASE01 — Application icon

**Status:** TODO
**Branch:** `feature/feature-743a-phase01-app-icon`

### Steps

1. `packaging/icon/enigma-file-explorer.svg` — the master for the 48, 64, 128 and 256 frames, with
   the regeneration recipe in its header comment.
2. `packaging/icon/enigma-file-explorer-small.svg` — the same drawing hinted for 16 and 32 px
   (heavier shapes, details that vanish at that size dropped).
3. Render the six frames as 32-bit RGBA PNGs and pack them, unchanged and smallest first, as the PNG
   entries of `src/Enigma.FileExplorer.Desktop/Assets/app.ico`; copy the 256 frame byte for byte to
   `Assets/app-256.png`.
4. `Enigma.FileExplorer.Desktop.csproj`: `<ApplicationIcon>Assets/app.ico</ApplicationIcon>` and
   `<AvaloniaResource Include="Assets/**" />`.
5. `MainWindow.axaml`: `Icon="/Assets/app.ico"`.
6. Tests (`Desktop.UnitTests`):
   - the icon holds six square 32-bit PNG frames, 16 to 256, each PNG's own header agreeing;
   - `app-256.png` is the icon's largest frame byte for byte and decodes as a 256 × 256 bitmap;
   - the main window has an icon.

### Acceptance criteria

- The built `.exe` carries the icon, and the running window shows it in its title bar and taskbar.
- The six frames are legible at their size: the folder reads at 16 px.
- Build clean with zero warnings (no AVLN warning); the whole suite green.

## PHASE02 — About dialog

**Status:** TODO
**Branch:** `feature/feature-743a-phase02-about-dialog`

### Steps

1. `Core/Diagnostics/ProductVersion.cs` — `readonly record struct ProductVersion(string Version,
   string? BuildSha)` with a pure `From(informational, fallback)`: the version is everything before
   the first `+`, the sha is shortened to 7 characters, and a blank or missing value falls back
   (to the assembly version, then `0.0.0`) instead of throwing.
2. `Core/Diagnostics/ProductInformation.cs` — `Name` ("Enigma.FileExplorer"), `DisplayName`
   ("Enigma File Explorer"), `Version`, `BuildSha`, `Copyright`, read once from the Core assembly.
   `MainWindowViewModel.ProductName` becomes `ProductInformation.Name`.
3. `Desktop/ViewModels/AboutViewModel.cs` — the name, the version, the build line and the copyright,
   `HasBuildSha` / `HasCopyright`, a `CloseCommand` over a close callback, and a constructor seam taking
   the values for tests.
4. `Desktop/Views/AboutView.axaml(.cs)` — GitClient's view, 380 wide, showing `/Assets/app-256.png`.
   It takes the keyboard as it opens, and `q` closes it.
5. `Desktop/Services/IAboutDialogService.cs` / `AboutDialogService.cs` — the view in the window's
   content dialog: no title, no icon, one *Close* button, the default. Registered in
   `AddFileExplorerApp`.
6. Command:
   - `CommandIds.About` ("app.about");
   - in the General category, "About Enigma File Explorer";
   - ex command `about`, abbreviable to `ab`;
   - handled beside the help in `CommandLineCommands`.
7. Tests:
   - `ProductVersion.From`: plain, with a sha, a short sha, blank, a leading `+`, a fallback;
   - `ProductInformation`: name, display name, version without `+`, copyright;
   - the ViewModel's build and copyright lines appear only when there is something to show;
   - the service shows the view untitled, with one *Close* button;
   - `:about` opens it;
   - in the real window, the dialog shows the name, the version and the icon, does not scroll at the
     window's minimum height, and `q` closes it.

### Acceptance criteria

- `:about` (or `:ab`) opens a dialog with the icon, "Enigma File Explorer", "Version 0.1.0", the build
  line when there is a revision, the copyright, and a *Close* button. Nothing else.
- *Close*, Enter, `Esc` and `q` close it; the listing gets the keyboard back.
- The help lists it in the General card.
- Build clean with zero warnings; the whole suite green.

## Out of scope

- A splash screen (GitClient has one; not asked for).
- Linux/macOS packaging, launcher entries and their icon copies.
- Renaming the product in the window title or the settings folder.
- A credits list, a licence viewer, links, or "check for updates".

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which GitClient About to copy | Today's simple one (FEATURE-62B7): icon, name, version, build, copyright, *Close* | "Like done in GitClient" means what it looks like now | The earlier titled box with a *BUILT WITH* list (GitClient removed it) |
| Splash screen | Not included | Not asked for; a separate product surface | Copying GitClient's splash too |
| Icon artwork | New art in the family: the dark tile, a blue folder, a green accent | Recognisably a sibling of Enigma Git Client, and says "files" | Reusing GitClient's art (wrong product); a Phosphor glyph on a tile (generic) |
| How the frames are produced | Rendered locally with Svg.Skia from the NuGet cache, packed by a throwaway script in the session scratchpad; the SVG header gives a recipe that works with any rasterizer | `rsvg-convert` (GitClient's tool) is not installed here; committing a renderer would add tooling the repository lacks | Committing a render script; drawing PNGs by hand |
| Linux icon copies | None | No Linux packaging exists here to install them | Adding `packaging/linux/icons/` with nothing using it |
| Where product identity lives | `Core/Diagnostics/ProductInformation` + `ProductVersion` | GitClient's layout; Core and Desktop carry one version; the parser is pure | A Desktop-only static class; reading attributes in the ViewModel |
| Product name literals | `MainWindowViewModel.ProductName` points at `ProductInformation.Name`; `AppPaths.ApplicationName` untouched | One source for the window title; the settings folder name is a storage contract | Changing the window title to the display name |
| Display name | "Enigma File Explorer" | GitClient's "Enigma Git Client" pattern | "Enigma.FileExplorer" (a technical name) |
| Entry point | `:about` (`:ab`), in the help's General card | The app has no toolbar or menu bar; every surface reads `CommandCatalog` | A key binding (keys are scarce, vim has none); a `:version` alias (a second name for one thing) |
| Closing keys | *Close* (the default button, so Enter), `Esc`, `q` | `Esc` comes with the library; `q` matches the help and the bookmarks | Only the button and `Esc` |
| Phasing | One item, two phases: icon first | The About view shows the icon asset; each phase is one reviewable commit | One combined dev; two separate items |
