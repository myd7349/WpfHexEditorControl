// Project      : WpfHexEditor.Plugins.FileComparison
// File         : Views/Controls/StructureDiffCanvas.cs
// Description  : Single-canvas virtualised renderer for the F2 Structure Diff grid.
//                Replaces ListView + DataTemplate with one FrameworkElement using
//                DrawingContext + GlyphRun. Sticky header row stays visible on scroll.
// Architecture : WPF-only, IScrollInfo pattern identical to BinaryDiffCanvas.

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
/// A single <see cref="FrameworkElement"/> that renders the F2 Structure Diff grid
/// with a sticky header row and virtualised data rows via <see cref="DrawingContext"/>.
/// </summary>
public sealed class StructureDiffCanvas : FrameworkElement, IScrollInfo
{
    // ── Base geometry ─────────────────────────────────────────────────────────

    private const double BaseStatusColW = 24.0;
    private const double BaseFieldColW  = 180.0;

    private double _baseRowH   = 20.0;
    private double _charWidth  = 8.0;
    private double _charHeight = 14.0;
    private double _baseline   = 11.0;

    // ── Scroll state ─────────────────────────────────────────────────────────

    private double _verticalOffset;

    // ── Typeface state ───────────────────────────────────────────────────────

    private Typeface?      _monoTypeface;
    private Typeface?      _uiTypeface;
    private GlyphTypeface? _monoGt;
    private double         _dpi;
    private double         _lastDpi;

    // ── Brush cache ──────────────────────────────────────────────────────────

    private Brush? _bgBrush;
    private Brush? _headerBgBrush;
    private Brush? _textFgBrush;
    private Brush? _modBrush;
    private Brush? _insBrush;
    private Brush? _delBrush;
    private Brush? _separatorBrush;
    private Brush? _hoverBrush;
    private Brush? _modFgBrush;
    private Brush? _insFgBrush;
    private Brush? _delFgBrush;
    private bool   _brushesDirty = true;

    // ── Hover state ──────────────────────────────────────────────────────────

    private int _hoverRowIndex = -1;

    // ── Cached geometry + header FTs ─────────────────────────────────────────

    private RectangleGeometry? _clipGeo;
    private FormattedText? _headerFieldFt;
    private FormattedText? _headerLeftFt;
    private FormattedText? _headerRightFt;
    private string? _cachedLeftFileName;
    private string? _cachedRightFileName;
    private double  _cachedHeaderFontSz;

    // ── Render-time ──────────────────────────────────────────────────────────

    private readonly Stopwatch _renderStopwatch = new();
    internal event EventHandler<long>? RefreshTimeUpdated;

    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty RowsProperty =
        DependencyProperty.Register(nameof(Rows),
            typeof(IReadOnlyList<StructureDiffRow>), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata(null, OnRowsChanged));

    public static readonly DependencyProperty LeftFileNameProperty =
        DependencyProperty.Register(nameof(LeftFileName),
            typeof(string), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RightFileNameProperty =
        DependencyProperty.Register(nameof(RightFileName),
            typeof(string), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(nameof(ZoomLevel),
            typeof(double), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((StructureDiffCanvas)d).OnZoomChanged()),
            v => v is double z && z >= 0.5 && z <= 4.0);

    public static readonly DependencyProperty HexFontFamilyProperty =
        DependencyProperty.Register(nameof(HexFontFamily),
            typeof(FontFamily), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata(new FontFamily("Consolas"),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                (d, _) => ((StructureDiffCanvas)d).OnFontChanged()));

    public static readonly DependencyProperty HexFontSizeProperty =
        DependencyProperty.Register(nameof(HexFontSize),
            typeof(double), typeof(StructureDiffCanvas),
            new FrameworkPropertyMetadata(12.0,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                (d, _) => ((StructureDiffCanvas)d).OnFontChanged()));

    public IReadOnlyList<StructureDiffRow>? Rows
    {
        get => (IReadOnlyList<StructureDiffRow>?)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }
    public string LeftFileName
    {
        get => (string)GetValue(LeftFileNameProperty);
        set => SetValue(LeftFileNameProperty, value);
    }
    public string RightFileName
    {
        get => (string)GetValue(RightFileNameProperty);
        set => SetValue(RightFileNameProperty, value);
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

    // ── Effective geometry ────────────────────────────────────────────────────

    public  double EffectiveRowH     => _baseRowH * ZoomLevel;
    private double EffectiveStatusW  => BaseStatusColW * ZoomLevel;
    private double EffectiveFieldW   => BaseFieldColW * ZoomLevel;
    private double EffectiveFontSz   => HexFontSize * ZoomLevel;
    private double EffectiveCharW    => _charWidth * ZoomLevel;
    private double HeaderH           => EffectiveRowH;

    private double HexColW => Math.Max(60, (ActualWidth - EffectiveStatusW - EffectiveFieldW) / 2);

    // ── IScrollInfo ──────────────────────────────────────────────────────────

    public ScrollViewer? ScrollOwner           { get; set; }
    public bool          CanHorizontallyScroll { get; set; }
    public bool          CanVerticallyScroll   { get; set; }

    public double ExtentHeight   => (Rows?.Count ?? 0) * EffectiveRowH + HeaderH;
    public double ExtentWidth    => EffectiveStatusW + EffectiveFieldW + HexColW * 2;
    public double ViewportHeight => ActualHeight;
    public double ViewportWidth  => ActualWidth;
    public double VerticalOffset   => _verticalOffset;
    public double HorizontalOffset => 0;

    public void SetVerticalOffset(double offset)
    {
        var clamped = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentHeight - ViewportHeight)));
        if (Math.Abs(clamped - _verticalOffset) < 0.001) return;
        _verticalOffset = clamped;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    public void SetHorizontalOffset(double offset) { }

    public void LineUp()          => SetVerticalOffset(_verticalOffset - EffectiveRowH);
    public void LineDown()        => SetVerticalOffset(_verticalOffset + EffectiveRowH);
    public void LineLeft()        { }
    public void LineRight()       { }
    public void PageUp()          => SetVerticalOffset(_verticalOffset - ViewportHeight);
    public void PageDown()        => SetVerticalOffset(_verticalOffset + ViewportHeight);
    public void PageLeft()        { }
    public void PageRight()       { }
    public void MouseWheelUp()    => SetVerticalOffset(_verticalOffset - 3 * EffectiveRowH);
    public void MouseWheelDown()  => SetVerticalOffset(_verticalOffset + 3 * EffectiveRowH);
    public void MouseWheelLeft()  { }
    public void MouseWheelRight() { }

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
        SetVerticalOffset(newV);
        return rectangle;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size available)
    {
        var w = double.IsInfinity(available.Width)  ? ExtentWidth  : available.Width;
        var h = double.IsInfinity(available.Height) ? ExtentHeight : available.Height;
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ScrollOwner?.InvalidateScrollInfo();
        return finalSize;
    }

    // ── Zoom / Font ───────────────────────────────────────────────────────────

    private void OnZoomChanged()
    {
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
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

    private void OnFontChanged()
    {
        RecalculateMetrics();
        _monoTypeface = null;
        _monoGt = null;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateMeasure();
    }

    private void RecalculateMetrics()
    {
        var dpi = _dpi > 0 ? _dpi : 96.0;
        var tf  = new Typeface(HexFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var (cw, ch, bl) = DiffGlyphHelper.MeasureCharMetrics(tf, HexFontSize, dpi);
        _charWidth  = cw;
        _charHeight = ch;
        _baseline   = bl;
        _baseRowH   = Math.Ceiling(ch) + 6;
    }

    // ── Rows change ───────────────────────────────────────────────────────────

    private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (StructureDiffCanvas)d;
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

    // ── Rendering ─────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        _renderStopwatch.Restart();

        EnsureBrushes();
        EnsureTypeface();

        dc.DrawRectangle(_bgBrush ?? Brushes.Transparent, null,
            new Rect(0, 0, ActualWidth, ActualHeight));

        var rows = Rows;
        var rowH = EffectiveRowH;
        var headerH = HeaderH;

        // ── Sticky header ────────────────────────────────────────────────
        DrawHeader(dc, headerH);

        if (rows is null || rows.Count == 0)
        {
            _renderStopwatch.Stop();
            RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
            return;
        }

        // ── Data rows (clipped below header) ─────────────────────────────
        var clipH = ActualHeight - headerH;
        if (_clipGeo is null || _clipGeo.Rect.Width != ActualWidth || _clipGeo.Rect.Height != clipH || _clipGeo.Rect.Y != headerH)
        {
            _clipGeo = new RectangleGeometry(new Rect(0, headerH, ActualWidth, clipH));
            _clipGeo.Freeze();
        }
        dc.PushClip(_clipGeo);

        int firstRow = Math.Max(0, (int)(_verticalOffset / rowH));
        int visible  = (int)Math.Ceiling((ActualHeight - headerH) / rowH) + 1;
        int lastRow  = Math.Min(rows.Count - 1, firstRow + visible);

        for (int i = firstRow; i <= lastRow; i++)
        {
            double y = headerH + (i - firstRow) * rowH - (_verticalOffset - firstRow * rowH);
            DrawStructureRow(dc, rows[i], y, rowH, i);
        }

        dc.Pop(); // clip

        _renderStopwatch.Stop();
        RefreshTimeUpdated?.Invoke(this, _renderStopwatch.ElapsedMilliseconds);
    }

    private void DrawHeader(DrawingContext dc, double headerH)
    {
        dc.DrawRectangle(_headerBgBrush, null, new Rect(0, 0, ActualWidth, headerH));

        var fontSz = EffectiveFontSz;
        double x0 = 0;
        double statusW = EffectiveStatusW;
        double fieldW  = EffectiveFieldW;
        double hexW    = HexColW;
        double midY    = headerH / 2;

        // Rebuild cached header FTs only when data changes
        EnsureHeaderFts(fontSz, fieldW, hexW);

        // Column headers (status column is blank — skip)
        x0 += statusW;
        if (_headerFieldFt is not null)
            dc.DrawText(_headerFieldFt, new Point(x0 + 4, midY - _headerFieldFt.Height / 2));
        x0 += fieldW;
        if (_headerLeftFt is not null)
            dc.DrawText(_headerLeftFt, new Point(x0 + 4, midY - _headerLeftFt.Height / 2));
        x0 += hexW;
        if (_headerRightFt is not null)
            dc.DrawText(_headerRightFt, new Point(x0 + 4, midY - _headerRightFt.Height / 2));

        // Bottom border
        dc.DrawRectangle(_separatorBrush, null, new Rect(0, headerH - 1, ActualWidth, 1));
    }

    private void EnsureHeaderFts(double fontSz, double fieldW, double hexW)
    {
        bool dirty = _headerFieldFt is null
                  || _cachedLeftFileName != LeftFileName
                  || _cachedRightFileName != RightFileName
                  || Math.Abs(_cachedHeaderFontSz - fontSz) > 0.001;
        if (!dirty) return;

        _headerFieldFt = MakeHeaderFt("Field", fontSz, fieldW);
        _headerLeftFt  = MakeHeaderFt(LeftFileName, fontSz, hexW);
        _headerRightFt = MakeHeaderFt(RightFileName, fontSz, hexW);
        _cachedLeftFileName  = LeftFileName;
        _cachedRightFileName = RightFileName;
        _cachedHeaderFontSz  = fontSz;
    }

    private FormattedText MakeHeaderFt(string text, double fontSz, double maxW)
    {
        var ft = DiffGlyphHelper.MakeFormattedText(text, _uiTypeface!,
            fontSz * 0.9, _textFgBrush!, _dpi);
        ft.SetFontWeight(FontWeights.SemiBold);
        ft.MaxTextWidth = Math.Max(1, maxW - 8);
        ft.MaxLineCount = 1;
        ft.Trimming = TextTrimming.CharacterEllipsis;
        return ft;
    }

    private void DrawStructureRow(DrawingContext dc, StructureDiffRow row, double y,
        double rowH, int rowIndex)
    {
        double statusW = EffectiveStatusW;
        double fieldW  = EffectiveFieldW;
        double hexW    = HexColW;
        double fontSz  = EffectiveFontSz;
        double bl      = _baseline * ZoomLevel;
        double baseY   = y + bl + 3;
        double midY    = y + rowH / 2;

        // Row background by status
        var rowBg = row.IsChanged    ? _modBrush
                  : row.IsOnlyInLeft ? _delBrush
                  : row.IsOnlyInRight? _insBrush
                  : null;
        if (rowBg is not null)
            dc.DrawRectangle(rowBg, null, new Rect(0, y, ActualWidth, rowH));

        // Hover highlight
        if (_hoverRowIndex == rowIndex && _hoverBrush is not null)
            dc.DrawRectangle(_hoverBrush, null, new Rect(0, y, ActualWidth, rowH));

        double x = 0;

        // Col 0: Status glyph
        var glyphFg = row.IsChanged    ? (_modFgBrush  ?? _textFgBrush!)
                    : row.IsOnlyInLeft ? (_delFgBrush  ?? _textFgBrush!)
                    : row.IsOnlyInRight? (_insFgBrush  ?? _textFgBrush!)
                    : _textFgBrush!;
        DiffGlyphHelper.RenderText(dc, row.StatusGlyph, x + statusW / 2 - _charWidth * ZoomLevel / 2,
            baseY, _monoGt, _monoTypeface!, fontSz, (float)_dpi, glyphFg, y);
        x += statusW;

        // Col 1: Field name (variable-width Segoe UI)
        DiffGlyphHelper.RenderFormattedTextClipped(dc, row.FieldName,
            x + 4, midY - _charHeight * ZoomLevel / 2,
            _uiTypeface!, fontSz, _dpi, _textFgBrush!, fieldW - 8);
        x += fieldW;

        // Col 2: Left hex (monospaced)
        if (!row.IsOnlyInRight)
        {
            if (row.IsChanged && _modBrush is not null)
                dc.DrawRectangle(_modBrush, null, new Rect(x, y, hexW, rowH));
            DiffGlyphHelper.RenderText(dc, row.LeftHex, x + 4, baseY,
                _monoGt, _monoTypeface!, fontSz, (float)_dpi, _textFgBrush!, y);
        }
        x += hexW;

        // Col 3: Right hex (monospaced)
        if (!row.IsOnlyInLeft)
        {
            if (row.IsChanged && _modBrush is not null)
                dc.DrawRectangle(_modBrush, null, new Rect(x, y, hexW, rowH));
            DiffGlyphHelper.RenderText(dc, row.RightHex, x + 4, baseY,
                _monoGt, _monoTypeface!, fontSz, (float)_dpi, _textFgBrush!, y);
        }

        // Column separator lines
        dc.DrawRectangle(_separatorBrush, null, new Rect(statusW, y, 1, rowH));
        dc.DrawRectangle(_separatorBrush, null, new Rect(statusW + fieldW, y, 1, rowH));
        dc.DrawRectangle(_separatorBrush, null, new Rect(statusW + fieldW + hexW, y, 1, rowH));
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int rowIdx = ResolveRowIndex(e.GetPosition(this).Y);
        if (rowIdx != _hoverRowIndex)
        {
            _hoverRowIndex = rowIdx;
            InvalidateVisual();

            // Tooltip
            if (rowIdx >= 0 && Rows is not null && rowIdx < Rows.Count)
            {
                var row = Rows[rowIdx];
                ToolTip = new ToolTip
                {
                    Content = $"{row.FieldName}  |  Left: {row.LeftOffsetHex}  |  Right: {row.RightOffsetHex}"
                };
            }
            else
                ToolTip = null;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverRowIndex >= 0)
        {
            _hoverRowIndex = -1;
            ToolTip = null;
            InvalidateVisual();
        }
    }

    private int ResolveRowIndex(double y)
    {
        if (y < HeaderH) return -1; // in header
        var rows = Rows;
        if (rows is null) return -1;
        int idx = (int)((_verticalOffset + y - HeaderH) / EffectiveRowH);
        return idx >= 0 && idx < rows.Count ? idx : -1;
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    public event EventHandler<long>? NavigateToOffsetRequested;

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        int rowIdx = ResolveRowIndex(e.GetPosition(this).Y);
        var menu = new ContextMenu();

        if (rowIdx >= 0 && Rows is not null && rowIdx < Rows.Count)
        {
            var row = Rows[rowIdx];
            menu.Items.Add(MakeMenuItem($"Copy Field: {row.FieldName}", () =>
                Clipboard.SetText($"{row.FieldName}: {row.LeftHex} → {row.RightHex}"), "\uE8C8"));

            if (!row.IsOnlyInRight)
                menu.Items.Add(MakeMenuItem($"Go to Left Offset ({row.LeftOffsetHex})", () =>
                    NavigateToOffsetRequested?.Invoke(this, row.LeftOffset), "\uE8A7"));
            if (!row.IsOnlyInLeft)
                menu.Items.Add(MakeMenuItem($"Go to Right Offset ({row.RightOffsetHex})", () =>
                    NavigateToOffsetRequested?.Invoke(this, row.RightOffset), "\uE8A7"));
        }

        if (Rows is { Count: > 0 })
        {
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            menu.Items.Add(MakeMenuItem("Copy All Fields", () =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{"Status",-3} {"Field",-30} {"Left Hex",-20} {"Right Hex",-20}");
                sb.AppendLine(new string('─', 75));
                foreach (var r in Rows!)
                    sb.AppendLine($"{r.StatusGlyph,-3} {r.FieldName,-30} {r.LeftHex,-20} {r.RightHex,-20}");
                Clipboard.SetText(sb.ToString());
            }, "\uE8C8"));
        }

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

    // ── Brush resolution ──────────────────────────────────────────────────────

    private void EnsureBrushes()
    {
        if (!_brushesDirty) return;

        _bgBrush        = TryFindResource("DockTabBackgroundBrush")       as Brush ?? Brushes.Black;
        _headerBgBrush  = TryFindResource("DF_GutterBackgroundBrush")     as Brush ?? Brushes.DimGray;
        _textFgBrush    = TryFindResource("DockMenuForegroundBrush")      as Brush ?? Brushes.White;
        _modBrush       = TryFindResource("DF_ModifiedLineBrush")         as Brush;
        _insBrush       = TryFindResource("DF_InsertedLineBrush")         as Brush;
        _delBrush       = TryFindResource("DF_DeletedLineBrush")          as Brush;
        _separatorBrush = TryFindResource("DockSplitterBrush")            as Brush ?? Brushes.Gray;
        _hoverBrush     = TryFindResource("BDiff_HoverBrush")             as Brush
                         ?? FreezeBrush(new SolidColorBrush(Color.FromArgb(0x50, 0x64, 0x96, 0xFF)));

        // Foreground colors for status glyphs
        _modFgBrush = TryFindResource("DF_OverviewModifiedBrush") as Brush ?? Brushes.Orange;
        _insFgBrush = TryFindResource("DF_OverviewAddedBrush")    as Brush ?? Brushes.Green;
        _delFgBrush = TryFindResource("DF_OverviewRemovedBrush")  as Brush ?? Brushes.Red;

        _brushesDirty = false;
        // Invalidate cached header FTs (brush may have changed)
        _headerFieldFt = null;
        _headerLeftFt  = null;
        _headerRightFt = null;
    }

    private static SolidColorBrush FreezeBrush(SolidColorBrush b) { b.Freeze(); return b; }

    private void EnsureTypeface()
    {
        _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (_monoTypeface is null || Math.Abs(_dpi - _lastDpi) > 0.001)
        {
            _monoTypeface = new Typeface(HexFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _uiTypeface   = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _monoGt       = DiffGlyphHelper.ResolveGlyphTypeface(_monoTypeface);
            _lastDpi      = _dpi;
            RecalculateMetrics();
        }
    }

    // ── Theme change ──────────────────────────────────────────────────────────

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
