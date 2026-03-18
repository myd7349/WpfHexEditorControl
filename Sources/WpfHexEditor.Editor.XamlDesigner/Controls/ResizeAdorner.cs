// ==========================================================
// Project: WpfHexEditor.Editor.XamlDesigner
// File: ResizeAdorner.cs
// Author: Derek Tremblay
// Created: 2026-03-17
// Description:
//     Interactive adorner combining selection highlight with 8 resize handles
//     and drag-to-move capability. Replaces SelectionAdorner when interaction
//     is enabled on the design canvas.
//
// Architecture Notes:
//     Adorner with VisualCollection of 8 Thumb controls for resize.
//     Cursor=SizeAll on the adorner body for drag-move.
//     Delegates all interaction logic to DesignInteractionService.
//     Theme-aware via XD_SelectionBorderBrush and XD_ResizeHandleFillBrush tokens.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Editor.XamlDesigner.Services;

namespace WpfHexEditor.Editor.XamlDesigner.Controls;

/// <summary>
/// Adorner that draws a dashed selection border and 8 interactive resize handles.
/// Supports drag-to-move and drag-to-resize via <see cref="DesignInteractionService"/>.
/// </summary>
public sealed class ResizeAdorner : Adorner
{
    // ── Handle layout (NW, N, NE, E, SE, S, SW, W) ───────────────────────────

    private static readonly (double NormX, double NormY, Cursor Cursor)[] HandleConfigs =
    [
        (0.0, 0.0, Cursors.SizeNWSE), // 0 NW
        (0.5, 0.0, Cursors.SizeNS),   // 1 N
        (1.0, 0.0, Cursors.SizeNESW), // 2 NE
        (1.0, 0.5, Cursors.SizeWE),   // 3 E
        (1.0, 1.0, Cursors.SizeNWSE), // 4 SE
        (0.5, 1.0, Cursors.SizeNS),   // 5 S
        (0.0, 1.0, Cursors.SizeNESW), // 6 SW
        (0.0, 0.5, Cursors.SizeWE),   // 7 W
    ];

    private const double HandleSize = 8.0;
    private const double BorderInset = 0.5;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly VisualCollection      _visuals;
    private readonly Thumb[]               _handles = new Thumb[8];
    private readonly DesignInteractionService _interaction;
    private readonly int                   _elementUid;

    private Point _dragStart;
    private bool  _isMoving;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ResizeAdorner(
        UIElement                 adornedElement,
        DesignInteractionService  interaction,
        int                       elementUid)
        : base(adornedElement)
    {
        _interaction = interaction;
        _elementUid  = elementUid;
        _visuals     = new VisualCollection(this);

        IsHitTestVisible = true;
        Cursor           = Cursors.SizeAll;

        BuildHandles();
        WireMouseEvents();
    }

    // ── Adorner overrides ─────────────────────────────────────────────────────

    protected override int    VisualChildrenCount        => _visuals.Count;
    protected override Visual GetVisualChild(int index)  => _visuals[index];

    protected override Size ArrangeOverride(Size finalSize)
    {
        var w = AdornedElement.RenderSize.Width;
        var h = AdornedElement.RenderSize.Height;

        for (int i = 0; i < 8; i++)
        {
            var cfg = HandleConfigs[i];
            var x   = cfg.NormX * w - HandleSize / 2.0;
            var y   = cfg.NormY * h - HandleSize / 2.0;
            _handles[i].Arrange(new Rect(x, y, HandleSize, HandleSize));
        }

        return finalSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        // Transparent hit-test background so move drag works anywhere on the border.
        dc.DrawRectangle(
            Brushes.Transparent,
            null,
            new Rect(AdornedElement.RenderSize));

        // Dashed selection border.
        var borderBrush = Application.Current?.TryFindResource("XD_SelectionBorderBrush") as Brush
                          ?? new SolidColorBrush(Color.FromRgb(0, 122, 204));

        var pen = new Pen(borderBrush, 1.5)
        {
            DashStyle = new DashStyle(new double[] { 4, 2 }, 0)
        };
        pen.Freeze();

        var bounds = new Rect(AdornedElement.RenderSize);
        dc.DrawRectangle(
            null, pen,
            new Rect(
                bounds.X + BorderInset,
                bounds.Y + BorderInset,
                Math.Max(0, bounds.Width  - BorderInset * 2),
                Math.Max(0, bounds.Height - BorderInset * 2)));
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void BuildHandles()
    {
        for (int i = 0; i < 8; i++)
        {
            int capturedIndex = i;

            var thumb = new Thumb
            {
                Width  = HandleSize,
                Height = HandleSize,
                Cursor = HandleConfigs[i].Cursor
            };

            ApplyHandleStyle(thumb);

            thumb.DragStarted  += (_, _) =>
            {
                if (AdornedElement is FrameworkElement fe)
                    _interaction.OnResizeStarted(fe, _elementUid);
            };

            thumb.DragDelta += (_, e) =>
            {
                if (AdornedElement is FrameworkElement fe)
                    _interaction.OnResizeDelta(fe, capturedIndex, e.HorizontalChange, e.VerticalChange, _elementUid);
                InvalidateArrange();
            };

            thumb.DragCompleted += (_, _) =>
            {
                if (AdornedElement is FrameworkElement fe)
                    _interaction.OnResizeCompleted(fe);
            };

            _handles[i] = thumb;
            _visuals.Add(thumb);
        }
    }

    private static void ApplyHandleStyle(Thumb thumb)
    {
        // Try to use the theme resource; fall back to a hardcoded VS-blue square.
        var fillBrush   = Application.Current?.TryFindResource("XD_ResizeHandleFillBrush")   as Brush
                          ?? Brushes.White;
        var strokeBrush = Application.Current?.TryFindResource("XD_ResizeHandleStrokeBrush") as Brush
                          ?? new SolidColorBrush(Color.FromRgb(0, 122, 204));

        // Build a simple rectangle template for the thumb.
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty,          fillBrush);
        factory.SetValue(Border.BorderBrushProperty,         strokeBrush);
        factory.SetValue(Border.BorderThicknessProperty,     new Thickness(1));

        thumb.Template = new ControlTemplate(typeof(Thumb))
        {
            VisualTree = factory
        };
    }

    // ── Move mouse events ─────────────────────────────────────────────────────

    private void WireMouseEvents()
    {
        MouseLeftButtonDown += OnMoveStart;
        MouseMove           += OnMoveMove;
        MouseLeftButtonUp   += OnMoveEnd;
    }

    private void OnMoveStart(object sender, MouseButtonEventArgs e)
    {
        // If the click landed on a resize handle, let the Thumb handle it.
        if (e.OriginalSource is Thumb) return;

        if (AdornedElement is not FrameworkElement fe) return;

        _dragStart = e.GetPosition(fe.Parent as UIElement ?? fe);
        _isMoving  = true;

        _interaction.OnMoveStart(fe, _dragStart, _elementUid);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMoveMove(object sender, MouseEventArgs e)
    {
        if (!_isMoving || AdornedElement is not FrameworkElement fe) return;

        var current = e.GetPosition(fe.Parent as UIElement ?? fe);
        _interaction.OnMoveDelta(fe, current);

        // Nudge the adorner to follow.
        InvalidateArrange();
    }

    private void OnMoveEnd(object sender, MouseButtonEventArgs e)
    {
        if (!_isMoving) return;
        _isMoving = false;
        ReleaseMouseCapture();

        if (AdornedElement is FrameworkElement fe)
            _interaction.OnMoveCompleted(fe);
    }
}
