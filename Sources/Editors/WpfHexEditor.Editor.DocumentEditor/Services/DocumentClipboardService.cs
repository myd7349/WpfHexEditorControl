// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Services/DocumentClipboardService.cs
// Description:
//     Rich Copy/Paste: writes plain-text + HTML + RTF payloads
//     onto the system clipboard so external apps (Word, browser,
//     mail) receive formatted text, and reads RTF/HTML back when
//     pasting from those apps.
// Architecture notes:
//     Plain text is always written (lowest-common denominator).
//     HTML uses the CF_HTML descriptor preamble required by
//     Windows clipboard. RTF reuses RtfSchemaEngine driven by
//     the embedded RTF.whfmt — same path as RtfDocumentSaver,
//     so output is consistent with file-level Save As.
// ==========================================================

using System.Globalization;
using System.Text;
using System.Windows;
using WpfHexEditor.Core.Definitions;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;
using WpfHexEditor.Editor.DocumentEditor.Core.Schema;

namespace WpfHexEditor.Editor.DocumentEditor.Services;

/// <summary>
/// Builds and consumes rich clipboard payloads (plain / HTML / RTF) for
/// DocumentEditor copy and paste operations.
/// </summary>
public static class DocumentClipboardService
{
    private const string CfHtml = "HTML Format";
    private const string CfRtf  = "Rich Text Format";

    /// <summary>
    /// Copies <paramref name="blocks"/> + <paramref name="plainText"/> onto the
    /// system clipboard with three formats: plain text, HTML, and RTF.
    /// </summary>
    public static void CopyRich(IReadOnlyList<DocumentBlock> blocks, string plainText)
    {
        var data = new DataObject();

        // Always include plain text.
        data.SetText(plainText ?? string.Empty);

        if (blocks is { Count: > 0 })
        {
            string html = BuildHtmlFragment(blocks);
            if (!string.IsNullOrEmpty(html))
                data.SetData(CfHtml, WrapCfHtml(html));

            string rtf = BuildRtf(blocks);
            if (!string.IsNullOrEmpty(rtf))
                data.SetData(CfRtf, rtf);
        }

        try { Clipboard.SetDataObject(data, copy: true); }
        catch (System.Runtime.InteropServices.COMException) { /* clipboard busy — best-effort */ }
    }

    /// <summary>
    /// Returns the richest available clipboard payload as plain text. For now
    /// we fall back to <see cref="Clipboard.GetText()"/>; future revisions may
    /// re-parse RTF/HTML back into <see cref="DocumentBlock"/> trees.
    /// </summary>
    public static string GetTextFromClipboard()
    {
        try
        {
            // Prefer HTML body when present — strip tags for now.
            if (Clipboard.ContainsData(CfHtml))
            {
                var raw = Clipboard.GetData(CfHtml) as string;
                var body = ExtractHtmlBody(raw);
                if (!string.IsNullOrEmpty(body)) return StripHtmlTags(body);
            }
            if (Clipboard.ContainsText()) return Clipboard.GetText();
        }
        catch (System.Runtime.InteropServices.COMException) { /* clipboard busy */ }
        return string.Empty;
    }

    // ── HTML fragment ──────────────────────────────────────────────────────

    private static string BuildHtmlFragment(IReadOnlyList<DocumentBlock> blocks)
    {
        var sb = new StringBuilder();
        foreach (var b in blocks) AppendHtmlBlock(sb, b);
        return sb.ToString();
    }

    private static void AppendHtmlBlock(StringBuilder sb, DocumentBlock b)
    {
        string tag = b.Kind switch
        {
            "heading"     => HeadingTag(b),
            "list-item"   => "li",
            "table"       => "table",
            "table-row"   => "tr",
            "table-cell"  => "td",
            "hyperlink"   => "a",
            "image"       => "img",
            _             => "p"   // paragraph / run / structured-tag
        };

        if (b.Kind == "run")
        {
            sb.Append(WrapInlineStyles(b, HtmlEscape(b.Text)));
            return;
        }

        if (b.Kind == "image")
        {
            sb.Append("<img alt=\"image\"/>");
            return;
        }

        sb.Append('<').Append(tag);
        AppendStyleAttr(sb, b);
        sb.Append('>');

        if (b.Children.Count == 0)
            sb.Append(HtmlEscape(b.Text));
        else
            foreach (var c in b.Children) AppendHtmlBlock(sb, c);

        sb.Append("</").Append(tag).Append('>');
    }

    private static string HeadingTag(DocumentBlock b)
    {
        int level = b.Attributes.TryGetValue("level", out var l) && l is int li
            ? Math.Clamp(li, 1, 6) : 1;
        return "h" + level;
    }

    private static void AppendStyleAttr(StringBuilder sb, DocumentBlock b)
    {
        var style = BuildCssStyle(b);
        if (!string.IsNullOrEmpty(style))
            sb.Append(" style=\"").Append(style).Append('"');
    }

    private static string WrapInlineStyles(DocumentBlock run, string innerHtml)
    {
        var style = BuildCssStyle(run);
        if (string.IsNullOrEmpty(style)) return "<span>" + innerHtml + "</span>";
        return "<span style=\"" + style + "\">" + innerHtml + "</span>";
    }

    private static string BuildCssStyle(DocumentBlock b)
    {
        var sb = new StringBuilder();
        if (b.Attributes.TryGetValue("fontFamily", out var ff) && ff is string s)
            sb.Append("font-family:").Append(s).Append(';');
        if (b.Attributes.TryGetValue("fontSize", out var fs))
        {
            double pt = fs switch
            {
                double d => d,
                int    i => i,
                _        => 0
            };
            if (pt > 0) sb.Append("font-size:").Append(pt.ToString(CultureInfo.InvariantCulture)).Append("pt;");
        }
        if (b.Attributes.TryGetValue("bold",      out var bo) && bo is true) sb.Append("font-weight:bold;");
        if (b.Attributes.TryGetValue("italic",    out var it) && it is true) sb.Append("font-style:italic;");
        if (b.Attributes.TryGetValue("underline", out var un) && un is true) sb.Append("text-decoration:underline;");
        if (b.Attributes.TryGetValue("color",     out var c)  && c  is string cs) sb.Append("color:").Append(cs).Append(';');
        return sb.ToString();
    }

    private static string HtmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>
    /// Wraps an HTML fragment in the CF_HTML descriptor envelope expected by
    /// Windows clipboard consumers. Offsets are computed after the header is built.
    /// </summary>
    private static string WrapCfHtml(string fragmentHtml)
    {
        const string header =
            "Version:0.9\r\n" +
            "StartHTML:{0:D10}\r\n" +
            "EndHTML:{1:D10}\r\n" +
            "StartFragment:{2:D10}\r\n" +
            "EndFragment:{3:D10}\r\n";

        const string startMarker = "<!--StartFragment-->";
        const string endMarker   = "<!--EndFragment-->";

        string html =
            "<html><body>" + startMarker + fragmentHtml + endMarker + "</body></html>";

        int headerLen     = string.Format(header, 0, 0, 0, 0).Length;
        int startHtml     = headerLen;
        int startFragment = startHtml + html.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
        int endFragment   = startHtml + html.IndexOf(endMarker,   StringComparison.Ordinal);
        int endHtml       = startHtml + html.Length;

        return string.Format(header, startHtml, endHtml, startFragment, endFragment) + html;
    }

    private static string ExtractHtmlBody(string? cfHtml)
    {
        if (string.IsNullOrEmpty(cfHtml)) return string.Empty;
        int s = cfHtml.IndexOf("<!--StartFragment-->", StringComparison.Ordinal);
        int e = cfHtml.IndexOf("<!--EndFragment-->",   StringComparison.Ordinal);
        if (s < 0 || e < 0 || e <= s) return cfHtml;
        s += "<!--StartFragment-->".Length;
        return cfHtml[s..e];
    }

    private static string StripHtmlTags(string html)
    {
        // Lightweight: collapse whitespace and drop tags. Block elements become newlines.
        var sb = new StringBuilder(html.Length);
        bool inTag = false;
        foreach (char ch in html)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; sb.Append(' '); continue; }
            if (!inTag) sb.Append(ch);
        }
        return System.Net.WebUtility.HtmlDecode(sb.ToString());
    }

    // ── RTF payload (reuses RtfSchemaEngine + embedded RTF.whfmt) ──────────

    private static string BuildRtf(IReadOnlyList<DocumentBlock> blocks)
    {
        var schema = LoadRtfSchema();
        return schema is not null
            ? RtfSchemaEngine.SerializeBlocks(blocks, schema)
            : FallbackRtf(blocks);
    }

    private static DocumentSchemaDefinition? LoadRtfSchema()
    {
        try
        {
            var catalog = EmbeddedFormatCatalog.Instance;
            var key = catalog.GetAll()
                .Select(e => e.ResourceKey)
                .FirstOrDefault(k => k is not null &&
                    k.EndsWith("RTF.whfmt", StringComparison.OrdinalIgnoreCase));
            if (key is null) return null;
            return DocumentSchemaReader.ReadFromJson(catalog.GetJson(key), "RTF.whfmt");
        }
        catch { return null; }
    }

    private static string FallbackRtf(IReadOnlyList<DocumentBlock> blocks)
    {
        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Times New Roman;}}\f0\fs24 ");
        foreach (var b in blocks)
            sb.Append(@"\pard\plain ").Append(EscapeRtf(b.Text)).Append(@"\par ");
        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeRtf(string text) =>
        text.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
}
