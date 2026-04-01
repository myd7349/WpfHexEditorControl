// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynFormattingProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Document and range formatting using Roslyn Formatter API.
//     Passes tabSize/insertSpaces options through to the formatter.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynFormattingProvider
{
    public static async Task<IReadOnlyList<LspTextEdit>> FormatDocumentAsync(
        Document document, int tabSize, bool insertSpaces, CancellationToken ct)
    {
        var docWithOptions = ApplyFormattingOptions(document, tabSize, insertSpaces);
        var formatted = await Formatter.FormatAsync(docWithOptions, cancellationToken: ct)
            .ConfigureAwait(false);
        return await ComputeEditsAsync(document, formatted, ct).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<LspTextEdit>> FormatRangeAsync(
        Document document,
        int startLine, int startColumn, int endLine, int endColumn,
        int tabSize, bool insertSpaces, CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var start = text.Lines.GetPosition(new LinePosition(startLine, startColumn));
        var end = text.Lines.GetPosition(new LinePosition(endLine, endColumn));
        var span = TextSpan.FromBounds(start, end);

        var docWithOptions = ApplyFormattingOptions(document, tabSize, insertSpaces);
        var formatted = await Formatter.FormatAsync(docWithOptions, span, cancellationToken: ct)
            .ConfigureAwait(false);
        return await ComputeEditsAsync(document, formatted, ct).ConfigureAwait(false);
    }

    private static Document ApplyFormattingOptions(Document document, int tabSize, bool insertSpaces)
    {
        var workspace = document.Project.Solution.Workspace;
        var options = workspace.Options
            .WithChangedOption(FormattingOptions.UseTabs, document.Project.Language, !insertSpaces)
            .WithChangedOption(FormattingOptions.TabSize, document.Project.Language, tabSize)
            .WithChangedOption(FormattingOptions.IndentationSize, document.Project.Language, tabSize);
        workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));
        return workspace.CurrentSolution.GetDocument(document.Id)!;
    }

    private static async Task<IReadOnlyList<LspTextEdit>> ComputeEditsAsync(
        Document original, Document formatted, CancellationToken ct)
    {
        var textChanges = await formatted.GetTextChangesAsync(original, ct).ConfigureAwait(false);
        var oldText = await original.GetTextAsync(ct).ConfigureAwait(false);

        var edits = new List<LspTextEdit>();
        foreach (var change in textChanges)
        {
            var startPos = oldText.Lines.GetLinePosition(change.Span.Start);
            var endPos = oldText.Lines.GetLinePosition(change.Span.End);
            edits.Add(new LspTextEdit
            {
                StartLine   = startPos.Line,
                StartColumn = startPos.Character,
                EndLine     = endPos.Line,
                EndColumn   = endPos.Character,
                NewText     = change.NewText ?? string.Empty,
            });
        }
        return edits;
    }
}
