// Project      : WpfHexEditor.Plugins.FileComparison
// File         : Views/Controls/DiffGlyphHelper.cs
// Description  : Lightweight static GlyphRun rendering helper for diff canvases.
//                Duplicates the core pattern from GlyphRunRenderer.cs (CodeEditor)
//                to avoid cross-project dependency. Optimised for monospaced text.
// Architecture : Static utility — no instance state. Canvases own their own
//                typeface/metric state and call into this helper for draw calls.
//                Flyweight GlyphTypeface cache shared across all diff canvases.

using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfHexEditor.Plugins.FileComparison.Views.Controls;

/// <summary>
/// Static helper providing GlyphRun-based text rendering for diff canvases.
/// Falls back to <see cref="FormattedText"/> when <see cref="GlyphTypeface"/> is unavailable.
/// </summary>
internal static class DiffGlyphHelper
{
    // ── Static GlyphTypeface cache (flyweight) ─────────────────────────────

    private static readonly Dictionary<Typeface, GlyphTypeface?> _gtCache
        = new(TypefaceEqualityComparer.Instance);

    /// <summary>Resolves and caches the <see cref="GlyphTypeface"/> for a given <see cref="Typeface"/>.</summary>
    public static GlyphTypeface? ResolveGlyphTypeface(Typeface typeface)
    {
        if (_gtCache.TryGetValue(typeface, out var cached))
            return cached;

        GlyphTypeface? gt = typeface.TryGetGlyphTypeface(out var resolved) ? resolved : null;
        _gtCache[typeface] = gt;
        return gt;
    }

    // ── Char metrics measurement ───────────────────────────────────────────

    /// <summary>
    /// Measures character dimensions for a monospaced font.
    /// Uses <see cref="GlyphTypeface"/> when available, falls back to <see cref="FormattedText"/>.
    /// </summary>
    public static (double charWidth, double charHeight, double baseline) MeasureCharMetrics(
        Typeface typeface, double fontSize, double pixelsPerDip)
    {
        var gt = ResolveGlyphTypeface(typeface);
        if (gt != null)
        {
            gt.CharacterToGlyphMap.TryGetValue('M', out ushort gi);
            double charW = gt.AdvanceWidths[gi] * fontSize;
            double charH = gt.Height * fontSize;
            double baseline = gt.Baseline * fontSize;
            return (charW, charH, baseline);
        }

        // Fallback via FormattedText
        var ft = MakeFormattedText("M", typeface, fontSize, Brushes.Black, pixelsPerDip);
        return (ft.Width, ft.Height, ft.Height * 0.8);
    }

    // ── GlyphRun text rendering ────────────────────────────────────────────

    /// <summary>
    /// Renders monospaced text via <see cref="GlyphRun"/>. If <paramref name="gt"/> is null,
    /// falls back to <see cref="FormattedText"/>.
    /// </summary>
    /// <param name="dc">Active drawing context (inside OnRender).</param>
    /// <param name="text">Text to render.</param>
    /// <param name="x">Left pixel edge.</param>
    /// <param name="baselineY">Baseline Y coordinate (top + baseline offset).</param>
    /// <param name="gt">Pre-resolved GlyphTypeface (null → FormattedText fallback).</param>
    /// <param name="typeface">Typeface for FormattedText fallback.</param>
    /// <param name="fontSize">Font size in device-independent pixels.</param>
    /// <param name="pixelsPerDip">DPI scale factor.</param>
    /// <param name="brush">Foreground brush.</param>
    /// <param name="topY">Top Y coordinate for FormattedText fallback (baselineY - baseline).</param>
    public static void RenderText(DrawingContext dc, string text,
        double x, double baselineY,
        GlyphTypeface? gt, Typeface typeface,
        double fontSize, float pixelsPerDip, Brush brush,
        double topY)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (gt != null)
        {
            RenderWithGlyphRun(dc, text, x, baselineY, gt, fontSize, pixelsPerDip, brush);
        }
        else
        {
            var ft = MakeFormattedText(text, typeface, fontSize, brush, pixelsPerDip);
            dc.DrawText(ft, new Point(x, topY));
        }
    }

    /// <summary>
    /// Simplified overload that renders text at a top-Y position using FormattedText only.
    /// Used for non-monospaced content (field names in Structure Diff).
    /// </summary>
    public static void RenderFormattedText(DrawingContext dc, string text,
        double x, double topY, Typeface typeface,
        double fontSize, double pixelsPerDip, Brush brush)
    {
        if (string.IsNullOrEmpty(text)) return;
        var ft = MakeFormattedText(text, typeface, fontSize, brush, pixelsPerDip);
        dc.DrawText(ft, new Point(x, topY));
    }

    /// <summary>
    /// Renders text via FormattedText with a max width constraint (for column clipping).
    /// Returns the actual width of the rendered text.
    /// </summary>
    public static double RenderFormattedTextClipped(DrawingContext dc, string text,
        double x, double topY, Typeface typeface,
        double fontSize, double pixelsPerDip, Brush brush, double maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var ft = MakeFormattedText(text, typeface, fontSize, brush, pixelsPerDip);
        ft.MaxTextWidth = Math.Max(1, maxWidth);
        ft.MaxLineCount = 1;
        ft.Trimming = TextTrimming.CharacterEllipsis;
        dc.DrawText(ft, new Point(x, topY));
        return ft.Width;
    }

    // ── FormattedText factory ──────────────────────────────────────────────

    public static FormattedText MakeFormattedText(string text, Typeface typeface,
        double fontSize, Brush brush, double pixelsPerDip)
        => new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
               typeface, fontSize, brush, pixelsPerDip);

    // ── Private GlyphRun builder (pooled lists) ─────────────────────────────

    [ThreadStatic] private static List<ushort>? _glyphPool;
    [ThreadStatic] private static List<double>? _advancePool;

    private static void RenderWithGlyphRun(DrawingContext dc, string text,
        double x, double baselineY,
        GlyphTypeface gt, double fontSize, float pixelsPerDip, Brush brush)
    {
        var glyphIndices  = _glyphPool   ??= new List<ushort>(256);
        var advanceWidths = _advancePool ??= new List<double>(256);
        glyphIndices.Clear();
        advanceWidths.Clear();
        var charMap = gt.CharacterToGlyphMap;

        foreach (char ch in text)
        {
            if (ch == '\t')
            {
                // Tab → space glyph with 4× advance
                charMap.TryGetValue(' ', out ushort spaceGi);
                glyphIndices.Add(spaceGi);
                advanceWidths.Add(gt.AdvanceWidths[spaceGi] * fontSize * 4);
                continue;
            }

            if (!charMap.TryGetValue(ch, out ushort gi))
                charMap.TryGetValue('\uFFFD', out gi); // replacement char fallback

            glyphIndices.Add(gi);
            advanceWidths.Add(gt.AdvanceWidths[gi] * fontSize);
        }

        var glyphRun = new GlyphRun(
            gt,
            bidiLevel:      0,
            isSideways:     false,
            renderingEmSize: fontSize,
            pixelsPerDip:   pixelsPerDip,
            glyphIndices:   glyphIndices,
            baselineOrigin: new Point(x, baselineY),
            advanceWidths:  advanceWidths,
            glyphOffsets:   null,
            characters:     null,
            deviceFontName: null,
            clusterMap:     null,
            caretStops:     null,
            language:       null);

        dc.DrawGlyphRun(brush, glyphRun);
    }

    // ── TypefaceEqualityComparer ───────────────────────────────────────────

    private sealed class TypefaceEqualityComparer : IEqualityComparer<Typeface>
    {
        public static readonly TypefaceEqualityComparer Instance = new();

        private TypefaceEqualityComparer() { }

        public bool Equals(Typeface? x, Typeface? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return x.FontFamily.Source == y.FontFamily.Source
                && x.Style   == y.Style
                && x.Weight  == y.Weight
                && x.Stretch == y.Stretch;
        }

        public int GetHashCode(Typeface obj)
            => HashCode.Combine(obj.FontFamily.Source, obj.Style, obj.Weight, obj.Stretch);
    }
}
