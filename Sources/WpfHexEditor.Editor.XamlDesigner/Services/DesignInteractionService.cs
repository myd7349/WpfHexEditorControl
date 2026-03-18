// ==========================================================
// Project: WpfHexEditor.Editor.XamlDesigner
// File: DesignInteractionService.cs
// Author: Derek Tremblay
// Created: 2026-03-17
// Description:
//     Orchestrates drag-move and resize interactions on the XAML design canvas.
//     Tracks drag state, computes attribute deltas, and produces DesignOperations
//     that are applied via DesignToXamlSyncService.
//
// Architecture Notes:
//     Service pattern (stateful per-session, one instance per split host).
//     Strategy pattern — move vs resize handled by distinct code paths.
//     Raises OperationCommitted event; the split host applies the XAML patch
//     and pushes the DesignOperation onto the undo stack.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHexEditor.Editor.XamlDesigner.Models;

namespace WpfHexEditor.Editor.XamlDesigner.Services;

/// <summary>
/// Manages interactive drag-move and resize on the XAML design canvas.
/// </summary>
public sealed class DesignInteractionService
{
    // ── State ─────────────────────────────────────────────────────────────────

    private Point  _moveStartPoint;
    private Thickness _startMargin;
    private double _startWidth;
    private double _startHeight;
    private double _startCanvasLeft;
    private double _startCanvasTop;
    private bool   _isInMove;
    private int    _activeUid = -1;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when a design operation is completed and ready to be applied.
    /// The handler should call DesignToXamlSyncService.PatchElement and
    /// push the operation onto the undo stack.
    /// </summary>
    public event EventHandler<DesignOperationCommittedEventArgs>? OperationCommitted;

    // ── Move API ──────────────────────────────────────────────────────────────

    /// <summary>Called when the user starts dragging an element to move it.</summary>
    public void OnMoveStart(FrameworkElement element, Point canvasPosition, int elementUid)
    {
        _moveStartPoint = canvasPosition;
        _startMargin    = element.Margin;
        _startWidth     = element.ActualWidth;
        _startHeight    = element.ActualHeight;
        _startCanvasLeft = Canvas.GetLeft(element);
        _startCanvasTop  = Canvas.GetTop(element);
        _isInMove        = true;
        _activeUid       = elementUid;
    }

    /// <summary>Called on each mouse move during a drag-move operation.</summary>
    public void OnMoveDelta(FrameworkElement element, Point canvasPosition)
    {
        if (!_isInMove) return;

        double dx = canvasPosition.X - _moveStartPoint.X;
        double dy = canvasPosition.Y - _moveStartPoint.Y;

        // Apply position change depending on parent panel type.
        if (element.Parent is Canvas)
        {
            double newLeft = (_startCanvasLeft is double.NaN ? 0 : _startCanvasLeft) + dx;
            double newTop  = (_startCanvasTop  is double.NaN ? 0 : _startCanvasTop)  + dy;
            Canvas.SetLeft(element, newLeft);
            Canvas.SetTop(element, newTop);
        }
        else
        {
            // Margin-based move (works for most panels).
            element.Margin = new Thickness(
                _startMargin.Left + dx,
                _startMargin.Top  + dy,
                _startMargin.Right,
                _startMargin.Bottom);
        }
    }

    /// <summary>Called when the user releases the mouse after a drag-move.</summary>
    public void OnMoveCompleted(FrameworkElement element)
    {
        if (!_isInMove) return;
        _isInMove = false;

        var before = new Dictionary<string, string?>();
        var after  = new Dictionary<string, string?>();

        if (element.Parent is Canvas)
        {
            double newLeft = Canvas.GetLeft(element);
            double newTop  = Canvas.GetTop(element);
            before["Canvas.Left"] = _startCanvasLeft.ToString(System.Globalization.CultureInfo.InvariantCulture);
            before["Canvas.Top"]  = _startCanvasTop.ToString(System.Globalization.CultureInfo.InvariantCulture);
            after["Canvas.Left"]  = newLeft.ToString(System.Globalization.CultureInfo.InvariantCulture);
            after["Canvas.Top"]   = newTop.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            before["Margin"] = ThicknessToString(_startMargin);
            after["Margin"]  = ThicknessToString(element.Margin);
        }

        if (IsSameState(before, after)) return;

        var op = DesignOperation.CreateMove(_activeUid, before, after);
        OperationCommitted?.Invoke(this, new DesignOperationCommittedEventArgs(op, element));
    }

    // ── Resize API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the user drags a resize handle (index 0–7, NW→N→NE→E→SE→S→SW→W).
    /// </summary>
    public void OnResizeDelta(FrameworkElement element, int handleIndex, double dx, double dy, int elementUid)
    {
        if (_activeUid < 0) _activeUid = elementUid;

        double w = element.ActualWidth;
        double h = element.ActualHeight;
        var    m = element.Margin;

        // Handle indices: 0=NW, 1=N, 2=NE, 3=E, 4=SE, 5=S, 6=SW, 7=W
        switch (handleIndex)
        {
            case 0: // NW — shrink/grow from top-left
                element.Width  = Math.Max(8, w - dx);
                element.Height = Math.Max(8, h - dy);
                element.Margin = new Thickness(m.Left + dx, m.Top + dy, m.Right, m.Bottom);
                break;
            case 1: // N — resize height from top
                element.Height = Math.Max(8, h - dy);
                element.Margin = new Thickness(m.Left, m.Top + dy, m.Right, m.Bottom);
                break;
            case 2: // NE — grow right, shrink/grow top
                element.Width  = Math.Max(8, w + dx);
                element.Height = Math.Max(8, h - dy);
                element.Margin = new Thickness(m.Left, m.Top + dy, m.Right, m.Bottom);
                break;
            case 3: // E — resize width from right
                element.Width  = Math.Max(8, w + dx);
                break;
            case 4: // SE — grow right and down
                element.Width  = Math.Max(8, w + dx);
                element.Height = Math.Max(8, h + dy);
                break;
            case 5: // S — resize height from bottom
                element.Height = Math.Max(8, h + dy);
                break;
            case 6: // SW — grow left and down
                element.Width  = Math.Max(8, w - dx);
                element.Height = Math.Max(8, h + dy);
                element.Margin = new Thickness(m.Left + dx, m.Top, m.Right, m.Bottom);
                break;
            case 7: // W — resize width from left
                element.Width  = Math.Max(8, w - dx);
                element.Margin = new Thickness(m.Left + dx, m.Top, m.Right, m.Bottom);
                break;
        }
    }

    /// <summary>Called when the user releases a resize handle.</summary>
    public void OnResizeStarted(FrameworkElement element, int elementUid)
    {
        _startWidth  = element.ActualWidth;
        _startHeight = element.ActualHeight;
        _startMargin = element.Margin;
        _activeUid   = elementUid;
    }

    /// <summary>Called when a resize drag is completed.</summary>
    public void OnResizeCompleted(FrameworkElement element)
    {
        if (_activeUid < 0) return;

        var before = new Dictionary<string, string?>();
        var after  = new Dictionary<string, string?>();

        before["Width"]  = DoubleToXaml(_startWidth);
        before["Height"] = DoubleToXaml(_startHeight);
        before["Margin"] = ThicknessToString(_startMargin);
        after["Width"]   = DoubleToXaml(element.Width);
        after["Height"]  = DoubleToXaml(element.Height);
        after["Margin"]  = ThicknessToString(element.Margin);

        if (IsSameState(before, after)) return;

        var op = DesignOperation.CreateResize(_activeUid, before, after);
        OperationCommitted?.Invoke(this, new DesignOperationCommittedEventArgs(op, element));
        _activeUid = -1;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string ThicknessToString(Thickness t) =>
        t.Left  == t.Right  && t.Left == t.Top && t.Left == t.Bottom
            ? t.Left.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : $"{t.Left},{t.Top},{t.Right},{t.Bottom}";

    private static string DoubleToXaml(double v) =>
        double.IsNaN(v) ? "Auto"
        : v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static bool IsSameState(
        Dictionary<string, string?> before,
        Dictionary<string, string?> after)
    {
        foreach (var key in before.Keys)
        {
            if (!after.TryGetValue(key, out var afterVal)) return false;
            if (before[key] != afterVal) return false;
        }
        return true;
    }
}

/// <summary>
/// Event arguments for a committed design operation.
/// </summary>
public sealed class DesignOperationCommittedEventArgs(
    DesignOperation operation,
    FrameworkElement element) : EventArgs
{
    public DesignOperation   Operation { get; } = operation;
    public FrameworkElement  Element   { get; } = element;
}
