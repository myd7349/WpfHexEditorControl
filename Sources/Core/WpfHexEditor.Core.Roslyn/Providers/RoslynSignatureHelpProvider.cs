// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynSignatureHelpProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Signature help (parameter info) using Roslyn SemanticModel.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynSignatureHelpProvider
{
    public static async Task<LspSignatureHelpResult?> GetSignatureHelpAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var position = text.Lines.GetPosition(new LinePosition(line, column));
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (root is null || semanticModel is null) return null;

        // Walk up from position to find invocation-like expression.
        var token = root.FindToken(position);
        var node = token.Parent;

        while (node is not null)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node, ct);
            if (symbolInfo.Symbol is IMethodSymbol method)
                return BuildResult(new[] { method }, 0);

            if (symbolInfo.CandidateSymbols.Length > 0)
            {
                var methods = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToArray();
                if (methods.Length > 0) return BuildResult(methods, 0);
            }

            node = node.Parent;
        }

        return null;
    }

    private static LspSignatureHelpResult BuildResult(IMethodSymbol[] methods, int activeParam)
    {
        var sigs = new List<LspSignatureInfo>(methods.Length);
        foreach (var m in methods)
        {
            var parameters = m.Parameters.Select(p => new LspParameterInfo
            {
                Label         = $"{p.Type.ToDisplayString()} {p.Name}",
                Documentation = p.GetDocumentationCommentXml(),
            }).ToList();

            sigs.Add(new LspSignatureInfo
            {
                Label         = m.ToDisplayString(),
                Documentation = ExtractSummary(m.GetDocumentationCommentXml()),
                Parameters    = parameters,
            });
        }

        return new LspSignatureHelpResult
        {
            Signatures           = sigs,
            ActiveSignatureIndex = 0,
            ActiveParameterIndex = activeParam,
        };
    }

    private static string? ExtractSummary(string? xml)
    {
        if (xml is null) return null;
        const string startTag = "<summary>";
        const string endTag = "</summary>";
        var start = xml.IndexOf(startTag, StringComparison.Ordinal);
        var end = xml.IndexOf(endTag, StringComparison.Ordinal);
        if (start < 0 || end < 0) return null;
        return xml[(start + startTag.Length)..end].Trim();
    }
}
