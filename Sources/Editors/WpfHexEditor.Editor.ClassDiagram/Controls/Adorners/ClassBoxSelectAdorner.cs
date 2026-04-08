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
//     Handle size is fixed at 6x6 logical pixels on screen.
//     AdornedBounds is set by DiagramCanvas in DIAGRAM coordinates (node.X/Y/W/H).
//     The WPF AdornerLayer applies GetDesiredTransform (AdornedElement → AdornerLayer)
//     as a RenderTransform on this adorner, so OnRender coordinates ARE diagram coords.
//     Drawing at _adornedBounds directly is correct — zoom/pan is handled by the
//     adorner layer transform, not by manual coordinate mapping.
//     Handle size is kept screen-constant by dividing by the zoom factor extracted
//     from the adorner layer transform.
//     Colors use CD_SelectionBorderBrush and CD_SelectionHandleFill tokens.
// ==========================================================

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfHexEditor.Editor.ClassDiagram.Controls.Adorners;

/// <summary>
/// Selection adorner with 8 resize handles drawn around a class box.
/// Draws in diagram (node) coordinates — AdornerLayer applies the zoom/pan transform.
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
    /// The AdornerLayer transform handles mapping to screen. Setting this triggers a redraw.
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

        // Diagram-space bounds — the adorner layer's GetDesiredTransform has already
        // applied zoom+pan as a RenderTransform, so OnRender coordinates = diagram coords.
        Rect bounds = _adornedBounds;

        // Extract zoom factor from the adorner's current transform so handles stay
        // a fixed size on screen regardless of zoom level.
        double zoom = GetZoomFromTransform();
        double halfH = zoom > 0 ? ScreenHandleSize / zoom / 2.0 : ScreenHandleSize / 2.0;

        Brush? borderBrush = TryFindResource("CD_SelectionBorderBrush") as Brush
            ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));

        Brush? handleFill = TryFindResource("CD_SelectionHandleFill") as Brush
            ?? Brushes.White;

        double strokeThickness = zoom > 0 ? 1.5 / zoom : 1.5;
        var selectionPen = new Pen(borderBrush, strokeThickness);
        var handlePen    = new Pen(borderBrush, zoom > 0 ? 1.0 / zoom : 1.0);

        // Selection border
        drawingContext.DrawRectangle(null, selectionPen, bounds);

        // 8 handle positions in diagram space (size adjusted for zoom)
        foreach (Rect handle in ComputeHandleRects(bounds, halfH))
            drawingContext.DrawRectangle(handleFill, handlePen, handle);
    }

    // ---------------------------------------------------------------------------
    // Zoom extraction
    // ---------------------------------------------------------------------------

    private double GetZoomFromTransform()
    {
        // The adorner layer sets a RenderTransform on the adorner via GetDesiredTransform.
        // For a uniform scale + translate, ScaleX gives the zoom factor.
        try
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
            if (adornerLayer is null) return 1.0;
            GeneralTransform gt = AdornedElement.TransformToVisual(adornerLayer);
            if (gt is MatrixTransform mt)
                return mt.Matrix.M11;
            if (gt is Transform t)
            {
                var matrix = t.Value;
                return matrix.M11;
            }
        }
        catch { /* visual tree not ready */ }
        return 1.0;
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
