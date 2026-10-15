# FEATURE-6526 — Leader key & g-pane focus

**Status:** DONE — see `docs/done/FEATURE-6526.md`
**Type:** FEATURE
**Branch:** `feature/feature-6526-leader-key`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Objective

Replace vim's `<C-w>` window keys with two easier schemes:

- a configurable **leader key** (`Space` by default) followed by a letter, for the split-group commands;
- **`gh` `gj` `gk` `gl`** to move to the pane on the left, below, above and on the right.

`Space` no longer toggles the mark.

## Context & constraints

- Keys are declared once, in `CommandCatalog.CreateDefault`. The keymap is a prefix-free trie, so a key
  bound on its own cannot also start a sequence. `Keymap`'s constructor throws `ArgumentException` on a
  conflict.
- Keys that move:
  - `Space` (toggle mark) becomes `m` and `Insert`;
  - `gh` (home folder) becomes `gH`;
  - `gl` (reveal a search hit) becomes `gf`, as in vim's "go to file".
- Today the catalog is the static `CommandCatalog.Default`, registered by Core's DI. With a configurable
  leader, the catalog is built at startup from the settings. The settings must therefore be read
  **before** the catalog is resolved, and **once**.
- Menus, help and the status line read their keys from the catalog, so they follow automatically.

## Steps

1. Core `CommandCatalog`:
   - add `Create(KeyStroke leader)` and keep `Default` as `Create(<Space>)`;
   - add `DefaultLeader`;
   - split-group bindings: `<leader>v` and `<leader>s` split, `<leader>w`/`<leader>W` next/previous,
     `<leader>c` and `<leader>q` close, `<leader>o` only, `<leader>=` equalize, and `<leader>+`,
     `<leader>-`, `<leader>>`, `<leader><lt>` resize;
   - directional focus on `gh` `gj` `gk` `gl`;
   - toggle mark on `m` and `<Insert>`, Home on `gH`, Reveal on `gf`;
   - remove every `<C-w>` binding.
2. Core `CommandCatalog.TryCreate(string? leaderNotation, out CommandCatalog catalog, out string? error)`
   accepts exactly one stroke that does not conflict with the keymap. Anything else returns the default
   catalog and an error message.
3. Core `AppSettings.LeaderKey` (string, default `"<Space>"`).
4. Desktop:
   - `Session.Settings` reads the store lazily, the first time it is needed, so the catalog factory and
     `Load()` share one read;
   - the catalog is registered as a factory over `Session` (moved from Core's DI to the app's);
   - an invalid leader is logged and reported on the status line at start, and `Space` is used.
5. Update doc comments that name the old keys (`WindowCommands`, `FocusDirection`, `KeyNotation`,
   `KeySequenceResolver`).
6. Tests:
   - catalog: leader bindings with the default leader and with a custom one; prefix-freedom for both;
     `TryCreate` rejects an empty value, a multi-stroke value and a conflicting key (`j`, `g`);
   - Desktop: keys drive the split groups through the keyboard controller (`<Space>v`, `gl`…);
   - the container resolves a catalog built from the stored leader;
   - existing tests move to the new keys.

## Acceptance criteria

- `Space v`/`Space s` split, `Space c` closes, `Space o` keeps only the active pane, and `Space w`
  cycles panes; `gh`/`gj`/`gk`/`gl` move between panes; `m` marks.
- `<C-w>` no longer does anything, and `Space` alone does nothing visible: it waits for the next key.
- `"leaderKey": ","` in settings.json makes `, v` split. An invalid value falls back to Space, with a
  status message and a log warning.
- The help and the context menus show the new keys.
- Build has zero warnings and all tests are green.

## Out of scope

- A user-editable keymap file, changing the leader at runtime (`:set leader`), and other remaps.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Keep `<C-w>` as an alternative | No, replace it | The user wants a simpler scheme. One scheme keeps the help readable and frees Ctrl+W | Keeping both |
| Commands under the leader | Split, close, only, equalize, resize, next/previous | They are the `<C-w>` family; pane focus gets the faster `g` keys the user asked for | Putting focus under the leader too |
| Where the leader is configured | `leaderKey` in settings.json, read at start | The existing settings file; no settings UI exists | A `:set leader=` command (needs live keymap rebuilding) |
| Invalid leader | Fall back to Space, log and show a status message | Never block startup; still tell the user why their setting was ignored | Crash; silent fallback |
| Toggle-mark key | `m` and `Insert` | `m` for "mark"; Insert is the Total Commander/mc convention | `t` (tag); Space under the leader |
| Displaced `gh`/`gl` | Home on `gH`, Reveal on `gf` | Both stay mnemonic and two-stroke. `~` is a dead key on Swiss layouts | `~`, `g~` |
| Settings read once | `Session.Settings` loads lazily on first access | The catalog factory and `Session.Load()` share one read, whatever the resolution order | Reading the store twice; reordering the startup code |
