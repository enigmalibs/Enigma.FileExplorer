namespace Enigma.FileExplorer.Core.Layout;

/// <summary>
/// A leaf: one pane.
/// </summary>
/// <param name="id">The pane.</param>
public sealed class PaneNode(PaneId id) : LayoutNode
{
    /// <summary>
    /// The pane.
    /// </summary>
    public PaneId Id { get; } = id;
}
