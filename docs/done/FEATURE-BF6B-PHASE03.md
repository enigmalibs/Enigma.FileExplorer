# FEATURE-BF6B-PHASE03 — Create, rename & delete UI

**Item:** FEATURE-BF6B — File operations
**Branch:** `feature/feature-bf6b-phase03-create-rename-delete-ui`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

`FileCommands` wires creating, renaming and deleting to keys and ex commands. Every operation runs
off the UI thread, refreshes the pane and reports on the status line.

- **Create.** `a` (or `:touch name`) creates a file, and a name ending in `/` creates a folder
  instead. `A`, Ctrl+Shift+N or `:mkdir name` creates a folder. The cursor lands on what was created.
- **Rename.** `r`, `cw`, F2 or `:rename name` prompts with the current name and its stem selected, so
  typing keeps the extension; a folder's whole name is selected. The cursor follows the renamed entry.
- **Invalid names.** An invalid or taken name is reported and the prompt reopens with what was typed;
  cancelling it ends the command.
- **Trash.** `dD`, Del or `:delete` moves the marks, or else the cursor entry, to the trash. Entries
  whose drive has no trash need an explicit "Delete permanently" confirmation first; declining
  changes nothing.
- **Permanent delete.** Shift+Del or `:delete!` asks first ("Delete 'x' permanently? This cannot be
  undone.", or "Delete 5 items…"), and only the confirming button deletes.
- **Afterwards.** Visual mode ends, the marks are cleared, and the cursor lands on the first surviving
  entry after the deleted ones (else the last one before them). Failures are named:
  "Could not delete a.txt, b.txt and 3 more".

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Commands/FileCommands.cs`
- `src/Enigma.FileExplorer.Desktop/Services/IConfirmationService.cs`, `ConfirmationService.cs`
- Tests: `Commands/FileCommandsTests`; `Infrastructure/FakeFileOperations`, `FakeTrash`, `ScriptedDialogs`
  (`ScriptedPrompt`, `ScriptedConfirmation`)

**Modified**

- `Core/Commands/CommandCatalog.cs` — the New file title mentions the trailing `/`
- `Desktop/ViewModels/PaneViewModel.cs` — `RefreshAsync(focusName, keepMarks)`
- `Desktop/Input/KeyboardController.cs` — `LastCommand` (internal, for tests to await)
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — file commands, confirmation service
- Tests: `Infrastructure/ExplorerHarness` (file commands, fakes, `PressAsync`, `RunAsync`),
  `Infrastructure/FakeDirectoryReader` (`EntriesOf`, `Find`)
- `docs/roadmap.md`, `docs/plan/FEATURE-BF6B.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What rename acts on | The cursor entry, even with marks | vim's `cw` works on what is under the cursor; bulk rename is a different feature |
| Prompt wording for `a` | "New file (end with / for a folder): " | Makes the yazi/lf trailing-slash convention discoverable where it is used |
| Mixed trash/no-trash targets | Trash what can be, ask once about the rest | The safe default stays the default; only what truly cannot be recovered needs a yes |
| Confirmation default button | Cancel | An Enter typed too fast must not destroy data |
| Prompt service in tests | A scripted fake | The real command line is covered by its own tests; scripting answers keeps these about the commands |

## Deviations & follow-ups

- **Inline rename in the list** is not done; rename uses the bottom prompt (as planned).
- **Real dialog not driven by tests:** `ConfirmationService` (the real dialog) is exercised only
  through composition. Its logic is one line: Primary means yes.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 540  failed: 0  skipped: 2
```

Fix cycles used: **1 of 3.** The first run failed `ShiftA_ATakenName_IsReported_AndCancellingStops`: a
real bug, where cancelling the correction prompt reopened a fresh one (`typed ??= …` at the top of the
loop). Create and rename now ask once up front, and a cancelled correction ends the command. Before
that run, a declined "delete permanently instead?" no longer refreshes or reports "Moved 0 items".
