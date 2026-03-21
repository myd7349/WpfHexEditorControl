// ==========================================================
// Project: WpfHexEditor.Editor.XamlDesigner
// File: Controls/GridInsertAdorner.cs
// Author: Derek Tremblay
// Created: 2026-03-19
// Updated: 2026-03-19 — v3:
//   • Toggle button placed at the INNER edge of the grid
//     (x = 0 for Row mode, y = 0 for Column mode) so it is
//     always reachable without leaving the DesignCanvas.
//   • Dashed guide line starts after the toggle button.
//   • LineHitTol raised to 8 px (16 px clickable strip).
//
// Description:
//   Non-hit-testable adorner showing a dashed "insert here" guide line
//   over a WPF Grid on the design canvas.
//
//   Row mode  (default)
//     • Toggle button on the LEFT inner edge (x = 0, centred on linePos Y).
//       Icon: three horizontal stripes + right arrow ▶ ("switch to column").
//     • Horizontal dashed guide line from the toggle's right edge to the
//       grid's right edge at linePos Y.
//     • Left-edge triangle ▶ drawn behind the toggle as a position marker.
//
//   Column mode
//     • Toggle button on the TOP inner edge (y = 0, centred on linePos X).
//       Icon: three vertical stripes + down arrow ▼ ("switch to row").
//     • Vertical dashed guide line from the toggle's bottom edge to the
//       grid's bottom edge at linePos X.
//
//   DesignCanvas rules:
//     • Guide appears as soon as the mouse enters any Grid (first contact).
//     • Position updates only when the mouse is in the active edge band
//       (left/right 18 px for Row, top/bottom 18 px for Column).
//     • Guide freezes in the interior (stays visible at last edge position).
//     • Clicking the guide line inserts the definition.
//     • Clicking the toggle switches between Row and Column mode.
//
// Architecture Notes:
//   IsHitTestVisible = false.  ToggleBounds / LineBounds in Grid-local coords.
//   Managed exclusively by DesignCanvas (created/updated/removed on mouse
//   move and mouse leave).
// ==========================================================

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfHexEditor.Editor.XamlDesigner.Controls;

/// <summary>
/// Dashed "insert here" guide adorner rendered over a
/// <see cref="System.Windows.Controls.Grid"/> on the design canvas.
/// </summary>
public sealed class GridInsertAdorner : Adorner
{
    // ── Mode ──────────────────────────────────────────────────────────────────

    public enum InsertMode { Row, Column }

    // ── Layout constants ──────────────────────────────────────────────────────

    /// <summary>Half-thickness (px) of the clickable strip on the guide line.</summary>
    private const double LineHitTol = 8.0;

    /// <summary>Toggle button width (px).</summary>
    private const double BtnW = 22.0;

    /// <summary>Toggle button height (px).</summary>
    private const double BtnH = 18.0;

    // ── State ─────────────────────────────────────────────────────────────────

    private InsertMode _mode        = InsertMode.Row;
    private double     _linePos     = -1.0;  // Y for Row, X for Column; -1 = hidden
    private int        _insertAfter = -1;

    // ── Hit regions (Grid-local coordinates) ──────────────────────────────────

    /// <summary>Bounding rect of the toggle button in Grid-local space.</summary>
    public Rect ToggleBounds { get; private set; } = Rect.Empty;

    /// <summary>Clickable strip along the guide line in Grid-local space.</summary>
    public Rect LineBounds   { get; private set; } = Rect.Empty;

    public InsertMode Mode        => _mode;
    public int        InsertAfter => _insertAfter;
    public bool       IsVisible   => _linePos >= 0;

    // ── Constructor ───────────────────────────────────────────────────────────

    public GridInsertAdorner(UIElement adornedElement) : base(adornedElement)
        => IsHitTestVisible = false;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Update(double linePos, InsertMode mode, int insertAfter)
    {
        _linePos     = linePos;
        _mode        = mode;
        _insertAfter = insertAfter;
        InvalidateVisual();
    }

    public void ToggleMode()
    {
        _mode = _mode == InsertMode.Row ? InsertMode.Column : InsertMode.Row;
        InvalidateVisual();
    }

    public void Hide()
    {
        if (_linePos < 0) return;
        _linePos     = -1;
        ToggleBounds = Rect.Empty;
        LineBounds   = Rect.Empty;
        InvalidateVisual();
    }

    // ── Adorner overrides ─────────────────────────────────────────────────────

    protected override HitTestResult HitTestCore(PointHitTestParameters p) => null!;

    protected override void OnRender(DrawingContext dc)
    {
        ToggleBounds = Rect.Empty;
        LineBounds   = Rect.Empty;

        if (_linePos < 0) return;

        double w = AdornedElement.RenderSize.Width;
        double h = AdornedElement.RenderSize.Height;

        // ── Colours ───────────────────────────────────────────────────────────
        var accent    = Application.Current?.TryFindResource("XD_SelectionBorderBrush") as Brush
                        ?? new SolidColorBrush(Color.FromRgb(0x00, 0xBF, 0xFF));
        var btnFill   = new SolidColorBrush(Color.FromRgb(0x10, 0x60, 0xBF)); btnFill.Freeze();
        var borderPen = new Pen(accent, 1.0); borderPen.Freeze();
        var linePen   = new Pen(accent, 1.5)
            { DashStyle = new DashStyle(new double[] { 5, 3 }, 0) };
        linePen.Freeze();

        if (_mode == InsertMode.Row)
        {
            double y = Math.Max(0, Math.Min(h, _linePos));

            // Toggle button on the LEFT inner edge (x = 0), centred on guide Y.
            // Clamp vertically so it stays within the grid.
            double btnY = Math.Max(0, Math.Min(h - BtnH, y - BtnH / 2));
            var    btn  = new Rect(0, btnY, BtnW, BtnH);

            // Dashed horizontal line starting after the toggle button.
            dc.DrawLine(linePen, new Point(BtnW, y), new Point(w, y));

            // Toggle button background + icon.
            dc.DrawRoundedRectangle(btnFill, borderPen, btn, 3, 3);
            DrawRowIcon(dc, btn);
            ToggleBounds = btn;

            // Clickable strip: from right edge of toggle to right grid edge.
            LineBounds = new Rect(BtnW, y - LineHitTol, w - BtnW, LineHitTol * 2);
        }
        else
        {
            double x = Math.Max(0, Math.Min(w, _linePos));

            // Toggle button on the TOP inner edge (y = 0), centred on guide X.
            double btnX = Math.Max(0, Math.Min(w - BtnW, x - BtnW / 2));
            var    btn  = new Rect(btnX, 0, BtnW, BtnH);

            // Dashed vertical line starting after the toggle button.
            dc.DrawLine(linePen, new Point(x, BtnH), new Point(x, h));

            // Toggle button background + icon.
            dc.DrawRoundedRectangle(btnFill, borderPen, btn, 3, 3);
            DrawColumnIcon(dc, btn);
            ToggleBounds = btn;

            // Clickable strip: from bottom edge of toggle to bottom grid edge.
            LineBounds = new Rect(x - LineHitTol, BtnH, LineHitTol * 2, h - BtnH);
        }
    }

    // ── Button icons ──────────────────────────────────────────────────────────

    /// <summary>Row-mode icon: three horizontal stripes (≡) + right arrow (▶).</summary>
    private static void DrawRowIcon(DrawingContext dc, Rect btn)
    {
        var pen = new Pen(Brushes.White, 1.2); pen.Freeze();
        double cy = btn.Y + btn.Height / 2;
        double mx = btn.X + btn.Width  / 2 - 1;

        for (double dy = -3.0; dy <= 3.0; dy += 3.0)
            dc.DrawLine(pen, new Point(btn.X + 3, cy + dy), new Point(mx, cy + dy));

        var fig = new PathFigure(
            new Point(mx + 2, cy - 3.5),
            new PathSegment[]
            {
                new LineSegment(new Point(btn.Right - 3, cy      ), true),
                new LineSegment(new Point(mx + 2,        cy + 3.5), true)
            }, true);
        var geo = new PathGeometry(new[] { fig }); geo.Freeze();
        dc.DrawGeometry(Brushes.White, null, geo);
    }

    /// <summary>Column-mode icon: three vertical stripes (|||) + down arrow (▼).</summary>
    private static void DrawColumnIcon(DrawingContext dc, Rect btn)
    {
        var pen = new Pen(Brushes.White, 1.2); pen.Freeze();
        double cx = btn.X + btn.Width  / 2;
        double my = btn.Y + btn.Height / 2 - 1;

        for (double dx = -3.0; dx <= 3.0; dx += 3.0)
            dc.DrawLine(pen, new Point(cx + dx, btn.Y + 3), new Point(cx + dx, my));

        var fig = new PathFigure(
            new Point(cx - 3.5, my + 2),
            new PathSegment[]
            {
                new LineSegment(new Point(cx,        btn.Bottom - 3), true),
                new LineSegment(new Point(cx + 3.5,  my + 2        ), true)
            }, true);
        var geo = new PathGeometry(new[] { fig }); geo.Freeze();
        dc.DrawGeometry(Brushes.White, null, geo);
    }
}
