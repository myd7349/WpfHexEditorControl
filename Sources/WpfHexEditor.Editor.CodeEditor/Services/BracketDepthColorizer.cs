// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Services/BracketDepthColorizer.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description:
//     Stateful depth tracker that assigns CE_Bracket_1/2/3/4 colors to bracket
//     tokens based on their nesting depth within the current document.
//     Bracket pairs are data-driven from LanguageDefinition.BracketPairs (whfmt).
//     Null BracketPairs → service is a no-op (single CE_Bracket color remains).
//
// Architecture Notes:
//     Service (stateful) — one instance per CodeEditor.
//     Call SetPairs() when the active language changes.
//     Call Reset() + pre-scan non-visible lines before each render pass.
//     Call ColorizeLine() for each rendered line in document order.
//
// Depth color palette (4 levels, cycling):
//     depth % 4 == 0 → CE_Bracket_1  (gold / yellow family)
//     depth % 4 == 1 → CE_Bracket_2  (pink / magenta family)
//     depth % 4 == 2 → CE_Bracket_3  (blue / teal family)
//     depth % 4 == 3 → CE_Bracket_4  (green / lime family)
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.CodeEditor.Helpers;

namespace WpfHexEditor.Editor.CodeEditor.Services;

/// <summary>
/// Assigns depth-based foreground colors to bracket tokens using whfmt-driven bracket pairs.
/// Nil-safe: when no pairs are configured the service returns tokens unchanged.
/// </summary>
internal sealed class BracketDepthColorizer
{
    // -- Depth color resource keys (CE_Bracket_N defined in all Colors.xaml) --
    private static readonly string[] s_depthKeys =
    [
        "CE_Bracket_1",
        "CE_Bracket_2",
        "CE_Bracket_3",
        "CE_Bracket_4",
    ];

    // -- Per-bracket-open-char depth counter --
    // Key = open char (e.g. '('), Value = current nesting depth.
    private readonly Dictionary<char, int> _depths = new();

    // -- Reverse lookup: close char → matching open char --
    private Dictionary<char, char> _closeToOpen = new();

    // -- Fast membership test for all bracket chars --
    private HashSet<char> _bracketChars = new();

    private bool _hasPairs;

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Configures the bracket pair set for the active language.
    /// Pass <c>null</c> to disable depth colorization (no pairs = no depth info).
    /// </summary>
    public void SetPairs(IReadOnlyList<(char Open, char Close)>? pairs)
    {
        _depths.Clear();
        _closeToOpen = new Dictionary<char, char>();
        _bracketChars = new HashSet<char>();
        _hasPairs = pairs is { Count: > 0 };

        if (!_hasPairs) return;

        foreach (var (o, c) in pairs!)
        {
            _depths[o] = 0;
            _closeToOpen[c] = o;
            _bracketChars.Add(o);
            _bracketChars.Add(c);
        }
    }

    /// <summary>
    /// Resets all nesting depths to zero.
    /// Call before scanning from the beginning of the document.
    /// </summary>
    public void Reset()
    {
        foreach (var key in _depths.Keys.ToArray())
            _depths[key] = 0;
    }

    /// <summary>
    /// Advances bracket depth state for a non-rendered line (before the visible range).
    /// Uses <paramref name="cachedTokens"/> for accuracy (skips brackets inside strings/comments);
    /// falls back to raw character scan when no token cache is available.
    /// </summary>
    public void AdvanceLine(
        string                               lineText,
        IReadOnlyList<SyntaxHighlightToken>? cachedTokens)
    {
        if (!_hasPairs || string.IsNullOrEmpty(lineText)) return;

        if (cachedTokens is { Count: > 0 })
        {
            foreach (var token in cachedTokens)
            {
                if (token.Kind != SyntaxTokenKind.Bracket || token.Length != 1) continue;
                if (token.StartColumn < lineText.Length)
                    UpdateDepth(lineText[token.StartColumn]);
            }
        }
        else
        {
            // Token cache not yet available — scan raw text.
            // Brackets inside string literals may cause slight inaccuracies,
            // but the background pipeline will soon populate the cache.
            foreach (char ch in lineText)
                UpdateDepth(ch);
        }
    }

    /// <summary>
    /// Returns the token list with bracket tokens' foreground replaced by CE_Bracket_N
    /// based on the current nesting depth.  Advances the depth state for this line.
    /// </summary>
    /// <param name="lineText">Raw source text of the line.</param>
    /// <param name="tokens">Token list (already brush-resolved).</param>
    /// <param name="resourceLookup">Delegate mapping a resource key to a <see cref="Brush"/>.</param>
    public IEnumerable<SyntaxHighlightToken> ColorizeLine(
        string                              lineText,
        IEnumerable<SyntaxHighlightToken>   tokens,
        Func<string, Brush?>                resourceLookup)
    {
        if (!_hasPairs)
        {
            foreach (var t in tokens) yield return t;
            yield break;
        }

        foreach (var token in tokens)
        {
            if (token.Kind == SyntaxTokenKind.Bracket
                && token.Length == 1
                && token.StartColumn < lineText.Length)
            {
                char ch = lineText[token.StartColumn];
                if (_bracketChars.Contains(ch))
                {
                    int depth = ComputeDepthAndAdvance(ch);
                    var brush = resourceLookup(s_depthKeys[depth % 4]);
                    if (brush is not null)
                    {
                        yield return token with { Foreground = brush };
                        continue;
                    }
                }
            }
            yield return token;
        }
    }

    // -- Private helpers ------------------------------------------------------

    private void UpdateDepth(char ch)
    {
        if (_depths.TryGetValue(ch, out int d))
            _depths[ch] = d + 1;
        else if (_closeToOpen.TryGetValue(ch, out char open) && _depths.TryGetValue(open, out int cd))
            _depths[open] = Math.Max(0, cd - 1);
    }

    /// <summary>
    /// Returns the depth index to use for <paramref name="ch"/> and advances the state.
    /// Open bracket: returns current depth, then increments.
    /// Close bracket: decrements first, returns new depth.
    /// </summary>
    private int ComputeDepthAndAdvance(char ch)
    {
        if (_depths.TryGetValue(ch, out int openDepth))
        {
            // Opening bracket: colour at current depth, then go deeper.
            _depths[ch] = openDepth + 1;
            return openDepth;
        }

        if (_closeToOpen.TryGetValue(ch, out char open) && _depths.TryGetValue(open, out int closeDepth))
        {
            // Closing bracket: come up first, colour at new depth.
            int newDepth = Math.Max(0, closeDepth - 1);
            _depths[open] = newDepth;
            return newDepth;
        }

        return 0; // Fallback (unmatched bracket).
    }
}
