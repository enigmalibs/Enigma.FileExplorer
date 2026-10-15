using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One split group: a directory browser with its own location, history, cursor and marks.
/// </summary>
/// <remarks>
/// <para>
/// The ViewModel is the single source of truth for the cursor and the marks. Views only render them
/// and translate mouse gestures into calls here; the keyboard drives the very same members, so mouse
/// and keys can never disagree about what is selected.
/// </para>
/// <para>
/// "Marks" are the explicit selection (Ctrl/Shift+click, <c>Space</c>, visual mode). When nothing is
/// marked, operations act on the cursor entry — see <see cref="GetTargets"/>.
/// </para>
/// </remarks>
public sealed class PaneViewModel : ObservableObject, IDisposable
{
    private const string EmptyFolderText = "This folder is empty";

    private readonly IDirectoryReader _reader;
    private readonly IShellLauncher _launcher;
    private readonly IStatusMessages _status;
    private readonly IKnownFolders _folders;
    private readonly IDirectoryWatcherFactory _watchers;
    private readonly IFileSearch _search;
    private readonly List<EntryViewModel> _resultItems = [];
    private readonly Stack<HistoryEntry> _back = new();
    private readonly Stack<HistoryEntry> _forward = new();
    private IReadOnlyList<FileSystemEntry> _entries = [];
    private CancellationTokenSource? _loading;
    private IReadOnlySet<string> _cutPaths = new HashSet<string>();
    private IDisposable? _watch;
    private CancellationTokenSource? _searching;
    private SearchState? _results;
    private bool _disposed;
    private int _anchorIndex = -1;

    /// <summary>
    /// Creates a pane. Panes are made by <see cref="IPaneFactory"/>, one per split group.
    /// </summary>
    /// <param name="reader">Reads directories.</param>
    /// <param name="launcher">Opens files with their default application.</param>
    /// <param name="status">The status bar's message line.</param>
    /// <param name="folders">The user's well-known folders.</param>
    /// <param name="watchers">Watches the directory on display, so the listing follows the disk.</param>
    /// <param name="search">Searches below the directory on display.</param>
    public PaneViewModel(
        IDirectoryReader reader,
        IShellLauncher launcher,
        IStatusMessages status,
        IKnownFolders folders,
        IDirectoryWatcherFactory watchers,
        IFileSearch search)
    {
        ArgumentNullException.ThrowIfNull(search);

        _search = search;
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(watchers);

        _reader = reader;
        _launcher = launcher;
        _status = status;
        _folders = folders;
        _watchers = watchers;

        NavigateToSegmentCommand = new AsyncRelayCommand<PathSegment>(OnNavigateToSegmentAsync);
        BeginEditPathCommand = new RelayCommand(BeginEditPath);
        CommitEditPathCommand = new AsyncRelayCommand(CommitEditPathAsync);
        CancelEditPathCommand = new RelayCommand(CancelEditPath);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync, () => CanGoBack);
        GoForwardCommand = new AsyncRelayCommand(GoForwardAsync, () => CanGoForward);
        GoParentCommand = new AsyncRelayCommand(GoParentAsync, () => CanGoParent);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SortByCommand = new RelayCommand<SortKey>(SortBy);
    }

    /// <summary>
    /// Raised when the keyboard asks for the context menu (the Menu key, Shift+F10); the view opens it at
    /// the cursor.
    /// </summary>
    public event EventHandler? ContextMenuRequested;

    /// <summary>
    /// The directory on display; empty until the first successful load.
    /// </summary>
    public string CurrentPath { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// The current directory's own name (or its root's), for titles.
    /// </summary>
    public string DisplayName => Breadcrumbs.Count > 0 ? Breadcrumbs[^1].Name : string.Empty;

    /// <summary>
    /// The path bar's breadcrumb, root first.
    /// </summary>
    public IReadOnlyList<PathSegment> Breadcrumbs { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>
    /// The visible entries, sorted. Replaced as a whole on every load, never mutated in place.
    /// </summary>
    public IReadOnlyList<EntryViewModel> Items { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>
    /// The cursor's index in <see cref="Items"/>, or -1 when the pane is empty.
    /// </summary>
    public int CursorIndex { get; private set => SetProperty(ref field, value); } = -1;

    /// <summary>
    /// The entry under the cursor.
    /// </summary>
    public EntryViewModel? CursorItem => (CursorIndex >= 0) && (CursorIndex < Items.Count) ? Items[CursorIndex] : null;

    /// <summary>
    /// How many entries are marked.
    /// </summary>
    public int MarkedCount { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// The total size of the marked files.
    /// </summary>
    public long MarkedSize { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether a directory read is in flight.
    /// </summary>
    public bool IsLoading { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// What the pane shows instead of a list: "empty", or why the directory could not be read.
    /// </summary>
    public string? PlaceholderText { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether hidden entries are listed.
    /// </summary>
    public bool ShowHidden
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && !IsSearchResults)
            {
                Rebuild(CursorItem?.Name, CursorIndex, MarkedNames());
            }
        }
    }

    /// <summary>
    /// How the entries are sorted.
    /// </summary>
    public EntrySort Sort
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Search results keep the order they were found in: nearest first.
                if (!IsSearchResults)
                {
                    Rebuild(CursorItem?.Name, CursorIndex, MarkedNames());
                }

                OnPropertyChanged(nameof(NameHeader));
                OnPropertyChanged(nameof(ModifiedHeader));
                OnPropertyChanged(nameof(TypeHeader));
                OnPropertyChanged(nameof(SizeHeader));
            }
        }
    } = EntrySort.Default;

    /// <summary>
    /// How the entries are presented.
    /// </summary>
    public ViewMode ViewMode
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RebuildRows();
                OnPropertyChanged(nameof(IsDetailsView));
                OnPropertyChanged(nameof(IsIconView));
                RaiseLayoutChanged();
                OnPropertyChanged(nameof(TileWidth));
                OnPropertyChanged(nameof(TileHeight));
                OnPropertyChanged(nameof(IconSize));
            }
        }
    }

    /// <summary>
    /// Whether the pane shows the details list.
    /// </summary>
    public bool IsDetailsView => ViewMode == ViewMode.Details;

    /// <summary>
    /// Whether the pane shows a grid of tiles.
    /// </summary>
    public bool IsIconView => !IsDetailsView;

    /// <summary>
    /// The width of a tile in the icon views.
    /// </summary>
    public double TileWidth => ViewMode == ViewMode.LargeIcons ? 148 : 104;

    /// <summary>
    /// The height of a tile in the icon views.
    /// </summary>
    public double TileHeight => ViewMode == ViewMode.LargeIcons ? 156 : 100;

    /// <summary>
    /// The glyph size of a tile.
    /// </summary>
    public double IconSize => ViewMode == ViewMode.LargeIcons ? 96 : 48;

    /// <summary>
    /// How many tiles fit on a row of an icon view; the view keeps it current. Always 1 for details.
    /// </summary>
    public int ItemsPerRow
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Max(1, value)))
            {
                RebuildRows();
            }
        }
    } = 1;

    /// <summary>
    /// How far <c>j</c> and <c>k</c> move: one row of the current view.
    /// </summary>
    public int RowStep => IsListLayout ? 1 : ItemsPerRow;

    /// <summary>
    /// Whether the pane shows a list (details, or search results) rather than a grid of tiles.
    /// </summary>
    public bool IsListLayout => IsDetailsView || IsSearchResults;

    /// <summary>
    /// Whether the details list is on screen.
    /// </summary>
    public bool ShowsDetailsList => IsDetailsView && !IsSearchResults;

    /// <summary>
    /// Whether the grid of tiles is on screen.
    /// </summary>
    public bool ShowsIconView => IsIconView && !IsSearchResults;

    /// <summary>
    /// Whether the pane shows search results instead of a directory.
    /// </summary>
    public bool IsSearchResults
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaiseLayoutChanged();
            }
        }
    }

    /// <summary>
    /// Whether a search is still finding results.
    /// </summary>
    public bool IsSearching { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// What the results are, e.g. <c>12 names matching "report" in Documents</c>.
    /// </summary>
    public string? SearchTitle { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// The entries in rows of <see cref="ItemsPerRow"/> tiles, for the icon views; empty in details.
    /// </summary>
    public IReadOnlyList<EntryRowViewModel> Rows { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>
    /// The Name column's header, with the sort arrow when it is the sort key.
    /// </summary>
    public string NameHeader => Header("Name", SortKey.Name);

    /// <summary>
    /// The Modified column's header.
    /// </summary>
    public string ModifiedHeader => Header("Modified", SortKey.Modified);

    /// <summary>
    /// The Type column's header.
    /// </summary>
    public string TypeHeader => Header("Type", SortKey.Type);

    /// <summary>
    /// The Size column's header.
    /// </summary>
    public string SizeHeader => Header("Size", SortKey.Size);

    /// <summary>
    /// Sorts by a column: its natural direction first, reversed on a second click.
    /// </summary>
    public RelayCommand<SortKey> SortByCommand { get; }

    /// <summary>
    /// Whether this is the active pane: the one the keyboard drives.
    /// </summary>
    public bool IsActive { get; set => SetProperty(ref field, value); } = true;

    /// <summary>
    /// Whether the pane draws the active frame: it is active and other panes are on screen.
    /// </summary>
    public bool IsFramed { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether files dragged over the pane would be dropped into the directory it shows.
    /// </summary>
    public bool IsDropTarget { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// How many entries one screen of the listing shows, for paging. The view keeps it current.
    /// </summary>
    public int PageSize { get; set => SetProperty(ref field, Math.Max(1, value)); } = 20;

    /// <summary>
    /// Whether visual mode is on: the marks follow the range from <see cref="VisualAnchorIndex"/> to the cursor.
    /// </summary>
    public bool IsVisualMode { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Where visual mode started, or -1 outside visual mode.
    /// </summary>
    public int VisualAnchorIndex { get; private set; } = -1;

    /// <summary>
    /// Whether the path bar shows its text box instead of the breadcrumb.
    /// </summary>
    public bool IsEditingPath { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// The path being typed in the path bar.
    /// </summary>
    public string EditPathText { get; set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Whether there is a location to go back to.
    /// </summary>
    public bool CanGoBack => _back.Count > 0;

    /// <summary>
    /// Whether there is a location to go forward to.
    /// </summary>
    public bool CanGoForward => _forward.Count > 0;

    /// <summary>
    /// Whether the current directory has a parent.
    /// </summary>
    public bool CanGoParent => (CurrentPath.Length > 0) && (PathUtilities.GetParent(CurrentPath) is not null);

    /// <summary>
    /// Navigates to a breadcrumb segment.
    /// </summary>
    public AsyncRelayCommand<PathSegment> NavigateToSegmentCommand { get; }

    /// <summary>
    /// Switches the path bar to its text box.
    /// </summary>
    public RelayCommand BeginEditPathCommand { get; }

    /// <summary>
    /// Navigates to the typed path.
    /// </summary>
    public AsyncRelayCommand CommitEditPathCommand { get; }

    /// <summary>
    /// Leaves the path bar's text box without navigating.
    /// </summary>
    public RelayCommand CancelEditPathCommand { get; }

    /// <summary>
    /// Goes back in history.
    /// </summary>
    public AsyncRelayCommand GoBackCommand { get; }

    /// <summary>
    /// Goes forward in history.
    /// </summary>
    public AsyncRelayCommand GoForwardCommand { get; }

    /// <summary>
    /// Goes to the parent directory.
    /// </summary>
    public AsyncRelayCommand GoParentCommand { get; }

    /// <summary>
    /// Reads the current directory again.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Opens the pane on its first directory: <paramref name="path"/> when given, else the home directory,
    /// falling back to the nearest existing ancestor and finally to the volume root.
    /// </summary>
    /// <param name="path">Where to start.</param>
    /// <returns>A task that completes when the first directory is on display (or could not be).</returns>
    public async Task StartAsync(string? path = null)
    {
        string start = string.IsNullOrWhiteSpace(path) ? _folders.Home : path;

        if (await NavigateAsync(start))
        {
            return;
        }

        string? fallback = TryNormalize(start) is { } normalized
            ? PathUtilities.FindNearestExisting(normalized, Directory.Exists)
            : null;

        fallback ??= TryNormalize(_folders.Home) is { } home ? PathUtilities.GetVolumeRoot(home) : null;

        if (fallback is not null)
        {
            await NavigateAsync(fallback);
        }
    }

    /// <summary>
    /// Navigates to a directory, recording the current one in the back history.
    /// </summary>
    /// <param name="path">The directory.</param>
    /// <param name="focusName">The entry to put the cursor on; the first entry when omitted or missing.</param>
    /// <returns><see langword="true"/> when the directory is now on display.</returns>
    public async Task<bool> NavigateAsync(string path, string? focusName = null)
        => await LoadAsync(path, new LoadIntent(focusName, FallbackIndex: 0, MarkedNames: null, RecordHistory: true)) == LoadOutcome.Loaded;

    /// <summary>
    /// Opens an entry: a directory is navigated into, a file is handed to its default application.
    /// </summary>
    /// <param name="item">The entry.</param>
    /// <returns>A task that completes when the directory is on display or the file was handed over.</returns>
    public async Task OpenAsync(EntryViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDirectory)
        {
            await NavigateAsync(item.FullPath);
            return;
        }

        if (_launcher.TryOpen(item.FullPath, out string? error))
        {
            _status.Clear();
        }
        else
        {
            _status.Error(error);
        }
    }

    /// <summary>
    /// Opens the entry under the cursor.
    /// </summary>
    /// <returns>A task that completes when it has been opened.</returns>
    public Task OpenCursorAsync() => CursorItem is { } item ? OpenAsync(item) : Task.CompletedTask;

    /// <summary>
    /// Searches below the current directory; the pane shows the hits as they arrive. The directory (or the
    /// previous results) goes on the back history, so <c>H</c> returns to it.
    /// </summary>
    /// <param name="kind">Names or contents.</param>
    /// <param name="pattern">What to look for.</param>
    /// <returns>A task that completes when the search has finished or stopped.</returns>
    public async Task SearchAsync(SearchKind kind, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if ((CurrentPath.Length == 0) || (pattern.Length == 0))
        {
            return;
        }

        _back.Push(Snapshot());
        _forward.Clear();
        RaiseHistoryChanged();
        await RunSearchAsync(new SearchQuery(CurrentPath, pattern, kind, ShowHidden));
    }

    /// <summary>
    /// Stops a running search; the hits found so far stay.
    /// </summary>
    /// <returns><see langword="true"/> when a search was running.</returns>
    public bool CancelSearch()
    {
        if (_searching is not { } searching)
        {
            return false;
        }

        searching.Cancel();
        return true;
    }

    /// <summary>
    /// Shows a search hit in its folder, with the cursor on it.
    /// </summary>
    /// <param name="item">The hit.</param>
    /// <returns>A task that completes when the folder is on display.</returns>
    public Task RevealAsync(EntryViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Path.GetDirectoryName(item.FullPath) is { } folder ? NavigateAsync(folder, item.Name) : Task.CompletedTask;
    }

    /// <summary>
    /// Goes to the parent directory, with the cursor on the directory just left. From search results, it
    /// returns to where the search started.
    /// </summary>
    /// <returns>A task that completes when the parent is on display.</returns>
    public async Task GoParentAsync()
    {
        if (IsSearchResults)
        {
            CancelSearch();
            await GoBackAsync();
            return;
        }

        if (CurrentPath.Length == 0 || PathUtilities.GetParent(CurrentPath) is not { } parent)
        {
            return;
        }

        await NavigateAsync(parent, Path.GetFileName(CurrentPath));
    }

    /// <summary>
    /// Goes back to the previous location, with the cursor where it was.
    /// </summary>
    /// <returns>A task that completes when the location is on display.</returns>
    public async Task GoBackAsync()
    {
        if (_back.Count == 0)
        {
            return;
        }

        HistoryEntry target = _back.Pop();
        HistoryEntry current = Snapshot();

        if (await RestoreAsync(target))
        {
            _forward.Push(current);
        }

        // A location that can no longer be read is dropped from the history rather than retried forever.
        RaiseHistoryChanged();
    }

    /// <summary>
    /// Goes forward to the location <see cref="GoBackAsync"/> left.
    /// </summary>
    /// <returns>A task that completes when the location is on display.</returns>
    public async Task GoForwardAsync()
    {
        if (_forward.Count == 0)
        {
            return;
        }

        HistoryEntry target = _forward.Pop();
        HistoryEntry current = Snapshot();

        if (await RestoreAsync(target))
        {
            _back.Push(current);
        }

        RaiseHistoryChanged();
    }

    /// <summary>
    /// Reads the current directory again, keeping the cursor and the marks on the same names. When the
    /// directory no longer exists, the pane moves to its nearest existing ancestor.
    /// </summary>
    /// <returns>A task that completes when the listing is fresh.</returns>
    public Task RefreshAsync() => RefreshAsync(focusName: null, keepMarks: true);

    /// <summary>
    /// Reads the current directory again with the cursor on a given entry, as after creating or renaming it.
    /// </summary>
    /// <param name="focusName">The entry to put the cursor on; the cursor stays on its entry when omitted or missing.</param>
    /// <param name="keepMarks">Whether the marks survive; an operation that consumed them clears them.</param>
    /// <returns>A task that completes when the listing is fresh.</returns>
    public async Task RefreshAsync(string? focusName, bool keepMarks)
    {
        if (CurrentPath.Length == 0)
        {
            return;
        }

        if (IsSearchResults && (_results is { } results))
        {
            // Refreshing results means searching again.
            await RunSearchAsync(results.Query);
            return;
        }

        string path = CurrentPath;
        LoadIntent intent = new(focusName ?? CursorItem?.Name, CursorIndex, keepMarks ? MarkedNames() : null, RecordHistory: false, ReportMissing: false);
        LoadOutcome outcome = await LoadAsync(path, intent);

        if (outcome == LoadOutcome.Missing)
        {
            await RecoverFromMissingAsync(path);
        }
    }

    /// <summary>
    /// Stops watching the directory and abandons any read in flight.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _loading?.Cancel();
        _searching?.Cancel();
        _watch?.Dispose();
        _watch = null;
    }

    /// <summary>
    /// Sorts by a key: its natural direction (see <see cref="EntrySort.For"/>), or reversed when the
    /// pane is already sorted by it.
    /// </summary>
    /// <param name="key">The key.</param>
    public void SortBy(SortKey key) => Sort = Sort.Key == key ? Sort.Reversed() : EntrySort.For(key);

    /// <summary>
    /// Moves the cursor by a number of entries, stopping at either end.
    /// </summary>
    /// <param name="delta">How far; negative moves up.</param>
    public void MoveCursor(int delta) => MoveCursorTo(CursorIndex + delta);

    /// <summary>
    /// Moves the cursor to an index, clamped to the list.
    /// </summary>
    /// <param name="index">The index.</param>
    public void MoveCursorTo(int index)
    {
        if (Items.Count == 0)
        {
            return;
        }

        SetCursor(Math.Clamp(index, 0, Items.Count - 1));

        if (IsVisualMode)
        {
            MarkRange(VisualAnchorIndex, CursorIndex);
        }
    }

    /// <summary>
    /// Fades the entries that were cut and wait to be pasted.
    /// </summary>
    /// <param name="cutPaths">The cut entries' full paths.</param>
    public void ApplyCut(IReadOnlySet<string> cutPaths)
    {
        ArgumentNullException.ThrowIfNull(cutPaths);

        _cutPaths = cutPaths;

        foreach (EntryViewModel item in Items)
        {
            item.IsCut = cutPaths.Contains(item.FullPath);
        }
    }

    /// <summary>
    /// The quick-jump pattern whose matches are highlighted, as vim's <c>hlsearch</c> does; <see langword="null"/>
    /// for none. It follows refreshes of the same listing and is cleared when the pane shows another one.
    /// </summary>
    public string? HighlightPattern
    {
        get;
        set
        {
            if (SetProperty(ref field, string.IsNullOrEmpty(value) ? null : value))
            {
                Highlight(Items);
            }
        }
    }

    /// <summary>
    /// Finds the next entry whose name contains a pattern, smart-case, wrapping around the list.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="start">Where the search starts.</param>
    /// <param name="forward">Whether to search towards the end of the list.</param>
    /// <param name="includeStart">Whether the entry at <paramref name="start"/> itself may match.</param>
    /// <returns>The match's index, or -1 when nothing matches.</returns>
    public int FindMatch(string pattern, int start, bool forward, bool includeStart)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        int count = Items.Count;

        if ((count == 0) || (pattern.Length == 0))
        {
            return -1;
        }

        int origin = Math.Clamp(start, 0, count - 1);
        int first = includeStart ? 0 : 1;

        for (int offset = first; offset < count + first; offset++)
        {
            int index = forward ? (origin + offset) % count : (((origin - offset) % count) + count) % count;

            if (SmartCase.Contains(Items[index].Name, pattern))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Enters visual mode at the cursor: the cursor entry is marked, and moving the cursor extends the range.
    /// </summary>
    public void StartVisual()
    {
        if (CursorIndex < 0)
        {
            return;
        }

        VisualAnchorIndex = CursorIndex;
        IsVisualMode = true;
        MarkRange(VisualAnchorIndex, CursorIndex);
    }

    /// <summary>
    /// Leaves visual mode.
    /// </summary>
    /// <param name="keepMarks">Whether the range stays marked (leaving with <c>v</c>) or is cleared (<c>Esc</c>).</param>
    public void EndVisual(bool keepMarks)
    {
        if (!IsVisualMode)
        {
            return;
        }

        IsVisualMode = false;
        VisualAnchorIndex = -1;

        if (!keepMarks)
        {
            ClearMarks();
        }
    }

    /// <summary>
    /// Applies a mouse click on an entry.
    /// </summary>
    /// <param name="item">The entry clicked.</param>
    /// <param name="modifiers">What the click means.</param>
    public void Click(EntryViewModel item, ClickModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(item);

        int index = IndexOf(item);

        if (index < 0)
        {
            return;
        }

        switch (modifiers)
        {
            case ClickModifiers.Toggle:
                item.IsMarked = !item.IsMarked;
                _anchorIndex = index;
                break;

            case ClickModifiers.Range:
                MarkRange(_anchorIndex < 0 ? index : _anchorIndex, index);
                break;

            default:
                ClearMarksCore();
                _anchorIndex = index;
                break;
        }

        SetCursor(index);
        UpdateMarkSummary();
    }

    /// <summary>
    /// Applies a right-click on an entry, before its menu opens: a marked entry keeps the marks it belongs
    /// to, so the menu acts on all of them; any other entry becomes the only target, as in every file manager.
    /// </summary>
    /// <param name="item">The entry right-clicked.</param>
    public void ContextClick(EntryViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        int index = IndexOf(item);

        if (index < 0)
        {
            return;
        }

        bool keepMarks = item.IsMarked;
        EndVisual(keepMarks);

        if (keepMarks)
        {
            SetCursor(index);
        }
        else
        {
            Click(item, ClickModifiers.None);
        }
    }

    /// <summary>
    /// Asks the view to open the context menu at the cursor.
    /// </summary>
    public void RequestContextMenu() => ContextMenuRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Toggles the mark on an entry.
    /// </summary>
    /// <param name="item">The entry.</param>
    public void ToggleMark(EntryViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.IsMarked = !item.IsMarked;
        _anchorIndex = IndexOf(item);
        UpdateMarkSummary();
    }

    /// <summary>
    /// Marks exactly the entries between two indexes, inclusive, in either order.
    /// </summary>
    /// <param name="from">One end.</param>
    /// <param name="to">The other end.</param>
    public void MarkRange(int from, int to)
    {
        if (Items.Count == 0)
        {
            return;
        }

        int low = Math.Clamp(Math.Min(from, to), 0, Items.Count - 1);
        int high = Math.Clamp(Math.Max(from, to), 0, Items.Count - 1);

        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].IsMarked = (i >= low) && (i <= high);
        }

        UpdateMarkSummary();
    }

    /// <summary>
    /// Marks every entry.
    /// </summary>
    public void MarkAll()
    {
        foreach (EntryViewModel item in Items)
        {
            item.IsMarked = true;
        }

        UpdateMarkSummary();
    }

    /// <summary>
    /// Clears every mark.
    /// </summary>
    public void ClearMarks()
    {
        ClearMarksCore();
        UpdateMarkSummary();
    }

    /// <summary>
    /// The entries an operation acts on: the marked ones, or the cursor entry when nothing is marked.
    /// </summary>
    /// <returns>The entries, in list order.</returns>
    public IReadOnlyList<EntryViewModel> GetTargets()
    {
        // A file with several matching lines is listed several times in content results; it is one target.
        List<EntryViewModel> marked = [.. Items.Where(item => item.IsMarked).DistinctBy(item => item.FullPath, StringComparer.FromComparison(PathUtilities.Comparison))];

        if (marked.Count > 0)
        {
            return marked;
        }

        return CursorItem is { } cursor ? [cursor] : [];
    }

    /// <summary>
    /// Switches the path bar to its text box, filled with the current path.
    /// </summary>
    public void BeginEditPath()
    {
        EditPathText = CurrentPath;
        IsEditingPath = true;
    }

    /// <summary>
    /// Leaves the path bar's text box without navigating.
    /// </summary>
    public void CancelEditPath() => IsEditingPath = false;

    /// <summary>
    /// Navigates to what was typed in the path bar. A path to a file opens its directory with the cursor
    /// on it. On failure the text box stays open so the typing can be corrected.
    /// </summary>
    /// <returns>A task that completes when the navigation has been attempted.</returns>
    public async Task CommitEditPathAsync()
    {
        string baseDirectory = CurrentPath.Length > 0 ? CurrentPath : _folders.Home;
        string target;

        try
        {
            target = PathUtilities.Resolve(EditPathText, baseDirectory, _folders.Home);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            _status.Error($"Invalid path: {exception.Message}");
            return;
        }

        bool navigated = File.Exists(target) && (PathUtilities.GetParent(target) is { } parent)
            ? await NavigateAsync(parent, Path.GetFileName(target))
            : await NavigateAsync(target);

        if (navigated)
        {
            IsEditingPath = false;
        }
    }

    private Task OnNavigateToSegmentAsync(PathSegment? segment)
        => segment is null ? Task.CompletedTask : NavigateAsync(segment.FullPath);

    private async Task RecoverFromMissingAsync(string vanished)
    {
        // Walk up until a directory can be read: the pane must never be left showing a ghost.
        for (string? candidate = PathUtilities.GetParent(vanished); candidate is not null; candidate = PathUtilities.GetParent(candidate))
        {
            LoadOutcome outcome = await LoadAsync(candidate, new LoadIntent(null, 0, null, RecordHistory: false, ReportMissing: false));

            if (outcome == LoadOutcome.Loaded)
            {
                _status.Info($"'{vanished}' no longer exists");
                return;
            }

            if (outcome != LoadOutcome.Missing)
            {
                return;
            }
        }

        _status.Error($"'{vanished}' no longer exists");
    }

    private void Watch(string path)
    {
        _watch?.Dispose();
        _watch = _disposed ? null : _watchers.Watch(path, OnDirectoryChanged);
    }

    private void OnDirectoryChanged()
    {
        // The watcher reports on the UI thread, after the burst of changes has settled. A read already in
        // flight will show the change anyway, and search results are a snapshot, refreshed on demand.
        if (!_disposed && (_loading is null) && !IsSearchResults)
        {
            _ = RefreshAsync();
        }
    }

    private async Task<LoadOutcome> LoadAsync(string path, LoadIntent intent)
    {
        string? target = TryNormalize(path);

        if (target is null)
        {
            _status.Error($"Invalid path: '{path}'");
            return LoadOutcome.Failed;
        }

        // A newer navigation always wins: the older read is cancelled and its result, if it still
        // arrives, is thrown away.
        _loading?.Cancel();
        CancellationTokenSource loading = new();
        _loading = loading;
        IsLoading = true;

        try
        {
            DirectoryListing listing = await _reader.ReadAsync(target, loading.Token);

            if (loading.IsCancellationRequested)
            {
                return LoadOutcome.Superseded;
            }

            if (intent.RecordHistory && (CurrentPath.Length > 0) && (IsSearchResults || !PathUtilities.AreSame(CurrentPath, listing.Path)))
            {
                _back.Push(Snapshot());
                _forward.Clear();
            }

            LeaveResults();

            bool changedDirectory = !string.Equals(CurrentPath, listing.Path, StringComparison.Ordinal);
            _entries = listing.Entries;
            CurrentPath = listing.Path;
            Breadcrumbs = PathSegment.Split(listing.Path);

            if (changedDirectory)
            {
                _anchorIndex = -1;
                IsVisualMode = false;
                VisualAnchorIndex = -1;
                HighlightPattern = null;
                OnPropertyChanged(nameof(DisplayName));
                Watch(listing.Path);
            }

            Rebuild(intent.FocusName, intent.FallbackIndex, intent.MarkedNames);
            RaiseHistoryChanged();
            return LoadOutcome.Loaded;
        }
        catch (OperationCanceledException) when (loading.IsCancellationRequested)
        {
            return LoadOutcome.Superseded;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or ArgumentException)
        {
            if (!ReferenceEquals(_loading, loading))
            {
                return LoadOutcome.Superseded;
            }

            bool missing = exception is DirectoryNotFoundException;

            if (!missing || intent.ReportMissing)
            {
                string message = Describe(exception, target);
                _status.Error(message);

                if (CurrentPath.Length == 0)
                {
                    PlaceholderText = message;
                }
            }

            return missing ? LoadOutcome.Missing : LoadOutcome.Failed;
        }
        finally
        {
            if (ReferenceEquals(_loading, loading))
            {
                _loading = null;
                IsLoading = false;
            }

            loading.Dispose();
        }
    }

    private void Rebuild(string? focusName, int fallbackIndex, IReadOnlySet<string>? markedNames)
    {
        IEnumerable<FileSystemEntry> visible = ShowHidden ? _entries : _entries.Where(entry => !entry.IsHidden);
        List<EntryViewModel> items = [.. EntrySorter.Sort(visible, Sort).Select(entry => new EntryViewModel(entry))];

        if (markedNames is { Count: > 0 })
        {
            foreach (EntryViewModel item in items)
            {
                item.IsMarked = markedNames.Contains(item.Name);
            }
        }

        if (_cutPaths.Count > 0)
        {
            foreach (EntryViewModel item in items)
            {
                item.IsCut = _cutPaths.Contains(item.FullPath);
            }
        }

        Highlight(items);

        int cursor = focusName is null ? -1 : items.FindIndex(item => string.Equals(item.Name, focusName, PathUtilities.Comparison));

        if (cursor < 0)
        {
            cursor = items.Count == 0 ? -1 : Math.Clamp(fallbackIndex, 0, items.Count - 1);
        }

        // The cursor index is reset first, so the new list's entry gets its flag even when the index is
        // numerically unchanged.
        CursorIndex = -1;
        Items = items;
        RebuildRows();
        SetCursor(cursor);

        if (_anchorIndex >= items.Count)
        {
            _anchorIndex = -1;
        }

        UpdateMarkSummary();
        PlaceholderText = items.Count == 0 ? EmptyFolderText : null;
    }

    private bool Matches(EntryViewModel item) => (HighlightPattern is { } pattern) && SmartCase.Contains(item.Name, pattern);

    private void Highlight(IEnumerable<EntryViewModel> items)
    {
        foreach (EntryViewModel item in items)
        {
            item.IsMatch = Matches(item);
        }
    }

    private void RebuildRows()
    {
        if (IsDetailsView)
        {
            Rows = [];
            return;
        }

        List<EntryRowViewModel> rows = new((Items.Count / ItemsPerRow) + 1);

        for (int start = 0; start < Items.Count; start += ItemsPerRow)
        {
            rows.Add(new EntryRowViewModel([.. Items.Skip(start).Take(ItemsPerRow)]));
        }

        Rows = rows;
    }

    private string Header(string title, SortKey key)
        => Sort.Key != key ? title : $"{title} {(Sort.Direction == SortDirection.Ascending ? "↑" : "↓")}";

    private void SetCursor(int index)
    {
        if (CursorItem is { } previous)
        {
            previous.IsCursor = false;
        }

        CursorIndex = index;

        if (CursorItem is { } current)
        {
            current.IsCursor = true;
        }

        OnPropertyChanged(nameof(CursorItem));
    }

    private void ClearMarksCore()
    {
        foreach (EntryViewModel item in Items)
        {
            item.IsMarked = false;
        }
    }

    private void UpdateMarkSummary()
    {
        int count = 0;
        long size = 0;

        foreach (EntryViewModel item in Items)
        {
            if (item.IsMarked)
            {
                count++;
                size += item.Entry.Size;
            }
        }

        MarkedCount = count;
        MarkedSize = size;
    }

    private HashSet<string>? MarkedNames()
    {
        HashSet<string> names = new(Items.Where(item => item.IsMarked).Select(item => item.Name), StringComparer.FromComparison(PathUtilities.Comparison));
        return names.Count > 0 ? names : null;
    }

    private int IndexOf(EntryViewModel item)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (ReferenceEquals(Items[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    private HistoryEntry Snapshot() => new(CurrentPath, CursorItem?.Name, IsSearchResults ? _results : null);

    private async Task<bool> RestoreAsync(HistoryEntry target)
    {
        if (target.Results is { } results)
        {
            ShowResults(results, target.CursorName);
            return true;
        }

        return await LoadAsync(target.Path, new LoadIntent(target.CursorName, 0, null, RecordHistory: false)) == LoadOutcome.Loaded;
    }

    private async Task RunSearchAsync(SearchQuery query)
    {
        CancelSearch();
        CancellationTokenSource searching = new();
        _searching = searching;

        SearchState state = new(query, []);
        EnterResults(state);
        IsSearching = true;
        SearchTitle = Title(state, finished: false);

        Stopwatch sinceFlush = Stopwatch.StartNew();
        bool stopped = false;

        try
        {
            await foreach (SearchHit hit in _search.SearchAsync(query, searching.Token))
            {
                state.Hits.Add(hit);

                // Hits arrive in bursts; the list is updated a few times a second, not once per hit.
                if (sinceFlush.ElapsedMilliseconds >= 100)
                {
                    Flush(state);
                    sinceFlush.Restart();
                }
            }
        }
        catch (OperationCanceledException) when (searching.IsCancellationRequested)
        {
            stopped = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _status.Error($"Search failed: {exception.Message}");
            stopped = true;
        }
        finally
        {
            if (ReferenceEquals(_searching, searching))
            {
                _searching = null;
                IsSearching = false;

                if (ReferenceEquals(_results, state))
                {
                    state.Stopped = stopped;
                    Flush(state);
                    SearchTitle = Title(state, finished: true);
                }
            }

            searching.Dispose();
        }
    }

    private void EnterResults(SearchState state)
    {
        EndVisual(keepMarks: false);
        _results = state;
        _resultItems.Clear();
        CursorIndex = -1;
        Items = [];
        Rows = [];
        PlaceholderText = null;
        HighlightPattern = null;
        IsSearchResults = true;
        UpdateMarkSummary();
    }

    private void ShowResults(SearchState state, string? cursorName)
    {
        CancelSearch();
        EnterResults(state);
        Flush(state);
        SearchTitle = Title(state, finished: true);

        int index = cursorName is null ? -1 : _resultItems.FindIndex(item => string.Equals(item.Name, cursorName, PathUtilities.Comparison));
        MoveCursorTo(Math.Max(index, 0));
    }

    private void LeaveResults()
    {
        if (!IsSearchResults)
        {
            return;
        }

        CancelSearch();
        _results = null;
        _resultItems.Clear();
        IsSearching = false;
        SearchTitle = null;
        IsSearchResults = false;
    }

    private void Flush(SearchState state)
    {
        if (!ReferenceEquals(_results, state))
        {
            return;
        }

        for (int i = _resultItems.Count; i < state.Hits.Count; i++)
        {
            SearchHit hit = state.Hits[i];
            EntryViewModel item = new(hit.Entry) { Location = hit.RelativeDirectory, LineNumber = hit.LineNumber, LineText = hit.LineText };
            item.IsCut = _cutPaths.Contains(item.FullPath);
            item.IsMatch = Matches(item);
            _resultItems.Add(item);
        }

        Items = [.. _resultItems];

        if ((CursorIndex < 0) && (Items.Count > 0))
        {
            SetCursor(0);
        }

        PlaceholderText = (Items.Count == 0) && !IsSearching ? "No results" : null;
        UpdateMarkSummary();
    }

    private string Title(SearchState state, bool finished)
    {
        string what = state.Query.Kind == SearchKind.Name ? "names matching" : "files containing";
        string where = DisplayName;

        if (!finished)
        {
            return $"Searching {what} \"{state.Query.Pattern}\" in {where}…";
        }

        string count = state.Hits.Count >= state.Query.MaxResults ? $"First {state.Hits.Count}" : state.Hits.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
        string unit = state.Query.Kind == SearchKind.Name ? "names matching" : "lines containing";
        return $"{count} {unit} \"{state.Query.Pattern}\" in {where}{(state.Stopped ? " (stopped)" : string.Empty)}";
    }

    private void RaiseLayoutChanged()
    {
        OnPropertyChanged(nameof(IsListLayout));
        OnPropertyChanged(nameof(ShowsDetailsList));
        OnPropertyChanged(nameof(ShowsIconView));
        OnPropertyChanged(nameof(RowStep));
    }

    private void RaiseHistoryChanged()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        OnPropertyChanged(nameof(CanGoParent));
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
        GoParentCommand.NotifyCanExecuteChanged();
    }

    private static string? TryNormalize(string path)
    {
        try
        {
            return PathUtilities.Normalize(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            return null;
        }
    }

    private static string Describe(Exception exception, string path) => exception switch
    {
        DirectoryNotFoundException => $"'{path}' does not exist",
        UnauthorizedAccessException or SecurityException => $"Access to '{path}' is denied",
        _ => $"Cannot open '{path}': {exception.Message}",
    };

    private sealed record HistoryEntry(string Path, string? CursorName, SearchState? Results = null);

    /// <summary>
    /// One search: its query and its hits, kept in the history so <c>H</c> and <c>L</c> can return to it.
    /// </summary>
    private sealed class SearchState(SearchQuery query, List<SearchHit> hits)
    {
        public SearchQuery Query { get; } = query;

        public List<SearchHit> Hits { get; } = hits;

        public bool Stopped { get; set; }
    }

    private sealed record LoadIntent(string? FocusName, int FallbackIndex, IReadOnlySet<string>? MarkedNames, bool RecordHistory, bool ReportMissing = true);

    private enum LoadOutcome
    {
        Loaded,
        Superseded,
        Missing,
        Failed,
    }
}
