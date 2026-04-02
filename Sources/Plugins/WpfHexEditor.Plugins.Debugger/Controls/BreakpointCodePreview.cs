// ==========================================================
// Project: WpfHexEditor.Plugins.Debugger
// File: Controls/BreakpointCodePreview.cs
// Description:
//     GlyphRun-based syntax-highlighted code preview FrameworkElement.
//     Resolves language from file extension via LanguageRegistry,
//     tokenizes using LanguageDefinition.SyntaxRules, renders with
//     DrawingContext GlyphRun for zero-allocation per-frame rendering.
// Architecture:
//     Plugin-local — depends only on WpfHexEditor.Core.ProjectSystem
//     (available via SDK transitive re-export). No CodeEditor/TextEditor dep.
//     GlyphRun pattern mirrors DiffGlyphHelper in FileComparison plugin.
// ==========================================================

using System.Windows;
using System.Windows.Media;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.Plugins.Debugger.Controls;

internal sealed class BreakpointCodePreview : FrameworkElement
{
    /// <summary>
    /// Set once during plugin init. All instances share the same service reference.
    /// </summary>
    internal static ISyntaxColoringService? SyntaxColoringService { get; set; }

    // ── Dependency Properties ──────────────────────────────────────────────

    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.Register(nameof(SourceText), typeof(string), typeof(BreakpointCodePreview),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((BreakpointCodePreview)d).RebuildCachedSpans()));

    public static readonly DependencyProperty FilePathProperty =
        DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(BreakpointCodePreview),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((BreakpointCodePreview)d).OnFilePathChanged()));

    public string SourceText
    {
        get => (string)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public string FilePath
    {
        get => (string)GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    // ── Font constants ─────────────────────────────────────────────────────

    private const double FontSize = 10.5;
    private static readonly Typeface _typeface = new("Consolas");

    // ── GlyphTypeface cache (flyweight, shared across instances) ───────────

    private static readonly Dictionary<Typeface, GlyphTypeface?> _gtCache = [];
    private static GlyphTypeface? _gt;
    private static double _charWidth;
    private static double _rowHeight;
    private static double _baseline;
    private static bool   _metricsReady;

    // ── Thread-static pooled GlyphRun lists ────────────────────────────────

    [ThreadStatic] private static List<ushort>? _glyphPool;
    [ThreadStatic] private static List<double>? _advancePool;

    // ── Per-instance coloring state ───────────────────────────────────────

    private string? _resolvedLanguageId;
    private IReadOnlyList<ColoredSpan>[]? _cachedLineSpans;
    private float _pixelsPerDip = 1f;

    // ── Constructor ────────────────────────────────────────────────────────

    public BreakpointCodePreview()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        EnsureMetrics();
    }

    // ── Metric initialisation ──────────────────────────────────────────────

    private void EnsureMetrics()
    {
        if (_metricsReady) return;

        if (!_gtCache.TryGetValue(_typeface, out _gt))
        {
            _gt = _typeface.TryGetGlyphTypeface(out var resolved) ? resolved : null;
            _gtCache[_typeface] = _gt;
        }

        if (_gt is not null)
        {
            _gt.CharacterToGlyphMap.TryGetValue('M', out ushort gi);
            _charWidth = _gt.AdvanceWidths[gi] * FontSize;
            _rowHeight  = _gt.Height           * FontSize;
            _baseline   = _gt.Baseline         * FontSize;
        }
        else
        {
            var ft = new FormattedText("M", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, _typeface, FontSize, Brushes.Black, _pixelsPerDip);
            _charWidth = ft.Width;
            _rowHeight  = ft.Height;
            _baseline   = ft.Height * 0.8;
        }

        _metricsReady = true;
    }

    // ── DP callbacks ───────────────────────────────────────────────────────

    private void OnFilePathChanged()
    {
        var fp = FilePath;
        _resolvedLanguageId = !string.IsNullOrEmpty(fp)
            ? SyntaxColoringService?.ResolveLanguageId(System.IO.Path.GetExtension(fp))
            : null;
        RebuildCachedSpans();
        InvalidateVisual();
    }

    private void RebuildCachedSpans()
    {
        _cachedLineSpans = null;
        var text = SourceText;
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(_resolvedLanguageId)) return;

        try
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd('\r');
            var spans = SyntaxColoringService?.ColorizeLines(lines, _resolvedLanguageId!);
            if (spans is not null)
            {
                _cachedLineSpans = new IReadOnlyList<ColoredSpan>[spans.Count];
                for (int i = 0; i < spans.Count; i++)
                    _cachedLineSpans[i] = spans[i];
            }
        }
        catch { /* graceful fallback */ }
    }

    // ── Layout ─────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureMetrics();
        var text = SourceText;
        if (string.IsNullOrEmpty(text) || _rowHeight == 0)
            return new Size(0, 0);

        var lines = text.Split('\n');
        double maxW = 0;
        foreach (var l in lines)
            maxW = Math.Max(maxW, l.TrimEnd('\r').Length * _charWidth);

        return new Size(maxW, lines.Length * _rowHeight);
    }

    // ── Rendering ──────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        var text = SourceText;
        if (string.IsNullOrEmpty(text)) return;

        EnsureMetrics();
        if (_rowHeight == 0) return;

        var defaultFg =
            System.Windows.Application.Current?.TryFindResource("DockMenuForegroundBrush") as Brush
            ?? Brushes.White;

        var lines = text.Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            var line = lines[li].TrimEnd('\r');
            if (line.Length == 0) continue;

            double topY = li * _rowHeight;
            var spans = _cachedLineSpans is not null && li < _cachedLineSpans.Length
                ? _cachedLineSpans[li] : null;

            if (spans is not null && spans.Count > 0)
            {
                int pos = 0;
                foreach (var span in spans)
                {
                    if (span.Start > pos)
                        DrawSegment(dc, line, pos, span.Start - pos, topY, defaultFg);
                    DrawSegment(dc, line, span.Start, span.Length, topY, span.Foreground);
                    pos = span.Start + span.Length;
                }
                if (pos < line.Length)
                    DrawSegment(dc, line, pos, line.Length - pos, topY, defaultFg);
            }
            else
            {
                DrawSegment(dc, line, 0, line.Length, topY, defaultFg);
            }
        }
    }

    private void DrawSegment(DrawingContext dc, string line, int start, int length,
                              double topY, Brush brush)
    {
        if (length <= 0) return;
        var  text      = line.Substring(start, length);
        double x       = start * _charWidth;
        double baseline = topY + _baseline;

        if (_gt is not null)
        {
            var glyphs   = _glyphPool   ??= new List<ushort>(256);
            var advances = _advancePool ??= new List<double>(256);
            glyphs.Clear();
            advances.Clear();

            var map = _gt.CharacterToGlyphMap;
            foreach (char ch in text)
            {
                if (ch == '\t')
                {
                    map.TryGetValue(' ', out ushort sp);
                    glyphs.Add(sp);
                    advances.Add(_gt.AdvanceWidths[sp] * FontSize * 4);
                    continue;
                }
                if (!map.TryGetValue(ch, out ushort gi))
                    map.TryGetValue('\uFFFD', out gi);
                glyphs.Add(gi);
                advances.Add(_gt.AdvanceWidths[gi] * FontSize);
            }
            if (glyphs.Count == 0) return;

            var run = new GlyphRun(
                _gt, 0, false, FontSize, _pixelsPerDip,
                glyphs.ToArray(),
                new Point(x, baseline),
                advances.ToArray(),
                null, null, null, null, null, null);

            dc.DrawGlyphRun(brush, run);
        }
        else
        {
            var ft = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, _typeface, FontSize, brush, _pixelsPerDip);
            dc.DrawText(ft, new Point(x, topY));
        }
    }
}
