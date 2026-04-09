// ==========================================================
// Project: WpfHexEditor.Editor.ClassDiagram
// File: Controls/Adorners/ClassBoxSelectAdorner.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-19
// Description:
//     WPF Adorner that renders a selection outline with 8 resize handles
//     (NW, N, NE, E, SE, S, SW, W) around a selected class box.
//
// Architecture Notes:
//     Pattern: Decorator (WPF Adorner).
//     AdornedBounds is supplied in DIAGRAM coordinates (node.X/Y/Width/Height).
//     OnRender maps diagram coords → adorner-layer coords via TransformToVisual.
//     TransformToVisual traverses via the common visual root and includes
//     ZoomPanCanvas.RenderTransform (scale + translate) automatically.
//     InvalidateVisual must be called on every drag step AND on every zoom/pan
//     frame so the adorner repaints with the updated transform.
//     Colors use CD_SelectionBorderBrush and CD_SelectionHandleFill tokens.
// ==========================================================

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfHexEditor.Editor.ClassDiagram.Controls.Adorners;

/// <summary>
/// Selection adorner with 8 resize handles drawn around a class box.
/// </summary>
public sealed class ClassBoxSelectAdorner : Adorner
{
    private const double ScreenHandleSize = 6.0;  // px on screen, zoom-invariant

    private Rect _adornedBounds = Rect.Empty;

    public ClassBoxSelectAdorner(UIElement adornedElement) : base(adornedElement)
    {
        IsHitTestVisible = false;
    }

    // ---------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Bounds of the adorned class box in DIAGRAM coordinates (node.X/Y/Width/Height).
    /// Setting this triggers a redraw.
    /// </summary>
    public Rect AdornedBounds
    {
        get => _adornedBounds;
        set { _adornedBounds = value; InvalidateVisual(); }
    }

    // ---------------------------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------------------------

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_adornedBounds == Rect.Empty) return;

        // Map diagram-local bounds → adorner-layer coordinate space.
        // The adorner layer is a sibling of ZoomPanCanvas under the AdornerDecorator;
        // TransformToVisual traverses via the common visual root and correctly includes
        // ZoomPanCanvas.RenderTransform (scale + translate).
        var adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
        Rect bounds = _adornedBounds;
        double zoom  = 1.0;
        if (adornerLayer is not null)
        {
            try
            {
                GeneralTransform gt = AdornedElement.TransformToVisual(adornerLayer);
                bounds = gt.TransformBounds(_adornedBounds);
                // Extract zoom so handles + stroke stay pixel-constant on screen
                zoom = bounds.Width > 0 && _adornedBounds.Width > 0
                    ? bounds.Width / _adornedBounds.Width
                    : 1.0;
            }
            catch { /* visual tree not ready — fall back to diagram-local coords */ }
        }

        Brush? borderBrush = TryFindResource("CD_SelectionBorderBrush") as Brush
            ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));

        Brush? handleFill = TryFindResource("CD_SelectionHandleFill") as Brush
            ?? Brushes.White;

        var selectionPen = new Pen(borderBrush, 1.5);
        var handlePen    = new Pen(borderBrush, 1.0);

        drawingContext.DrawRectangle(null, selectionPen, bounds);

        double halfH = ScreenHandleSize / 2.0;   // handles already in adorner-layer (screen) px
        foreach (Rect handle in ComputeHandleRects(bounds, halfH))
            drawingContext.DrawRectangle(handleFill, handlePen, handle);
    }

    // ---------------------------------------------------------------------------
    // Handle computation
    // ---------------------------------------------------------------------------

    private static IEnumerable<Rect> ComputeHandleRects(Rect bounds, double halfH)
    {
        double left   = bounds.Left;
        double right  = bounds.Right;
        double top    = bounds.Top;
        double bottom = bounds.Bottom;
        double midX   = (left + right) / 2.0;
        double midY   = (top + bottom) / 2.0;

        yield return HandleAt(left,  top,    halfH);   // NW
        yield return HandleAt(midX,  top,    halfH);   // N
        yield return HandleAt(right, top,    halfH);   // NE
        yield return HandleAt(right, midY,   halfH);   // E
        yield return HandleAt(right, bottom, halfH);   // SE
        yield return HandleAt(midX,  bottom, halfH);   // S
        yield return HandleAt(left,  bottom, halfH);   // SW
        yield return HandleAt(left,  midY,   halfH);   // W
    }

    private static Rect HandleAt(double cx, double cy, double half) =>
        new(cx - half, cy - half, half * 2, half * 2);
}
