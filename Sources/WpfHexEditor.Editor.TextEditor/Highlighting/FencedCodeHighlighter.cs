// ==========================================================
// Project: WpfHexEditor.Editor.TextEditor
// File: Highlighting/FencedCodeHighlighter.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-22
// Description:
//     Generic fence-aware contextual highlighter.
//     Reads an embedded-language map from SyntaxDefinition.EmbeddedLanguages
//     (declared in the .whfmt syntaxDefinition block) and delegates per-line
//     highlighting to the correct language's RegexSyntaxHighlighter based on
//     which fenced code block the line belongs to.
//
//     Supported fence delimiters: ``` (3+ backticks) and ~~~ (3+ tildes).
//     Fence delimiter lines themselves are highlighted by the base highlighter
//     so they keep their existing Markdown colors (TE_String / TE_Keyword).
//     Lines inside an unknown fence tag are returned plain (empty span list).
// ==========================================================

using WpfHexEditor.Editor.TextEditor.Services;

namespace WpfHexEditor.Editor.TextEditor.Highlighting;

/// <summary>
/// <see cref="IContextualHighlighter"/> that applies embedded-language syntax
/// highlighting inside fenced code blocks declared in a <c>.whfmt</c> file.
/// </summary>
internal sealed class FencedCodeHighlighter : IContextualHighlighter
{
    // -----------------------------------------------------------------------
    // Fields
    // -----------------------------------------------------------------------

    private readonly RegexSyntaxHighlighter                       _base;
    private readonly Dictionary<string, string>                   _langMap;   // id → extension
    private readonly Dictionary<string, RegexSyntaxHighlighter?>  _langCache; // extension → highlighter (null = not found)

    // Per-line context built by Prepare(): null = outside fence, "" = unknown lang, else = lang id
    private string?[] _lineLanguage = [];

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    internal FencedCodeHighlighter(
        IReadOnlyList<EmbeddedLanguageEntry> entries,
        RegexSyntaxHighlighter baseHighlighter)
    {
        _base      = baseHighlighter;
        _langMap   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _langCache = new Dictionary<string, RegexSyntaxHighlighter?>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
            _langMap[e.Id] = e.Extension;
    }

    // -----------------------------------------------------------------------
    // IContextualHighlighter
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scans all lines and builds the per-line language context array.
    /// Called once per highlight pass before any <see cref="Highlight"/> call.
    /// </summary>
    public void Prepare(IReadOnlyList<string> allLines)
    {
        if (_lineLanguage.Length != allLines.Count)
            _lineLanguage = new string?[allLines.Count];

        string? currentLang = null; // null = outside fence
        string? fenceMarker = null; // "```" or "~~~" — the opening delimiter sequence

        for (int i = 0; i < allLines.Count; i++)
        {
            var line = allLines[i].TrimEnd();

            if (currentLang is null)
            {
                // Look for a fence opening line: ``` or ~~~, optionally followed by a lang tag
                var fence = DetectFenceOpen(line);
                if (fence.HasValue)
                {
                    fenceMarker        = fence.Value.Delimiter;
                    currentLang        = fence.Value.LangId;   // "" for anonymous fences
                    _lineLanguage[i]   = null; // the delimiter line itself is "base" (Markdown colors)
                }
                else
                {
                    _lineLanguage[i] = null; // normal Markdown line
                }
            }
            else
            {
                // Check for matching closing delimiter
                if (IsClosingFence(line, fenceMarker!))
                {
                    _lineLanguage[i] = null; // closing delimiter → base Markdown colors
                    currentLang      = null;
                    fenceMarker      = null;
                }
                else
                {
                    _lineLanguage[i] = currentLang; // "" = unknown lang, else = lang id
                }
            }
        }
    }

    /// <summary>
    /// Returns highlight spans for the given line using context from <see cref="Prepare"/>.
    /// </summary>
    public IReadOnlyList<ColoredSpan> Highlight(string lineText, int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lineLanguage.Length)
            return _base.Highlight(lineText);

        var lang = _lineLanguage[lineIndex];

        // null = outside fence or fence delimiter line → use base Markdown highlighter
        if (lang is null)
            return _base.Highlight(lineText);

        // "" = anonymous or unknown fence language → plain (no colors)
        if (lang.Length == 0)
            return [];

        // Known language — resolve highlighter (lazy, cached per extension)
        var highlighter = ResolveHighlighter(lang);
        return highlighter is not null ? highlighter.Highlight(lineText) : [];
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private readonly record struct FenceOpen(string Delimiter, string LangId);

    /// <summary>
    /// Returns a <see cref="FenceOpen"/> when <paramref name="line"/> is an opening fence.
    /// <c>LangId</c> is the tag text (e.g. "csharp") or <c>""</c> for anonymous fences.
    /// Returns <see langword="null"/> if the line is not a fence opener.
    /// </summary>
    private static FenceOpen? DetectFenceOpen(string line)
    {
        if (line.Length < 3) return null;

        char c = line[0];
        if (c != '`' && c != '~') return null;

        int run = 0;
        while (run < line.Length && line[run] == c) run++;
        if (run < 3) return null;

        var delimiter = new string(c, run);
        var tag       = line[run..].Trim();
        return new FenceOpen(delimiter, tag); // tag may be ""
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="line"/> closes the current fence.</summary>
    private static bool IsClosingFence(string line, string openDelimiter)
    {
        var trimmed = line.TrimEnd();
        if (trimmed.Length < openDelimiter.Length) return false;
        // The closing line must consist entirely of the same character, at least as many as opener
        char c = openDelimiter[0];
        foreach (var ch in trimmed)
            if (ch != c) return false;
        return trimmed.Length >= openDelimiter.Length;
    }

    /// <summary>
    /// Lazily resolves (and caches) the <see cref="RegexSyntaxHighlighter"/> for a lang id.
    /// Returns <see langword="null"/> when no definition exists for that extension.
    /// </summary>
    private RegexSyntaxHighlighter? ResolveHighlighter(string langId)
    {
        if (!_langMap.TryGetValue(langId, out var ext))
            return null;

        if (_langCache.TryGetValue(ext, out var cached))
            return cached;

        var def        = SyntaxDefinitionCatalog.Instance.FindByExtension(ext);
        var highlighter = def is not null ? new RegexSyntaxHighlighter(def) : null;
        _langCache[ext] = highlighter;
        return highlighter;
    }
}
