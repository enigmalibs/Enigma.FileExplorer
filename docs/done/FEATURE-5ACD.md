# FEATURE-5ACD — Trash confirmation & d/D keys

**Item:** FEATURE-5ACD — Trash confirmation & d/D keys
**Branch:** `feature/feature-5acd-delete-keys`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

**New keys.** `d` moves the targets to the trash, and `D` deletes them permanently. `Del` and `Shift+Del`
still do the same. Cut moved from `dd` to `x` (`Ctrl+X` is unchanged): with `d` bound on its own, the
prefix-free keymap cannot also hold `dd`.

**The trash asks first.** Moving to the trash now asks "Move 'name' / N items to the trash?". A no leaves
everything as it was, marks included.

- The confirming button is the default, so `d` then Enter is fast; the trash can be undone.
- A permanent delete still defaults to Cancel.
- Items that cannot go to the trash still get the existing "delete permanently instead?" question
  afterwards.

**Switching the question off.**

- `:set noconfirmtrash` switches it off, `:set confirmtrash` switches it back on, and
  `:set confirmtrash!` / `invconfirmtrash` toggles it. `:set` completion offers the new values.
- The choice is saved at once, as `confirmTrash` in `settings.json`, like the hidden-files switch.
- Deleting permanently always asks.

## Files / modules touched

**Modified**

- `Core/Commands/CommandCatalog.cs` — Trash on `d`/`Del`, DeletePermanently on `D`/`Shift+Del`, Cut on
  `x`/`Ctrl+X`; new `:set` completion values.
- `Core/Commands/CommandIds.cs` — the `Set` doc lists the new values.
- `Core/Settings/AppSettings.cs` — `ConfirmTrash` (default true).
- `Desktop/Services/ViewOptions.cs` — `ConfirmTrash` (default true); the doc now says "the explorer's
  options".
- `Desktop/Services/Session.cs` — loads, saves and save-on-change for `ConfirmTrash`.
- `Desktop/Commands/ViewCommands.cs` — the new `:set` values, each with a status message.
- `Desktop/Services/IConfirmationService.cs`, `ConfirmationService.cs` — `defaultToConfirm` (Primary or
  Close as the dialog's default button).
- `Desktop/Commands/FileCommands.cs` — the trash question; takes `ViewOptions`.
- `Desktop/Commands/ClipboardCommands.cs` — the doc names `x`.
- Tests:
  - `FileCommandsTests` — `d` asks with Enter as yes; declining does nothing; with the question off, `d`
    trashes at once; `D` asks with Cancel as the default. Existing trash tests switch the question off,
    since they test what trashing does.
  - `ViewCommandsTests` — `:set [no]confirmtrash[!]`.
  - `SessionTests` — the option loads and saves at once.
  - `CommandCatalogTests` — the new keys.
  - `ClipboardCommandsTests`, `ContextMenusTests`, `CommandLineViewModelTests` — moved to the new keys.
  - `ScriptedConfirmation` — records the default; `ExplorerHarness` — passes the options.
- `docs/roadmap.md`, `docs/plan/FEATURE-5ACD.md` — status.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How a test opts out of the question | Each test that is about trashing itself sets `Options.ConfirmTrash = false` in its class constructor | Keeps those tests about what trashing does, while dedicated tests cover the question; the product default stays on |
| Mixed targets (some trashable, some not) with the question on | The trash question comes first for all targets; the "no trash here" question follows for the others | One clear yes before anything happens; a permanent delete always gets its own question |
| How Enter maps to the default button | `DefaultButton` set on the Enigma `ContentDialog` | Its documentation calls the default button the keyboard-focused action. This was not checked by hand in the real window |

## Deviations & follow-ups

- No deviation from the plan.
- **Not verified by hand.** Whether Enter confirms in the real window depends on the Enigma `ContentDialog`
  giving the default button keyboard focus. The tests check that the flag is passed; nobody has tried it
  in the running app.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 710  failed: 0  succeeded: 707  skipped: 3
```

- 12 tests were added: 4 file-command tests, 1 view-command test, 1 session test and 6 catalog key cases.
- The first run was green, so no fix cycle was used.
- The three skips are the known platform-specific ones.
