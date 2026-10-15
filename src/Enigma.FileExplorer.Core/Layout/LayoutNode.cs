namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// A node of a <see cref="LayoutTree"/>: a pane, or a split of two subtrees.
/// </summary>
public abstract class LayoutNode
{
    /// <summary>
    /// The split this node is one side of; <see langword="null"/> for the root.
    /// </summary>
    public SplitNode? Parent { get; internal set; }
}
