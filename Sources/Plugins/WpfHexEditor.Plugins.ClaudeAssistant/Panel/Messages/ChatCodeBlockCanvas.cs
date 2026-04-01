// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ChatCodeBlockCanvas.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     GlyphRun/DrawingContext renderer for syntax-highlighted code blocks.
//     Syntax rules driven by .whfmt via LanguageRegistry + SyntaxRuleHighlighter.
//     Never hardcodes language patterns — unknown languages render plain monospace.
// ==========================================================
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.CodeEditor.Helpers;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;

/// <summary>
/// FrameworkElement that renders syntax-highlighted code via DrawingContext/GlyphRun.
/// Language highlighting is driven by whfmt definitions via <see cref="LanguageRegistry"/>.
/// </summary>
internal sealed class ChatCodeBlockCanvas : FrameworkElement
{
    // ── Language alias map (fenced block label → LanguageRegistry ID) ─────
    private static readonly Dictionary<string, string> s_aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c#"] = "csharp",
        ["cs"] = "csharp",
        ["js"] = "javascript",
        ["ts"] = "typescript",
        ["py"] = "python",
        ["sh"] = "bash",
        ["yml"] = "yaml",
        ["md"] = "markdown",
        ["rb"] = "ruby",
        ["rs"] = "rust",
    };

    // ── Rendering state ──────────────────────────────────────────────────
    private static readonly Typeface s_typeface = new("Cascadia Code,Consolas,Courier New");
    private const double FontSize = 12.0;
    private const double LinePadding = 2.0;
    private const double LeftPadding = 10.0;
    private const double TopPadding = 4.0;

    private GlyphTypeface? _gt;
    private double _charW;
    private double _lineH;
    private double _baseline;
    private float _pixelsPerDip = 1f;

    private string _code = "";
    private string? _language;
    private string[]? _lines;
    private ISyntaxHighlighter? _highlighter;
    private Brush? _defaultFg;

    // ── Public properties ────────────────────────────────────────────────

    public string Code
    {
        get => _code;
        set
        {
            _code = value ?? "";
            _lines = _code.Split('\n');
            for (int i = 0; i < _lines.Length; i++)
                _lines[i] = _lines[i].TrimEnd('\r');
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public string? Language
    {
        get => _language;
        set
        {
            _language = value;
            _highlighter = ResolveHighlighter(value);
            InvalidateVisual();
        }
    }

    // ── Highlighter resolution (whfmt-driven) ────────────────────────────

    private static ISyntaxHighlighter? ResolveHighlighter(string? language)
    {
        if (string.IsNullOrEmpty(language)) return null;

        var id = s_aliases.TryGetValue(language, out var mapped) ? mapped : language.ToLowerInvariant();

        try
        {
            var langDef = LanguageRegistry.Instance.FindById(id);
            if (langDef is null || langDef.SyntaxRules.Count == 0) return null;

            // Replicate CodeEditorFactory.BuildHighlighter logic
            var rules = langDef.SyntaxRules.Select(rule => new RegexHighlightRule(
                rule.Pattern,
                TokenKindToBrush(rule.Kind),
                isBold: rule.Kind is SyntaxTokenKind.Keyword,
                isItalic: rule.Kind is SyntaxTokenKind.Comment,
                kind: rule.Kind));

            return new SyntaxRuleHighlighter(
                rules,
                langDef.Name,
                langDef.BlockCommentStart,
                langDef.BlockCommentEnd);
        }
        catch
        {
            return null; // graceful fallback — no highlighting
        }
    }

    private static Brush TokenKindToBrush(SyntaxTokenKind kind)
    {
        var resourceKey = kind switch
        {
            SyntaxTokenKind.Keyword => "CE_Keyword",
            SyntaxTokenKind.String => "CE_String",
            SyntaxTokenKind.Number => "CE_Number",
            SyntaxTokenKind.Comment => "CE_Comment",
            SyntaxTokenKind.Type => "CE_Type",
            SyntaxTokenKind.Identifier => "CE_Identifier",
            SyntaxTokenKind.Operator => "CE_Operator",
            SyntaxTokenKind.Bracket => "CE_Bracket",
            SyntaxTokenKind.Attribute => "CE_Attribute",
            SyntaxTokenKind.ControlFlow => "CE_ControlFlow",
            _ => "CE_Foreground"
        };

        if (Application.Current?.TryFindResource(resourceKey) is Brush b)
            return b;

        // VS Dark fallbacks
        return kind switch
        {
            SyntaxTokenKind.Keyword => Freeze(Color.FromRgb(86, 156, 214)),
            SyntaxTokenKind.String => Freeze(Color.FromRgb(206, 145, 120)),
            SyntaxTokenKind.Number => Freeze(Color.FromRgb(181, 206, 168)),
            SyntaxTokenKind.Comment => Freeze(Color.FromRgb(106, 153, 85)),
            SyntaxTokenKind.Type => Freeze(Color.FromRgb(78, 201, 176)),
            SyntaxTokenKind.ControlFlow => Freeze(Color.FromRgb(197, 134, 192)),
            _ => Freeze(Color.FromRgb(212, 212, 212))
        };

        static Brush Freeze(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    }

    // ── Measure / Arrange ────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureMetrics();
        var lineCount = _lines?.Length ?? 0;
        var height = TopPadding + lineCount * _lineH + TopPadding;
        return new Size(availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    // ── OnRender (DrawingContext) ─────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        if (_lines is null || _lines.Length == 0) return;

        EnsureMetrics();

        _defaultFg = Application.Current?.TryFindResource("CA_MessageForegroundBrush") as Brush
                     ?? Brushes.WhiteSmoke;

        _highlighter?.Reset();

        double y = TopPadding;

        for (int i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i];
            double x = LeftPadding;
            double baselineY = y + _baseline;

            if (_highlighter != null && !string.IsNullOrEmpty(line))
            {
                var tokens = _highlighter.Highlight(line, i);
                if (tokens.Count > 0)
                {
                    int pos = 0;
                    foreach (var token in tokens)
                    {
                        // Gap before token
                        if (token.StartColumn > pos)
                        {
                            var gap = line[pos..token.StartColumn];
                            RenderSegment(dc, gap, x, baselineY, y, _defaultFg);
                            x += MeasureWidth(gap);
                        }

                        // Token
                        RenderSegment(dc, token.Text, x, baselineY, y, token.Foreground);
                        x += MeasureWidth(token.Text);
                        pos = token.StartColumn + token.Length;
                    }

                    // Remainder
                    if (pos < line.Length)
                    {
                        var rest = line[pos..];
                        RenderSegment(dc, rest, x, baselineY, y, _defaultFg);
                    }
                }
                else
                {
                    RenderSegment(dc, line, x, baselineY, y, _defaultFg);
                }
            }
            else if (!string.IsNullOrEmpty(line))
            {
                RenderSegment(dc, line, x, baselineY, y, _defaultFg);
            }

            y += _lineH;
        }
    }

    // ── Rendering helpers ────────────────────────────────────────────────

    private void RenderSegment(DrawingContext dc, string text, double x, double baselineY, double topY, Brush brush)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_gt != null)
        {
            RenderGlyphRun(dc, text, x, baselineY, brush);
        }
        else
        {
            var ft = new FormattedText(text, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, s_typeface, FontSize, brush, _pixelsPerDip);
            dc.DrawText(ft, new Point(x, topY));
        }
    }

    [ThreadStatic] private static List<ushort>? _glyphPool;
    [ThreadStatic] private static List<double>? _advancePool;

    private void RenderGlyphRun(DrawingContext dc, string text, double x, double baselineY, Brush brush)
    {
        var gt = _gt!;
        var glyphIndices = _glyphPool ??= new List<ushort>(256);
        var advanceWidths = _advancePool ??= new List<double>(256);
        glyphIndices.Clear();
        advanceWidths.Clear();

        var charMap = gt.CharacterToGlyphMap;
        foreach (char ch in text)
        {
            if (ch == '\t')
            {
                charMap.TryGetValue(' ', out ushort spaceGi);
                glyphIndices.Add(spaceGi);
                advanceWidths.Add(gt.AdvanceWidths[spaceGi] * FontSize * 4);
                continue;
            }

            if (!charMap.TryGetValue(ch, out ushort gi))
                charMap.TryGetValue('\uFFFD', out gi);

            glyphIndices.Add(gi);
            advanceWidths.Add(gt.AdvanceWidths[gi] * FontSize);
        }

        var glyphRun = new GlyphRun(
            gt, 0, false, FontSize, _pixelsPerDip,
            glyphIndices.ToArray(),
            new Point(x, baselineY),
            advanceWidths.ToArray(),
            null, null, null, null, null, null);

        dc.DrawGlyphRun(brush, glyphRun);
    }

    private double MeasureWidth(string text)
    {
        double w = 0;
        if (_gt != null)
        {
            var charMap = _gt.CharacterToGlyphMap;
            foreach (char ch in text)
            {
                if (ch == '\t')
                {
                    charMap.TryGetValue(' ', out ushort spaceGi);
                    w += _gt.AdvanceWidths[spaceGi] * FontSize * 4;
                }
                else
                {
                    if (!charMap.TryGetValue(ch, out ushort gi))
                        charMap.TryGetValue('M', out gi);
                    w += _gt.AdvanceWidths[gi] * FontSize;
                }
            }
        }
        else
        {
            w = _charW * text.Length;
        }
        return w;
    }

    private void EnsureMetrics()
    {
        if (_lineH > 0) return;

        var source = PresentationSource.FromVisual(this);
        _pixelsPerDip = source != null
            ? (float)(source.CompositionTarget?.TransformToDevice.M22 ?? 1.0)
            : 1f;

        _gt = s_typeface.TryGetGlyphTypeface(out var resolved) ? resolved : null;

        if (_gt != null)
        {
            _gt.CharacterToGlyphMap.TryGetValue('M', out ushort gi);
            _charW = _gt.AdvanceWidths[gi] * FontSize;
            _lineH = _gt.Height * FontSize + LinePadding;
            _baseline = _gt.Baseline * FontSize;
        }
        else
        {
            var ft = new FormattedText("M", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, s_typeface, FontSize, Brushes.White, _pixelsPerDip);
            _charW = ft.Width;
            _lineH = ft.Height + LinePadding;
            _baseline = ft.Height * 0.8;
        }
    }
}
