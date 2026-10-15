# FEATURE-2E0F — Vim keyboard & command system

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-2e0f-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-14-vim-split-file-explorer

## Objective

Make the explorer drivable almost entirely from the keyboard with vim-style commands: a normal mode
with multi-key sequences and counts, a visual mode for range selection, a `:` command line with
history and completion, `/` quick-jump inside the listing, and a generated key-binding cheat sheet.
Standard desktop shortcuts stay available as aliases so the app is not hostile to mouse users.

## Context & constraints

- Builds on FEATURE-C4E7's `PaneViewModel` (cursor/selection model, navigation, history).
- **One command catalog** is the source of truth: every command has an id, a title and a category;
  the default keymap and the ex-command table reference those ids. Later items (splits, views, file
  operations, search, context menus) register their handlers against ids declared here or add ids to
  the catalog.
- **Layout-aware keys.** Printable keys are matched on the character the keyboard layout produced
  (Avalonia `KeyEventArgs.KeySymbol`), not on the physical `Key` — on a Swiss QWERTZ keyboard `:` is
  Shift+`.`, `/` is Shift+7 and `z`/`y` are swapped. `Ctrl`/`Alt` chords and special keys (arrows,
  F-keys, Esc, Enter, Delete…) are matched on `Key` + modifiers. AltGr (reported as Ctrl+Alt on
  Windows) producing a printable character is treated as that character.
- Key notation is vim's: `gg`, `<C-w>v`, `<S-Del>`, `<CR>`, `<Esc>`, `<Space>`, `<F2>`, `<A-Left>`.
- No key-sequence timeout: a prefix waits until the next key; an unmatched continuation resets (and
  `Esc` always resets). The default keymap must therefore have no binding that is also a prefix of
  another — enforced by a test.
- Text inputs (command line, path bar edit mode, prompts) receive keys normally; the keymap only runs
  when a pane has focus.

## Default keymap

| Keys | Command |
|---|---|
| `j` `<Down>` / `k` `<Up>` | cursor down / up (count) |
| `h` `<Left>` `<BS>` `<A-Up>` | parent directory |
| `l` `<Right>` `<CR>` | open (enter directory / open file) |
| `gg` `<Home>` / `G` `<End>` | first / last item (`5G` → 5th) |
| `<C-d>` / `<C-u>` | half page down / up |
| `<PageDown>` / `<PageUp>` | page down / up |
| `H` `<A-Left>` / `L` `<A-Right>` | history back / forward |
| `gh` / `g/` | home directory / root of the current volume |
| `g1` … `g9` | n-th place of the sidebar |
| `<Space>` | toggle mark on the cursor item, move down |
| `v` | visual mode (range selection) |
| `<C-a>` | select all |
| `<Esc>` | cancel pending keys, leave visual mode, clear marks |
| `:` | command line |
| `/` then `n` / `N` | quick-jump to a name in the listing; next / previous match |
| `?` `<F1>` | key-binding help |
| `R` `<F5>` | refresh |
| `<C-l>` | edit the path bar |
| `<C-w>v` / `<C-w>s` | split vertically / horizontally (FEATURE-95D6) |
| `<C-w>h/j/k/l` `<C-w><Arrow>` | focus the pane left/below/above/right (FEATURE-95D6) |
| `<C-w>w` / `<C-w>W` | next / previous pane (FEATURE-95D6) |
| `<C-w>c` `<C-w>q` / `<C-w>o` | close pane / close all others (FEATURE-95D6) |
| `<C-w>=` `<C-w>+` `<C-w>-` `<C-w>>` `<C-w><` | equalize / resize (FEATURE-95D6) |
| `zd` / `zm` / `zl` | details / medium icons / large icons view (FEATURE-2A19) |
| `<C-S-6>` / `<C-S-3>` / `<C-S-2>` | same, Windows Explorer chords (FEATURE-2A19) |
| `zh` | toggle hidden files (FEATURE-2A19) |
| `on` `om` `os` `ot` / `or` | sort by name, modified, size, type / reverse (FEATURE-2A19) |
| `a` / `A` `<C-S-n>` | new file / new directory prompt (FEATURE-BF6B) |
| `r` `cw` `<F2>` | rename prompt (FEATURE-BF6B) |
| `dD` `<Del>` / `<S-Del>` | move to trash / delete permanently (FEATURE-BF6B) |
| `yy` `<C-c>` / `dd` `<C-x>` / `p` `<C-v>` | copy / cut / paste (FEATURE-BF6B) |
| `yp` | copy the selected paths as text (FEATURE-BF6B) |
| `f` `<C-f>` / `F` `<C-S-f>` | find by name / find in file contents (FEATURE-6964) |
| `gl` | reveal a search hit in its folder (FEATURE-6964) |
| `<Apps>` `<S-F10>` | context menu (FEATURE-A6C2) |

## Ex commands

`:cd <path>` · `:sp[lit] [path]` · `:vs[plit] [path]` · `:clo[se]` · `:on[ly]` · `:q[uit]` ·
`:qa[ll]` · `:mkdir <name>` · `:touch <name>` · `:rename <name>` · `:delete` / `:delete!` ·
`:find <pattern>` · `:grep <text>` · `:view details|medium|large` · `:sort name|modified|size|type`
(`:sort!` reverses) · `:set hidden|nohidden|hidden!` · `:theme dark|light|system` · `:refresh` ·
`:h[elp]`. Arguments may be double-quoted to contain spaces. Commands owned by later items are
declared in the catalog here and report "not available yet" until their item wires them.

## PHASE01 — Core keymap & ex-command parser

**Status:** DONE — see `docs/done/FEATURE-2E0F-PHASE01.md`
**Branch:** `feature/feature-2e0f-phase01-keymap-engine`

### Steps

1. `KeyStroke` (either a printable character, or a special key name, plus Ctrl/Alt/Shift flags;
   canonical form so `<C-W>` and `<C-w>` compare equal, Ctrl+letter stored lowercase + Shift flag).
2. `KeyNotation.Parse` / `Format` for vim notation (`<C-w>v`, `<S-Del>`, `<lt>`, `<Space>`, F-keys…).
3. `CommandIds` + `CommandDescriptor` (id, title, category) + `CommandCatalog` with the default keymap
   and ex-command table above.
4. `Keymap` (trie) + `KeySequenceResolver`: feed strokes → `Pending` (with display text) /
   `Matched(commandId, count, argument)` / `Unmatched`; count prefix (digits, `0` only after a digit),
   `Esc` resets.
5. `ExCommandParser`: name, abbreviation by minimal unique prefix with explicit vim short forms, `!`
   bang, quoted arguments; unknown command → error result.
6. Tests: notation round-trip, resolver sequences and counts, no-prefix-ambiguity of the default
   keymap, every binding/ex entry references a catalog id, parser cases.

### Acceptance criteria

- All Core tests green; default keymap validated free of prefix ambiguity.
- Build zero warnings.

## PHASE02 — Normal & visual mode, status line

**Status:** DONE — see `docs/done/FEATURE-2E0F-PHASE02.md`
**Branch:** `feature/feature-2e0f-phase02-normal-visual-mode`

### Steps

1. Desktop `KeyStrokeMapper`: `KeyEventArgs` → `KeyStroke` per the layout-aware rule above (unit
   tested with synthetic events).
2. `ICommandDispatcher` + handler registration by command id; unknown/unwired ids show
   "not available yet" in the status line.
3. Key routing: the main window feeds keys from the focused pane to the resolver; handled keys are
   marked handled so Avalonia's own navigation does not also act.
4. Wire navigation and selection commands: cursor motions with counts, half/full page (page size from
   the view), first/last, parent/open, back/forward, `gh`, `g/`, `g1`–`g9`, refresh, `<C-l>` path edit,
   `<Space>` mark, `v` visual (range from anchor to cursor follows motions), `<C-a>`, `<Esc>`.
5. Status line: mode (`NORMAL`/`VISUAL`), pending keys/count (vim showcmd), item and selection counts
   with total selected size, message line (info/error colours).
6. Tests: dispatcher routing, motions with counts, visual-mode selection, Esc behaviour, status text.

### Acceptance criteria

- Every navigation/selection key in the keymap works on a pane, including on a Swiss layout (manual
  check) — characters matched by symbol.
- Status line shows mode, pending keys and counts.
- Build zero warnings, all tests green.

## PHASE03 — Command line, quick-jump & help

**Status:** DONE — see `docs/done/FEATURE-2E0F-PHASE03.md`
**Branch:** `feature/feature-2e0f-phase03-command-line`

### Steps

1. Command line bar at the bottom (vim style): `:` opens it focused; Enter executes through
   `ExCommandParser` + dispatcher; Esc cancels and returns focus to the pane; Up/Down history
   (session-only, de-duplicated); Tab/Shift+Tab cycles completions — command names, and paths for
   path arguments (`:cd`, `:sp`, `:vs`) resolved against the pane directory.
2. Generic prompt API (`IPromptService.PromptAsync(label, initialText, selection)`) on the same bar,
   used later by create/rename.
3. `/` quick-jump: incremental, smart-case substring match on names in the listing, cursor jumps to
   the first match as you type, Enter confirms, Esc restores; `n`/`N` cycle matches.
4. `:cd` (with `~`, relative paths, volume roots), `:refresh`, `:help`.
5. Help (`?`, `<F1>`, `:help`): a ContentDialog listing every binding grouped by category, generated
   from the catalog (key notation shown human-readably).
6. Tests: completion engine (commands and paths), history, quick-jump matching, `:cd` resolution,
   help content generated from the catalog.

### Acceptance criteria

- `:cd ~/Documents`, `:cd ..`, Tab completion and history work.
- `/` jumps incrementally; `n`/`N` cycle; Esc restores the cursor.
- Help lists every binding of the catalog.
- Build zero warnings, all tests green.

## Out of scope

- A user-editable keymap file (the keymap is data-driven so this is a cheap follow-up).
- vim registers, marks/bookmarks (`m`/`'`), macros, `.` repeat.
- Command history persistence across sessions.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Keyboard model | vim normal mode + visual mode + `:` command line | Exactly what the draft asks; proven by ranger/vifm/lf | Modeless shortcuts only |
| Keymap flavour | ranger/vifm-inspired with desktop aliases | Familiar to vim users; aliases keep mouse users at home | Pure vim (no aliases); custom scheme |
| Key matching | Layout-aware characters for printable keys, `Key` for chords/specials | The user types on non-US layouts; physical-key matching would put `:` and `/` in the wrong place | Physical `Key` only |
| Sequence timeout | None; the default keymap is prefix-free | Deterministic and testable; vim's `timeoutlen` exists only because vim has ambiguous maps | `timeoutlen`-style timer |
| Counts | Supported for motions and `G` | vim muscle memory (`5j`, `10G`), cheap in the resolver | No counts |
| Command catalog location | Core (pure data + parser) | Testable without UI; one source for keys, ex commands, help and menus | Hard-coded handlers in views |
| Ctrl+F | Find by name (desktop convention) | More users expect it; paging stays on `<C-d>`/`<C-u>`/PageDown | vim page-forward |
| Prompts | Bottom command-line prompt | Keyboard-fast, consistent with `:`; visible to mouse users too | Modal dialogs for text input |
| Help | Generated cheat sheet dialog | Discoverability for a key-driven app; never drifts from the keymap | Static README table |
