// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynInlayHintsProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Parameter name inlay hints for C# and VB.NET.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynInlayHintsProvider
{
    public static async Task<IReadOnlyList<LspInlayHint>> GetInlayHintsAsync(
        Document document, int startLine, int endLine, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        if (root is null || semanticModel is null) return [];

        var startPos = text.Lines[startLine].Start;
        var endPos = text.Lines[Math.Min(endLine, text.Lines.Count - 1)].End;
        var span = TextSpan.FromBounds(startPos, endPos);

        var results = new List<LspInlayHint>();

        if (document.Project.Language == LanguageNames.CSharp)
            CollectCSharpHints(root, semanticModel, span, text, results, ct);
        else if (document.Project.Language == LanguageNames.VisualBasic)
            CollectVbHints(root, semanticModel, span, text, results, ct);

        return results;
    }

    private static void CollectCSharpHints(
        SyntaxNode root, SemanticModel model, TextSpan span, SourceText text,
        List<LspInlayHint> results, CancellationToken ct)
    {
        foreach (var node in root.DescendantNodes(span))
        {
            if (node is not ArgumentSyntax arg) continue;
            if (arg.NameColon is not null) continue; // Already named.
            if (arg.Parent is not ArgumentListSyntax argList) continue;

            var invocation = argList.Parent;
            if (invocation is null) continue;

            var symbolInfo = model.GetSymbolInfo(invocation, ct);
            if (symbolInfo.Symbol is not IMethodSymbol method) continue;

            var argIndex = argList.Arguments.IndexOf(arg);
            if (argIndex < 0 || argIndex >= method.Parameters.Length) continue;

            // Skip if argument is a literal or simple identifier matching the param name.
            var param = method.Parameters[argIndex];
            if (arg.Expression is LiteralExpressionSyntax ||
                arg.Expression is ObjectCreationExpressionSyntax)
            {
                var pos = text.Lines.GetLinePosition(arg.SpanStart);
                results.Add(new LspInlayHint
                {
                    Line   = pos.Line,
                    Column = pos.Character,
                    Label  = $"{param.Name}:",
                    Kind   = "parameter",
                });
            }
        }
    }

    private static void CollectVbHints(
        SyntaxNode root, SemanticModel model, TextSpan span, SourceText text,
        List<LspInlayHint> results, CancellationToken ct)
    {
        foreach (var node in root.DescendantNodes(span))
        {
            // VB.NET: SimpleArgumentSyntax is the equivalent of C# ArgumentSyntax.
            if (node is not Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax vbArg) continue;
            if (vbArg.NameColonEquals is not null) continue; // Already named.

            var argList = vbArg.Parent as Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax;
            if (argList is null) continue;

            var invocation = argList.Parent;
            if (invocation is null) continue;

            var symbolInfo = model.GetSymbolInfo(invocation, ct);
            if (symbolInfo.Symbol is not IMethodSymbol method) continue;

            var argIndex = argList.Arguments.IndexOf(vbArg);
            if (argIndex < 0 || argIndex >= method.Parameters.Length) continue;

            var param = method.Parameters[argIndex];
            var pos = text.Lines.GetLinePosition(vbArg.SpanStart);
            results.Add(new LspInlayHint
            {
                Line   = pos.Line,
                Column = pos.Character,
                Label  = $"{param.Name}:",
                Kind   = "parameter",
            });
        }
    }
}
