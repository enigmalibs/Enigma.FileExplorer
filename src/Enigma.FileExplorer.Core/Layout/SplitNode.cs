using System;

namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// Two subtrees sharing a space along an axis.
/// </summary>
public sealed class SplitNode : LayoutNode
{
    /// <summary>
    /// The smallest share either side may have.
    /// </summary>
    public const double MinimumRatio = 0.1;

    /// <summary>
    /// The largest share the first side may have.
    /// </summary>
    public const double MaximumRatio = 0.9;

    /// <summary>
    /// Creates a split and adopts both sides.
    /// </summary>
    /// <param name="axis">How the space is divided.</param>
    /// <param name="first">The left or top side.</param>
    /// <param name="second">The right or bottom side.</param>
    /// <param name="ratio">The first side's share of the space.</param>
    internal SplitNode(SplitAxis axis, LayoutNode first, LayoutNode second, double ratio)
    {
        Axis = axis;
        First = first;
        Second = second;
        Ratio = ratio;
        first.Parent = this;
        second.Parent = this;
    }

    /// <summary>
    /// How the space is divided.
    /// </summary>
    public SplitAxis Axis { get; }

    /// <summary>
    /// The left or top side.
    /// </summary>
    public LayoutNode First { get; internal set; }

    /// <summary>
    /// The right or bottom side.
    /// </summary>
    public LayoutNode Second { get; internal set; }

    /// <summary>
    /// The first side's share of the space, between <see cref="MinimumRatio"/> and <see cref="MaximumRatio"/>.
    /// </summary>
    public double Ratio
    {
        get;
        internal set => field = double.IsFinite(value) ? Math.Clamp(value, MinimumRatio, MaximumRatio) : 0.5;
    }

    /// <summary>
    /// Replaces one side, adopting the new child.
    /// </summary>
    /// <param name="oldChild">The side to replace.</param>
    /// <param name="newChild">Its replacement.</param>
    internal void Replace(LayoutNode oldChild, LayoutNode newChild)
    {
        if (ReferenceEquals(First, oldChild))
        {
            First = newChild;
        }
        else
        {
            Second = newChild;
        }

        newChild.Parent = this;
    }
}
