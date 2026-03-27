//////////////////////////////////////////////
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/MinimapControl.cs
// Description:
//     VS Code-style code overview minimap. Renders a compressed view of the
//     entire document using tiny colored rectangles per syntax token.
//     Supports character-level rendering, hover highlight, viewport slider,
//     side placement, and a VS Code-matching context menu.
// Architecture:
//     Standalone FrameworkElement — placed beside the CodeEditor in SplitHost.
//     Reads document lines + syntax tokens from the attached CodeEditor.
//     Renders via DrawingContext (zero WPF containers — same pattern as
//     BinaryDiffCanvas and BarChartPanel).
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.CodeEditor.Helpers;

namespace WpfHexEditor.Editor.CodeEditor.Controls;

// ── Enums ────────────────────────────────────────────────────────────────────

public enum MinimapVerticalSize { Proportional, Fill, Fit }
public enum MinimapSliderMode { Always, MouseOver }
public enum MinimapSide { Left, Right }

// ── MinimapControl ───────────────────────────────────────────────────────────

/// <summary>
/// VS Code-style minimap overview of the code document. Each line is rendered
/// as a row of colored rectangles matching syntax token colors when
/// <see cref="RenderCharacters"/> is true; otherwise falls back to a single
/// heuristic-colored rect per line.
/// </summary>
public sealed class MinimapControl : FrameworkElement
{
    private CodeEditor? _editor;

    // ── Layout constants ─────────────────────────────────────────────────────

    private const double RowHeight = 3.0;
    private const double CharWidth = 0.85;
    private const int MaxVisibleChars = 120;
    private const double LeftPad = 4.0;
    private const double RowFillRatio = 0.55; // 55% fill, 45% gap — visible line spacing

    // ── Static frozen brushes ────────────────────────────────────────────────

    private static readonly Brush ViewportBrush;
    private static readonly Brush ViewportBorderBrush;
    private static readonly Pen ViewportPen;
    private static readonly Brush HoverBandBrush;
    private static readonly Brush DarkenOverlay;
    private static readonly Brush DefaultTextBrush;
    private static readonly Brush KeywordBrush;
    private static readonly Brush StringBrush;
    private static readonly Brush CommentBrush;
    private static readonly Brush NumberBrush;
    private static readonly Brush TypeBrush;

    static MinimapControl()
    {
        ViewportBrush = Freeze(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)));
        ViewportBorderBrush = Freeze(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)));
        ViewportPen = FreezePen(new Pen(ViewportBorderBrush, 1.0));
        HoverBandBrush = Freeze(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)));
        DarkenOverlay = Freeze(new SolidColorBrush(Color.FromArgb(25, 0, 0, 0)));
        DefaultTextBrush = Freeze(new SolidColorBrush(Color.FromArgb(100, 200, 200, 200)));
        KeywordBrush = Freeze(new SolidColorBrush(Color.FromArgb(180, 86, 156, 214)));
        StringBrush = Freeze(new SolidColorBrush(Color.FromArgb(180, 206, 145, 120)));
        CommentBrush = Freeze(new SolidColorBrush(Color.FromArgb(120, 106, 153, 85)));
        NumberBrush = Freeze(new SolidColorBrush(Color.FromArgb(180, 181, 206, 168)));
        TypeBrush = Freeze(new SolidColorBrush(Color.FromArgb(180, 78, 201, 176)));
    }

    private static Brush Freeze(SolidColorBrush b) { b.Freeze(); return b; }
    private static Pen FreezePen(Pen p) { p.Freeze(); return p; }

    // ── Token kind → theme key mapping ───────────────────────────────────────

    // Indexed by (int)SyntaxTokenKind. null = use fallback brush.
    private static readonly string?[] TokenKindToThemeKey =
    {
        null,            // Default
        "CE_Keyword",    // Keyword
        "CE_String",     // String
        "CE_Number",     // Number
        "CE_Comment",    // Comment
        "CE_Identifier", // Identifier
        "CE_Operator",   // Operator
        "CE_Bracket",    // Bracket
        "CE_Type",       // Type
        "CE_Attribute",  // Attribute
        "CE_Keyword",    // ControlFlow → reuse keyword
    };

    // Per-kind alpha multiplier (0.0–1.0). Faded silhouette — not full-color.
    private static readonly double[] TokenKindAlpha =
    {
        0.30, // Default      — barely visible
        0.60, // Keyword      — visible but soft
        0.55, // String       — moderate
        0.45, // Number       — subtle
        0.25, // Comment      — nearly invisible (VS Code style)
        0.30, // Identifier   — barely visible
        0.25, // Operator     — very subtle
        0.25, // Bracket      — very subtle
        0.55, // Type         — moderate
        0.45, // Attribute    — subtle
        0.60, // ControlFlow  — same as keyword
    };

    // Theme-resolved, alpha-blended brush cache. Invalidated on theme change.
    // Cache key encodes both theme key AND alpha: "CE_Keyword:0.85"
    private readonly Dictionary<string, Brush> _themeAlphaBrushCache = new();

    // Cached separator pen (rebuilt on theme change)
    private Pen? _separatorPen;

    // ── Public properties ────────────────────────────────────────────────────

    /// <summary>Width of the minimap in pixels.</summary>
    public double MinimapWidth { get; set; } = 100;

    private bool _renderCharacters = true;
    /// <summary>When true, renders per-token colored blocks. When false, one rect per line.</summary>
    public bool RenderCharacters
    {
        get => _renderCharacters;
        set { if (_renderCharacters != value) { _renderCharacters = value; InvalidateVisual(); } }
    }

    private MinimapVerticalSize _verticalSize = MinimapVerticalSize.Proportional;
    /// <summary>Controls how the minimap scales vertically.</summary>
    public MinimapVerticalSize VerticalSize
    {
        get => _verticalSize;
        set { if (_verticalSize != value) { _verticalSize = value; InvalidateVisual(); } }
    }

    private MinimapSliderMode _sliderMode = MinimapSliderMode.Always;
    /// <summary>Controls when the viewport slider is visible.</summary>
    public MinimapSliderMode SliderMode
    {
        get => _sliderMode;
        set { if (_sliderMode != value) { _sliderMode = value; InvalidateVisual(); } }
    }

    private MinimapSide _side = MinimapSide.Right;
    /// <summary>Minimap placement side (Left or Right). Fires <see cref="SideChangeRequested"/>.</summary>
    public MinimapSide Side
    {
        get => _side;
        set { if (_side != value) { _side = value; } }
    }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired when user toggles minimap visibility from context menu.</summary>
    public event Action<bool>? MinimapToggled;

    /// <summary>Fired when user changes minimap side from context menu.</summary>
    public event Action<MinimapSide>? SideChangeRequested;

    // ── Hover state ──────────────────────────────────────────────────────────

    private bool _isMouseOver;
    private double _hoverY = -1;
    private bool _isDragging;
    private double _lastMinimapScrollOffset;

    // ── Constructor ──────────────────────────────────────────────────────────

    public MinimapControl()
    {
        InitializeContextMenu();
    }

    // ── Editor attachment ────────────────────────────────────────────────────

    /// <summary>Attaches to a CodeEditor for document and viewport tracking.</summary>
    public void SetEditor(CodeEditor editor)
    {
        _editor = editor;
        InvalidateVisual();
    }

    /// <summary>Called by the host when the document or viewport changes.</summary>
    public void Refresh() => InvalidateVisual();

    /// <summary>Clears the cached alpha brushes — call on theme change.</summary>
    public void InvalidateThemeCache()
    {
        _themeAlphaBrushCache.Clear();
        _separatorPen = null;
        InvalidateVisual();
    }

    // ── Measure ──────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var h = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
        return new Size(MinimapWidth, h);
    }

    // ── Render ───────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w < 1 || h < 1 || _editor is null) return;

        // Background — base + darken overlay for visual separation from editor
        var bgBrush = TryFindResource("TE_Background") as Brush ?? Brushes.Black;
        dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));
        dc.DrawRectangle(DarkenOverlay, null, new Rect(0, 0, w, h));

        var doc = _editor.Document;
        if (doc is null || doc.Lines.Count == 0) return;

        int totalLines = doc.Lines.Count;
        double scale = ComputeScale(totalLines, h);

        // For proportional mode with large files, compute a scroll offset
        // so the editor's visible area stays centered in the minimap.
        double minimapScrollOffset = 0;
        int editorFirstVisible = 0;
        int editorVisibleCount = 0;

        if (_editor.VirtualizationEngine is { } ve && ve.TotalLines > 0)
        {
            editorFirstVisible = ve.FirstVisibleLine;
            editorVisibleCount = ve.VisibleLineCount;
        }

        double totalMinimapHeight = totalLines * scale;
        if (totalMinimapHeight > h)
        {
            // Proportional scroll: minimap scrolls in sync with editor.
            // At editor top (ratio=0) → minimap shows first lines.
            // At editor bottom (ratio=1) → minimap shows last lines.
            // Guarantees ALL lines are reachable.
            double editorMaxScroll = Math.Max(1.0, totalLines - editorVisibleCount);
            double editorScrollRatio = Math.Clamp((double)editorFirstVisible / editorMaxScroll, 0, 1);
            double minimapMaxScroll = totalMinimapHeight - h;
            minimapScrollOffset = editorScrollRatio * minimapMaxScroll / scale;
        }

        _lastMinimapScrollOffset = minimapScrollOffset;

        int firstLine = (int)minimapScrollOffset;
        int maxLine = Math.Min(totalLines, firstLine + (int)(h / scale) + 2);

        // Effective row drawing height (with gap)
        double drawH = Math.Max(scale * RowFillRatio, 0.5);

        // Draw lines
        for (int i = firstLine; i < maxLine; i++)
        {
            var line = doc.Lines[i];
            if (line.Text is null || line.Text.Length == 0) continue;

            double y = (i - minimapScrollOffset) * scale;
            if (y + scale < 0) continue;
            if (y > h) break;

            if (_renderCharacters && line.TokensCache is { } tokens && !line.IsCacheDirty && tokens.Count > 0)
            {
                // Character-level rendering: one rect per syntax token
                for (int t = 0; t < tokens.Count; t++)
                {
                    var token = tokens[t];
                    double x = LeftPad + token.StartColumn * CharWidth;
                    if (x >= w - 2) break;
                    double tw = token.Length * CharWidth;
                    tw = Math.Min(tw, w - x - 2);
                    if (tw <= 0) continue;

                    var brush = ResolveTokenBrush(token.Kind, token.Foreground);
                    dc.DrawRectangle(brush, null, new Rect(x, y, tw, drawH));
                }
            }
            else
            {
                // Fallback: single rect per line with heuristic color
                int chars = Math.Min(line.Text.Length, MaxVisibleChars);
                double lineWidth = chars * CharWidth;
                var brush = GetLineBrush(line.Text);
                dc.DrawRectangle(brush, null,
                    new Rect(LeftPad, y, Math.Min(lineWidth, w - LeftPad - 2), drawH));
            }
        }

        // Content height — slider and hover band are clamped to this
        double contentHeight = Math.Min(totalMinimapHeight, h);

        // Hover highlight band
        if (_isMouseOver && !_isDragging && _hoverY >= 0 && editorVisibleCount > 0)
        {
            double bandHeight = Math.Max(editorVisibleCount * scale, 10);
            double bandTop = Math.Clamp(_hoverY - bandHeight / 2, 0, contentHeight - bandHeight);
            dc.DrawRectangle(HoverBandBrush, null, new Rect(0, bandTop, w, bandHeight));
        }

        // Viewport slider — clamped to content area, not full minimap height
        bool showSlider = _sliderMode == MinimapSliderMode.Always || _isMouseOver;
        if (showSlider && editorVisibleCount > 0)
        {
            double vpTop = (editorFirstVisible - minimapScrollOffset) * scale;
            double vpHeight = Math.Max(editorVisibleCount * scale, 10);
            vpTop = Math.Clamp(vpTop, 0, Math.Max(contentHeight - 1, 0));
            vpHeight = Math.Min(vpHeight, Math.Max(contentHeight - vpTop, 1));

            dc.DrawRectangle(ViewportBrush, ViewportPen, new Rect(0, vpTop, w, vpHeight));
        }

        // Separator line on the editor-facing edge
        var sepPen = GetSeparatorPen();
        double sepX = (_side == MinimapSide.Right) ? 0.5 : w - 0.5;
        dc.DrawLine(sepPen, new Point(sepX, 0), new Point(sepX, h));
    }

    // ── Scale computation ────────────────────────────────────────────────────

    private double ComputeScale(int totalLines, double viewportHeight)
    {
        if (totalLines <= 0) return RowHeight;

        return _verticalSize switch
        {
            MinimapVerticalSize.Fill => viewportHeight / totalLines,
            MinimapVerticalSize.Fit => Math.Clamp(viewportHeight / totalLines, 1.0, RowHeight),
            _ /* Proportional */ => Math.Min(RowHeight, viewportHeight / totalLines),
        };
    }

    // ── Token brush resolution ───────────────────────────────────────────────

    private Brush ResolveTokenBrush(SyntaxTokenKind kind, Brush fallback)
    {
        int idx = (int)kind;
        double alpha = (idx >= 0 && idx < TokenKindAlpha.Length) ? TokenKindAlpha[idx] : 0.55;

        if (idx < 0 || idx >= TokenKindToThemeKey.Length)
            return ApplyAlpha(fallback, alpha);

        var key = TokenKindToThemeKey[idx];
        if (key is null)
            return ApplyAlpha(fallback ?? DefaultTextBrush, alpha);

        // Cache key includes alpha so different kinds sharing the same theme key
        // (e.g. Keyword and ControlFlow → CE_Keyword) get separate entries if alpha differs.
        var cacheKey = $"{key}:{alpha:F2}";
        if (_themeAlphaBrushCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (TryFindResource(key) is SolidColorBrush themeBrush)
        {
            var c = themeBrush.Color;
            var a = (byte)(c.A * alpha);
            var result = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
            result.Freeze();
            _themeAlphaBrushCache[cacheKey] = result;
            return result;
        }

        var fb = ApplyAlpha(fallback ?? DefaultTextBrush, alpha);
        _themeAlphaBrushCache[cacheKey] = fb;
        return fb;
    }

    private static Brush ApplyAlpha(Brush brush, double alpha)
    {
        if (brush is SolidColorBrush scb)
        {
            var c = scb.Color;
            var a = (byte)(c.A * alpha);
            var result = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
            result.Freeze();
            return result;
        }
        return brush;
    }

    private Pen GetSeparatorPen()
    {
        if (_separatorPen is not null) return _separatorPen;
        var brush = TryFindResource("CE_LineNumFg") as Brush ?? ViewportBorderBrush;
        // Make it subtle — 50% opacity
        if (brush is SolidColorBrush scb)
        {
            var c = scb.Color;
            brush = new SolidColorBrush(Color.FromArgb((byte)(c.A * 0.3), c.R, c.G, c.B));
            ((SolidColorBrush)brush).Freeze();
        }
        _separatorPen = new Pen(brush, 1.0);
        _separatorPen.Freeze();
        return _separatorPen;
    }

    // ── Heuristic line brush (fallback) ──────────────────────────────────────

    private static Brush GetLineBrush(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        if (trimmed.Length == 0) return Brushes.Transparent;
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            return CommentBrush;
        if (trimmed.StartsWith("\"") || trimmed.StartsWith("'") || trimmed.StartsWith("@\"") || trimmed.StartsWith("$\""))
            return StringBrush;
        if (trimmed.StartsWith("using ") || trimmed.StartsWith("namespace ") ||
            trimmed.StartsWith("public ") || trimmed.StartsWith("private ") ||
            trimmed.StartsWith("protected ") || trimmed.StartsWith("internal ") ||
            trimmed.StartsWith("class ") || trimmed.StartsWith("interface ") ||
            trimmed.StartsWith("struct ") || trimmed.StartsWith("enum ") ||
            trimmed.StartsWith("if ") || trimmed.StartsWith("else") ||
            trimmed.StartsWith("for ") || trimmed.StartsWith("foreach ") ||
            trimmed.StartsWith("while ") || trimmed.StartsWith("return ") ||
            trimmed.StartsWith("var ") || trimmed.StartsWith("async ") ||
            trimmed.StartsWith("await "))
            return KeywordBrush;
        return DefaultTextBrush;
    }

    // ── Mouse interaction ────────────────────────────────────────────────────

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _isMouseOver = true;
        InvalidateVisual();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _isMouseOver = false;
        _hoverY = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _isDragging = true;
        NavigateToY(e.GetPosition(this).Y);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);

        if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
        {
            NavigateToY(pos.Y);
        }
        else
        {
            _hoverY = pos.Y;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        _isDragging = false;
        ReleaseMouseCapture();
        InvalidateVisual();
    }

    private void NavigateToY(double y)
    {
        if (_editor?.Document is null) return;
        int totalLines = _editor.Document.Lines.Count;
        double h = ActualHeight;
        double scale = ComputeScale(totalLines, h);
        int visibleCount = _editor.VirtualizationEngine?.VisibleLineCount ?? 0;

        double totalContentHeight = totalLines * scale;
        if (totalContentHeight < 1) return;

        // Convert viewport-relative Y to absolute position in full content,
        // accounting for the minimap's own scroll offset.
        double absoluteY = y + _lastMinimapScrollOffset * scale;
        double clickRatio = Math.Clamp(absoluteY / totalContentHeight, 0, 1);

        // Map proportionally to the full editor scroll range.
        int editorMaxTopLine = Math.Max(0, totalLines - visibleCount);
        int topLine = (int)(clickRatio * editorMaxTopLine);
        _editor.ScrollViewToLine(topLine);
    }

    // ── Context menu ─────────────────────────────────────────────────────────

    private MenuItem? _minimapToggleItem;
    private MenuItem? _renderCharsItem;
    private MenuItem? _vProp, _vFill, _vFit;
    private MenuItem? _sAlways, _sMouseOver;
    private MenuItem? _sideLeft, _sideRight;

    private void InitializeContextMenu()
    {
        var menu = new ContextMenu();

        // Minimap toggle
        _minimapToggleItem = new MenuItem { Header = "_Minimap", IsCheckable = true, IsChecked = true };
        _minimapToggleItem.Click += (_, _) =>
        {
            MinimapToggled?.Invoke(false);
        };
        menu.Items.Add(_minimapToggleItem);

        menu.Items.Add(new Separator());

        // Render Characters toggle
        _renderCharsItem = new MenuItem { Header = "Render _Characters", IsCheckable = true, IsChecked = _renderCharacters };
        _renderCharsItem.Click += (_, _) =>
        {
            RenderCharacters = _renderCharsItem.IsChecked;
        };
        menu.Items.Add(_renderCharsItem);

        // Vertical size submenu
        var vSizeMenu = new MenuItem { Header = "_Vertical size" };
        _vProp = new MenuItem { Header = "Proportional" };
        _vFill = new MenuItem { Header = "Fill" };
        _vFit = new MenuItem { Header = "Fit" };
        _vProp.Click += (_, _) => VerticalSize = MinimapVerticalSize.Proportional;
        _vFill.Click += (_, _) => VerticalSize = MinimapVerticalSize.Fill;
        _vFit.Click += (_, _) => VerticalSize = MinimapVerticalSize.Fit;
        vSizeMenu.Items.Add(_vProp);
        vSizeMenu.Items.Add(_vFill);
        vSizeMenu.Items.Add(_vFit);
        menu.Items.Add(vSizeMenu);

        // Slider submenu
        var sliderMenu = new MenuItem { Header = "S_lider" };
        _sAlways = new MenuItem { Header = "Always" };
        _sMouseOver = new MenuItem { Header = "Mouse Over" };
        _sAlways.Click += (_, _) => SliderMode = MinimapSliderMode.Always;
        _sMouseOver.Click += (_, _) => SliderMode = MinimapSliderMode.MouseOver;
        sliderMenu.Items.Add(_sAlways);
        sliderMenu.Items.Add(_sMouseOver);
        menu.Items.Add(sliderMenu);

        // Side submenu
        var sideMenu = new MenuItem { Header = "Si_de" };
        _sideLeft = new MenuItem { Header = "Left" };
        _sideRight = new MenuItem { Header = "Right" };
        _sideLeft.Click += (_, _) => { Side = MinimapSide.Left; SideChangeRequested?.Invoke(MinimapSide.Left); };
        _sideRight.Click += (_, _) => { Side = MinimapSide.Right; SideChangeRequested?.Invoke(MinimapSide.Right); };
        sideMenu.Items.Add(_sideLeft);
        sideMenu.Items.Add(_sideRight);
        menu.Items.Add(sideMenu);

        // Sync check states when menu opens
        menu.Opened += (_, _) => SyncContextMenuChecks();

        ContextMenu = menu;
    }

    private void SyncContextMenuChecks()
    {
        if (_minimapToggleItem is not null) _minimapToggleItem.IsChecked = true; // visible if context menu shows
        if (_renderCharsItem is not null) _renderCharsItem.IsChecked = _renderCharacters;

        // Radio-style checks for submenus
        SetRadio(_vProp, _verticalSize == MinimapVerticalSize.Proportional);
        SetRadio(_vFill, _verticalSize == MinimapVerticalSize.Fill);
        SetRadio(_vFit, _verticalSize == MinimapVerticalSize.Fit);

        SetRadio(_sAlways, _sliderMode == MinimapSliderMode.Always);
        SetRadio(_sMouseOver, _sliderMode == MinimapSliderMode.MouseOver);

        SetRadio(_sideLeft, _side == MinimapSide.Left);
        SetRadio(_sideRight, _side == MinimapSide.Right);
    }

    private static void SetRadio(MenuItem? item, bool selected)
    {
        if (item is null) return;
        item.IsCheckable = true;
        item.IsChecked = selected;
    }
}
