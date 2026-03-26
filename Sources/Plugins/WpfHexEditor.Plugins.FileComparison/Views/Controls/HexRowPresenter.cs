// Project      : WpfHexEditorControl
// File         : Views/Controls/HexRowPresenter.cs
// Description  : Lightweight DrawingContext renderer for a 16-cell hex diff row.
//                Replaces four nested ItemsControls (64 WPF containers per row) with
//                a single FrameworkElement that paints all cells via DrawRectangle +
//                DrawText — same visual output, ~10× fewer WPF elements.
// Architecture : WPF-only, no ViewModel dependency.  Bound in DataTemplate via DPs.

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WpfHexEditor.Core.Diff.Models;

namespace WpfHexEditor.Plugins.FileComparison.Views.Controls;

/// <summary>
/// Identifies which side of the diff row to render.
/// </summary>
public enum DiffSide { Left, Right }

/// <summary>
/// A <see cref="FrameworkElement"/> that paints 16 hex cells of one side
/// of a <see cref="BinaryHexDiffRow"/> using <see cref="DrawingContext"/>
/// instead of 16 individual WPF containers.
/// </summary>
public sealed class HexRowPresenter : FrameworkElement
{
    // ── Cell geometry constants ──────────────────────────────────────────────

    private const double CellW  = 20.0;
    private const double CellH  = 22.0;
    private const double TextX  = 2.0;
    private const double TextY  = 3.0;
    private const double FontSz = 10.0;

    // ── Per-instance FormattedText cache (keyed by hex text) ────────────────
    // Invalidated when foreground brush or font size changes.

    private Dictionary<string, FormattedText>? _ftCache;
    private Brush? _lastFgBrush;

    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(
            nameof(Row), typeof(BinaryHexDiffRow), typeof(HexRowPresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SideProperty =
        DependencyProperty.Register(
            nameof(Side), typeof(DiffSide), typeof(HexRowPresenter),
            new FrameworkPropertyMetadata(DiffSide.Left, FrameworkPropertyMetadataOptions.AffectsRender));

    public BinaryHexDiffRow? Row
    {
        get => (BinaryHexDiffRow?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public DiffSide Side
    {
        get => (DiffSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
        => new(BinaryHexDiffRow.BytesPerRow * CellW, CellH);

    protected override Size ArrangeOverride(Size finalSize)
        => new(BinaryHexDiffRow.BytesPerRow * CellW, CellH);

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        var row = Row;
        if (row is null) return;

        var cells = Side == DiffSide.Left ? row.LeftCells : row.RightCells;
        if (cells.Count == 0) return;

        var fgBrush = TryFindResource("DockMenuForegroundBrush") as Brush
                      ?? Brushes.LightGray;

        // Invalidate FormattedText cache when the foreground brush changes.
        if (!ReferenceEquals(fgBrush, _lastFgBrush))
        {
            _ftCache    = null;
            _lastFgBrush = fgBrush;
        }
        _ftCache ??= new Dictionary<string, FormattedText>(capacity: 257);

        var typeface = new Typeface("Consolas");
        double dpi   = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            double x = i * CellW;

            // Background
            var bgBrush = ResolveBgBrush(cell.Kind);
            dc.DrawRectangle(bgBrush, null, new Rect(x, 0, CellW, CellH));

            // Text
            if (cell.HexText.Length > 0 && cell.HexText != "  ")
            {
                if (!_ftCache.TryGetValue(cell.HexText, out var ft))
                {
                    ft = new FormattedText(
                        cell.HexText,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        FontSz,
                        fgBrush,
                        dpi);
                    _ftCache[cell.HexText] = ft;
                }
                dc.DrawText(ft, new Point(x + TextX, TextY));
            }
        }
    }

    // ── Brush resolution ─────────────────────────────────────────────────────

    private Brush ResolveBgBrush(BinaryByteKind kind) => kind switch
    {
        BinaryByteKind.Modified      => TryFindResource("BDiff_ModifiedByteBrush")  as Brush ?? Brushes.Transparent,
        BinaryByteKind.InsertedRight => TryFindResource("BDiff_InsertedByteBrush")  as Brush ?? Brushes.Transparent,
        BinaryByteKind.DeletedLeft   => TryFindResource("BDiff_DeletedByteBrush")   as Brush ?? Brushes.Transparent,
        BinaryByteKind.Padding       => TryFindResource("BDiff_PaddingBrush")       as Brush ?? Brushes.Transparent,
        _                            => Brushes.Transparent
    };
}
