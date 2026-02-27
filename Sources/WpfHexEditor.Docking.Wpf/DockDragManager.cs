using System.Windows;
using System.Windows.Input;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// Manages drag and drop operations for dock tabs.
/// Coordinates between the <see cref="DockOverlayWindow"/> and the <see cref="DockEngine"/>.
/// </summary>
public class DockDragManager
{
    private readonly DockControl _dockControl;
    private DockOverlayWindow? _overlay;
    private DockItem? _draggedItem;
    private bool _isDragging;

    public DockDragManager(DockControl dockControl)
    {
        _dockControl = dockControl;
    }

    /// <summary>
    /// Starts a drag operation for the given item.
    /// </summary>
    public void BeginDrag(DockItem item)
    {
        if (_isDragging) return;

        _draggedItem = item;
        _isDragging = true;

        // Start WPF drag-drop
        var data = new DataObject("DockItem", item);
        DragDrop.DoDragDrop(_dockControl, data, DragDropEffects.Move);

        EndDrag();
    }

    /// <summary>
    /// Shows the overlay for the given target element during drag.
    /// </summary>
    public void ShowOverlay(UIElement target)
    {
        _overlay ??= new DockOverlayWindow();
        _overlay.ShowOverTarget(target);
    }

    /// <summary>
    /// Updates the overlay highlight based on the current mouse position.
    /// </summary>
    public DockDirection? UpdateOverlay(Point screenPoint)
    {
        if (_overlay is null) return null;

        var direction = _overlay.HitTest(screenPoint);
        _overlay.HighlightedDirection = direction;
        return direction;
    }

    /// <summary>
    /// Hides the overlay.
    /// </summary>
    public void HideOverlay()
    {
        _overlay?.Hide();
    }

    /// <summary>
    /// Ends the drag operation.
    /// </summary>
    public void EndDrag()
    {
        _isDragging = false;
        _draggedItem = null;
        HideOverlay();
    }

    /// <summary>
    /// Completes the drop by docking the dragged item at the given target and direction.
    /// </summary>
    public void CompleteDrop(DockGroupNode target, DockDirection direction)
    {
        if (_draggedItem is null || _dockControl.Engine is null) return;

        _dockControl.Engine.Dock(_draggedItem, target, direction);
        _dockControl.RebuildVisualTree();
    }
}
