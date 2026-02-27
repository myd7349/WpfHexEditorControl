namespace WpfHexEditor.Docking.Core.Nodes;

/// <summary>
/// Represents an individual panel or document in the dock layout.
/// </summary>
public class DockItem
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Title { get; set; }

    /// <summary>
    /// Unique identifier used to match content when restoring layouts.
    /// </summary>
    public required string ContentId { get; set; }

    public bool CanClose { get; set; } = true;

    public bool CanFloat { get; set; } = true;

    public DockItemState State { get; set; } = DockItemState.Docked;

    /// <summary>
    /// The group this item belongs to (null if floating or hidden).
    /// </summary>
    public DockGroupNode? Owner { get; internal set; }
}
