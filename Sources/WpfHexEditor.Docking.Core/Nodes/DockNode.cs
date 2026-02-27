namespace WpfHexEditor.Docking.Core.Nodes;

/// <summary>
/// Abstract base class for all dock tree nodes.
/// </summary>
public abstract class DockNode
{
    public Guid Id { get; } = Guid.NewGuid();

    public DockNode? Parent { get; internal set; }

    public DockLockMode LockMode { get; set; } = DockLockMode.None;

    /// <summary>
    /// Detaches this node from its parent.
    /// </summary>
    internal void DetachFromParent()
    {
        if (Parent is DockSplitNode split)
        {
            split.RemoveChild(this);
        }

        Parent = null;
    }
}
