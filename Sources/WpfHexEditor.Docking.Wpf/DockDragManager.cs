//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
    /// Converts a physical screen pixel point to WPF DIPs using the given visual's DPI.
    /// </summary>
    private static Point ScreenToDip(Visual visual, Point screenPoint)
    {
        var source = PresentationSource.FromVisual(visual);
        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformFromDevice.Transform(screenPoint);
        return screenPoint;
    }

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
        var dipPos = ScreenToDip(_dockControl, _dockControl.PointToScreen(Mouse.GetPosition(_dockControl)));
        _previewWindow.MoveTo(dipPos);
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

        // Record offset from cursor to window corner in DIPs for smooth drag
        var cursorDip = ScreenToDip(sourceWindow, sourceWindow.PointToScreen(Mouse.GetPosition(sourceWindow)));
        _dragOffset = new Point(cursorDip.X - sourceWindow.Left, cursorDip.Y - sourceWindow.Top);
        _originalWindowPos = new Point(sourceWindow.Left, sourceWindow.Top);

        // Show compass overlay over the dock center area
        _overlay ??= new DockOverlayWindow();

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

        // Get cursor position in physical pixels (for overlay HitTest) and DIPs (for preview window)
        var screenPos = _dockControl.PointToScreen(e.GetPosition(_dockControl));
        var dipPos = ScreenToDip(_dockControl, screenPos);

        // Move preview window to follow cursor (DIPs)
        _previewWindow?.MoveTo(dipPos);

        // Check if mouse is within the DockControl bounds (local coords are already in DIPs)
        var localPos = e.GetPosition(_dockControl.CenterHost);
        var bounds = new Rect(0, 0, _dockControl.CenterHost.ActualWidth, _dockControl.CenterHost.ActualHeight);

        if (bounds.Contains(localPos))
        {
            if (!_overlay.IsVisible)
                _overlay.ShowOverTarget(_dockControl.CenterHost);

            // HitTest accepts physical screen pixels and converts internally
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
            engine.Dock(_draggedItem, layout.MainDocumentHost, _lastDirection.Value);
        }
        else
        {
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

        // Get cursor in both physical pixels and DIPs
        var screenPos = _sourceFloatingWindow.PointToScreen(e.GetPosition(_sourceFloatingWindow));
        var dipPos = ScreenToDip(_sourceFloatingWindow, screenPos);

        // Move the floating window following cursor (DIPs)
        _sourceFloatingWindow.Left = dipPos.X - _dragOffset.X;
        _sourceFloatingWindow.Top = dipPos.Y - _dragOffset.Y;

        // Check if cursor is over the dock area using physical pixel bounds
        // (both endpoints from PointToScreen so units are consistent)
        var topLeft = _dockControl.CenterHost.PointToScreen(new Point(0, 0));
        var bottomRight = _dockControl.CenterHost.PointToScreen(
            new Point(_dockControl.CenterHost.ActualWidth, _dockControl.CenterHost.ActualHeight));
        var dockBounds = new Rect(topLeft, bottomRight);

        _overlay ??= new DockOverlayWindow();

        if (dockBounds.Contains(screenPos))
        {
            if (!_overlay.IsVisible)
                _overlay.ShowOverTarget(_dockControl.CenterHost);

            // HitTest accepts physical screen pixels and converts internally
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
            engine.Dock(_draggedItem, layout.MainDocumentHost, _lastDirection.Value);
            _dockControl.RebuildVisualTree();
            _sourceFloatingWindow.Close();
        }

        EndFloatingDrag();
    }

    private void OnFloatingKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isDragging && _sourceFloatingWindow is not null)
        {
            _sourceFloatingWindow.Left = _originalWindowPos.X;
            _sourceFloatingWindow.Top = _originalWindowPos.Y;
            EndFloatingDrag();
            e.Handled = true;
        }
    }

    #endregion

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
}
