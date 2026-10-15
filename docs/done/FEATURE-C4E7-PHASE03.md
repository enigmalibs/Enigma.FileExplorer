# FEATURE-C4E7-PHASE03 — Pane browsing & navigation UI

**Item:** FEATURE-C4E7 — Foundation & directory browsing
**Branch:** `feature/feature-c4e7-phase03-pane-browsing`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The explorer now browses. A `PaneViewModel` owns one split group's state:

- the current directory, read asynchronously; a newer navigation cancels and supersedes an older read;
- back/forward history that remembers the cursor entry per location;
- the cursor and the marks;
- the path bar's breadcrumb and edit mode;
- the hidden-entries and sort settings later items switch.

Failures keep the pane where it was and report on the status line. The first load falls back from a
missing start directory to its nearest existing ancestor.

`PaneView` renders a virtualized details list (icon, name, modified, type, size) and translates mouse
gestures into ViewModel calls:

- click moves the cursor;
- Ctrl/Cmd+click toggles a mark;
- Shift+click marks a range;
- double-click opens;
- clicking the empty area clears the marks;
- the mouse's back/forward buttons go back and forward in history.

`PathBarView` shows history/parent/refresh buttons and a clickable breadcrumb. A click on its empty
area switches to a text box: Enter navigates (`~`, relative paths, and a file path opens its folder
on the file), and Esc or leaving the box cancels. Files open with the OS default application through
`ShellLauncher`. The main window shows the pane and a status bar: a vim-style message line on the
left, and the item/marked summary on the right.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Places/IKnownFolders.cs`, `KnownFolders.cs`
- `src/Enigma.FileExplorer.Desktop/Formatting/ByteSize.cs`
- `src/Enigma.FileExplorer.Desktop/Services/` — `IShellLauncher`, `ShellLauncher`, `IStatusMessages`,
  `StatusMessages`, `StatusMessage`
- `src/Enigma.FileExplorer.Desktop/ViewModels/` — `EntryViewModel`, `PathSegment`, `ClickModifiers`,
  `PaneViewModel`, `IPaneFactory`, `PaneFactory`
- `src/Enigma.FileExplorer.Desktop/Views/` — `PaneView.axaml(.cs)`, `PathBarView.axaml(.cs)`
- `tests/Enigma.FileExplorer.Desktop.UnitTests/Infrastructure/` — `FakeDirectoryReader`, `TestEntries`,
  `FakeShellLauncher`, `FakeKnownFolders`, `PaneHarness`, `TempDirectory`
- `tests/Enigma.FileExplorer.Desktop.UnitTests/` — `ViewModels/PaneViewModelTests`,
  `ViewModels/MainWindowViewModelTests`, `ViewModels/PathSegmentTests`, `Formatting/ByteSizeTests`,
  `Services/ShellLauncherTests`, `Views/PaneViewTests`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — `IKnownFolders`
- `src/Enigma.FileExplorer.Desktop/ViewModels/MainWindowViewModel.cs` — pane, title, status summary
- `src/Enigma.FileExplorer.Desktop/Views/MainWindow.axaml(.cs)` — pane + status bar, initial focus
- `src/Enigma.FileExplorer.Desktop/App.axaml.cs` — posts the first directory load
- `src/Enigma.FileExplorer.Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — new services
- `src/Enigma.FileExplorer.Desktop/Themes/Styles.axaml` — row, header, breadcrumb, status bar styles
- `docs/roadmap.md`, `docs/plan/FEATURE-C4E7.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What a plain click does | Moves the cursor and clears the marks; it does not mark | Keeps one meaning of "selection" for mouse and keys: marks are explicit, and an operation targets the marks or else the cursor entry (`GetTargets`), as in ranger/vifm |
| Naming | `IsMarked`/`MarkedCount` instead of the plan's `IsSelected` | "Selected" is ambiguous once a cursor exists; the vim file-manager word is "mark" |
| Cursor vs mark styling | Cursor = full-row selection fill; marks = accent bar + semibold name | Both stay readable on the same row |
| Items collection | An immutable list replaced per load | One change notification per load instead of thousands; the virtualized list rebuilds cheaply |
| Status line | A singleton `IStatusMessages` (vim message line) | Every pane, and later every command, reports through one place |
| Home directory | `IKnownFolders` in Core, ahead of PHASE04's places | `~` and the start directory need it now; injectable for tests |
| A file typed in the path bar | Opens its folder with the cursor on the file | A typed path is often copied from somewhere; refusing it would be pedantic |
| Mouse back/forward buttons | Wired to history | Expected by mouse users; costs four lines |
| Disabled path-bar buttons | Transparent background | FluentTheme's grey disabled fill looked like a pressed state on the dark bar |

## Deviations & follow-ups

- Keyboard handling is deliberately absent; it arrives with FEATURE-2E0F. The ViewModel already exposes
  the members the keys will drive (`MoveCursor`, `OpenCursorAsync`, `GoParentAsync`, marks, history).
- The breadcrumb shows a trailing chevron after the last segment (as Windows Explorer does).

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx --no-incremental
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 147  failed: 0
```

Manual check: the app launched on the home directory and a captured window screenshot showed the
breadcrumb, the column header, folders first with icons, dates, types and sizes, and the status bar
summary ("18 items"). The first full build and test run was green; no fix cycle was used.
