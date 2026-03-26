// Project      : WpfHexEditorControl
// File         : Views/Controls/AsciiRowPresenter.cs
// Description  : Lightweight DrawingContext renderer for the 16-character ASCII column
//                of a binary diff hex row.  Companion to HexRowPresenter.
// Architecture : WPF-only.  One instance per row side; bound via DataTemplate DPs.

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WpfHexEditor.Core.Diff.Models;

namespace WpfHexEditor.Plugins.FileComparison.Views.Controls;

/// <summary>
/// Renders 16 ASCII characters for one side of a <see cref="BinaryHexDiffRow"/>
/// using <see cref="DrawingContext.DrawText"/> — no ItemsControl / containers.
/// </summary>
public sealed class AsciiRowPresenter : FrameworkElement
{
    // ── Cell geometry constants ──────────────────────────────────────────────

    private const double CellW  = 8.0;
    private const double CellH  = 22.0;
    private const double TextY  = 3.0;
    private const double FontSz = 10.0;

    // ── Per-instance FormattedText caches ────────────────────────────────────

    private Dictionary<string, FormattedText>? _printableCache;
    private Dictionary<string, FormattedText>? _dimCache;
    private Brush? _lastPrintableBrush;
    private Brush? _lastDimBrush;

    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(
            nameof(Row), typeof(BinaryHexDiffRow), typeof(AsciiRowPresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SideProperty =
        DependencyProperty.Register(
            nameof(Side), typeof(DiffSide), typeof(AsciiRowPresenter),
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

        var printableBrush = TryFindResource("BDiff_AsciiForegroundBrush")  as Brush ?? Brushes.LightGray;
        var dimBrush       = TryFindResource("BDiff_AsciiNonPrintableBrush") as Brush ?? Brushes.DimGray;

        // Invalidate caches when brushes change (theme switch).
        if (!ReferenceEquals(printableBrush, _lastPrintableBrush)
         || !ReferenceEquals(dimBrush, _lastDimBrush))
        {
            _printableCache   = null;
            _dimCache         = null;
            _lastPrintableBrush = printableBrush;
            _lastDimBrush       = dimBrush;
        }
        _printableCache ??= new Dictionary<string, FormattedText>(capacity: 96);
        _dimCache       ??= new Dictionary<string, FormattedText>(capacity: 2);

        var typeface = new Typeface("Consolas");
        double dpi   = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell.Kind == BinaryByteKind.Padding) continue;

            var cache  = cell.IsPrintable ? _printableCache : _dimCache;
            var brush  = cell.IsPrintable ? printableBrush  : dimBrush;
            var ch     = cell.AsciiChar;

            if (!cache.TryGetValue(ch, out var ft))
            {
                ft = new FormattedText(
                    ch,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    FontSz,
                    brush,
                    dpi);
                cache[ch] = ft;
            }

            dc.DrawText(ft, new Point(i * CellW, TextY));
        }
    }
}
