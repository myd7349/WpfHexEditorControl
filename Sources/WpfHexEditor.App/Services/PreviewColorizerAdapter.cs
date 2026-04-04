// ==========================================================
// Project: WpfHexEditor.App
// File: Services/PreviewColorizerAdapter.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Created: 2026-04-04
// Updated: 2026-04-04
// Description:
//     Adapts ISyntaxColoringService (SDK) to IPreviewColorizer (Core.Options).
//     Fills gaps between colorised spans with plain-text PreviewSpans so that
//     the rendered preview line matches the full original text (spaces included).
// ==========================================================

using System.Windows.Media;
using WpfHexEditor.Core.Options.Preview;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.CodeEditor.Services;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Wraps <see cref="ISyntaxColoringService"/> as an <see cref="IPreviewColorizer"/>.
/// Reconstructs full lines by inserting un-colorised gap spans between the
/// coloured tokens returned by the highlighter.
/// </summary>
internal sealed class PreviewColorizerAdapter : IPreviewColorizer
{
    private readonly ISyntaxColoringService _inner;

    // Fallback brush for plain text gaps — resolved from theme at first use.
    private static readonly Brush DefaultFg =
        new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));   // VS Dark default text

    public PreviewColorizerAdapter(ISyntaxColoringService inner)
        => _inner = inner;

    public IReadOnlyList<IReadOnlyList<PreviewSpan>> ColorizeLines(
        IReadOnlyList<string> lines,
        string                languageId)
    {
        var sdkResult = _inner.ColorizeLines(lines, languageId);
        var result    = new List<IReadOnlyList<PreviewSpan>>(lines.Count);

        for (int li = 0; li < lines.Count; li++)
        {
            var rawLine   = lines[li];
            var sdkLine   = li < sdkResult.Count ? sdkResult[li] : null;

            // No spans → emit the whole line as plain text
            if (sdkLine is null || sdkLine.Count == 0)
            {
                result.Add(rawLine.Length == 0
                    ? Array.Empty<PreviewSpan>()
                    : [new PreviewSpan(rawLine, DefaultFg)]);
                continue;
            }

            // Sort spans by start position (normally already sorted)
            var sorted  = sdkLine.OrderBy(s => s.Start).ToList();
            var spans   = new List<PreviewSpan>(sorted.Count * 2 + 1);
            int cursor  = 0;

            foreach (var s in sorted)
            {
                // Fill gap before this span with plain text
                if (s.Start > cursor)
                    spans.Add(new PreviewSpan(rawLine[cursor..s.Start], DefaultFg));

                // Add the colorised span
                spans.Add(new PreviewSpan(s.Text, s.Foreground, s.IsBold, s.IsItalic));
                cursor = s.Start + s.Length;
            }

            // Fill any trailing plain text after last span
            if (cursor < rawLine.Length)
                spans.Add(new PreviewSpan(rawLine[cursor..], DefaultFg));

            result.Add(spans);
        }

        return result;
    }
}

/// <summary>
/// Adapts <see cref="StructuralFormatter"/> as an <see cref="IPreviewFormatter"/>.
/// </summary>
internal sealed class PreviewFormatterAdapter : IPreviewFormatter
{
    public string Format(string text, FormattingRules? rules)
        => StructuralFormatter.FormatDocument(text, rules);
}
