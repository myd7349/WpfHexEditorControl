// ==========================================================
// Project: WpfHexEditor.Editor.TextEditor
// File: Highlighting/IContextualHighlighter.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-22
// Description:
//     Contract for stateful, multi-line syntax highlighters.
//     Unlike RegexSyntaxHighlighter (which is stateless and line-by-line),
//     an IContextualHighlighter first receives a full document snapshot
//     via Prepare() so it can build cross-line context (e.g. fence regions),
//     then handles per-line Highlight() calls that delegate to the correct
//     language highlighter based on that context.
// ==========================================================

namespace WpfHexEditor.Editor.TextEditor.Highlighting;

/// <summary>
/// Stateful, multi-line syntax highlighter that builds cross-line context
/// before processing individual lines.
/// </summary>
internal interface IContextualHighlighter
{
    /// <summary>
    /// Scans the entire document to build cross-line context (e.g. fence regions).
    /// Must be called once per highlight pass before any <see cref="Highlight"/> call.
    /// </summary>
    /// <param name="allLines">All document lines at the time of the highlight pass.</param>
    void Prepare(IReadOnlyList<string> allLines);

    /// <summary>
    /// Returns syntax highlight spans for the given line using context built by <see cref="Prepare"/>.
    /// </summary>
    /// <param name="lineText">Text content of the line.</param>
    /// <param name="lineIndex">Zero-based line index in the document.</param>
    IReadOnlyList<ColoredSpan> Highlight(string lineText, int lineIndex);
}
