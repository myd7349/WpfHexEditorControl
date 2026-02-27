//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Input;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// Manages drag and drop operations for dock tabs and floating windows using mouse capture.
/// Shows a compass rose overlay on the entire dock area and a floating preview window.
/// Supports two modes: tab drag (from docked tabs) and floating window drag (from title bar).
/// </summary>
public class DockDragManager
{
    private readonly DockControl _dockControl;
    private DockOverlayWindow? _overlay;
    private DragPreviewWindow? _previewWindow;

    private DockItem? _draggedItem;
    private DockGroupNode? _originalGroup;
    private DockDirection? _lastDirection;
    private bool _isDragging;

    // Floating window drag state
    private FloatingWindow? _sourceFloatingWindow;
    private Point _dragOffset;
    private Point _originalWindowPos;

    public DockDragManager(DockControl dockControl)
    {
        _dockControl = dockControl;
    }

    public bool IsDragging => _isDragging;

    /// <summary>
    /// Starts a drag operation for the given item using mouse capture on the DockControl.
    /// Used when dragging a tab from a docked panel.
    /// </summary>
    public void BeginDrag(DockItem item)
    {
        if (_isDragging) return;
        if (_dockControl.Engine is null || _dockControl.Layout is null) return;

        _isDragging = true;
        _draggedItem = item;
        _originalGroup = item.Owner;
        _lastDirection = null;
        _sourceFloatingWindow = null;

        // Create the floating preview window
        _previewWindow = new DragPreviewWindow(item.Title);
        var screenPos = GetMouseScreenPosition();
        _previewWindow.MoveTo(screenPos);
        _previewWindow.Show();

        // Show compass overlay over the entire dock center area
        _overlay ??= new DockOverlayWindow();
        _overlay.ShowOverTarget(_dockControl.CenterHost);

        // Capture mouse on the DockControl to receive all mouse events
        _dockControl.PreviewMouseMove += OnPreviewMouseMove;
        _dockControl.PreviewMouseLeftButtonUp += OnPreviewMouseUp;
        _dockControl.KeyDown += OnKeyDown;
        Mouse.Capture(_dockControl, CaptureMode.SubTree);
    }

    /// <summary>
    /// Starts a drag operation for a floating window. The window follows the cursor
    /// and the compass overlay appears when over the dock area.
    /// </summary>
    public void BeginFloatingDrag(DockItem item, FloatingWindow sourceWindow)
    {
        if (_isDragging) return;
        if (_dockControl.Engine is null || _dockControl.Layout is null) return;

        _isDragging = true;
        _draggedItem = item;
        _originalGroup = item.Owner;
        _lastDirection = null;
        _sourceFloatingWindow = sourceWindow;

        // Record offset from cursor to window corner for smooth drag
        var cursorScreen = sourceWindow.PointToScreen(Mouse.GetPosition(sourceWindow));
        _dragOffset = new Point(cursorScreen.X - sourceWindow.Left, cursorScreen.Y - sourceWindow.Top);
        _originalWindowPos = new Point(sourceWindow.Left, sourceWindow.Top);

        // Show compass overlay over the dock center area
        _overlay ??= new DockOverlayWindow();
        // Don't show overlay immediately - only when mouse enters dock area

        // Capture mouse on the floating window
        sourceWindow.PreviewMouseMove += OnFloatingMouseMove;
        sourceWindow.PreviewMouseLeftButtonUp += OnFloatingMouseUp;
        sourceWindow.KeyDown += OnFloatingKeyDown;
        Mouse.Capture(sourceWindow, CaptureMode.SubTree);
    }

    #region Tab drag handlers

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _overlay is null) return;

        var screenPos = _dockControl.PointToScreen(e.GetPosition(_dockControl));

        // Move preview window to follow cursor
        _previewWindow?.MoveTo(screenPos);

        // Check if mouse is within the DockControl bounds
        var localPos = e.GetPosition(_dockControl.CenterHost);
        var bounds = new Rect(0, 0, _dockControl.CenterHost.ActualWidth, _dockControl.CenterHost.ActualHeight);

        if (bounds.Contains(localPos))
        {
            if (!_overlay.IsVisible)
                _overlay.ShowOverTarget(_dockControl.CenterHost);

            // HitTest the compass rose
            _lastDirection = _overlay.HitTest(screenPos);
            _overlay.HighlightedDirection = _lastDirection;
        }
        else
        {
            // Mouse outside dock area - will float on drop
            _lastDirection = null;
            _overlay.HighlightedDirection = null;
            if (_overlay.IsVisible)
                _overlay.Hide();
        }
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || _draggedItem is null) return;

        var engine = _dockControl.Engine;
        var layout = _dockControl.Layout;
        if (engine is null || layout is null)
        {
            EndDrag();
            return;
        }

        if (_lastDirection.HasValue)
        {
            // Drop on compass zone: dock to MainDocumentHost with the chosen direction
            engine.Dock(_draggedItem, layout.MainDocumentHost, _lastDirection.Value);
        }
        else
        {
            // Drop outside compass: float the item
            engine.Float(_draggedItem);
            _dockControl.RebuildVisualTree();
        }

        EndDrag();
        _dockControl.RebuildVisualTree();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isDragging)
        {
            EndDrag();
            e.Handled = true;
        }
    }

    #endregion

    #region Floating window drag handlers

    private void OnFloatingMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _sourceFloatingWindow is null) return;

        var screenPos = _sourceFloatingWindow.PointToScreen(e.GetPosition(_sourceFloatingWindow));

        // Move the floating window following cursor with offset
        _sourceFloatingWindow.Left = screenPos.X - _dragOffset.X;
        _sourceFloatingWindow.Top = screenPos.Y - _dragOffset.Y;

        // Check if cursor is over the dock area (screen coordinates)
        var dockScreenPos = _dockControl.CenterHost.PointToScreen(new Point(0, 0));
        var dockBounds = new Rect(
            dockScreenPos.X, dockScreenPos.Y,
            _dockControl.CenterHost.ActualWidth,
            _dockControl.CenterHost.ActualHeight);

        _overlay ??= new DockOverlayWindow();

        if (dockBounds.Contains(screenPos))
        {
            if (!_overlay.IsVisible)
                _overlay.ShowOverTarget(_dockControl.CenterHost);

            _lastDirection = _overlay.HitTest(screenPos);
            _overlay.HighlightedDirection = _lastDirection;
        }
        else
        {
            _lastDirection = null;
            _overlay.HighlightedDirection = null;
            if (_overlay.IsVisible)
                _overlay.Hide();
        }
    }

    private void OnFloatingMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || _draggedItem is null || _sourceFloatingWindow is null) return;

        var engine = _dockControl.Engine;
        var layout = _dockControl.Layout;
        if (engine is null || layout is null)
        {
            EndFloatingDrag();
            return;
        }

        if (_lastDirection.HasValue)
        {
            // Drop on compass zone: dock the item and close the floating window
            engine.Dock(_draggedItem, layout.MainDocumentHost, _lastDirection.Value);
            _dockControl.RebuildVisualTree();
            _sourceFloatingWindow.Close();
        }
        // else: leave floating window at its new position

        EndFloatingDrag();
    }

    private void OnFloatingKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isDragging && _sourceFloatingWindow is not null)
        {
            // Restore original window position
            _sourceFloatingWindow.Left = _originalWindowPos.X;
            _sourceFloatingWindow.Top = _originalWindowPos.Y;
            EndFloatingDrag();
            e.Handled = true;
        }
    }

    #endregion

    /// <summary>
    /// Ends a tab drag operation and cleans up all state.
    /// </summary>
    private void EndDrag()
    {
        _isDragging = false;
        _draggedItem = null;
        _originalGroup = null;
        _lastDirection = null;
        _sourceFloatingWindow = null;

        Mouse.Capture(null);

        _dockControl.PreviewMouseMove -= OnPreviewMouseMove;
        _dockControl.PreviewMouseLeftButtonUp -= OnPreviewMouseUp;
        _dockControl.KeyDown -= OnKeyDown;

        _previewWindow?.Close();
        _previewWindow = null;

        _overlay?.Hide();
    }

    /// <summary>
    /// Ends a floating window drag operation and cleans up all state.
    /// </summary>
    private void EndFloatingDrag()
    {
        var window = _sourceFloatingWindow;

        _isDragging = false;
        _draggedItem = null;
        _originalGroup = null;
        _lastDirection = null;
        _sourceFloatingWindow = null;

        Mouse.Capture(null);

        if (window is not null)
        {
            window.PreviewMouseMove -= OnFloatingMouseMove;
            window.PreviewMouseLeftButtonUp -= OnFloatingMouseUp;
            window.KeyDown -= OnFloatingKeyDown;
        }

        _overlay?.Hide();
    }

    private Point GetMouseScreenPosition()
    {
        var pos = Mouse.GetPosition(_dockControl);
        return _dockControl.PointToScreen(pos);
    }
}
