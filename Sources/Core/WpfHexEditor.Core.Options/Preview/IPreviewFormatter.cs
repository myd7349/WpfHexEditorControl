// ==========================================================
// Project: WpfHexEditor.Core.Options
// File: Preview/IPreviewFormatter.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Created: 2026-04-04
// Description:
//     Minimal formatting abstraction for the Formatting options preview.
//     Defined in Core.Options to avoid a dependency on Editor.CodeEditor.
//     Implemented in the App layer by wrapping StructuralFormatter.
//
// Architecture Notes:
//     Core.Options → Core.ProjectSystem only (no SDK, no Editor).
//     The App layer passes an adapter that satisfies this interface.
// ==========================================================

using WpfHexEditor.Core.ProjectSystem.Languages;

namespace WpfHexEditor.Core.Options.Preview;

/// <summary>
/// Minimal formatting contract used by the Formatting options preview.
/// Implemented in the App layer by wrapping <c>StructuralFormatter</c>.
/// </summary>
public interface IPreviewFormatter
{
    /// <summary>
    /// Formats <paramref name="text"/> using the <paramref name="rules"/>
    /// of the selected language and returns the formatted result.
    /// </summary>
    string Format(string text, FormattingRules? rules);
}
