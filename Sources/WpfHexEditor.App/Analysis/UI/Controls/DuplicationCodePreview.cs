// ==========================================================
// Project: WpfHexEditor.App
// File: Analysis/UI/Controls/DuplicationCodePreview.cs
// Description: Side-by-side preview of a clone group's two occurrences with
//              line numbers, monospace text, and a whitespace-only diff
//              highlight. Reads files on demand, caches by (path, mtime).
// Architecture Notes:
//     - FrameworkElement with a single DrawingVisual: each render rebuilds
//       FormattedText for visible lines only (capped via MaxLinesShown).
//     - File cache: bounded LRU (20 entries) keyed by (path, mtime, length).
//     - Theme-aware: pulls foreground from inherited TextElement.Foreground;
//       background tints from DynamicResource theme brushes resolved once
//       per render via Application.Current.FindResource.
// ==========================================================

using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfHexEditor.App.Analysis.UI.ViewModels;

namespace WpfHexEditor.App.Analysis.UI.Controls;

public sealed class DuplicationCodePreview : FrameworkElement
{
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(nameof(Group), typeof(DuplicationGroupViewModel), typeof(DuplicationCodePreview),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public DuplicationGroupViewModel? Group
    {
        get => (DuplicationGroupViewModel?)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    private const int MaxLinesShown = 200;
    private const double LineHeight = 16;
    private const double GutterWidth = 56;
    private const double FontSize = 11;

    private static readonly Typeface MonoFace = new("Consolas");

    // ── File cache (bounded LRU) ─────────────────────────────────────────────

    private sealed record CacheEntry(string[] Lines, DateTime Mtime, long Length);
    private static readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<string> _lru = new();
    private const int MaxCacheEntries = 20;

    private static string[]? ReadFileCached(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var info = new FileInfo(path);
            if (_cache.TryGetValue(path, out var hit)
             && hit.Mtime == info.LastWriteTimeUtc
             && hit.Length == info.Length)
            {
                _lru.Remove(path); _lru.AddFirst(path);
                return hit.Lines;
            }
            var lines = File.ReadAllLines(path);
            _cache[path] = new CacheEntry(lines, info.LastWriteTimeUtc, info.Length);
            _lru.AddFirst(path);
            while (_lru.Count > MaxCacheEntries)
            {
                var oldest = _lru.Last!.Value;
                _lru.RemoveLast();
                _cache.Remove(oldest);
            }
            return lines;
        }
        catch { return null; }
    }

    // ── Render ───────────────────────────────────────────────────────────────

    public DuplicationCodePreview()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding   = true;
        MinHeight = 120;
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Group is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        var occA = Group.OccurrenceA;
        var occB = Group.OccurrenceB;
        if (occA is null) return;

        double half = (ActualWidth - 8) / 2;
        var fg = TextElement.GetForeground(this) ?? SystemColors.ControlTextBrush;
        var gutterFg = new SolidColorBrush(((SolidColorBrush)Brushes.Gray).Color) { Opacity = 0.6 };
        gutterFg.Freeze();

        var (linesA, snippetA) = LoadSnippet(occA);
        DrawPane(dc, 0, half, ActualHeight, linesA, snippetA, fg, gutterFg,
                 occA.StartLine, occA.EndLine, otherSnippet: occB is null ? null : LoadSnippet(occB).snippet);

        if (occB is not null)
        {
            var (linesB, snippetB) = LoadSnippet(occB);
            DrawPane(dc, half + 8, half, ActualHeight, linesB, snippetB, fg, gutterFg,
                     occB.StartLine, occB.EndLine, otherSnippet: snippetA);
        }
    }

    private static (string[] all, string[] snippet) LoadSnippet(Models.DuplicationOccurrence occ)
    {
        var all = ReadFileCached(occ.FilePath) ?? [];
        if (all.Length == 0) return (all, []);
        int from = Math.Max(0, occ.StartLine - 1);
        int to   = Math.Min(all.Length - 1, occ.EndLine - 1);
        int count = Math.Min(MaxLinesShown, Math.Max(0, to - from + 1));
        var snippet = new string[count];
        for (int i = 0; i < count; i++) snippet[i] = all[from + i];
        return (all, snippet);
    }

    private static void DrawPane(
        DrawingContext dc, double x, double width, double height,
        string[] allFile, string[] snippet,
        Brush fg, Brush gutterFg,
        int startLine, int endLine,
        string[]? otherSnippet)
    {
        double y = 4;
        var rect = new Rect(x, 0, width, height);
        dc.PushClip(new RectangleGeometry(rect));

        var sameBg  = (Application.Current?.TryFindResource("CodeAnalysis_Dup_SameBg")  as Brush)
                      ?? new SolidColorBrush(Color.FromArgb(28, 60, 200, 100));
        var wsBg    = (Application.Current?.TryFindResource("CodeAnalysis_Dup_WsBg")    as Brush)
                      ?? new SolidColorBrush(Color.FromArgb(28, 220, 180, 0));
        if (sameBg.CanFreeze) sameBg.Freeze();
        if (wsBg.CanFreeze)   wsBg.Freeze();

        for (int i = 0; i < snippet.Length; i++)
        {
            if (y + LineHeight > height - 4) break;

            // Diff-light: compare normalized line with the other pane at same index
            Brush? rowBg = null;
            if (otherSnippet is not null && i < otherSnippet.Length)
            {
                var a = snippet[i];
                var b = otherSnippet[i];
                if (string.Equals(a, b, StringComparison.Ordinal)) rowBg = sameBg;
                else if (string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal)) rowBg = wsBg;
            }
            if (rowBg is not null)
                dc.DrawRectangle(rowBg, null, new Rect(x, y, width, LineHeight));

            // Gutter line number
            int lineNo = startLine + i;
            var gutterFt = new FormattedText(lineNo.ToString(), System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, MonoFace, FontSize, gutterFg, 1.0)
            { TextAlignment = TextAlignment.Right, MaxTextWidth = GutterWidth - 8 };
            dc.DrawText(gutterFt, new Point(x + 4, y));

            // Code text
            string text = snippet[i] ?? string.Empty;
            text = text.Replace("\t", "    ");
            var codeFt = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, MonoFace, FontSize, fg, 1.0)
            { MaxTextWidth = Math.Max(20, width - GutterWidth - 8) };
            dc.DrawText(codeFt, new Point(x + GutterWidth, y));

            y += LineHeight;
        }

        if (snippet.Length > MaxLinesShown)
        {
            var footer = new FormattedText($"… +{snippet.Length - MaxLinesShown} lines",
                System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                MonoFace, FontSize, gutterFg, 1.0);
            dc.DrawText(footer, new Point(x + GutterWidth, y + 4));
        }

        dc.Pop();
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder(s.Length);
        bool prevSpace = false;
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace && sb.Length > 0) sb.Append(' ');
                prevSpace = true;
            }
            else { sb.Append(ch); prevSpace = false; }
        }
        return sb.ToString().Trim();
    }
}
