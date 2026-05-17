// Project      : WpfHexEditor.Editor.CodeEditor
// File         : Snippets/SnippetBodyTokenizer.cs
// Description  : Tokenizes a snippet body string into typed segments
//                (plain text, variable tokens, cursor marker).
// Architecture : Pure string logic — no WPF, no IO. Used by highlight box.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WpfHexEditor.Editor.CodeEditor.Snippets;

public enum SnippetTokenKind { PlainText, Variable, CursorMarker }

public readonly record struct SnippetBodyToken(SnippetTokenKind Kind, string Text);

/// <summary>Splits a snippet body into <see cref="SnippetBodyToken"/> segments.</summary>
public static class SnippetBodyTokenizer
{
    /// <summary>The literal token that marks the final caret position after expansion.</summary>
    public const string CursorMarker = "$cursor";

    private static readonly Regex TokenRegex = new(
        @"(\$cursor|\$\{([A-Za-z_][A-Za-z0-9_]*)\})",
        RegexOptions.Compiled);

    /// <summary>
    /// Returns a flat list of tokens covering every character of <paramref name="body"/>.
    /// Unknown <c>${Name}</c> tokens are returned as <see cref="SnippetTokenKind.Variable"/>.
    /// </summary>
    public static IReadOnlyList<SnippetBodyToken> Tokenize(string body)
    {
        if (string.IsNullOrEmpty(body))
            return [];

        var result = new List<SnippetBodyToken>();
        var pos    = 0;

        foreach (Match m in TokenRegex.Matches(body))
        {
            if (m.Index > pos)
                result.Add(new SnippetBodyToken(SnippetTokenKind.PlainText, body[pos..m.Index]));

            var kind = m.Value == CursorMarker
                ? SnippetTokenKind.CursorMarker
                : SnippetTokenKind.Variable;

            result.Add(new SnippetBodyToken(kind, m.Value));
            pos = m.Index + m.Length;
        }

        if (pos < body.Length)
            result.Add(new SnippetBodyToken(SnippetTokenKind.PlainText, body[pos..]));

        return result;
    }
}
