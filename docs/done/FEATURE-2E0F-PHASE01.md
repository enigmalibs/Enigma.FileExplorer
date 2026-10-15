# FEATURE-2E0F-PHASE01 — Core keymap & ex-command parser

**Item:** FEATURE-2E0F — Vim keyboard & command system
**Branch:** `feature/feature-2e0f-phase01-keymap-engine`
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Summary

The UI-free command system the rest of the run plugs into:

- **Keymaps.** `KeyStroke` is a canonical keystroke: a layout-produced character, or a special key,
  plus Ctrl/Alt/Shift. `KeyNotation` parses and formats vim notation (`gg`, `<C-w>v`, `<S-Del>`,
  `<lt>`, `<C->>`) and describes sequences for people ("Ctrl+W v", "Shift+Del"). `Keymap` is a
  prefix-free trie (ambiguity and duplicates throw at construction). `KeySequenceResolver` resolves
  one stroke at a time with counts (`5j`, `10G`), `Esc` cancellation, vim-style discard of a broken
  sequence, and a `PendingText` for the status line's showcmd.
- **Commands.** `CommandIds` declares every command of the plan, including those later items wire.
  `CommandCatalog.Default` gives each one a title, a help category, its default keys (the plan's
  keymap table) and its ex command. `ExCommandParser` handles vim abbreviations (`:vs`, `:clo`, `:q`,
  `:qa`), `!` (including `:delete!` → permanent delete), quoted arguments, and argument requirements,
  with terse vim-style errors, plus command-name completion.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Core/Keymaps/` — `StrokeModifiers`, `KeyStroke`, `SpecialKeys`, `KeyNotation`,
  `KeyBinding`, `Keymap`, `KeyResolution`, `KeySequenceResolver`
- `src/Enigma.FileExplorer.Core/Commands/` — `CommandIds`, `CommandDescriptor`, `ExArgumentKind`,
  `ExCommandDefinition`, `ExInvocation`, `ExParseResult`, `ExCommandParser`, `CommandCatalog`
- `tests/Enigma.FileExplorer.Core.UnitTests/Keymaps/` — `KeyNotationTests`, `KeySequenceResolverTests`
- `tests/Enigma.FileExplorer.Core.UnitTests/Commands/` — `ExCommandParserTests`, `CommandCatalogTests`

**Modified**

- `src/Enigma.FileExplorer.Core/DependencyInjection/ServiceCollectionExtensions.cs` — registers the catalog
- `docs/roadmap.md`, `docs/plan/FEATURE-2E0F.md` — status updates

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Modifier enum name | `StrokeModifiers` | `KeyModifiers` would collide with `Avalonia.Input.KeyModifiers` in every Desktop file that maps keys |
| Ctrl+letter case | Lowercase with explicit Shift (`<C-F>` = `<C-f>`; Ctrl+Shift+F = `<C-S-f>`) | vim's own rule; avoids two spellings of one chord |
| A broken sequence (`gx`) | Discarded whole, and consumed | vim behaviour; replaying the second key would make typos run surprise commands |
| Unbound key with nothing pending | Not consumed | Lets the UI keep ordinary behaviour (e.g. Tab focus) for keys the keymap does not use |
| `:mkdir`, `:touch`, `:rename`, `:find`, `:grep` argument | Optional (missing → prompt in later phases) | `:mkdir` alone prompting is friendlier than an error; the plan listed them with arguments, which still work |
| `:cd` with no argument | Allowed (path optional) | vim's `:cd` alone goes home |
| Arrow keys Left/Right | Their own commands (`cursor.left/right`) | Their meaning depends on the view mode (FEATURE-2A19); `h`/`l` stay bound to parent/open |
| Place keys `g1`–`g9` | One command with the place number as the binding argument | One handler, and help shows a single "Go to place n" line |

## Deviations & follow-ups

- The status text for `Esc` uses the catalog id `mark.escape` (the plan's table calls it "cancel");
  behaviour is as planned.
- No user-editable keymap file (out of scope, as planned). The catalog is data, so a follow-up can load
  overrides into `Keymap`.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 300  failed: 0
```

Fix cycles used: **1 of 3.** The first full run failed `ACount_ExtendsWithZero`: a `0` typed after a
count ran a root binding of `0` instead of extending the count. The resolver now lets every digit
extend a started count, and only 1–9 that are not keys of their own start one. Run 2 was green.
