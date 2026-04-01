// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ChatMarkdownRenderer.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Converts markdown text to WPF UIElements for rich chat display.
//     Handles: bold, italic, inline code, fenced code blocks with syntax
//     highlighting, headings, lists, hyperlinks, bare URLs, file paths.
// ==========================================================
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;

internal static class ChatMarkdownRenderer
{
    // ── Regex patterns ───────────────────────────────────────────────────

    private static readonly Regex s_fencedBlock =
        new(@"```(\w*)\r?\n(.*?)\r?\n```", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex s_mdLink =
        new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

    private static readonly Regex s_bareUrl =
        new(@"(?<!\[.*?\]\()https?://[^\s""'<>\)]+", RegexOptions.Compiled);

    private static readonly Regex s_filePath =
        new(@"(?<![""'\w])([A-Z]:\\[\w\\.\-]+\.\w{1,10})", RegexOptions.Compiled);

    /// <summary>Event raised when a file path is clicked in a message.</summary>
    public static event Action<string>? FilePathClicked;

    // ── Public API ───────────────────────────────────────────────────────

    public static IReadOnlyList<UIElement> Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [BuildParagraph("")];

        var elements = new List<UIElement>();
        int pos = 0;

        foreach (Match m in s_fencedBlock.Matches(markdown))
        {
            if (m.Index > pos)
                RenderParagraphs(markdown[pos..m.Index], elements);

            var lang = m.Groups[1].Value;
            var code = m.Groups[2].Value;
            elements.Add(BuildCodeBlock(code, string.IsNullOrEmpty(lang) ? null : lang));
            pos = m.Index + m.Length;
        }

        if (pos < markdown.Length)
            RenderParagraphs(markdown[pos..], elements);

        return elements;
    }

    // ── Paragraph renderer ───────────────────────────────────────────────

    private static void RenderParagraphs(string text, List<UIElement> elements)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Heading: ## text
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                var tb = BuildInlineTextBlock(line[4..]);
                tb.FontSize = 13;
                tb.FontWeight = FontWeights.SemiBold;
                tb.Margin = new Thickness(0, 6, 0, 2);
                elements.Add(tb);
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                var tb = BuildInlineTextBlock(line[3..]);
                tb.FontSize = 14;
                tb.FontWeight = FontWeights.Bold;
                tb.Margin = new Thickness(0, 8, 0, 2);
                elements.Add(tb);
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                var tb = BuildInlineTextBlock(line[2..]);
                tb.FontSize = 15;
                tb.FontWeight = FontWeights.Bold;
                tb.Margin = new Thickness(0, 10, 0, 4);
                elements.Add(tb);
            }
            // Unordered list
            else if (line.StartsWith("- ", StringComparison.Ordinal) ||
                     line.StartsWith("* ", StringComparison.Ordinal))
            {
                var tb = BuildInlineTextBlock("  \u2022 " + line[2..]);
                tb.Margin = new Thickness(4, 1, 0, 1);
                elements.Add(tb);
            }
            // Ordered list
            else if (line.Length > 2 && char.IsDigit(line[0]) && line.IndexOf(". ", StringComparison.Ordinal) is > 0 and int dotIdx && dotIdx < 4)
            {
                var tb = BuildInlineTextBlock("  " + line);
                tb.Margin = new Thickness(4, 1, 0, 1);
                elements.Add(tb);
            }
            else
            {
                elements.Add(BuildInlineTextBlock(line));
            }
        }
    }

    // ── Inline TextBlock builder ─────────────────────────────────────────

    private static TextBlock BuildParagraph(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12.5,
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");
        return tb;
    }

    private static TextBlock BuildInlineTextBlock(string markdown)
    {
        var tb = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12.5,
            Margin = new Thickness(0, 0, 0, 2),
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");

        foreach (var inline in ParseInlines(markdown))
            tb.Inlines.Add(inline);

        return tb;
    }

    // ── Inline parser ────────────────────────────────────────────────────

    private static IEnumerable<Inline> ParseInlines(string text)
    {
        int i = 0;
        var sb = new StringBuilder();

        while (i < text.Length)
        {
            // Bold: **text**
            if (TryMatchDelim(text, i, "**", out int end))
            {
                if (sb.Length > 0) { foreach (var r in EmitTextWithLinks(sb.ToString())) yield return r; sb.Clear(); }
                var inner = text[(i + 2)..end];
                yield return new Bold(new Run(inner));
                i = end + 2;
                continue;
            }

            // Italic: *text* (single, not double)
            if (TryMatchItalic(text, i, out end))
            {
                if (sb.Length > 0) { foreach (var r in EmitTextWithLinks(sb.ToString())) yield return r; sb.Clear(); }
                var inner = text[(i + 1)..end];
                yield return new Italic(new Run(inner));
                i = end + 1;
                continue;
            }

            // Inline code: `code`
            if (text[i] == '`')
            {
                var close = text.IndexOf('`', i + 1);
                if (close > i)
                {
                    if (sb.Length > 0) { foreach (var r in EmitTextWithLinks(sb.ToString())) yield return r; sb.Clear(); }
                    yield return BuildInlineCode(text[(i + 1)..close]);
                    i = close + 1;
                    continue;
                }
            }

            // Markdown link: [text](url)
            if (text[i] == '[')
            {
                var lm = s_mdLink.Match(text, i);
                if (lm.Success && lm.Index == i)
                {
                    if (sb.Length > 0) { foreach (var r in EmitTextWithLinks(sb.ToString())) yield return r; sb.Clear(); }
                    yield return BuildHyperlink(lm.Groups[1].Value, lm.Groups[2].Value);
                    i += lm.Length;
                    continue;
                }
            }

            sb.Append(text[i++]);
        }

        if (sb.Length > 0)
            foreach (var r in EmitTextWithLinks(sb.ToString())) yield return r;
    }

    /// <summary>Emits Runs with bare URLs and file paths converted to Hyperlinks.</summary>
    private static IEnumerable<Inline> EmitTextWithLinks(string text)
    {
        // Merge URL and file path matches, process in order
        var matches = new List<(int Index, int Length, string Url, string Display, bool IsFile)>();

        foreach (Match m in s_bareUrl.Matches(text))
            matches.Add((m.Index, m.Length, m.Value, m.Value, false));
        foreach (Match m in s_filePath.Matches(text))
        {
            // Skip if overlapping with a URL match
            if (matches.Any(u => m.Index >= u.Index && m.Index < u.Index + u.Length))
                continue;
            matches.Add((m.Index, m.Length, m.Value, m.Value, true));
        }

        matches.Sort((a, b) => a.Index.CompareTo(b.Index));

        int pos = 0;
        foreach (var (idx, len, url, display, isFile) in matches)
        {
            if (idx > pos)
                yield return new Run(text[pos..idx]);

            if (isFile)
                yield return BuildFileLink(display);
            else
                yield return BuildHyperlink(display, url);

            pos = idx + len;
        }

        if (pos < text.Length)
            yield return new Run(text[pos..]);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static bool TryMatchDelim(string text, int start, string delim, out int closeStart)
    {
        closeStart = -1;
        if (start + delim.Length >= text.Length) return false;
        if (text[start..(start + delim.Length)] != delim) return false;

        var close = text.IndexOf(delim, start + delim.Length, StringComparison.Ordinal);
        if (close <= start) return false;

        closeStart = close;
        return true;
    }

    private static bool TryMatchItalic(string text, int start, out int closeStart)
    {
        closeStart = -1;
        var ch = text[start];
        if (ch != '*' && ch != '_') return false;
        if (start + 1 < text.Length && text[start + 1] == ch) return false;

        var close = text.IndexOf(ch, start + 1);
        if (close > start + 1 && (close + 1 >= text.Length || text[close + 1] != ch))
        {
            closeStart = close;
            return true;
        }
        return false;
    }

    // ── Inline code ──────────────────────────────────────────────────────

    private static Inline BuildInlineCode(string code)
    {
        var tb = new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Cascadia Code,Consolas,Courier New"),
            FontSize = 11.5,
            Padding = new Thickness(4, 1, 4, 1),
        };
        tb.SetResourceReference(TextBlock.BackgroundProperty, "CA_CodeBlockBackgroundBrush");
        tb.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");

        return new InlineUIContainer(tb) { BaselineAlignment = BaselineAlignment.Center };
    }

    // ── Hyperlink ────────────────────────────────────────────────────────

    private static Inline BuildHyperlink(string text, string url)
    {
        var run = new Run(text);
        var link = new Hyperlink(run);
        link.SetResourceReference(Hyperlink.ForegroundProperty, "CA_AccentBrandingBrush");
        link.TextDecorations = null;

        try { link.NavigateUri = new Uri(url); }
        catch { return new Run(text); }

        link.RequestNavigate += (_, e) =>
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch { }
            e.Handled = true;
        };

        return link;
    }

    private static Inline BuildFileLink(string path)
    {
        var run = new Run(path);
        var link = new Hyperlink(run)
        {
            Cursor = Cursors.Hand,
            TextDecorations = TextDecorations.Underline,
        };
        link.SetResourceReference(Hyperlink.ForegroundProperty, "CA_AccentBrandingBrush");
        link.Click += (_, _) => FilePathClicked?.Invoke(path);
        return link;
    }

    // ── Fenced code block ────────────────────────────────────────────────

    private static UIElement BuildCodeBlock(string code, string? language)
    {
        var outerBorder = new Border
        {
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 4, 0, 4),
            BorderThickness = new Thickness(1),
        };
        outerBorder.SetResourceReference(Border.BackgroundProperty, "CA_CodeBlockBackgroundBrush");
        outerBorder.SetResourceReference(Border.BorderBrushProperty, "DockBorderBrush");

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // ── Header row: language + copy button ───────────────────────────
        var header = new DockPanel { Margin = new Thickness(10, 6, 6, 4) };

        if (!string.IsNullOrEmpty(language))
        {
            var langLabel = new TextBlock
            {
                Text = language,
                FontSize = 10.5,
                Opacity = 0.6,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Cascadia Code,Consolas,Courier New"),
            };
            langLabel.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");
            header.Children.Add(langLabel);
        }

        var trimmedCode = code.Trim();

        var copyBtn = new Button
        {
            Content = "\uE8C8",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12,
            Width = 28, Height = 22,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Copy code",
            HorizontalAlignment = HorizontalAlignment.Right,
            Opacity = 0.6,
        };
        copyBtn.SetResourceReference(Button.BackgroundProperty, "CA_CodeBlockBackgroundBrush");
        copyBtn.SetResourceReference(Button.ForegroundProperty, "CA_MessageForegroundBrush");
        copyBtn.Click += (sender, _) =>
        {
            Clipboard.SetText(trimmedCode);
            if (sender is Button btn)
            {
                btn.Content = "\uE73E"; // checkmark
                btn.ToolTip = "Copied!";
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (_, _) =>
                {
                    btn.Content = "\uE8C8";
                    btn.ToolTip = "Copy code";
                    timer.Stop();
                };
                timer.Start();
            }
        };
        DockPanel.SetDock(copyBtn, Dock.Right);
        header.Children.Insert(0, copyBtn);

        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // ── Code content with syntax highlighting ────────────────────────
        var codePanel = new StackPanel { Margin = new Thickness(10, 0, 10, 8) };

        var lines = trimmedCode.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var tb = new TextBlock
            {
                FontFamily = new FontFamily("Cascadia Code,Consolas,Courier New"),
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0),
            };

            if (!string.IsNullOrEmpty(language))
            {
                foreach (var run in SyntaxHighlight(line, language))
                    tb.Inlines.Add(run);
            }
            else
            {
                tb.Text = line;
                tb.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");
            }

            codePanel.Children.Add(tb);
        }

        Grid.SetRow(codePanel, 1);
        grid.Children.Add(codePanel);

        outerBorder.Child = grid;
        return outerBorder;
    }

    // ── Simple syntax highlighting ───────────────────────────────────────

    private static IEnumerable<Run> SyntaxHighlight(string line, string language)
    {
        var tokens = Tokenize(line, language);
        if (tokens.Count == 0)
        {
            var r = new Run(line);
            r.SetResourceReference(Run.ForegroundProperty, "CA_MessageForegroundBrush");
            yield return r;
            yield break;
        }

        int pos = 0;
        foreach (var (start, length, kind) in tokens)
        {
            if (start > pos)
            {
                var plain = new Run(line[pos..start]);
                plain.SetResourceReference(Run.ForegroundProperty, "CA_MessageForegroundBrush");
                yield return plain;
            }

            var run = new Run(line[start..(start + length)]);
            var brush = kind switch
            {
                TokenKind.Keyword => "#569CD6",
                TokenKind.String => "#CE9178",
                TokenKind.Number => "#B5CEA8",
                TokenKind.Comment => "#6A9955",
                TokenKind.Type => "#4EC9B0",
                TokenKind.Attribute => "#9CDCFE",
                TokenKind.Operator => "#D4D4D4",
                TokenKind.Bracket => "#FFD700",
                _ => null
            };

            if (brush != null)
                run.Foreground = (Brush)new BrushConverter().ConvertFromString(brush)!;
            else
                run.SetResourceReference(Run.ForegroundProperty, "CA_MessageForegroundBrush");

            yield return run;
            pos = start + length;
        }

        if (pos < line.Length)
        {
            var rest = new Run(line[pos..]);
            rest.SetResourceReference(Run.ForegroundProperty, "CA_MessageForegroundBrush");
            yield return rest;
        }
    }

    private enum TokenKind { Keyword, String, Number, Comment, Type, Attribute, Operator, Bracket }

    private static readonly Dictionary<string, Regex> s_commentPatterns = new()
    {
        ["csharp"] = new(@"//.*$", RegexOptions.Compiled),
        ["c#"] = new(@"//.*$", RegexOptions.Compiled),
        ["javascript"] = new(@"//.*$", RegexOptions.Compiled),
        ["js"] = new(@"//.*$", RegexOptions.Compiled),
        ["typescript"] = new(@"//.*$", RegexOptions.Compiled),
        ["ts"] = new(@"//.*$", RegexOptions.Compiled),
        ["python"] = new(@"#.*$", RegexOptions.Compiled),
        ["py"] = new(@"#.*$", RegexOptions.Compiled),
        ["java"] = new(@"//.*$", RegexOptions.Compiled),
        ["go"] = new(@"//.*$", RegexOptions.Compiled),
        ["rust"] = new(@"//.*$", RegexOptions.Compiled),
        ["bash"] = new(@"#.*$", RegexOptions.Compiled),
        ["sh"] = new(@"#.*$", RegexOptions.Compiled),
        ["sql"] = new(@"--.*$", RegexOptions.Compiled),
        ["xml"] = new(@"<!--.*?-->", RegexOptions.Compiled),
        ["json"] = new(@"$^", RegexOptions.Compiled), // JSON has no comments
    };

    private static readonly Dictionary<string, string[]> s_keywords = new()
    {
        ["csharp"] = ["abstract","as","async","await","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object","operator","out","override","params","private","protected","public","readonly","record","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using","var","virtual","void","volatile","when","where","while","yield"],
        ["c#"] = ["abstract","as","async","await","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object","operator","out","override","params","private","protected","public","readonly","record","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using","var","virtual","void","volatile","when","where","while","yield"],
        ["javascript"] = ["async","await","break","case","catch","class","const","continue","debugger","default","delete","do","else","export","extends","false","finally","for","from","function","if","import","in","instanceof","let","new","null","of","return","static","super","switch","this","throw","true","try","typeof","undefined","var","void","while","with","yield"],
        ["js"] = ["async","await","break","case","catch","class","const","continue","debugger","default","delete","do","else","export","extends","false","finally","for","from","function","if","import","in","instanceof","let","new","null","of","return","static","super","switch","this","throw","true","try","typeof","undefined","var","void","while","with","yield"],
        ["typescript"] = ["abstract","any","as","async","await","boolean","break","case","catch","class","const","continue","declare","default","delete","do","else","enum","export","extends","false","finally","for","from","function","if","implements","import","in","instanceof","interface","let","new","null","number","of","private","protected","public","readonly","return","static","string","super","switch","this","throw","true","try","type","typeof","undefined","var","void","while","with","yield"],
        ["ts"] = ["abstract","any","as","async","await","boolean","break","case","catch","class","const","continue","declare","default","delete","do","else","enum","export","extends","false","finally","for","from","function","if","implements","import","in","instanceof","interface","let","new","null","number","of","private","protected","public","readonly","return","static","string","super","switch","this","throw","true","try","type","typeof","undefined","var","void","while","with","yield"],
        ["python"] = ["False","None","True","and","as","assert","async","await","break","class","continue","def","del","elif","else","except","finally","for","from","global","if","import","in","is","lambda","nonlocal","not","or","pass","raise","return","try","while","with","yield"],
        ["py"] = ["False","None","True","and","as","assert","async","await","break","class","continue","def","del","elif","else","except","finally","for","from","global","if","import","in","is","lambda","nonlocal","not","or","pass","raise","return","try","while","with","yield"],
        ["java"] = ["abstract","assert","boolean","break","byte","case","catch","char","class","const","continue","default","do","double","else","enum","extends","false","final","finally","float","for","goto","if","implements","import","instanceof","int","interface","long","native","new","null","package","private","protected","public","return","short","static","strictfp","super","switch","synchronized","this","throw","throws","transient","true","try","void","volatile","while"],
        ["go"] = ["break","case","chan","const","continue","default","defer","else","fallthrough","for","func","go","goto","if","import","interface","map","package","range","return","select","struct","switch","type","var","true","false","nil"],
        ["rust"] = ["as","async","await","break","const","continue","crate","dyn","else","enum","extern","false","fn","for","if","impl","in","let","loop","match","mod","move","mut","pub","ref","return","self","Self","static","struct","super","trait","true","type","unsafe","use","where","while"],
        ["sql"] = ["SELECT","FROM","WHERE","AND","OR","NOT","INSERT","INTO","VALUES","UPDATE","SET","DELETE","CREATE","TABLE","ALTER","DROP","INDEX","JOIN","INNER","LEFT","RIGHT","OUTER","ON","GROUP","BY","ORDER","ASC","DESC","HAVING","LIMIT","OFFSET","UNION","ALL","DISTINCT","AS","NULL","IS","IN","LIKE","BETWEEN","EXISTS","CASE","WHEN","THEN","ELSE","END"],
        ["bash"] = ["if","then","else","elif","fi","for","while","do","done","case","esac","in","function","return","local","export","source","echo","exit"],
        ["sh"] = ["if","then","else","elif","fi","for","while","do","done","case","esac","in","function","return","local","export","source","echo","exit"],
    };

    private static readonly Regex s_string = new(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", RegexOptions.Compiled);
    private static readonly Regex s_number = new(@"\b\d+(?:\.\d+)?(?:f|d|m|L|u|UL)?\b", RegexOptions.Compiled);
    private static readonly Regex s_word = new(@"\b[A-Za-z_]\w*\b", RegexOptions.Compiled);

    private static List<(int Start, int Length, TokenKind Kind)> Tokenize(string line, string language)
    {
        var tokens = new List<(int Start, int Length, TokenKind Kind)>();
        var lang = language.ToLowerInvariant();

        // Comments first (highest priority)
        if (s_commentPatterns.TryGetValue(lang, out var commentRx))
        {
            var cm = commentRx.Match(line);
            if (cm.Success)
            {
                // Tokenize before comment
                TokenizeContent(line[..cm.Index], lang, tokens);
                tokens.Add((cm.Index, cm.Length, TokenKind.Comment));
                return tokens;
            }
        }

        TokenizeContent(line, lang, tokens);
        return tokens;
    }

    private static void TokenizeContent(string text, string lang, List<(int Start, int Length, TokenKind Kind)> tokens)
    {
        // Strings
        foreach (Match m in s_string.Matches(text))
            tokens.Add((m.Index, m.Length, TokenKind.String));

        // Numbers (not inside strings)
        foreach (Match m in s_number.Matches(text))
        {
            if (!tokens.Any(t => m.Index >= t.Start && m.Index < t.Start + t.Length))
                tokens.Add((m.Index, m.Length, TokenKind.Number));
        }

        // Keywords
        if (s_keywords.TryGetValue(lang, out var kws))
        {
            foreach (Match m in s_word.Matches(text))
            {
                if (tokens.Any(t => m.Index >= t.Start && m.Index < t.Start + t.Length))
                    continue;

                if (kws.Contains(m.Value))
                    tokens.Add((m.Index, m.Length, TokenKind.Keyword));
                else if (m.Value.Length > 1 && char.IsUpper(m.Value[0]))
                    tokens.Add((m.Index, m.Length, TokenKind.Type));
            }
        }

        tokens.Sort((a, b) => a.Start.CompareTo(b.Start));
    }
}
