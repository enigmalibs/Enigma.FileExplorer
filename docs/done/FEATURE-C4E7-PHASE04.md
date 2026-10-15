# FEATURE-C4E7-PHASE04 — Places, file icons & auto-refresh

**Item:** FEATURE-C4E7 — Foundation & directory browsing
**Branch:** `feature/feature-c4e7-phase04-places-icons-refresh`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

- **Places sidebar.** Core's `PlacesProvider` lists the user's existing well-known folders (Home,
  Desktop, Documents, Downloads, Pictures, Music, Videos — deduplicated, missing ones skipped), then the
  devices:
  - **Windows:** ready drives, named "Label (C:)".
  - **Linux:** `/` as Root plus user mounts under `/media`, `/mnt` and `/run/media`, with pseudo file
    systems dropped.
  - **macOS:** `/` plus `/Volumes/*`.

  The Linux Downloads folder comes from the XDG user-dirs file, so localized desktops work. The
  sidebar is filled off the UI thread and refreshed whenever the window is activated (drives come and
  go). It highlights the place the pane shows; a click navigates the pane.
- **File-type icons.** `FileIcons` maps about 120 extensions onto Phosphor's file-family glyphs (text,
  code by language, images, audio, video, archives, office documents, packages, keys). A link or
  junction directory shows a dashed folder.
- **Auto-refresh.** Each pane watches its directory with a debounced `FileSystemWatcher`: a 250 ms quiet
  period, capped at 1 s while events keep coming, reported on the UI thread. A change refreshes the
  listing and keeps the cursor and the marks by name. If the directory itself vanishes, a watched
  change or a manual refresh moves the pane to its nearest existing ancestor with an informational
  message. Panes are now `IDisposable` and stop watching when disposed.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Places/` — `XdgUserDirs`, `PlaceKind`, `Place`, `VolumeInfo`,
  `IVolumeSource`, `SystemVolumeSource`, `IPlacesProvider`, `PlacesProvider`, `PlacesPlatform`
- `src/Enigma.FileExplorer.Desktop/Services/` — `FileIcons`, `IDirectoryWatcherFactory`,
  `DirectoryWatcherFactory`
- `src/Enigma.FileExplorer.Desktop/ViewModels/` — `PlaceViewModel`, `PlacesViewModel`
- `src/Enigma.FileExplorer.Desktop/Views/PlacesView.axaml(.cs)`
- Tests: `Core.UnitTests/Places/PlacesProviderTests`, `PlacesProviderSystemTests`;
  `Desktop.UnitTests/Services/FileIconsTests`, `DirectoryWatcherFactoryTests`;
  `ViewModels/PaneAutoRefreshTests`, `PlacesViewModelTests`; `Views/PlacesViewTests`;
  `Infrastructure/FakeDirectoryWatcherFactory`, `FakePlacesProvider`

**Modified**

- `Core/Places/IKnownFolders.cs`, `KnownFolders.cs` — the six well-known folders besides home
- `Core/DependencyInjection/ServiceCollectionExtensions.cs` — volume source and places provider
- `Desktop/ViewModels/PaneViewModel.cs` — watching, `IDisposable`, vanished-directory recovery,
  load outcomes
- `Desktop/ViewModels/PaneFactory.cs`, `EntryViewModel.cs` (icons), `MainWindowViewModel.cs` (places)
- `Desktop/Views/MainWindow.axaml(.cs)` — sidebar with a splitter, refresh on activation
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs`, `Desktop/Themes/Styles.axaml`
- `tests/.../Infrastructure/FakeKnownFolders.cs`, `PaneHarness.cs`,
  `ViewModels/MainWindowViewModelTests.cs`
- `docs/roadmap.md`, `docs/plan/FEATURE-C4E7.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Downloads folder | XDG file on Linux, `~/Downloads` elsewhere | .NET has no Downloads special folder; Windows keeps the on-disk name `Downloads` in every language, while Linux desktops localize it |
| Watcher threading | The factory captures the caller's `SynchronizationContext` and posts the callback there | The pane stays free of Avalonia's dispatcher and is tested synchronously with a fake |
| Debounce | Trailing 250 ms, capped at 1 s | Coalesces copy bursts, yet a continuously written log file still refreshes every second |
| Refresh while a load is in flight | Skipped | That load shows the change anyway |
| Linked directories | Dashed folder glyph (the plan suggested folder-open) | "Open" reads as state; dashed reads as "not the real thing", which a link or junction is |
| When places refresh | At start and on window activation, off the UI thread | Catches plugged/unplugged drives without polling; disconnected network drives can block for seconds |
| Sidebar/path-bar buttons | `Focusable=false` | A mouse click on them must not steal the keyboard from the listing |
| Real-machine places test | Kept (`PlacesProviderSystemTests`) | It catches a provider that throws on real drive enumeration, which the pure tests cannot |

## Deviations & follow-ups

- **Deviation:** the plan's "current place highlighted" is a full-path match only; a pane inside
  Documents does not highlight Documents. This is deliberate, because a prefix match would also light
  up Home.
- **Follow-up:** native shell icons/thumbnails remain out of scope (as planned).
- A first test version blocked the headless UI thread on an `await` (deadlock). It was rewritten with
  the fixture's pumping `RunAsync` before the first full run.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 189  failed: 0
```

Manual check: the running app showed the sidebar with Home…Videos (OneDrive-redirected Desktop and
Documents resolved) and the devices "Windows (C:)" plus four network shares with network glyphs. Home
was highlighted, files carried their family icons, and the OneDrive junction showed a dashed folder.
The first full build and test run was green; no fix cycle was used.
