// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: TagFoldingStrategy.cs
// Author: WpfHexEditor Team
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-22
// Updated: 2026-03-22 — ADR-048: Added NormalizeLines() pre-processor for multi-line
//                        tag support (XAML/HTML attributes spanning several lines).
// Description:
//     Tag-based folding for HTML, XML, and XAML documents.
//     Matches open and close tags across lines to build fold regions.
//     When multilineTagSupport=true, attribute continuation lines are merged
//     into a single virtual line before regex matching.
//
// Architecture Notes:
//     Strategy Pattern — implements IFoldingStrategy.
//     Data-driven: self-closing tag list + multilineTagSupport come from
//     FoldingRules deserialized from .whfmt.
// ==========================================================

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WpfHexEditor.Editor.CodeEditor.Models;

namespace WpfHexEditor.Editor.CodeEditor.Folding;

/// <summary>
/// Produces foldable regions by matching XML/HTML opening and closing tags.
/// Self-closing tags (e.g. &lt;br/&gt;, &lt;img/&gt;) are excluded from region creation.
/// When <paramref name="multilineTagSupport"/> is true, attribute continuation lines
/// (lines that are part of an opening tag whose &gt; appears on a later line) are merged
/// into a single virtual line before matching, so multi-line XAML/HTML elements fold correctly.
/// </summary>
internal sealed class TagFoldingStrategy : IFoldingStrategy
{
    private static readonly Regex s_openTag  = new(@"<(?![\!/])(?<tag>[\w:.\-]+)[^>]*(?<!/)>", RegexOptions.Compiled);
    private static readonly Regex s_closeTag = new(@"</(?<tag>[\w:.\-]+)\s*>",                  RegexOptions.Compiled);

    private readonly HashSet<string> _selfClosing;
    private readonly bool            _multilineTagSupport;

    public TagFoldingStrategy(IReadOnlyList<string> selfClosingTags,
                               bool multilineTagSupport = false)
    {
        _selfClosing         = new HashSet<string>(selfClosingTags, StringComparer.OrdinalIgnoreCase);
        _multilineTagSupport = multilineTagSupport;
    }

    public IReadOnlyList<FoldingRegion> Analyze(IReadOnlyList<CodeLine> lines)
    {
        var regions = new List<FoldingRegion>();
        var stack   = new Stack<(int Line, string Tag)>();

        // Use virtual normalized lines when multi-line tag support is on.
        var virtualLines = _multilineTagSupport
            ? NormalizeLines(lines)
            : lines.Select((l, idx) => (ActualLine: idx, Text: l.Text ?? string.Empty)).ToList();

        foreach (var (actualLine, text) in virtualLines)
        {
            foreach (Match m in s_openTag.Matches(text))
            {
                var tag = m.Groups["tag"].Value;
                if (!_selfClosing.Contains(tag))
                    stack.Push((actualLine, tag));
            }

            foreach (Match m in s_closeTag.Matches(text))
            {
                var tag = m.Groups["tag"].Value;
                if (stack.Count > 0
                 && stack.Peek().Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    var (startLine, _) = stack.Pop();
                    if (actualLine > startLine + 1)
                        regions.Add(new FoldingRegion(startLine, actualLine, "{ \u2026 }", FoldingRegionKind.Brace));
                }
            }
        }

        return regions;
    }

    // ── Multi-line pre-processing ─────────────────────────────────────────────

    /// <summary>
    /// Joins continuation lines belonging to the same opening tag into a single
    /// virtual line. Returns a list of (actualStartLine, joinedText) pairs.
    /// <para>
    /// A line "opens" a multi-line tag when it starts with &lt;TagName but does NOT
    /// contain a &gt; before the end of the line. Subsequent lines are accumulated
    /// until the closing &gt; is found, all mapped to the same actual start line.
    /// </para>
    /// </summary>
    private static List<(int ActualLine, string Text)> NormalizeLines(IReadOnlyList<CodeLine> lines)
    {
        var result = new List<(int, string)>(lines.Count);
        int i = 0;

        while (i < lines.Count)
        {
            var text = lines[i].Text ?? string.Empty;

            // Detect an opening tag that hasn't closed on this line yet:
            // starts with '<' (not '</' or '<!--' or '<!') and has no '>'.
            var trimmed = text.TrimStart();
            bool startsUnclosedTag = trimmed.StartsWith('<')
                                  && !trimmed.StartsWith("</")
                                  && !trimmed.StartsWith("<!--")
                                  && !trimmed.StartsWith("<!")
                                  && !text.Contains('>');

            if (startsUnclosedTag)
            {
                int startLine = i;
                var sb = new StringBuilder(text);
                i++;

                // Accumulate continuation lines until we find the > that closes the tag.
                while (i < lines.Count && !sb.ToString().Contains('>'))
                {
                    sb.Append(' ').Append((lines[i].Text ?? string.Empty).TrimStart());
                    i++;
                }

                result.Add((startLine, sb.ToString()));
            }
            else
            {
                result.Add((i, text));
                i++;
            }
        }

        return result;
    }
}
