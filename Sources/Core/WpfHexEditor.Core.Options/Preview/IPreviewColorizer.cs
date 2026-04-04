// ==========================================================
// Project: WpfHexEditor.Core.Options
// File: Preview/IPreviewColorizer.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Created: 2026-04-04
// Description:
//     Minimal coloring abstraction for the Formatting options preview.
//     Defined in Core.Options to avoid a dependency on WpfHexEditor.SDK.
//     Implemented in the App layer by adapting ISyntaxColoringService.
//
// Architecture Notes:
//     Core.Options → Core.ProjectSystem only (no SDK, no Editor).
//     The App layer passes a lambda/adapter that satisfies this interface.
// ==========================================================

using System.Windows.Media;

namespace WpfHexEditor.Core.Options.Preview;

/// <summary>
/// A single colorised text segment within one line.
/// </summary>
public readonly record struct PreviewSpan(
    string Text,
    Brush  Foreground,
    bool   IsBold   = false,
    bool   IsItalic = false);

/// <summary>
/// Minimal coloring contract used by the Formatting options preview.
/// Implemented in the App layer by wrapping <c>ISyntaxColoringService</c>.
/// </summary>
public interface IPreviewColorizer
{
    /// <summary>
    /// Colorises a set of lines for the given language ID.
    /// Returns one span list per input line.
    /// </summary>
    IReadOnlyList<IReadOnlyList<PreviewSpan>> ColorizeLines(
        IReadOnlyList<string> lines,
        string                languageId);
}
