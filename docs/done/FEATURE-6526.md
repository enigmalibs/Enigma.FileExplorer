# FEATURE-6526 — Leader key & g-pane focus

**Item:** FEATURE-6526 — Leader key & g-pane focus
**Branch:** `feature/feature-6526-leader-key`
**Run:** vibe/2026-10-15-leader-keys-bookmarks

## Summary

The `<C-w>` window keys are gone. Split-group commands now start with a configurable **leader key**,
`Space` by default, and moving between panes uses `g` plus a direction.

| Action | Before | Now |
|---|---|---|
| Split side by side / top and bottom | `Ctrl+W v` / `Ctrl+W s` | `Space v` / `Space s` |
| Focus left / down / up / right | `Ctrl+W h/j/k/l` (and arrows) | `gh` / `gj` / `gk` / `gl` |
| Next / previous pane | `Ctrl+W w` / `Ctrl+W W` | `Space w` / `Space W` |
| Close pane / only this pane | `Ctrl+W c`, `q` / `Ctrl+W o` | `Space c`, `Space q` / `Space o` |
| Equalize / resize | `Ctrl+W =`, `+`, `-`, `>`, `<` | `Space =`, `+`, `-`, `>`, `<` |
| Toggle mark | `Space` | `m`, `Insert` |
| Home folder | `gh` | `gH` |
| Reveal a search hit | `gl` | `gf` |

**Configuring the leader.** Set `"leaderKey"` in `settings.json`, in vim notation (`","`, `"<C-Space>"`,
`"\\"`). It is read at start, and a running explorer overwrites the file at exit, so edit it while the
explorer is closed. The value must be a single key, not a digit, and not already used by the keymap:
`j` or `o` are refused, because `os` is a sort key. An unusable value falls back to Space, with a status
line message and a log warning.

**Where the keys show.** The help, the context menus and the path bar's split and close tooltips all read
the keys from the catalog, so they show the actual leader.

## Files / modules touched

**Created**

- `src/Enigma.FileExplorer.Desktop/Services/CommandCatalogFactory.cs` — builds the catalog from the
  settings, with the fallback and its report.

**Modified**

- `Core/Commands/CommandCatalog.cs`:
  - added `DefaultLeader`, `Leader`, `Create(KeyStroke leader)` and `TryCreate(string?, out, out)`;
  - `Default` is now `Create(DefaultLeader)`;
  - the new bindings from the table above; every `<C-w>` binding removed.
- `Core/Settings/AppSettings.cs` — `LeaderKey` (default `"<Space>"`).
- `Core/DependencyInjection/ServiceCollectionExtensions.cs` — no longer registers the static catalog.
- `Core/Layout/FocusDirection.cs`, `Desktop/Commands/WindowCommands.cs`, `NavigationCommands.cs` — doc
  comments name the new keys.
- `Desktop/Commands/SearchCommands.cs` — the "outside results" hint names `gf`.
- `Desktop/Services/Session.cs` — `Settings` is read lazily, once, and shared with `Load()`.
- `Desktop/DependencyInjection/ServiceCollectionExtensions.cs` — the catalog is a factory over `Session`.
- `Desktop/ViewModels/MainWindowViewModel.cs` — takes the catalog; adds `SplitColumnsTip`, `SplitRowsTip`
  and `ClosePaneTip`.
- `Desktop/Views/PathBarView.axaml` — the split and close tooltips bind to those properties.
- Tests:
  - `Core/Commands/CommandCatalogTests` — new keys; `Ctrl+W` unbound; default leader; custom leaders
    (`,`, `<C-Space>`, `\`) resolve and stay prefix-free; refused leaders.
  - `Desktop/CompositionRootTests` — the container uses a fake settings store; the leader comes from the
    settings; an unusable leader falls back and says so.
  - `WindowCommandsTests`, `NormalModeTests` (`m`, `Insert`, `gH`, Space as a pending leader),
    `ClipboardCommandsTests`, `SearchCommandsTests`, `ContextMenusTests`, `CommandLineViewModelTests`
    (help rows), `ExplorerHarness` — moved to the new keys.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How a custom leader shows in key descriptions | As the catalog describes any key run: `,v` for a plain character, `Space v` for a named key | Reuses `KeyNotation.Describe`, which already shows plain characters as typed |
| Is `g` a valid leader? | Yes. The plan's test assumed not; the test now uses `o` | `gv`, `gs`, `gw`… clash with no existing binding, so the prefix-free keymap accepts them. `o` clashes on `os` |
| Path-bar tooltips | Computed from the catalog on `MainWindowViewModel` | The workspace cannot take the catalog: that would make a DI cycle through `Session` |
| Test isolation | `CompositionRootTests.BuildProvider` always registers a `FakeSettingsStore` | Resolving the ViewModel now reads the settings. The tests must never read the developer's own settings file, and its leader key |

## Deviations & follow-ups

- **Doc comments.** `KeyNotation` and `KeySequenceResolver` still use `<C-w>v` as a *notation* example.
  They describe the notation, not a binding, so they were left as they are.
- **Startup message.** An unusable leader is reported when the catalog is first built, which is while the
  window is being set up. The message stays on the status line until the next one.
- **Follow-up.** The leader cannot be changed while the explorer runs. A `:set leader=…` would need a live
  keymap rebuild.

## Build/test evidence

```
dotnet build Enigma.FileExplorer.slnx
    0 Warning(s)
    0 Error(s)

dotnet test --solution Enigma.FileExplorer.slnx
  total: 698  failed: 0  succeeded: 695  skipped: 3
```

Two fix cycles were used. Both changed test expectations, not product code:

1. The first run had 7 failures:
   - 4 expectations still named the old keys (the help row, the menu gestures) or the run-together
     description `,v`;
   - 2 leader tests had wrong premises: `g` is a valid leader, and `Space v` resolves to `v` once Space
     is unbound.
2. The second cycle fixed one more menu expectation that still said `Ctrl+W v`.

The three skips are the known platform-specific ones.
