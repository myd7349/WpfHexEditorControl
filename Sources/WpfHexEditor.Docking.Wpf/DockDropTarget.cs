using System.Windows;
using WpfHexEditor.Docking.Core;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// Represents a potential drop target during a dock drag operation.
/// </summary>
public class DockDropTarget
{
    /// <summary>
    /// The target element (DockTabControl or DocumentTabHost) where the drop would occur.
    /// </summary>
    public required UIElement TargetElement { get; init; }

    /// <summary>
    /// The direction of the dock relative to the target.
    /// </summary>
    public DockDirection Direction { get; init; }

    /// <summary>
    /// The screen-space rectangle of the drop zone for hit-testing.
    /// </summary>
    public Rect DropZone { get; init; }
}
