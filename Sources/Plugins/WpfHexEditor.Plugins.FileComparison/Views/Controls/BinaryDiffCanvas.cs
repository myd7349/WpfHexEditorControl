// Project      : WpfHexEditorControl
// File         : Views/Controls/BinaryDiffCanvas.cs
// Description  : Single-canvas virtualised renderer for the binary hex diff view.
//                Replaces VirtualizingStackPanel + per-row FrameworkElements with one
//                FrameworkElement that paints only the visible rows via DrawingContext.
//                Implements IScrollInfo so the parent ScrollViewer routes all scroll
//                events through this control — zero WPF containers per row.
// Architecture : WPF-only, IScrollInfo pattern identical to HexViewport / TextViewport.
//                FrameworkElement.OnRender is the single draw call per frame.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Core.Diff.Models;

namespace WpfHexEditor.Plugins.FileComparison.Views.Controls;

/// <summary>
/// A single <see cref="FrameworkElement"/> that renders all visible rows of a binary
/// hex diff using <see cref="DrawingContext"/> calls — no per-row WPF containers.
/// Implements <see cref="IScrollInfo"/> so the parent <see cref="ScrollViewer"/>
/// (with <c>CanContentScroll="True"</c>) routes scroll events to this control.
/// </summary>
public sealed class BinaryDiffCanvas : FrameworkElement, IScrollInfo
{
    // ── Base geometry constants (at ZoomLevel 1.0) ───────────────────────────

    private const double BaseRowH     = 22.0;
    private const double BaseCellW    = 20.0;   // hex cell
    private const double BaseAsciiW   = 8.0;    // ascii cell
    private const double BaseOffsetW  = 72.0;   // "00000000  " column
    private const double BaseSepW     = 14.0;   // centre separator
    private const double BaseFontSz   = 10.0;

    // ── Scroll state ─────────────────────────────────────────────────────────

    private double _verticalOffset;
    private double _horizontalOffset;

    // ── Brush / typeface cache ────────────────────────────────────────────────

    private Brush? _bgBrush;
    private Brush? _offsetBgBrush;
    private Brush? _offsetFgBrush;
    private Brush? _hexFg1;          // HexEditor_ForegroundFirstColor  (even bytes)
    private Brush? _hexFg2;          // HexEditor_ForegroundSecondColor (odd bytes)
    private Brush? _asciiFgBrush;
    private Brush? _separatorBrush;
    private Brush? _modBrush;
    private Brush? _insBrush;
    private Brush? _delBrush;
    private Brush? _padBrush;
    private Brush? _collBgBrush;
    private Brush? _collFgBrush;
    private bool   _brushesDirty = true;

    private Typeface? _typeface;
    private double    _dpi;
    private double    _lastDpi;

    // Per-brush FormattedText caches — keyed by the display string.
    // Separate dictionaries for the two alternating hex foreground brushes.
    private readonly Dictionary<string, FormattedText> _hexFt1Cache   = new(capacity: 257);
    private readonly Dictionary<string, FormattedText> _hexFt2Cache   = new(capacity: 257);
    private readonly Dictionary<string, FormattedText> _asciiFtCache  = new(capacity: 100);
    private readonly Dictionary<string, FormattedText> _offsetFtCache = new(capacity: 32);
    private readonly Dictionary<int,    FormattedText> _collFtCache   = new(capacity: 8);

    // ── Render-time measurement ────────────────────────────────────────────────

    private readonly Stopwatch _renderStopwatch = new();

    /// <summary>
    /// Raised at the end of each <see cref="OnRender"/> call with the elapsed milliseconds.
    /// Wire to a <see cref="WpfHexEditor.Editor.Core.StatusBarItem"/> to surface the metric.
    /// </summary>
    internal event EventHandler<long>? RefreshTimeUpdated;

    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty RowsProperty =
        DependencyProperty.Register(
            nameof(Rows),
            typeof(IReadOnlyList<BinaryHexDiffRow>),
            typeof(BinaryDiffCanvas),
            new FrameworkPropertyMetadata(null, OnRowsChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(BinaryDiffCanvas),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((BinaryDiffCanvas)d).OnZoomChanged()),
            v => v is double z && z >= 0.5 && z <= 4.0);

    public IReadOnlyList<BinaryHexDiffRow>? Rows
    {
        get => (IReadOnlyList<BinaryHexDiffRow>?)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>Raised when <see cref="ZoomLevel"/> changes (mirrors HexViewport pattern).</summary>
    public event Action<BinaryDiffCanvas, double>? ZoomLevelChanged;

    // ── Effective (zoom-scaled) geometry ─────────────────────────────────────

    public  double EffectiveRowH   => BaseRowH    * ZoomLevel;
    private double EffectiveCellW  => BaseCellW   * ZoomLevel;
    private double EffectiveAsciiW => BaseAsciiW  * ZoomLevel;
    private double EffectiveOffsetW=> BaseOffsetW * ZoomLevel;
    private double EffectiveSepW   => BaseSepW    * ZoomLevel;
    private double EffectiveFontSz => BaseFontSz  * ZoomLevel;

    private double TotalContentWidth
        => EffectiveOffsetW + BinaryHexDiffRow.BytesPerRow * EffectiveCellW
         + BinaryHexDiffRow.BytesPerRow * EffectiveAsciiW
         + EffectiveSepW
         + EffectiveOffsetW + BinaryHexDiffRow.BytesPerRow * EffectiveCellW
         + BinaryHexDiffRow.BytesPerRow * EffectiveAsciiW;

    // ── IScrollInfo ───────────────────────────────────────────────────────────

    public ScrollViewer? ScrollOwner           { get; set; }
    public bool          CanHorizontallyScroll { get; set; }
    public bool          CanVerticallyScroll   { get; set; }

    public double ExtentHeight   => (Rows?.Count ?? 0) * EffectiveRowH;
    public double ExtentWidth    => TotalContentWidth;
    public double ViewportHeight => ActualHeight;
    public double ViewportWidth  => ActualWidth;
    public double VerticalOffset   => _verticalOffset;
    public double HorizontalOffset => _horizontalOffset;

    public void SetVerticalOffset(double offset)
    {
        var clamped = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentHeight - ViewportHeight)));
        if (Math.Abs(clamped - _verticalOffset) < 0.001) return;
        _verticalOffset = clamped;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    public void SetHorizontalOffset(double offset)
    {
        var clamped = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentWidth - ViewportWidth)));
        if (Math.Abs(clamped - _horizontalOffset) < 0.001) return;
        _horizontalOffset = clamped;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    public void LineUp()          => SetVerticalOffset(_verticalOffset - EffectiveRowH);
    public void LineDown()        => SetVerticalOffset(_verticalOffset + EffectiveRowH);
    public void LineLeft()        => SetHorizontalOffset(_horizontalOffset - EffectiveCellW);
    public void LineRight()       => SetHorizontalOffset(_horizontalOffset + EffectiveCellW);
    public void PageUp()          => SetVerticalOffset(_verticalOffset - ViewportHeight);
    public void PageDown()        => SetVerticalOffset(_verticalOffset + ViewportHeight);
    public void PageLeft()        => SetHorizontalOffset(_horizontalOffset - ViewportWidth);
    public void PageRight()       => SetHorizontalOffset(_horizontalOffset + ViewportWidth);
    public void MouseWheelUp()    => SetVerticalOffset(_verticalOffset - 3 * EffectiveRowH);
    public void MouseWheelDown()  => SetVerticalOffset(_verticalOffset + 3 * EffectiveRowH);
    public void MouseWheelLeft()  => SetHorizontalOffset(_horizontalOffset - EffectiveCellW * 3);
    public void MouseWheelRight() => SetHorizontalOffset(_horizontalOffset + EffectiveCellW * 3);

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        // Translate the rectangle into this coordinate space
        if (visual != this)
        {
            var transform = visual.TransformToAncestor(this);
            rectangle     = transform.TransformBounds(rectangle);
        }

        var newV = _verticalOffset;
        if (rectangle.Top < _verticalOffset)
            newV = rectangle.Top;
        else if (rectangle.Bottom > _verticalOffset + ViewportHeight)
            newV = rectangle.Bottom - ViewportHeight;

        var newH = _horizontalOffset;
        if (rectangle.Left < _horizontalOffset)
            newH = rectangle.Left;
        else if (rectangle.Right > _horizontalOffset + ViewportWidth)
            newH = rectangle.Right - ViewportWidth;

        SetVerticalOffset(newV);
        SetHorizontalOffset(newH);
        return rectangle;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width)  ? TotalContentWidth : availableSize.Width;
        var h = double.IsInfinity(availableSize.Height) ? ExtentHeight      : availableSize.Height;
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ScrollOwner?.InvalidateScrollInfo();
        return finalSize;
    }

    // ── Zoom ─────────────────────────────────────────────────────────────────

    private void OnZoomChanged()
    {
        ClearFormattedTextCaches();
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
        ZoomLevelChanged?.Invoke(this, ZoomLevel);
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var delta = e.Delta > 0 ? 0.1 : -0.1;
            var next  = Math.Clamp(Math.Round(ZoomLevel + delta, 1), 0.5, 4.0);
            if (Math.Abs(next - ZoomLevel) > 0.001)
                ZoomLevel = next;
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseWheel(e);
    }

    // ── Rows change ──────────────────────────────────────────────────────────

    private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (BinaryDiffCanvas)d;

        if (e.OldValue is INotifyCollectionChanged oldColl)
            oldColl.CollectionChanged -= canvas.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += canvas.OnCollectionChanged;

        canvas._verticalOffset = 0;
        canvas.ScrollOwner?.InvalidateScrollInfo();
        canvas.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        _renderStopwatch.Restart();

        var rows = Rows;
        if (rows is null || rows.Count == 0)
        {
            dc.DrawRectangle(_bgBrush ?? Brushes.Transparent, null,
                new Rect(0, 0, ActualWidth, ActualHeight));
            _renderStopwatch.Stop();
            RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
            return;
        }

        EnsureBrushes();
        EnsureTypeface();

        var rowH       = EffectiveRowH;
        var firstRow   = Math.Max(0, (int)(_verticalOffset / rowH));
        var visible    = (int)Math.Ceiling(ActualHeight / rowH) + 1;
        var lastRow    = Math.Min(rows.Count - 1, firstRow + visible);

        // Clip + translate for horizontal offset
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        dc.PushTransform(new TranslateTransform(-_horizontalOffset, 0));

        // Full background
        dc.DrawRectangle(_bgBrush, null,
            new Rect(_horizontalOffset, 0, Math.Max(ActualWidth, TotalContentWidth), ActualHeight));

        for (int i = firstRow; i <= lastRow; i++)
        {
            // fractional-pixel offset so first row can be partially scrolled
            double y = (i - firstRow) * rowH - (_verticalOffset - firstRow * rowH);
            DrawRow(dc, rows[i], y);
        }

        dc.Pop(); // translate
        dc.Pop(); // clip

        _renderStopwatch.Stop();
        RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
    }

    private void DrawRow(DrawingContext dc, BinaryHexDiffRow row, double y)
    {
        if (row.IsCollapsedContext)
        {
            DrawCollapsedBanner(dc, row, y);
            return;
        }

        var rowH     = EffectiveRowH;
        var cellW    = EffectiveCellW;
        var asciiW   = EffectiveAsciiW;
        var offsetW  = EffectiveOffsetW;
        var sepW     = EffectiveSepW;
        var n        = BinaryHexDiffRow.BytesPerRow;

        // ── Left side ────────────────────────────────────────────────────────
        DrawOffset(dc, row.LeftOffsetText, 0, y);
        DrawHexCells(dc, row.LeftCells, offsetW, y);
        DrawAsciiCells(dc, row.LeftCells, offsetW + n * cellW, y);

        // ── Separator ────────────────────────────────────────────────────────
        double sepX = offsetW + n * cellW + n * asciiW;
        dc.DrawRectangle(_separatorBrush, null,
            new Rect(sepX + sepW / 2 - 1, y + 2, 2, rowH - 4));

        // ── Right side ───────────────────────────────────────────────────────
        double rx = sepX + sepW;
        DrawOffset(dc, row.RightOffsetText, rx, y);
        DrawHexCells(dc, row.RightCells, rx + offsetW, y);
        DrawAsciiCells(dc, row.RightCells, rx + offsetW + n * cellW, y);
    }

    private void DrawOffset(DrawingContext dc, string text, double x, double y)
    {
        var offsetW = EffectiveOffsetW;
        var rowH    = EffectiveRowH;
        dc.DrawRectangle(_offsetBgBrush, null, new Rect(x, y, offsetW, rowH));

        if (text.Length == 0 || text.Trim().Length == 0) return;
        var ft = GetOrCreateFt(_offsetFtCache, text, _offsetFgBrush!);
        dc.DrawText(ft, new Point(x + 2, y + rowH / 2 - ft.Height / 2));
    }

    private void DrawHexCells(DrawingContext dc,
        IReadOnlyList<BinaryHexByteCell> cells, double x, double y)
    {
        var cellW = EffectiveCellW;
        var rowH  = EffectiveRowH;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            double cx = x + i * cellW;

            // Background
            var bg = ResolveCellBg(cell.Kind);
            dc.DrawRectangle(bg, null, new Rect(cx, y, cellW, rowH));

            // Text (skip blank padding)
            if (cell.HexText.Length > 0 && cell.HexText != "  ")
            {
                var cache = i % 2 == 0 ? _hexFt1Cache : _hexFt2Cache;
                var fg    = i % 2 == 0 ? _hexFg1!     : _hexFg2!;
                var ft    = GetOrCreateFt(cache, cell.HexText, fg);
                dc.DrawText(ft, new Point(cx + 2, y + rowH / 2 - ft.Height / 2));
            }
        }
    }

    private void DrawAsciiCells(DrawingContext dc,
        IReadOnlyList<BinaryHexByteCell> cells, double x, double y)
    {
        var asciiW = EffectiveAsciiW;
        var rowH   = EffectiveRowH;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell.Kind == BinaryByteKind.Padding) continue;

            double cx  = x + i * asciiW;
            var    ft  = GetOrCreateFt(_asciiFtCache, cell.AsciiChar, _asciiFgBrush!);
            dc.DrawText(ft, new Point(cx, y + rowH / 2 - ft.Height / 2));
        }
    }

    private void DrawCollapsedBanner(DrawingContext dc, BinaryHexDiffRow row, double y)
    {
        var rowH   = EffectiveRowH;
        var totalW = TotalContentWidth;

        dc.DrawRectangle(_collBgBrush, null, new Rect(0, y, totalW, rowH));

        if (!_collFtCache.TryGetValue(row.CollapsedRowCount, out var ft))
        {
            ft = CreateFt($"··· {row.CollapsedRowCount} identical rows ···",
                _collFgBrush ?? Brushes.Gray);
            _collFtCache[row.CollapsedRowCount] = ft;
        }
        dc.DrawText(ft, new Point(totalW / 2 - ft.Width / 2, y + rowH / 2 - ft.Height / 2));
    }

    // ── FormattedText helpers ─────────────────────────────────────────────────

    private FormattedText GetOrCreateFt(Dictionary<string, FormattedText> cache,
        string text, Brush fg)
    {
        if (!cache.TryGetValue(text, out var ft))
        {
            ft = CreateFt(text, fg);
            cache[text] = ft;
        }
        return ft;
    }

    private FormattedText CreateFt(string text, Brush fg)
        => new(text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            _typeface!,
            EffectiveFontSz,
            fg,
            _dpi);

    private void ClearFormattedTextCaches()
    {
        _hexFt1Cache.Clear();
        _hexFt2Cache.Clear();
        _asciiFtCache.Clear();
        _offsetFtCache.Clear();
        _collFtCache.Clear();
    }

    // ── Brush resolution ─────────────────────────────────────────────────────

    private Brush ResolveCellBg(BinaryByteKind kind) => kind switch
    {
        BinaryByteKind.Modified      => _modBrush ?? Brushes.Transparent,
        BinaryByteKind.InsertedRight => _insBrush ?? Brushes.Transparent,
        BinaryByteKind.DeletedLeft   => _delBrush ?? Brushes.Transparent,
        BinaryByteKind.Padding       => _padBrush ?? Brushes.Transparent,
        _                            => Brushes.Transparent
    };

    private void EnsureBrushes()
    {
        if (!_brushesDirty) return;

        _bgBrush       = ColorToBrush("HexEditor_BackgroundColor")           ?? Brushes.Black;
        _offsetBgBrush = ColorToBrush("HexEditor_HeaderBackgroundColor")     ?? Brushes.DimGray;
        _offsetFgBrush = ColorToBrush("HexEditor_ForegroundOffSetHeaderColor") ?? Brushes.LightGray;
        _hexFg1        = ColorToBrush("HexEditor_ForegroundFirstColor")      ?? Brushes.White;
        _hexFg2        = ColorToBrush("HexEditor_ForegroundSecondColor")     ?? Brushes.LightGray;
        _asciiFgBrush  = ColorToBrush("HexEditor_AsciiForegroundColor")      ?? Brushes.LightGray;
        _separatorBrush= ColorToBrush("HexEditor_ColumnSeparatorColor")      ?? Brushes.Gray;

        _modBrush  = TryFindResource("BDiff_ModifiedByteBrush")          as Brush;
        _insBrush  = TryFindResource("BDiff_InsertedByteBrush")          as Brush;
        _delBrush  = TryFindResource("BDiff_DeletedByteBrush")           as Brush;
        _padBrush  = TryFindResource("BDiff_PaddingBrush")               as Brush;
        _collBgBrush= TryFindResource("BDiff_CollapsedContextBrush")     as Brush
                     ?? new SolidColorBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
        _collFgBrush= TryFindResource("BDiff_CollapsedContextFgBrush")   as Brush
                     ?? Brushes.Silver;

        _brushesDirty = false;

        // Brush change invalidates all FormattedText (they bake the brush)
        ClearFormattedTextCaches();
    }

    private Brush? ColorToBrush(string key)
    {
        if (TryFindResource(key) is Color c)
            return new SolidColorBrush(c);
        return null;
    }

    private void EnsureTypeface()
    {
        _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Rebuild typeface + caches if DPI changes (e.g. monitor change)
        if (_typeface is null || Math.Abs(_dpi - _lastDpi) > 0.001)
        {
            _typeface = new Typeface("Consolas");
            _lastDpi  = _dpi;
            ClearFormattedTextCaches();
        }
    }

    // ── Theme change detection ────────────────────────────────────────────────

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        // Invalidate brush cache when the element is added to a new logical tree
        // (resources may differ between trees) or when Background changes.
        // Nothing extra needed — brush invalidation is handled in OnVisualParentChanged
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        _brushesDirty = true;
        InvalidateVisual();
    }

    // ── Resource change notification ─────────────────────────────────────────

    /// <summary>
    /// Call this when the application theme changes so brush and FormattedText caches
    /// are invalidated before the next render.
    /// </summary>
    public void InvalidateBrushCache()
    {
        _brushesDirty = true;
        InvalidateVisual();
    }
}
