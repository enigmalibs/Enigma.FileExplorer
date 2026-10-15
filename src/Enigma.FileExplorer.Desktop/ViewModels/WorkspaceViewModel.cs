using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Layout;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The explorer's split groups: the layout tree, a pane per leaf, and which pane is active. Commands act
/// on the active pane; vim's window operations act on the layout.
/// </summary>
public sealed class WorkspaceViewModel : ObservableObject
{
    private readonly IPaneFactory _panes;
    private readonly ViewOptions _options;
    private readonly IFileClipboard _clipboard;
    private readonly Dictionary<PaneId, PaneViewModel> _byId = [];
    private int _batch;

    /// <summary>
    /// Creates the workspace with its first pane.
    /// </summary>
    /// <param name="panes">Creates panes.</param>
    /// <param name="options">The view settings every pane shares.</param>
    /// <param name="clipboard">The clipboard of files, whose cut entries every pane fades.</param>
    public WorkspaceViewModel(IPaneFactory panes, ViewOptions options, IFileClipboard clipboard)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clipboard);

        _panes = panes;
        _options = options;
        _clipboard = clipboard;
        _clipboard.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IFileClipboard.CutPaths))
            {
                foreach (PaneViewModel pane in _byId.Values)
                {
                    pane.ApplyCut(_clipboard.CutPaths);
                }
            }
        };
        _options.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ViewOptions.ShowHidden))
            {
                foreach (PaneViewModel pane in _byId.Values)
                {
                    pane.ShowHidden = _options.ShowHidden;
                }
            }
        };
        Layout = new LayoutTree();
        Layout.Changed += (_, _) =>
        {
            // Inside a batch the panes are not in step with the tree yet; the batch reports once at its end.
            if (_batch == 0)
            {
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        ActivePaneId = Layout.Panes[0];
        ActivePane = Add(ActivePaneId);
        UpdateActiveFlags();

        SplitColumnsCommand = new AsyncRelayCommand<PaneViewModel>(pane => SplitAsync(pane, SplitAxis.Columns));
        SplitRowsCommand = new AsyncRelayCommand<PaneViewModel>(pane => SplitAsync(pane, SplitAxis.Rows));
        CloseCommand = new RelayCommand<PaneViewModel>(pane =>
        {
            if (pane is not null)
            {
                Activate(pane);
                Close();
            }
        });
    }

    /// <summary>
    /// Raised after the layout changed and every pane in it exists: what views must rebuild from, rather
    /// than <see cref="LayoutTree.Changed"/>, which fires mid-operation.
    /// </summary>
    public event EventHandler? LayoutChanged;

    /// <summary>
    /// How the window is divided.
    /// </summary>
    public LayoutTree Layout { get; }

    /// <summary>
    /// Every pane, in reading order.
    /// </summary>
    public IReadOnlyList<PaneViewModel> Panes => [.. Layout.Panes.Select(id => _byId[id])];

    /// <summary>
    /// The id of the active pane in <see cref="Layout"/>.
    /// </summary>
    public PaneId ActivePaneId { get; private set; }

    /// <summary>
    /// The pane commands act on and the keyboard drives.
    /// </summary>
    public PaneViewModel ActivePane { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Splits a pane (the toolbar buttons): side by side.
    /// </summary>
    public AsyncRelayCommand<PaneViewModel> SplitColumnsCommand { get; }

    /// <summary>
    /// Splits a pane (the toolbar buttons): stacked.
    /// </summary>
    public AsyncRelayCommand<PaneViewModel> SplitRowsCommand { get; }

    /// <summary>
    /// Closes a pane (the toolbar button).
    /// </summary>
    public RelayCommand<PaneViewModel> CloseCommand { get; }

    /// <summary>
    /// The pane of a layout leaf.
    /// </summary>
    /// <param name="id">The leaf.</param>
    /// <returns>The pane.</returns>
    public PaneViewModel PaneFor(PaneId id) => _byId[id];

    /// <summary>
    /// The layout leaf of a pane.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>Its id, or <see langword="null"/> when it is not (or no longer) part of the workspace.</returns>
    public PaneId? IdOf(PaneViewModel pane)
    {
        foreach ((PaneId id, PaneViewModel candidate) in _byId)
        {
            if (ReferenceEquals(candidate, pane))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>
    /// Makes a pane the active one.
    /// </summary>
    /// <param name="pane">The pane.</param>
    public void Activate(PaneViewModel pane)
    {
        ArgumentNullException.ThrowIfNull(pane);

        if (IdOf(pane) is { } id)
        {
            Activate(id);
        }
    }

    /// <summary>
    /// Splits the active pane; the new pane opens where the active one is (or on <paramref name="path"/>)
    /// with the same settings, and becomes active.
    /// </summary>
    /// <param name="axis">Side by side or stacked.</param>
    /// <param name="path">Where the new pane opens; the active pane's directory when omitted.</param>
    /// <returns>A task that completes when the new pane shows its directory.</returns>
    public Task<PaneViewModel> SplitAsync(SplitAxis axis, string? path = null) => SplitAsync(ActivePane, axis, path);

    /// <summary>
    /// Closes the active pane; the pane that takes its space becomes active.
    /// </summary>
    /// <returns><see langword="false"/> when it is the last pane, which cannot be closed.</returns>
    public bool Close()
    {
        PaneId closing = ActivePaneId;
        bool closed = false;

        Batch(() =>
        {
            if (Layout.Close(closing, out PaneId successor))
            {
                closed = true;
                Activate(successor);
                Remove(closing);
            }
        });

        if (closed)
        {
            UpdateActiveFlags();
            OnPropertyChanged(nameof(Panes));
        }

        return closed;
    }

    /// <summary>
    /// Closes every pane but the active one.
    /// </summary>
    public void Only()
    {
        Batch(() =>
        {
            foreach (PaneId closed in Layout.Only(ActivePaneId))
            {
                Remove(closed);
            }
        });

        UpdateActiveFlags();
        OnPropertyChanged(nameof(Panes));
    }

    /// <summary>
    /// Moves to the pane next to the active one in a direction.
    /// </summary>
    /// <param name="direction">Where to look.</param>
    /// <returns><see langword="false"/> at the window's edge.</returns>
    public bool Focus(FocusDirection direction)
    {
        if (Layout.FindNeighbor(ActivePaneId, direction) is not { } neighbour)
        {
            return false;
        }

        Activate(neighbour);
        return true;
    }

    /// <summary>
    /// Moves to the next pane in reading order, wrapping.
    /// </summary>
    public void FocusNext() => Activate(Layout.Next(ActivePaneId));

    /// <summary>
    /// Moves to the previous pane in reading order, wrapping.
    /// </summary>
    public void FocusPrevious() => Activate(Layout.Previous(ActivePaneId));

    /// <summary>
    /// Gives every pane an equal share.
    /// </summary>
    public void Equalize() => Layout.Equalize();

    /// <summary>
    /// Grows or shrinks the active pane.
    /// </summary>
    /// <param name="axis">Wider/narrower (columns) or taller/shorter (rows).</param>
    /// <param name="delta">The change, as a fraction of the split's space; negative shrinks.</param>
    /// <returns><see langword="false"/> when no split on that axis contains the pane.</returns>
    public bool Resize(SplitAxis axis, double delta) => Layout.Resize(ActivePaneId, axis, delta);

    /// <summary>
    /// Opens the workspace's single pane at start, shown the way it was last shown.
    /// </summary>
    /// <param name="view">The view and sort to show it with; the defaults when omitted.</param>
    /// <param name="path">Where it opens (a command-line argument); the home directory when omitted.</param>
    /// <returns>A task that completes when the pane shows its directory.</returns>
    public Task StartAsync(PaneSettings? view = null, string? path = null)
    {
        if (view is not null)
        {
            ActivePane.ViewMode = view.ViewMode;
            ActivePane.Sort = new EntrySort(view.SortKey, view.SortDirection);
        }

        return ActivePane.StartAsync(path);
    }

    /// <summary>
    /// Captures how the active pane shows its entries, for the next start.
    /// </summary>
    /// <returns>The view and sort.</returns>
    public PaneSettings CaptureView() => new()
    {
        ViewMode = ActivePane.ViewMode,
        SortKey = ActivePane.Sort.Key,
        SortDirection = ActivePane.Sort.Direction,
    };

    /// <summary>
    /// Refreshes the panes a copy or move changed: those showing its target or a source's directory. The
    /// pane it was made in puts its cursor on the first entry created, as after any paste.
    /// </summary>
    /// <param name="origin">The pane the paste or drop was made in, if any.</param>
    /// <param name="target">The directory transferred into.</param>
    /// <param name="sources">What was transferred.</param>
    /// <param name="result">What the transfer did.</param>
    /// <returns>A task that completes when the panes show the result.</returns>
    public async Task RefreshAfterTransferAsync(PaneViewModel? origin, string target, IReadOnlyList<string> sources, TransferResult result)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(result);

        HashSet<string> affected = new(StringComparer.FromComparison(PathUtilities.Comparison)) { PathUtilities.Normalize(target) };

        foreach (string source in sources)
        {
            if (PathUtilities.GetParent(source) is { } parent)
            {
                affected.Add(parent);
            }
        }

        string? focus = result.Created.Count > 0 ? Path.GetFileName(result.Created[0]) : null;

        foreach (PaneViewModel pane in Panes)
        {
            if (ReferenceEquals(pane, origin) && PathUtilities.AreSame(pane.CurrentPath, target))
            {
                await pane.RefreshAsync(focus, keepMarks: false);
            }
            else if (affected.Contains(pane.CurrentPath))
            {
                await pane.RefreshAsync();
            }
        }
    }

    private async Task<PaneViewModel> SplitAsync(PaneViewModel? source, SplitAxis axis, string? path = null)
    {
        source ??= ActivePane;
        PaneId sourceId = IdOf(source) ?? ActivePaneId;
        PaneViewModel? pane = null;

        Batch(() =>
        {
            PaneId created = Layout.Split(sourceId, axis);
            pane = Add(created);

            // A split shows the same place the same way, as vim's split shows the same buffer.
            pane.Sort = source.Sort;
            pane.ViewMode = source.ViewMode;
            Activate(created);
        });

        OnPropertyChanged(nameof(Panes));

        if (pane is null)
        {
            throw new InvalidOperationException("The split did not create a pane.");
        }

        if (path is not null)
        {
            await pane.StartAsync(path);
        }
        else if (source.CurrentPath.Length > 0)
        {
            await pane.NavigateAsync(source.CurrentPath, source.CursorItem?.Name);
        }
        else
        {
            await pane.StartAsync();
        }

        return pane;
    }

    private void Batch(Action action)
    {
        _batch++;

        try
        {
            action();
        }
        finally
        {
            _batch--;
        }

        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private PaneViewModel Add(PaneId id)
    {
        PaneViewModel pane = _panes.Create();
        pane.ShowHidden = _options.ShowHidden;
        pane.ApplyCut(_clipboard.CutPaths);
        _byId[id] = pane;
        return pane;
    }

    private void Remove(PaneId id)
    {
        if (_byId.Remove(id, out PaneViewModel? pane))
        {
            pane.Dispose();
        }
    }

    private void Activate(PaneId id)
    {
        if (!_byId.TryGetValue(id, out PaneViewModel? pane))
        {
            return;
        }

        ActivePaneId = id;
        ActivePane = pane;
        UpdateActiveFlags();
    }

    private void UpdateActiveFlags()
    {
        bool several = _byId.Count > 1;

        foreach ((PaneId id, PaneViewModel pane) in _byId)
        {
            bool active = id == ActivePaneId;
            pane.IsActive = active;
            pane.IsFramed = active && several;
        }
    }
}
