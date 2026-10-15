using System;
using System.Collections.Generic;
using System.Linq;

namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// How the window is divided into panes: a binary tree of splits, with vim's window operations
/// (split, close, only, equalize, resize, move focus to a neighbour).
/// </summary>
/// <remarks>The tree always holds at least one pane. Every mutation raises <see cref="Changed"/>.</remarks>
public sealed class LayoutTree
{
    private const double Tolerance = 1e-9;

    private int _nextId;

    /// <summary>
    /// Creates a layout with a single pane.
    /// </summary>
    public LayoutTree()
    {
        Root = new PaneNode(NewId());
    }

    private LayoutTree(LayoutNode root, int nextId)
    {
        Root = root;
        _nextId = nextId;
    }

    /// <summary>
    /// Raised after the structure or a ratio changed.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// The root of the tree.
    /// </summary>
    public LayoutNode Root { get; private set; }

    /// <summary>
    /// Every pane, in reading order (left to right, top to bottom, depth first).
    /// </summary>
    public IReadOnlyList<PaneId> Panes => [.. Leaves(Root).Select(leaf => leaf.Id)];

    /// <summary>
    /// Rebuilds a layout from its snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns>The layout.</returns>
    /// <exception cref="ArgumentException">The snapshot is not a well-formed tree, or repeats a pane id.</exception>
    public static LayoutTree FromSnapshot(LayoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        HashSet<int> seen = [];
        LayoutNode root = Build(snapshot, seen);
        return new LayoutTree(root, seen.Max() + 1);
    }

    /// <summary>
    /// Replaces the whole layout with a saved one, keeping this instance (and whoever listens to it).
    /// </summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <exception cref="ArgumentException">The snapshot is not a well-formed tree, or repeats a pane id; the
    /// layout is then left as it was.</exception>
    public void Restore(LayoutSnapshot snapshot)
    {
        LayoutTree restored = FromSnapshot(snapshot);
        Root = restored.Root;
        _nextId = restored._nextId;
        OnChanged();
    }

    /// <summary>
    /// Captures the layout as plain data.
    /// </summary>
    /// <returns>The snapshot.</returns>
    public LayoutSnapshot ToSnapshot() => Capture(Root);

    /// <summary>
    /// Whether a pane belongs to the layout.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public bool Contains(PaneId pane) => Find(pane) is not null;

    /// <summary>
    /// Splits a pane in two; the new pane goes to the right of it or below it.
    /// </summary>
    /// <param name="pane">The pane to split.</param>
    /// <param name="axis">Side by side (<see cref="SplitAxis.Columns"/>) or stacked (<see cref="SplitAxis.Rows"/>).</param>
    /// <returns>The new pane.</returns>
    public PaneId Split(PaneId pane, SplitAxis axis)
    {
        PaneNode existing = Require(pane);
        SplitNode? parent = existing.Parent;
        PaneNode created = new(NewId());
        SplitNode split = new(axis, existing, created, 0.5);

        Attach(parent, existing, split);
        OnChanged();
        return created.Id;
    }

    /// <summary>
    /// Closes a pane; the other side of its split takes the space.
    /// </summary>
    /// <param name="pane">The pane to close.</param>
    /// <param name="successor">The pane that should take the focus: the one that took the space, nearest to the closed one.</param>
    /// <returns><see langword="false"/> when the pane is the last one, which cannot be closed.</returns>
    public bool Close(PaneId pane, out PaneId successor)
    {
        PaneNode closing = Require(pane);

        if (closing.Parent is not { } parent)
        {
            successor = pane;
            return false;
        }

        bool wasFirst = ReferenceEquals(parent.First, closing);
        LayoutNode sibling = wasFirst ? parent.Second : parent.First;
        Attach(parent.Parent, parent, sibling);

        // The pane that was adjacent in reading order is the one standing where the closed one was.
        List<PaneNode> leaves = Leaves(sibling);
        successor = wasFirst ? leaves[0].Id : leaves[^1].Id;
        OnChanged();
        return true;
    }

    /// <summary>
    /// Closes every pane but one.
    /// </summary>
    /// <param name="pane">The pane to keep.</param>
    /// <returns>The panes that were closed, in reading order.</returns>
    public IReadOnlyList<PaneId> Only(PaneId pane)
    {
        PaneNode kept = Require(pane);
        List<PaneId> closed = [.. Panes.Where(id => id != pane)];

        if (closed.Count > 0)
        {
            kept.Parent = null;
            Root = kept;
            OnChanged();
        }

        return closed;
    }

    /// <summary>
    /// Gives every pane an equal share: each split's ratio follows how many panes lie along its axis on
    /// either side, so three side-by-side panes get a third each.
    /// </summary>
    public void Equalize()
    {
        EqualizeNode(Root);
        OnChanged();
    }

    /// <summary>
    /// Grows (or, with a negative delta, shrinks) a pane along an axis, by moving the nearest split on that axis.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="axis">Wider/narrower (<see cref="SplitAxis.Columns"/>) or taller/shorter (<see cref="SplitAxis.Rows"/>).</param>
    /// <param name="delta">The change, as a fraction of the split's space.</param>
    /// <returns><see langword="false"/> when no split on that axis contains the pane.</returns>
    public bool Resize(PaneId pane, SplitAxis axis, double delta)
    {
        LayoutNode node = Require(pane);

        for (SplitNode? split = node.Parent; split is not null; node = split, split = split.Parent)
        {
            if (split.Axis == axis)
            {
                bool inFirst = ReferenceEquals(split.First, node);
                split.Ratio += inFirst ? delta : -delta;
                OnChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets a split's ratio, as a dragged splitter does.
    /// </summary>
    /// <param name="split">The split.</param>
    /// <param name="ratio">The first side's new share; clamped to the allowed range.</param>
    public void SetRatio(SplitNode split, double ratio)
    {
        ArgumentNullException.ThrowIfNull(split);

        split.Ratio = ratio;
        OnChanged();
    }

    /// <summary>
    /// Where every pane sits, as fractions of the window.
    /// </summary>
    /// <returns>The rectangles by pane.</returns>
    public IReadOnlyDictionary<PaneId, LayoutRect> Geometry()
    {
        Dictionary<PaneId, LayoutRect> rects = [];
        Measure(Root, new LayoutRect(0, 0, 1, 1), rects);
        return rects;
    }

    /// <summary>
    /// The pane next to another in a direction: one that touches it on that side and overlaps it across,
    /// preferring the one facing its centre.
    /// </summary>
    /// <param name="pane">Where to start.</param>
    /// <param name="direction">Where to look.</param>
    /// <returns>The neighbour, or <see langword="null"/> at the window's edge.</returns>
    public PaneId? FindNeighbor(PaneId pane, FocusDirection direction)
    {
        Require(pane);
        IReadOnlyDictionary<PaneId, LayoutRect> rects = Geometry();
        LayoutRect source = rects[pane];

        PaneId? best = null;
        double bestScore = double.NegativeInfinity;

        foreach ((PaneId candidate, LayoutRect rect) in rects)
        {
            if ((candidate == pane) || !Touches(source, rect, direction))
            {
                continue;
            }

            (double overlap, bool facesCenter) = Across(source, rect, direction);

            if (overlap <= Tolerance)
            {
                continue;
            }

            // Facing the source's centre wins outright; among the others the largest overlap does.
            double score = (facesCenter ? 2 : 0) + overlap;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// The pane after another in reading order, wrapping around.
    /// </summary>
    /// <param name="pane">Where to start.</param>
    /// <returns>The next pane.</returns>
    public PaneId Next(PaneId pane) => Step(pane, 1);

    /// <summary>
    /// The pane before another in reading order, wrapping around.
    /// </summary>
    /// <param name="pane">Where to start.</param>
    /// <returns>The previous pane.</returns>
    public PaneId Previous(PaneId pane) => Step(pane, -1);

    private static LayoutNode Build(LayoutSnapshot snapshot, HashSet<int> seen)
    {
        if (snapshot.Pane is { } id)
        {
            if (!seen.Add(id))
            {
                throw new ArgumentException($"The pane {id} appears twice in the layout.", nameof(snapshot));
            }

            return new PaneNode(new PaneId(id));
        }

        if (snapshot is { Axis: { } axis, First: { } first, Second: { } second })
        {
            return new SplitNode(axis, Build(first, seen), Build(second, seen), snapshot.Ratio ?? 0.5);
        }

        throw new ArgumentException("A layout node must be a pane or a split with both sides.", nameof(snapshot));
    }

    private static LayoutSnapshot Capture(LayoutNode node) => node switch
    {
        PaneNode pane => new LayoutSnapshot(Pane: pane.Id.Value),
        SplitNode split => new LayoutSnapshot(Axis: split.Axis, Ratio: split.Ratio, First: Capture(split.First), Second: Capture(split.Second)),
        _ => throw new InvalidOperationException("Unknown layout node."),
    };

    private static List<PaneNode> Leaves(LayoutNode node)
    {
        List<PaneNode> leaves = [];
        Collect(node, leaves);
        return leaves;

        static void Collect(LayoutNode node, List<PaneNode> leaves)
        {
            if (node is SplitNode split)
            {
                Collect(split.First, leaves);
                Collect(split.Second, leaves);
            }
            else
            {
                leaves.Add((PaneNode)node);
            }
        }
    }

    private static int CountAlong(LayoutNode node, SplitAxis axis) => node switch
    {
        SplitNode split when split.Axis == axis => CountAlong(split.First, axis) + CountAlong(split.Second, axis),
        SplitNode split => Math.Max(CountAlong(split.First, axis), CountAlong(split.Second, axis)),
        _ => 1,
    };

    private static void EqualizeNode(LayoutNode node)
    {
        if (node is not SplitNode split)
        {
            return;
        }

        double first = CountAlong(split.First, split.Axis);
        double second = CountAlong(split.Second, split.Axis);
        split.Ratio = first / (first + second);
        EqualizeNode(split.First);
        EqualizeNode(split.Second);
    }

    private static void Measure(LayoutNode node, LayoutRect rect, Dictionary<PaneId, LayoutRect> rects)
    {
        switch (node)
        {
            case PaneNode pane:
                rects[pane.Id] = rect;
                break;

            case SplitNode { Axis: SplitAxis.Columns } columns:
                double width = rect.Width * columns.Ratio;
                Measure(columns.First, rect with { Width = width }, rects);
                Measure(columns.Second, rect with { X = rect.X + width, Width = rect.Width - width }, rects);
                break;

            case SplitNode rows:
                double height = rect.Height * rows.Ratio;
                Measure(rows.First, rect with { Height = height }, rects);
                Measure(rows.Second, rect with { Y = rect.Y + height, Height = rect.Height - height }, rects);
                break;
        }
    }

    private static bool Touches(LayoutRect source, LayoutRect candidate, FocusDirection direction) => direction switch
    {
        FocusDirection.Left => Math.Abs(candidate.Right - source.X) < Tolerance,
        FocusDirection.Right => Math.Abs(candidate.X - source.Right) < Tolerance,
        FocusDirection.Up => Math.Abs(candidate.Bottom - source.Y) < Tolerance,
        _ => Math.Abs(candidate.Y - source.Bottom) < Tolerance,
    };

    private static (double Overlap, bool FacesCenter) Across(LayoutRect source, LayoutRect candidate, FocusDirection direction)
    {
        bool horizontal = direction is FocusDirection.Left or FocusDirection.Right;

        (double sourceStart, double sourceEnd, double center) = horizontal
            ? (source.Y, source.Bottom, source.CenterY)
            : (source.X, source.Right, source.CenterX);

        (double start, double end) = horizontal ? (candidate.Y, candidate.Bottom) : (candidate.X, candidate.Right);

        double overlap = Math.Min(sourceEnd, end) - Math.Max(sourceStart, start);
        return (overlap, (center >= start) && (center < end));
    }

    private void Attach(SplitNode? parent, LayoutNode oldChild, LayoutNode newChild)
    {
        if (parent is null)
        {
            newChild.Parent = null;
            Root = newChild;
        }
        else
        {
            parent.Replace(oldChild, newChild);
        }
    }

    private PaneId Step(PaneId pane, int direction)
    {
        Require(pane);
        IReadOnlyList<PaneId> panes = Panes;
        int index = 0;

        while (panes[index] != pane)
        {
            index++;
        }

        return panes[(((index + direction) % panes.Count) + panes.Count) % panes.Count];
    }

    private PaneNode? Find(PaneId pane) => Leaves(Root).FirstOrDefault(leaf => leaf.Id == pane);

    private PaneNode Require(PaneId pane)
        => Find(pane) ?? throw new ArgumentException($"The pane {pane} is not in the layout.", nameof(pane));

    private PaneId NewId() => new(_nextId++);

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
