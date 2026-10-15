using System;
using System.Collections.Generic;
using Enigma.FileExplorer.Core.Layout;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Layout;

public sealed class LayoutTreeTests
{
    private const double Precision = 1e-9;

    [Fact]
    public void ANewTree_HasOnePaneFillingTheWindow()
    {
        LayoutTree tree = new();

        PaneId only = Assert.Single(tree.Panes);
        Assert.Equal(new LayoutRect(0, 0, 1, 1), tree.Geometry()[only]);
    }

    [Fact]
    public void Split_Columns_PutsTheNewPaneToTheRight()
    {
        LayoutTree tree = new();
        PaneId left = tree.Panes[0];

        PaneId right = tree.Split(left, SplitAxis.Columns);

        Assert.Equal([left, right], tree.Panes);
        IReadOnlyDictionary<PaneId, LayoutRect> rects = tree.Geometry();
        Assert.Equal(new LayoutRect(0, 0, 0.5, 1), rects[left]);
        Assert.Equal(new LayoutRect(0.5, 0, 0.5, 1), rects[right]);
    }

    [Fact]
    public void Split_Rows_PutsTheNewPaneBelow()
    {
        LayoutTree tree = new();
        PaneId top = tree.Panes[0];

        PaneId bottom = tree.Split(top, SplitAxis.Rows);

        Assert.Equal(new LayoutRect(0, 0.5, 1, 0.5), tree.Geometry()[bottom]);
    }

    [Fact]
    public void Split_Nested_SplitsOnlyThatPane()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);

        IReadOnlyDictionary<PaneId, LayoutRect> rects = tree.Geometry();

        Assert.Equal([a, b, c], tree.Panes);
        Assert.Equal(new LayoutRect(0, 0, 0.5, 1), rects[a]);
        Assert.Equal(new LayoutRect(0.5, 0, 0.5, 0.5), rects[b]);
        Assert.Equal(new LayoutRect(0.5, 0.5, 0.5, 0.5), rects[c]);
    }

    [Fact]
    public void Split_RaisesChanged()
    {
        LayoutTree tree = new();
        int changes = 0;
        tree.Changed += (_, _) => changes++;

        tree.Split(tree.Panes[0], SplitAxis.Columns);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Close_TheSiblingTakesTheSpace()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);

        Assert.True(tree.Close(a, out PaneId successor));

        Assert.Equal(b, successor);
        Assert.Equal([b], tree.Panes);
        Assert.Equal(new LayoutRect(0, 0, 1, 1), tree.Geometry()[b]);
        Assert.Null(tree.Root.Parent);
    }

    [Fact]
    public void Close_InANestedTree_KeepsTheRestAndPicksTheAdjacentSuccessor()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);
        PaneId d = tree.Split(c, SplitAxis.Columns);

        Assert.True(tree.Close(b, out PaneId successor));

        Assert.Equal(c, successor);
        Assert.Equal([a, c, d], tree.Panes);
        Assert.Equal(new LayoutRect(0.5, 0, 0.25, 1), tree.Geometry()[c]);

        Assert.True(tree.Close(d, out successor));
        Assert.Equal(c, successor);
    }

    [Fact]
    public void Close_TheLastPane_IsRefused()
    {
        LayoutTree tree = new();
        PaneId only = tree.Panes[0];

        Assert.False(tree.Close(only, out PaneId successor));
        Assert.Equal(only, successor);
        Assert.Single(tree.Panes);
    }

    [Fact]
    public void Only_KeepsOnePaneAndReportsTheOthers()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);

        IReadOnlyList<PaneId> closed = tree.Only(b);

        Assert.Equal([a, c], closed);
        Assert.Equal([b], tree.Panes);
        Assert.Equal(new LayoutRect(0, 0, 1, 1), tree.Geometry()[b]);
    }

    [Fact]
    public void Equalize_GivesThreeColumnsAThirdEach()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Columns);

        tree.Equalize();

        IReadOnlyDictionary<PaneId, LayoutRect> rects = tree.Geometry();
        Assert.Equal(1.0 / 3, rects[a].Width, Precision);
        Assert.Equal(1.0 / 3, rects[b].Width, Precision);
        Assert.Equal(1.0 / 3, rects[c].Width, Precision);
    }

    [Fact]
    public void Equalize_CountsColumnsAcrossNestedRows()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);
        PaneId d = tree.Split(c, SplitAxis.Columns);

        tree.Equalize();

        IReadOnlyDictionary<PaneId, LayoutRect> rects = tree.Geometry();
        Assert.Equal(1.0 / 3, rects[a].Width, Precision);
        Assert.Equal(2.0 / 3, rects[b].Width, Precision);
        Assert.Equal(1.0 / 3, rects[c].Width, Precision);
        Assert.Equal(1.0 / 3, rects[d].Width, Precision);
        Assert.Equal(0.5, rects[b].Height, Precision);
    }

    [Fact]
    public void Resize_GrowsThePaneAlongTheAxis()
    {
        LayoutTree tree = new();
        PaneId left = tree.Panes[0];
        PaneId right = tree.Split(left, SplitAxis.Columns);

        Assert.True(tree.Resize(right, SplitAxis.Columns, 0.1));
        Assert.Equal(0.6, tree.Geometry()[right].Width, Precision);

        Assert.True(tree.Resize(left, SplitAxis.Columns, 0.2));
        Assert.Equal(0.6, tree.Geometry()[left].Width, Precision);
    }

    [Fact]
    public void Resize_UsesTheNearestSplitOnThatAxis_AndRefusesWithoutOne()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);

        Assert.True(tree.Resize(c, SplitAxis.Columns, -0.1));
        Assert.Equal(0.6, tree.Geometry()[a].Width, Precision);

        Assert.False(tree.Resize(a, SplitAxis.Rows, 0.1));
    }

    [Fact]
    public void Ratios_AreClamped()
    {
        LayoutTree tree = new();
        PaneId left = tree.Panes[0];
        PaneId right = tree.Split(left, SplitAxis.Columns);

        tree.Resize(left, SplitAxis.Columns, 5);
        Assert.Equal(SplitNode.MaximumRatio, tree.Geometry()[left].Width, Precision);

        tree.SetRatio((SplitNode)tree.Root, -1);
        Assert.Equal(SplitNode.MinimumRatio, tree.Geometry()[left].Width, Precision);
        Assert.Equal(1 - SplitNode.MinimumRatio, tree.Geometry()[right].Width, Precision);
    }

    [Fact]
    public void FindNeighbor_InAGrid()
    {
        // a | b
        // --+--
        // c | d
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId c = tree.Split(a, SplitAxis.Rows);
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId d = tree.Split(c, SplitAxis.Columns);

        Assert.Equal(b, tree.FindNeighbor(a, FocusDirection.Right));
        Assert.Equal(c, tree.FindNeighbor(a, FocusDirection.Down));
        Assert.Equal(a, tree.FindNeighbor(b, FocusDirection.Left));
        Assert.Equal(d, tree.FindNeighbor(b, FocusDirection.Down));
        Assert.Equal(b, tree.FindNeighbor(d, FocusDirection.Up));
        Assert.Null(tree.FindNeighbor(a, FocusDirection.Left));
        Assert.Null(tree.FindNeighbor(a, FocusDirection.Up));
    }

    [Fact]
    public void FindNeighbor_PrefersThePaneFacingTheCentre()
    {
        // A tall pane on the left, two stacked on the right: from the left, "right" reaches the pane that
        // faces its centre, the lower one once the upper is short.
        LayoutTree tree = new();
        PaneId left = tree.Panes[0];
        PaneId top = tree.Split(left, SplitAxis.Columns);
        PaneId bottom = tree.Split(top, SplitAxis.Rows);
        tree.Resize(top, SplitAxis.Rows, -0.3);

        Assert.Equal(bottom, tree.FindNeighbor(left, FocusDirection.Right));
        Assert.Equal(left, tree.FindNeighbor(top, FocusDirection.Left));
        Assert.Equal(left, tree.FindNeighbor(bottom, FocusDirection.Left));
    }

    [Fact]
    public void NextAndPrevious_WrapInReadingOrder()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        PaneId c = tree.Split(b, SplitAxis.Rows);

        Assert.Equal(b, tree.Next(a));
        Assert.Equal(c, tree.Next(b));
        Assert.Equal(a, tree.Next(c));
        Assert.Equal(c, tree.Previous(a));
    }

    [Fact]
    public void Snapshot_RoundTrips()
    {
        LayoutTree tree = new();
        PaneId a = tree.Panes[0];
        PaneId b = tree.Split(a, SplitAxis.Columns);
        tree.Split(b, SplitAxis.Rows);
        tree.Resize(a, SplitAxis.Columns, 0.15);

        LayoutTree copy = LayoutTree.FromSnapshot(tree.ToSnapshot());

        Assert.Equal(tree.Panes, copy.Panes);
        Assert.Equal(tree.Geometry(), copy.Geometry());

        // New ids never collide with restored ones.
        PaneId added = copy.Split(a, SplitAxis.Rows);
        Assert.DoesNotContain(added, tree.Panes);
    }

    [Fact]
    public void Restore_ReplacesTheTree_InPlace_AndKeepsNewIdsUnique()
    {
        LayoutTree tree = new();
        int changes = 0;
        tree.Changed += (_, _) => changes++;
        LayoutSnapshot saved = new(Axis: SplitAxis.Rows, Ratio: 0.25, First: new LayoutSnapshot(Pane: 4), Second: new LayoutSnapshot(Pane: 7));

        tree.Restore(saved);
        PaneId added = tree.Split(new PaneId(7), SplitAxis.Columns);

        Assert.Equal(2, changes);
        Assert.Equal([new PaneId(4), new PaneId(7), added], tree.Panes);
        Assert.Equal(8, added.Value);
        Assert.Equal(0.25, Assert.IsType<SplitNode>(tree.Root).Ratio, Precision);
    }

    [Fact]
    public void Restore_AMalformedTree_ChangesNothing()
    {
        LayoutTree tree = new();
        tree.Split(tree.Panes[0], SplitAxis.Columns);
        LayoutSnapshot before = tree.ToSnapshot();

        Assert.Throws<ArgumentException>(() => tree.Restore(new LayoutSnapshot(Axis: SplitAxis.Rows, First: new LayoutSnapshot(Pane: 1), Second: new LayoutSnapshot(Pane: 1))));

        Assert.Equal(before, tree.ToSnapshot());
    }

    [Fact]
    public void FromSnapshot_RejectsMalformedTrees()
    {
        Assert.Throws<ArgumentException>(() => LayoutTree.FromSnapshot(new LayoutSnapshot()));
        Assert.Throws<ArgumentException>(() => LayoutTree.FromSnapshot(new LayoutSnapshot(Axis: SplitAxis.Rows, First: new LayoutSnapshot(Pane: 1))));
        Assert.Throws<ArgumentException>(() => LayoutTree.FromSnapshot(
            new LayoutSnapshot(Axis: SplitAxis.Rows, First: new LayoutSnapshot(Pane: 1), Second: new LayoutSnapshot(Pane: 1))));
    }

    [Fact]
    public void Operations_OnAnUnknownPane_Throw()
    {
        LayoutTree tree = new();
        PaneId stranger = new(42);

        Assert.False(tree.Contains(stranger));
        Assert.Throws<ArgumentException>(() => tree.Split(stranger, SplitAxis.Rows));
        Assert.Throws<ArgumentException>(() => tree.FindNeighbor(stranger, FocusDirection.Left));
    }
}
