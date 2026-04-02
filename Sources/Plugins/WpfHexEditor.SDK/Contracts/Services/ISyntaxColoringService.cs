// ==========================================================
// Project: WpfHexEditor.SDK
// File: ISyntaxColoringService.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-04-02
// Description:
//     Centralised syntax coloring service for plugins.
//     Eliminates duplicated TokenKindToBrush / BuildHighlighter logic
//     across ChatCodeBlockCanvas, BreakpointCodePreview, etc.
//
// Architecture Notes:
//     Single source of truth: whfmt-driven LanguageDefinition → theme brushes (CE_*).
//     Plugins access via IIDEHostContext.SyntaxColoring.
//     Thread-safe: each call creates a fresh highlighter (stateful for block comments).
// ==========================================================

using System.Windows.Media;
using WpfHexEditor.Core.ProjectSystem.Languages;

namespace WpfHexEditor.SDK.Contracts.Services;

/// <summary>
/// A coloured text segment produced by <see cref="ISyntaxColoringService"/>.
/// Lightweight value type — no dependency on CodeEditor internals.
/// </summary>
public readonly record struct ColoredSpan(
    int             Start,
    int             Length,
    string          Text,
    Brush           Foreground,
    bool            IsBold   = false,
    bool            IsItalic = false,
    SyntaxTokenKind Kind     = SyntaxTokenKind.Default);

/// <summary>
/// Centralised syntax coloring service available to all plugins via
/// <see cref="IIDEHostContext.SyntaxColoring"/>.
/// Resolves languages from IDs, file paths, or common aliases (c# → csharp, js → javascript),
/// then tokenises text using whfmt-driven rules and current theme brushes.
/// </summary>
public interface ISyntaxColoringService
{
    /// <summary>
    /// Colorises a single line of code.
    /// </summary>
    /// <param name="line">Raw text of the line (no trailing newline).</param>
    /// <param name="languageId">Language ID, file extension, or common alias.</param>
    /// <returns>Ordered, non-overlapping coloured spans. Empty list when language is unknown.</returns>
    IReadOnlyList<ColoredSpan> ColorizeLine(string line, string languageId);

    /// <summary>
    /// Colorises multiple lines, correctly handling multi-line constructs (block comments, etc.).
    /// </summary>
    /// <param name="lines">Lines of code (no trailing newlines).</param>
    /// <param name="languageId">Language ID, file extension, or common alias.</param>
    /// <returns>One list of spans per input line. Empty outer list when language is unknown.</returns>
    IReadOnlyList<IReadOnlyList<ColoredSpan>> ColorizeLines(IReadOnlyList<string> lines, string languageId);

    /// <summary>
    /// Resolves the theme-aware brush for a given token kind.
    /// Uses CE_* resources from the active theme with VS Dark fallbacks.
    /// </summary>
    Brush GetTokenBrush(SyntaxTokenKind kind);

    /// <summary>
    /// Resolves a language alias or file extension to a canonical language ID.
    /// Returns <c>null</c> when no match is found.
    /// </summary>
    /// <param name="aliasOrExtension">Alias ("c#", "js"), extension (".cs"), or ID ("csharp").</param>
    string? ResolveLanguageId(string aliasOrExtension);
}
