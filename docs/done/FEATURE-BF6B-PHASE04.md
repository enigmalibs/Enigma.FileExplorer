# FEATURE-BF6B-PHASE04 — Clipboard, transfer queue & conflicts

**Item:** FEATURE-BF6B — File operations
**Branch:** `feature/feature-bf6b-phase04-clipboard-transfers`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

Copy, cut and paste work between panes, and with other applications.

- **Clipboard.** `yy`/Ctrl+C copies and `dd`/Ctrl+X cuts the marks, or else the cursor entry, into
  the explorer's `FileClipboard`. It is authoritative for cut and mirrors the files to the system
  clipboard so other applications can paste them. Cut entries are faded in every pane until they are
  pasted or the clipboard changes. The marks are cleared and visual mode ends.
- **Paste.** `p`/Ctrl+V pastes into the active pane's directory. If files were copied in another
  application since (the system clipboard no longer matches), those are pasted as a copy instead. A
  successful paste of a cut empties the clipboard. The target pane refreshes with the cursor on the
  first pasted entry, and other panes showing a source or the target refresh too.
- **`yp`** copies the marked or cursor paths as text, one per line.
- **Transfer queue.** `TransferQueue` runs transfers one at a time. The status line shows the running
  one (title, current file, a progress bar and a ✕ button; `:cancel` also stops it), then a one-line
  outcome: done, N skipped, N failed, or cancelled. Failures open a dialog listing them (up to 20). A
  request refused outright, such as a folder into itself, is reported on the status line.
- **Conflicts.** `ConflictDialogService` asks "'name' already exists in 'folder'" with both sides'
  size and date. Overwrite / Keep both / Skip are buttons, "Do this for every remaining conflict" is a
  checkbox, and Esc cancels the whole transfer. It is marshalled to the UI thread for the engine.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Services/` — `ISystemClipboard`, `SystemClipboard`, `IFileClipboard`,
  `FileClipboard`, `ITransferQueue`, `TransferQueue`, `ITransferErrorReport`, `TransferErrorReport`,
  `ConflictDialogService`
- `src/Enigma.FileExplorer.Desktop/Commands/ClipboardCommands.cs`
- `src/Enigma.FileExplorer.Desktop/ViewModels/ConflictViewModel.cs`, `Views/ConflictView.axaml(.cs)`
- Tests: `Commands/ClipboardCommandsTests`; `Infrastructure/FakeSystemClipboard`, `FakeTransferEngine`,
  `FakeTransferErrorReport` (+ `FixedConflictResolver`)

**Modified**

- `Desktop/ViewModels/EntryViewModel.cs` (`IsCut`), `PaneViewModel.cs` (`ApplyCut`),
  `WorkspaceViewModel.cs` (fades cut entries in every pane), `MainWindowViewModel.cs` (transfers, cancel)
- `Desktop/Views/MainWindow.axaml` (transfer progress on the status line), `PaneView.axaml` (`cut` class),
  `Desktop/Themes/Styles.axaml` (faded cut entries)
- `Desktop/App.axaml.cs` — hands the window to the system clipboard
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — the services and the module
- Tests: `Infrastructure/ExplorerHarness`; `Input/NormalModeTests` (the unwired-command test uses a
  navigation-only harness)
- `docs/roadmap.md`, `docs/plan/FEATURE-BF6B.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| App vs system clipboard on paste | Ours, unless the system clipboard now holds different files | Copying in another application after `yy` is a clear signal of intent; our cut must not hijack it |
| Cut on the system clipboard | Published as a plain copy | Cut has no portable representation; pasting our cut elsewhere copies, which is safe |
| Conflict buttons | Overwrite (primary), Keep both (secondary), Skip (close, default); Esc = cancel transfer | The dialog has three buttons; the most destructive one is never the default |
| Progress threading | `Progress<T>` created on the caller's (UI) context inside the queue | Engine updates land on the UI thread without the queue knowing about Avalonia |
| Refresh after paste | The target pane with the cursor on the first pasted entry, plus panes showing a source or the target | The watcher would catch up, but the explicit refresh makes the result visible at once |
| Status after a paste | The queue's outcome line, not a second message | One line, one outcome |

## Deviations & follow-ups

- **Manual check** of exchanging files with Explorer/Dolphin through the system clipboard was not
  possible from this session: synthetic input is refused here, and the clipboard needs an interactive
  desktop. The adapter only calls `SetFilesAsync`/`TryGetFilesAsync`/`SetTextAsync` and never throws.
  Please try `yy` here then paste in Explorer, and the reverse.
- A throwaway headless render (not committed) confirmed the conflict dialog and the status-line
  progress with its cancel button.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 553  failed: 0  skipped: 2
```

Fix cycles used: **1 of 3.** The first run failed only the PHASE02 test that used `yy` as its example
of an unwired command. Copy is wired now, so that test builds a navigation-only harness and stays
valid as later items wire more commands.
