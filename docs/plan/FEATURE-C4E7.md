# FEATURE-C4E7 — Foundation & directory browsing

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-c4e7-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Stand up the Enigma.FileExplorer solution to house conventions and deliver a working single-pane
directory browser: an Avalonia 12 shell themed with Enigma.Avalonia.Desktop, a UI-free Core library
that lists and sorts directories, a pane that navigates the file system (into, parent, back, forward,
path bar), opens files with the system's default application, a places sidebar, file-type icons from
Enigma.Icons.Avalonia, and automatic refresh when the directory changes on disk.

Every later item (keyboard, splits, views, file operations, search, mouse) builds on the pane and the
Core types introduced here.

## Context & constraints

- Greenfield repository: one commit with a placeholder `README.md`. Every root artifact comes from the
  house templates (`dotnet-solution-setup` bootstrap checklist, `dotnet-solution-config`,
  `git-repo-hygiene`).
- Layout mirrors the sibling apps (Enigma.GitClient, Enigma.Launchpad): `src/Enigma.FileExplorer.Core`
  (UI-free, app-internal, `net10.0` only), `src/Enigma.FileExplorer.Desktop` (Avalonia `WinExe`),
  `tests/Enigma.FileExplorer.Core.UnitTests`, `tests/Enigma.FileExplorer.Desktop.UnitTests` (headless).
- Package set pinned exactly like Enigma.GitClient: Avalonia 12.1.1 (coupled set, bumped together),
  Enigma.Avalonia.Desktop 1.2.0, Enigma.Icons.Avalonia 1.0.0, CommunityToolkit.Mvvm 8.4.2,
  Microsoft.Extensions.Hosting 10.0.13, xunit.v3 4.0.2, Microsoft.Testing.Extensions.CodeCoverage
  18.12.0, Avalonia.Headless / Avalonia.Skia 12.1.1.
- Explicit MVVM (no source generators), IHost composition root, file-scope `using` directives (the
  `Enigma.Avalonia` ↔ `Avalonia` namespace collision), `x:DataType` on every view and template.
- Target platforms: Windows and Linux; macOS code paths kept portable but untested.
- The pane's ViewModel is the single source of truth for cursor and selection; views only render it
  (later items drive the same model from the keyboard).

## Design

```
src/Enigma.FileExplorer.Core/
  FileSystem/      FileSystemEntry, EntryKind, IDirectoryReader, DirectoryReader, HiddenRules,
                   NaturalStringComparer, SortKey, SortDirection, EntrySort, EntrySorter, PathUtilities
  Places/          Place, PlaceKind, IPlacesProvider, PlacesProvider
  DependencyInjection/ServiceCollectionExtensions (AddFileExplorerCore)
src/Enigma.FileExplorer.Desktop/
  Program, App, app.manifest, DependencyInjection/ServiceCollectionExtensions
  Views/           MainWindow, PaneView, PlacesView
  ViewModels/      MainWindowViewModel, PaneViewModel, EntryViewModel, PlacesViewModel, PlaceViewModel
  Services/        IShellLauncher/ShellLauncher, IDirectoryWatcherFactory/DirectoryWatcher, FileIcons
  Controls/        PathBar (breadcrumb + editable path)
  Themes/          Styles.axaml
```

## PHASE01 — Solution scaffold & app shell

**Status:** DONE — see `docs/done/FEATURE-C4E7-PHASE01.md`
**Branch:** `feature/feature-c4e7-phase01-scaffold`

### Steps

1. Root files from the templates: `.gitignore`, `.gitattributes` (git-repo-hygiene), the full C#
   `.editorconfig`, `Directory.Build.props` (authors Josué Clément, 2026, `<Version>0.1.0</Version>`),
   `Directory.Packages.props` (the package set above), `global.json` (SDK 10.0.100 `latestFeature` +
   MTP runner), `LICENSE.md` (MIT), empty `RELEASENOTES.md`. `README.md` already exists — kept.
2. `Enigma.FileExplorer.slnx` with `/src/` and `/tests/` folders and the four projects.
3. Core csproj (`net10.0`, `GenerateDocumentationFile`, single-TFM reason in a comment) with an
   `AddFileExplorerCore()` registration extension.
4. Desktop csproj (`WinExe`, compiled bindings, `app.manifest`, `InternalsVisibleTo` the tests),
   `Program`, `App` (IHost started not run, Enigma theme `ResourceInclude`, Dark starting variant),
   `AddEnigmaDesktopServices()` + `AddFileExplorerApp()`, `MainWindow` with the three Enigma host
   controls (`ContentDialog`, `Overlay`, `InfoBar`) registered once at startup.
5. Test projects with the headless fixture (Desktop) and one smoke test each; a composition-root test
   that validates the container (`ValidateOnBuild`, `ValidateScopes`).

### Acceptance criteria

- `dotnet build Enigma.FileExplorer.slnx` succeeds with zero warnings (AVLN included).
- `dotnet test --solution Enigma.FileExplorer.slnx` passes (Core smoke + Desktop composition/headless
  window test).
- The app starts and shows an Enigma-themed window titled "Enigma.FileExplorer".

## PHASE02 — Core directory listing & sorting

**Status:** DONE — see `docs/done/FEATURE-C4E7-PHASE02.md`
**Branch:** `feature/feature-c4e7-phase02-directory-listing`

### Steps

1. `FileSystemEntry` (immutable record: name, full path, kind, size for files, last write time,
   hidden, symbolic link, extension) and `EntryKind` (Directory, File).
2. `HiddenRules`: hidden = name starts with `.` or carries `FileAttributes.Hidden`.
3. `IDirectoryReader.ReadAsync(path, cancellationToken)` → `DirectoryListing` (path + entries); runs
   the enumeration off the calling thread, `IgnoreInaccessible`, skips nothing by attribute (hidden
   filtering is the caller's choice); throws `DirectoryNotFoundException` /
   `UnauthorizedAccessException` for the directory itself so the UI can report it. Broken entries
   (vanished between enumeration and stat) are skipped.
4. `NaturalStringComparer` (case-insensitive, digit runs compared numerically: `file2` < `file10`).
5. `EntrySort` (key: Name, Modified, Size, Type; direction) and `EntrySorter`: directories always
   first, then the key, ties broken by natural name.
6. `PathUtilities`: normalize (trailing separator, `~` expansion, relative to a base), parent,
   volume root, nearest existing ancestor.
7. Unit tests against temp directories.

### Acceptance criteria

- Listing a temp directory returns every entry with correct kind/size/hidden flags.
- Missing directory → `DirectoryNotFoundException`; cancellation honoured.
- Natural sort and every sort key (both directions) verified by tests; directories always first.
- `PathUtilities` cases (root parent is null, `~`, relative, nearest existing ancestor) tested.

## PHASE03 — Pane browsing & navigation UI

**Status:** TODO
**Branch:** `feature/feature-c4e7-phase03-pane-browsing`

### Steps

1. `EntryViewModel` (entry + display strings: size, modified date in current culture, type text;
   `IsCursor`, `IsSelected`).
2. `PaneViewModel`: `CurrentPath`, `Items`, `CursorIndex`, `IsLoading`, `ErrorMessage`, show-hidden
   flag; `NavigateAsync(path)` (cancels the previous load; failure keeps the current directory and
   reports the error); `OpenAsync(entry)` (directory → navigate, file → `IShellLauncher`);
   `GoParentAsync` (cursor lands on the directory we came from); back/forward history that remembers
   the cursor item per location; selection model: click = cursor + single selection, Ctrl+click
   toggles, Shift+click selects the range, double-click opens.
3. `IShellLauncher`: open a file with the default app (`UseShellExecute` on Windows, `xdg-open` on
   Linux, `open` on macOS); failures are reported, never thrown into the UI.
4. `PaneView`: details list (icon placeholder, name, modified, type, size) as a virtualized
   `ItemsControl` whose rows render `IsCursor`/`IsSelected`; mouse handling maps onto the ViewModel;
   cursor kept in view.
5. `PathBar` control: breadcrumb segments (click navigates) and an edit mode (TextBox; Enter navigates,
   Esc cancels); empty-directory placeholder and error line.
6. `MainWindowViewModel` hosts a single pane (start directory: user home) and a minimal status bar
   (item count, selection count, message).
7. Tests: PaneViewModel navigation/history/selection with a fake reader and launcher; headless view
   test that the pane renders rows.

### Acceptance criteria

- Clicking/double-clicking navigates; parent and back/forward restore the cursor item.
- An unreadable or missing directory leaves the pane where it was with an error message.
- Path bar breadcrumb and edit mode navigate correctly.
- Files open with the default application (manual check); launcher failure shows a message.
- Build zero warnings, all tests green.

## PHASE04 — Places, file icons & auto-refresh

**Status:** TODO
**Branch:** `feature/feature-c4e7-phase04-places-icons-refresh`

### Steps

1. Core `IPlacesProvider`: well-known folders that exist (Home, Desktop, Documents, Downloads,
   Pictures, Music, Videos) + ready drives (Windows) / root and real mounts (Linux: `/`, `/home`,
   `/media/*`, `/mnt/*`, `/run/media/<user>/*`; pseudo file systems excluded); tested with injected
   inputs.
2. `PlacesView` sidebar (Phosphor icon + label), click navigates the active pane, current place
   highlighted.
3. `FileIcons`: Phosphor glyph per entry (folder, folder-open for symlink dirs, file-text, file-code,
   file-image, file-audio, file-video, file-pdf, file-zip, file-doc/xls/ppt families, generic file)
   chosen by extension; rendered with `<ei:Icon>` so it follows the theme.
4. Auto-refresh: a debounced (250 ms) `FileSystemWatcher` per pane on its current directory
   (non-recursive); refresh keeps the cursor item and selection by name; if the current directory
   disappears, the pane moves to the nearest existing ancestor with a message. Watcher errors
   (buffer overflow) trigger a full refresh.
5. Tests: places filtering, icon mapping, refresh-preserves-cursor, vanished-directory fallback (with a
   fake watcher).

### Acceptance criteria

- Sidebar lists existing places and drives/mounts; clicking navigates.
- Entries show type-appropriate icons in both themes.
- Creating/deleting a file externally updates the pane within ~a second, preserving the cursor.
- Build zero warnings, all tests green.

## Out of scope

- Keyboard handling (FEATURE-2E0F), splits (FEATURE-95D6), icon view modes and sorting UI
  (FEATURE-2A19), file operations (FEATURE-BF6B), search (FEATURE-6964), context menus and drag & drop
  (FEATURE-A6C2), session persistence (FEATURE-CA94).
- Native shell icons and image thumbnails; "Open with…"; tabs; an application `.ico` (art is
  user-owned — follow-up).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Solution layout | Core + Desktop + Core.UnitTests + Desktop.UnitTests | House pattern of every sibling app; Core stays UI-free and unit-testable | Single project (untestable logic in the UI) |
| Core TFM | `net10.0` only | App-internal library never published; netstandard2.0 would only buy polyfills | Multi-targeting |
| Package versions | GitClient's coupled set (Avalonia 12.1.1, Enigma.Avalonia.Desktop 1.2.0, …) | The exact set Enigma.Avalonia.Desktop 1.2.0 is built against; proven in siblings | Newest Avalonia 12.1.3 alone (breaks coupling) |
| Selection ownership | ViewModel owns cursor + marks; views render | One source of truth for mouse and vim keys; fully testable | ListBox selection synced with a VM (two sources of truth) |
| List control | Virtualized `ItemsControl` with custom row rendering | No built-in keyboard/selection to fight; `ScrollIntoView` available | `ListBox`/`DataGrid` (selection and key handling conflict with vim model) |
| Opening files | Default application via the OS (`UseShellExecute` / `xdg-open` / `open`) | What every file manager does | Built-in viewers |
| Hidden definition | Dot-prefix or Hidden attribute | Matches Linux and Windows expectations | Attribute only |
| Sort order | Natural, case-insensitive, directories first | Matches Explorer/Dolphin | Ordinal |
| Places | Existing well-known folders + drives/real mounts | "Navigate in system directories" from the draft | Full bookmarks system (follow-up) |
| Auto-refresh | Debounced `FileSystemWatcher` on the pane's directory | Panes stay truthful after external or own changes | Manual refresh only |
| Start directory | User home | Predictable; session restore comes in FEATURE-CA94 | Current working directory |
| App icon | None in v1 | Icon art is user-owned per house convention; a missing file would fail the build | Generated placeholder art |
