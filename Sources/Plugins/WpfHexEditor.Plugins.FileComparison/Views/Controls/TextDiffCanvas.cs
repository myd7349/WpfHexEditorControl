// Project      : WpfHexEditor.Plugins.FileComparison
// File         : Views/Controls/TextDiffCanvas.cs
// Description  : Single-canvas virtualised renderer for the side-by-side text diff view.
//                Replaces 2 ItemsControls + VirtualizingStackPanel + nested ItemsControl
//                per row with one FrameworkElement using DrawingContext + GlyphRun.
//                Implements IScrollInfo — zero WPF containers per row.
// Architecture : WPF-only, IScrollInfo pattern identical to BinaryDiffCanvas.
//                FrameworkElement.OnRender is the single draw call per frame.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Plugins.FileComparison.ViewModels;

namespace WpfHexEditor.Plugins.FileComparison.Views.Controls;

/// <summary>
/// A single <see cref="FrameworkElement"/> that renders both left and right text diff panes
/// side-by-side using <see cref="DrawingContext"/> + <see cref="GlyphRun"/> calls.
/// Implements <see cref="IScrollInfo"/> for virtualised scrolling.
/// </summary>
public sealed class TextDiffCanvas : FrameworkElement, IScrollInfo
{
    // ── Base geometry (computed from font metrics, scaled by ZoomLevel) ─────

    private const double SeparatorW = 4.0;
    private const double ContentPad = 4.0;

    private double _baseRowH    = 18.0;
    private double _baseGutterW = 40.0;  // charWidth × 5
    private double _charWidth   = 8.0;
    private double _charHeight  = 14.0;
    private double _baseline    = 11.0;

    // ── Scroll state ─────────────────────────────────────────────────────────

    private double _verticalOffset;
    private double _horizontalOffset;

    // ── Typeface / GlyphRun state ────────────────────────────────────────────

    private Typeface?      _typeface;
    private GlyphTypeface? _glyphTypeface;
    private double         _dpi;
    private double         _lastDpi;

    // ── Brush cache ──────────────────────────────────────────────────────────

    private Brush? _bgBrush;
    private Brush? _gutterBgBrush;
    private Brush? _gutterFgBrush;
    private Brush? _textFgBrush;
    private Brush? _modLineBrush;
    private Brush? _insLineBrush;
    private Brush? _delLineBrush;
    private Brush? _wordModBrush;
    private Brush? _separatorBrush;
    private Brush? _hoverBrush;
    private Brush? _hoverSegBrush;
    private bool   _brushesDirty = true;

    // ── FormattedText cache (line numbers only — GlyphRun handles content) ──

    private readonly Dictionary<int, FormattedText> _gutterFtCache = new(128);

    // ── Hover state ──────────────────────────────────────────────────────────

    private int  _hoverRowIndex = -1;
    private bool _hoverIsLeftPane;

    /// <summary>Raised when the hovered row changes. For status bar updates.</summary>
    public event EventHandler<(int RowIndex, bool IsLeft)>? HoverRowChanged;

    // ── Render-time measurement ──────────────────────────────────────────────

    private readonly Stopwatch _renderStopwatch = new();
    private RectangleGeometry? _clipGeo;

    internal event EventHandler<long>? RefreshTimeUpdated;

    // ── Max line length (for horizontal extent) ─────────────────────────────

    private int _maxLineLength;

    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty LeftRowsProperty =
        DependencyProperty.Register(nameof(LeftRows),
            typeof(IReadOnlyList<DiffLineRow>), typeof(TextDiffCanvas),
            new FrameworkPropertyMetadata(null, OnRowsChanged));

    public static readonly DependencyProperty RightRowsProperty =
        DependencyProperty.Register(nameof(RightRows),
            typeof(IReadOnlyList<DiffLineRow>), typeof(TextDiffCanvas),
            new FrameworkPropertyMetadata(null, OnRowsChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel),
            typeof(double), typeof(TextDiffCanvas),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((TextDiffCanvas)d).OnZoomChanged()),
            v => v is double z && z >= 0.5 && z <= 4.0);

    public static readonly DependencyProperty HexFontFamilyProperty =
        DependencyProperty.Register(nameof(HexFontFamily),
            typeof(FontFamily), typeof(TextDiffCanvas),
            new FrameworkPropertyMetadata(new FontFamily("Consolas"),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                (d, _) => ((TextDiffCanvas)d).OnFontChanged()));

    public static readonly DependencyProperty HexFontSizeProperty =
        DependencyProperty.Register(nameof(HexFontSize),
            typeof(double), typeof(TextDiffCanvas),
            new FrameworkPropertyMetadata(13.0,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                (d, _) => ((TextDiffCanvas)d).OnFontChanged()));

    public IReadOnlyList<DiffLineRow>? LeftRows
    {
        get => (IReadOnlyList<DiffLineRow>?)GetValue(LeftRowsProperty);
        set => SetValue(LeftRowsProperty, value);
    }

    public IReadOnlyList<DiffLineRow>? RightRows
    {
        get => (IReadOnlyList<DiffLineRow>?)GetValue(RightRowsProperty);
        set => SetValue(RightRowsProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public FontFamily HexFontFamily
    {
        get => (FontFamily)GetValue(HexFontFamilyProperty);
        set => SetValue(HexFontFamilyProperty, value);
    }

    public double HexFontSize
    {
        get => (double)GetValue(HexFontSizeProperty);
        set => SetValue(HexFontSizeProperty, value);
    }

    public event Action<TextDiffCanvas, double>? ZoomLevelChanged;

    // ── Effective (zoom-scaled) geometry ──────────────────────────────────────

    public  double EffectiveRowH    => _baseRowH    * ZoomLevel;
    private double EffectiveGutterW => _baseGutterW * ZoomLevel;
    private double EffectiveCharW   => _charWidth   * ZoomLevel;
    private double EffectiveFontSz  => HexFontSize  * ZoomLevel;

    private double LeftPaneW  => Math.Max(100, (ActualWidth - SeparatorW) / 2);
    private double RightPaneX => LeftPaneW + SeparatorW;

    private double TotalContentWidth
    {
        get
        {
            var paneContentW = EffectiveGutterW + ContentPad + _maxLineLength * EffectiveCharW + ContentPad;
            return paneContentW * 2 + SeparatorW;
        }
    }

    // ── IScrollInfo ──────────────────────────────────────────────────────────

    public ScrollViewer? ScrollOwner           { get; set; }
    public bool          CanHorizontallyScroll { get; set; }
    public bool          CanVerticallyScroll   { get; set; }

    public double ExtentHeight => Math.Max(LeftRows?.Count ?? 0, RightRows?.Count ?? 0) * EffectiveRowH;
    public double ExtentWidth  => TotalContentWidth;
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
    public void LineLeft()        => SetHorizontalOffset(_horizontalOffset - EffectiveCharW * 4);
    public void LineRight()       => SetHorizontalOffset(_horizontalOffset + EffectiveCharW * 4);
    public void PageUp()          => SetVerticalOffset(_verticalOffset - ViewportHeight);
    public void PageDown()        => SetVerticalOffset(_verticalOffset + ViewportHeight);
    public void PageLeft()        => SetHorizontalOffset(_horizontalOffset - ViewportWidth);
    public void PageRight()       => SetHorizontalOffset(_horizontalOffset + ViewportWidth);
    public void MouseWheelUp()    => SetVerticalOffset(_verticalOffset - 3 * EffectiveRowH);
    public void MouseWheelDown()  => SetVerticalOffset(_verticalOffset + 3 * EffectiveRowH);
    public void MouseWheelLeft()  => SetHorizontalOffset(_horizontalOffset - EffectiveCharW * 8);
    public void MouseWheelRight() => SetHorizontalOffset(_horizontalOffset + EffectiveCharW * 8);

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        if (visual != this)
        {
            var transform = visual.TransformToAncestor(this);
            rectangle = transform.TransformBounds(rectangle);
        }
        var newV = _verticalOffset;
        if (rectangle.Top < _verticalOffset) newV = rectangle.Top;
        else if (rectangle.Bottom > _verticalOffset + ViewportHeight) newV = rectangle.Bottom - ViewportHeight;
        var newH = _horizontalOffset;
        if (rectangle.Left < _horizontalOffset) newH = rectangle.Left;
        else if (rectangle.Right > _horizontalOffset + ViewportWidth) newH = rectangle.Right - ViewportWidth;
        SetVerticalOffset(newV);
        SetHorizontalOffset(newH);
        return rectangle;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size available)
    {
        var w = double.IsInfinity(available.Width)  ? TotalContentWidth : available.Width;
        var h = double.IsInfinity(available.Height) ? ExtentHeight      : available.Height;
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ScrollOwner?.InvalidateScrollInfo();
        return finalSize;
    }

    // ── Zoom ──────────────────────────────────────────────────────────────────

    private void OnZoomChanged()
    {
        ClearCaches();
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
            if (Math.Abs(next - ZoomLevel) > 0.001) ZoomLevel = next;
            e.Handled = true;
            return;
        }
        base.OnPreviewMouseWheel(e);
    }

    // ── Font change ───────────────────────────────────────────────────────────

    private void OnFontChanged()
    {
        RecalculateMetrics();
        _typeface = null;
        _glyphTypeface = null;
        ClearCaches();
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
    }

    private void RecalculateMetrics()
    {
        var dpi = _dpi > 0 ? _dpi : 96.0;
        var tf  = new Typeface(HexFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var (cw, ch, bl) = DiffGlyphHelper.MeasureCharMetrics(tf, HexFontSize, dpi);
        _charWidth   = cw;
        _charHeight  = ch;
        _baseline    = bl;
        _baseRowH    = Math.Ceiling(ch) + 4;
        _baseGutterW = Math.Ceiling(cw * 5); // 5 chars for line numbers
    }

    // ── Rows change ───────────────────────────────────────────────────────────

    private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (TextDiffCanvas)d;
        if (e.OldValue is INotifyCollectionChanged oldColl)
            oldColl.CollectionChanged -= canvas.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += canvas.OnCollectionChanged;

        canvas._verticalOffset = 0;
        canvas.ComputeMaxLineLength();
        canvas.ScrollOwner?.InvalidateScrollInfo();
        canvas.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ComputeMaxLineLength();
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    private void ComputeMaxLineLength()
    {
        int max = 0;
        if (LeftRows is { } left)
            foreach (var r in left) max = Math.Max(max, r.Content.Length);
        if (RightRows is { } right)
            foreach (var r in right) max = Math.Max(max, r.Content.Length);
        _maxLineLength = Math.Max(max, 80);
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        _renderStopwatch.Restart();

        var leftRows  = LeftRows;
        var rightRows = RightRows;
        int rowCount  = Math.Max(leftRows?.Count ?? 0, rightRows?.Count ?? 0);

        EnsureBrushes();
        EnsureTypeface();

        // Background
        dc.DrawRectangle(_bgBrush ?? Brushes.Transparent, null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        if (rowCount == 0)
        {
            _renderStopwatch.Stop();
            RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
            return;
        }

        var rowH     = EffectiveRowH;
        var firstRow = Math.Max(0, (int)(_verticalOffset / rowH));
        var visible  = (int)Math.Ceiling(ActualHeight / rowH) + 1;
        var lastRow  = Math.Min(rowCount - 1, firstRow + visible);

        if (_clipGeo is null || _clipGeo.Rect.Width != ActualWidth || _clipGeo.Rect.Height != ActualHeight)
        {
            _clipGeo = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            _clipGeo.Freeze();
        }
        dc.PushClip(_clipGeo);

        for (int i = firstRow; i <= lastRow; i++)
        {
            double y = (i - firstRow) * rowH - (_verticalOffset - firstRow * rowH);
            var leftRow  = leftRows  is not null && i < leftRows.Count  ? leftRows[i]  : null;
            var rightRow = rightRows is not null && i < rightRows.Count ? rightRows[i] : null;
            DrawTextRow(dc, leftRow, rightRow, y, i);
        }

        // Center separator
        double sepX = LeftPaneW;
        dc.DrawRectangle(_separatorBrush, null,
            new Rect(sepX, 0, SeparatorW, ActualHeight));

        dc.Pop(); // clip

        _renderStopwatch.Stop();
        RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
    }

    private void DrawTextRow(DrawingContext dc, DiffLineRow? leftRow, DiffLineRow? rightRow,
        double y, int rowIndex)
    {
        var rowH    = EffectiveRowH;
        var gutterW = EffectiveGutterW;
        var fontSz  = EffectiveFontSz;
        var charW   = EffectiveCharW;
        var bl      = _baseline * ZoomLevel;
        var baseY   = y + bl + 2; // +2 for top padding

        // ── Left pane ────────────────────────────────────────────────────
        DrawPane(dc, leftRow, 0, y, rowH, gutterW, fontSz, charW, baseY, rowIndex, true);

        // ── Right pane ───────────────────────────────────────────────────
        DrawPane(dc, rightRow, RightPaneX, y, rowH, gutterW, fontSz, charW, baseY, rowIndex, false);
    }

    private void DrawPane(DrawingContext dc, DiffLineRow? row,
        double paneX, double y, double rowH, double gutterW,
        double fontSz, double charW, double baseY,
        int rowIndex, bool isLeft)
    {
        double paneW = LeftPaneW;
        double contentX = paneX + gutterW + ContentPad;

        // Gutter background
        dc.DrawRectangle(_gutterBgBrush, null, new Rect(paneX, y, gutterW, rowH));

        if (row is null) return;

        // Row diff background
        var rowBg = ResolveKindBrush(row.Kind);
        if (rowBg != null)
            dc.DrawRectangle(rowBg, null, new Rect(paneX + gutterW, y, paneW - gutterW, rowH));

        // Hover highlight
        if (_hoverRowIndex == rowIndex && _hoverIsLeftPane == isLeft && _hoverBrush is not null)
            dc.DrawRectangle(_hoverBrush, null, new Rect(paneX, y, paneW, rowH));

        // Line number
        if (row.LineNumber.HasValue)
        {
            var numFt = GetGutterFt(row.LineNumber.Value);
            dc.DrawText(numFt, new Point(paneX + gutterW - numFt.Width - 4, y + rowH / 2 - numFt.Height / 2));
        }

        // Clip content to pane width
        dc.PushClip(new RectangleGeometry(new Rect(contentX - ContentPad, y, paneW - gutterW, rowH)));

        // Word segments (GlyphRun rendering)
        double cx = contentX - _horizontalOffset;
        foreach (var seg in row.Segments)
        {
            double segW = seg.Text.Length * charW;

            // Word-level modified highlight
            if (seg.IsChanged && _wordModBrush is not null)
                dc.DrawRectangle(_wordModBrush, null, new Rect(cx, y, segW, rowH));

            // Render text
            DiffGlyphHelper.RenderText(dc, seg.Text, cx, baseY,
                _glyphTypeface, _typeface!, fontSz, (float)_dpi,
                _textFgBrush!, y);

            cx += segW;
        }

        dc.Pop(); // clip
    }

    private Brush? ResolveKindBrush(string kind) => kind switch
    {
        "Modified"      => _modLineBrush,
        "InsertedRight" => _insLineBrush,
        "DeletedLeft"   => _delLineBrush,
        _               => null
    };

    private FormattedText GetGutterFt(int lineNumber)
    {
        if (!_gutterFtCache.TryGetValue(lineNumber, out var ft))
        {
            ft = DiffGlyphHelper.MakeFormattedText(
                lineNumber.ToString(), _typeface!, EffectiveFontSz,
                _gutterFgBrush!, _dpi);
            _gutterFtCache[lineNumber] = ft;
        }
        return ft;
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);

        int rowIdx = ResolveRowIndex(pos.Y);
        bool isLeft = pos.X < LeftPaneW;

        if (rowIdx != _hoverRowIndex || isLeft != _hoverIsLeftPane)
        {
            _hoverRowIndex   = rowIdx;
            _hoverIsLeftPane = isLeft;
            InvalidateVisual();
            HoverRowChanged?.Invoke(this, (rowIdx, isLeft));
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverRowIndex >= 0)
        {
            _hoverRowIndex = -1;
            InvalidateVisual();
            HoverRowChanged?.Invoke(this, (-1, false));
        }
    }

    private int ResolveRowIndex(double y)
    {
        int rowCount = Math.Max(LeftRows?.Count ?? 0, RightRows?.Count ?? 0);
        int idx = (int)((_verticalOffset + y) / EffectiveRowH);
        return idx >= 0 && idx < rowCount ? idx : -1;
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    /// <summary>Raised when the user requests navigation to a HexEditor offset.</summary>
    public event EventHandler<long>? NavigateToOffsetRequested;

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        var pos = e.GetPosition(this);
        int rowIdx = ResolveRowIndex(pos.Y);
        bool isLeft = pos.X < LeftPaneW;

        var menu = new ContextMenu();

        // Copy Line
        if (rowIdx >= 0)
        {
            var rows = isLeft ? LeftRows : RightRows;
            if (rows is not null && rowIdx < rows.Count)
            {
                var row = rows[rowIdx];
                menu.Items.Add(MakeMenuItem($"Copy Line {row.LineNumber}", () =>
                    Clipboard.SetText(row.Content), "\uE8C8"));
            }
        }

        // Copy Left / Right file
        if (LeftRows is { Count: > 0 })
            menu.Items.Add(MakeMenuItem("Copy Left File", () =>
                Clipboard.SetText(string.Join("\n", LeftRows.Select(r => r.Content))), "\uE8C8"));
        if (RightRows is { Count: > 0 })
            menu.Items.Add(MakeMenuItem("Copy Right File", () =>
                Clipboard.SetText(string.Join("\n", RightRows.Select(r => r.Content))), "\uE8C8"));

        if (menu.Items.Count > 0)
        {
            menu.IsOpen = true;
            e.Handled = true;
        }
    }

    private static MenuItem MakeMenuItem(string header, Action action, string? iconGlyph = null)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        if (iconGlyph is not null)
            item.Icon = new System.Windows.Controls.TextBlock
            {
                Text = iconGlyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        return item;
    }

    private static SolidColorBrush FreezeBrush(SolidColorBrush b) { b.Freeze(); return b; }

    // ── Brush resolution ──────────────────────────────────────────────────────

    private void EnsureBrushes()
    {
        if (!_brushesDirty) return;

        _bgBrush        = TryFindResource("DockTabBackgroundBrush")       as Brush ?? Brushes.Black;
        _gutterBgBrush  = TryFindResource("DF_GutterBackgroundBrush")     as Brush ?? Brushes.DimGray;
        _gutterFgBrush  = TryFindResource("DF_LineNumberForegroundBrush") as Brush ?? Brushes.Gray;
        _textFgBrush    = TryFindResource("DockMenuForegroundBrush")      as Brush ?? Brushes.White;
        _modLineBrush   = TryFindResource("DF_ModifiedLineBrush")         as Brush;
        _insLineBrush   = TryFindResource("DF_InsertedLineBrush")         as Brush;
        _delLineBrush   = TryFindResource("DF_DeletedLineBrush")          as Brush;
        _wordModBrush   = TryFindResource("DF_WordModifiedBrush")         as Brush;
        _separatorBrush = TryFindResource("DockSplitterBrush")            as Brush ?? Brushes.Gray;
        _hoverBrush     = TryFindResource("BDiff_HoverBrush")             as Brush
                         ?? FreezeBrush(new SolidColorBrush(Color.FromArgb(0x50, 0x64, 0x96, 0xFF)));
        _hoverSegBrush  = TryFindResource("TDiff_HoverSegmentBrush")      as Brush;

        _brushesDirty = false;
        ClearCaches();
    }

    private void EnsureTypeface()
    {
        _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (_typeface is null || Math.Abs(_dpi - _lastDpi) > 0.001)
        {
            _typeface      = new Typeface(HexFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _glyphTypeface = DiffGlyphHelper.ResolveGlyphTypeface(_typeface);
            _lastDpi       = _dpi;
            RecalculateMetrics();
            ClearCaches();
        }
    }

    private void ClearCaches()
    {
        _gutterFtCache.Clear();
    }

    // ── Theme change detection ────────────────────────────────────────────────

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        _brushesDirty = true;
        InvalidateVisual();
    }

    public void InvalidateBrushCache()
    {
        _brushesDirty = true;
        InvalidateVisual();
    }
}
