# FEATURE-5ACD — Trash confirmation & d/D keys

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5acd-delete-keys`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

- `d` moves the targets to the trash and `D` deletes them permanently.
- Moving to the trash asks for confirmation. The confirmation can be switched off once the user trusts
  their fingers.

## Context & constraints

- Today `dD` and `Del` trash, `Shift+Del` deletes permanently, and `dd` cuts. The keymap is prefix-free,
  so `d` alone cannot coexist with `dd`: Cut has to move.
- `FileCommands.TrashAsync` only asks when an item cannot go to the trash. Permanent deletes always ask,
  through `IConfirmationService` (a `ContentDialog`, where only the primary button counts as a yes).
- Settings live in `settings.json` through `Session`. The hidden-files switch is the pattern: an
  observable option, saved at once on change, with `:set hidden`/`nohidden`/`hidden!` and completion
  values.

## Steps

1. Catalog:
   - Trash on `d` and `<Del>`;
   - DeletePermanently on `D` and `<S-Del>`;
   - Cut on `x` and `<C-x>`;
   - `:set` values gain `confirmtrash`, `noconfirmtrash` and `confirmtrash!`.
2. Core `AppSettings.ConfirmTrash` (bool, default true).
3. Desktop:
   - `ViewOptions` gains `ConfirmTrash` (renamed in its doc to "explorer options shared by every pane");
   - `Session` loads it, applies it, and saves at once when it changes;
   - `ViewCommands.SetOption` handles `confirmtrash`/`noconfirmtrash`/`confirmtrash!`/`invconfirmtrash`,
     with a status message.
4. `FileCommands.TrashAsync`: when `ConfirmTrash` is on, ask "Move 'name' / N items to the trash?" before
   anything else. A no does nothing. The existing "No trash here → delete permanently?" question still
   follows for items that cannot go to the trash.
5. `IConfirmationService.ConfirmAsync` gains an optional `defaultToConfirm` flag. The trash question
   sets it, so Enter confirms (the action can be undone). The permanent delete keeps Cancel as the
   default.
6. Tests:
   - `d` asks and trashes; a declined question leaves everything untouched;
   - with the option off, `d` trashes without asking;
   - `D` asks and deletes permanently;
   - `x` cuts;
   - `:set noconfirmtrash` switches the option and the session saves it;
   - the session loads the option;
   - existing tests move to the new keys.

## Acceptance criteria

- `d` on the cursor item, or on marked items, asks "Move … to the trash?". Enter or the button confirms;
  Esc or Cancel does nothing.
- `:set noconfirmtrash` (or `"confirmTrash": false` in settings.json) makes `d` trash at once. The setting
  survives a restart.
- `D` and `Shift+Del` always ask before a permanent delete. `x` and `Ctrl+X` cut.
- Build has zero warnings and all tests are green.

## Out of scope

- An undo for the trash, and a confirmation for cut or paste.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| New key for Cut | `x` (plus `Ctrl+X`) | vim's `x` deletes into a register, which is close to cutting; it is free | `X`; dropping the letter key |
| How the confirmation is configured | `confirmTrash` in settings.json **and** `:set [no]confirmtrash`, saved at once | The app overwrites settings.json at exit, so a file-only switch is fragile while it runs; `:set` matches `:set hidden` | settings.json only |
| Default button of the trash question | Confirm (Enter = yes) | The trash can be undone; `d` + Enter stays fast | Cancel as the default, as for permanent deletes |
| Where the option lives | `ViewOptions` (the shared, observable explorer options) | It already carries the persisted `ShowHidden` switch and `:set`; no new type for one flag | A new options class |
