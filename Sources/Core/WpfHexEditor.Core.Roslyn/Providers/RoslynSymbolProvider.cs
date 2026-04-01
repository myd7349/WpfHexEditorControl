// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynSymbolProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Document symbols (outline/breadcrumb) and workspace symbols (Ctrl+T).
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynSymbolProvider
{
    public static async Task<IReadOnlyList<LspDocumentSymbol>> GetDocumentSymbolsAsync(
        Document document, CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (root is null || semanticModel is null) return [];

        var results = new List<LspDocumentSymbol>();

        foreach (var node in root.DescendantNodes())
        {
            var symbol = semanticModel.GetDeclaredSymbol(node, ct);
            if (symbol is null) continue;

            var kind = MapSymbolKind(symbol);
            if (kind is null) continue;

            var span = node.GetLocation().GetLineSpan();
            if (!span.IsValid) continue;

            results.Add(new LspDocumentSymbol
            {
                Name          = symbol.Name,
                Kind          = kind,
                StartLine     = span.StartLinePosition.Line,
                StartColumn   = span.StartLinePosition.Character,
                EndLine       = span.EndLinePosition.Line,
                EndColumn     = span.EndLinePosition.Character,
                ContainerName = symbol.ContainingType?.Name ?? symbol.ContainingNamespace?.ToDisplayString(),
            });
        }

        return results;
    }

    public static async Task<IReadOnlyList<LspWorkspaceSymbol>> GetWorkspaceSymbolsAsync(
        Solution solution, string query, CancellationToken ct)
    {
        var symbols = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(
            solution, query, SymbolFilter.TypeAndMember, ct).ConfigureAwait(false);

        var results = new List<LspWorkspaceSymbol>();
        foreach (var symbol in symbols)
        {
            var kind = MapSymbolKind(symbol);
            if (kind is null) continue;

            var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
            if (loc is null) continue;

            var span = loc.GetLineSpan();
            if (span.Path is null) continue;

            results.Add(new LspWorkspaceSymbol
            {
                Name          = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                Kind          = kind,
                Uri           = new Uri(span.Path).AbsoluteUri,
                StartLine     = span.StartLinePosition.Line,
                StartColumn   = span.StartLinePosition.Character,
                ContainerName = symbol.ContainingType?.Name ?? symbol.ContainingNamespace?.ToDisplayString(),
            });
        }

        return results;
    }

    private static string? MapSymbolKind(ISymbol symbol) => symbol switch
    {
        INamespaceSymbol                               => "namespace",
        INamedTypeSymbol { TypeKind: TypeKind.Class }  => "class",
        INamedTypeSymbol { TypeKind: TypeKind.Struct } => "struct",
        INamedTypeSymbol { TypeKind: TypeKind.Interface } => "interface",
        INamedTypeSymbol { TypeKind: TypeKind.Enum }   => "enum",
        INamedTypeSymbol { TypeKind: TypeKind.Delegate } => "class",
        IMethodSymbol                                  => "method",
        IPropertySymbol                                => "property",
        IFieldSymbol { IsConst: true }                 => "constant",
        IFieldSymbol                                   => "field",
        IEventSymbol                                   => "event",
        _                                              => null,
    };
}
