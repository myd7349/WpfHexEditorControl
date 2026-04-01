// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynNavigationProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Go to Definition, References, Implementation, TypeDefinition
//     using Roslyn SymbolFinder APIs.
// ==========================================================

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynNavigationProvider
{
    private static readonly Services.MetadataAsSourceCache s_metadataCache = new();

    public static async Task<IReadOnlyList<LspLocation>> GetDefinitionAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await FindSymbolAsync(document, line, column, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var sourceLocations = MapLocations(symbol.Locations);
        if (sourceLocations.Count > 0) return sourceLocations;

        // Metadata-only symbol — generate decompiled stub file.
        var filePath = await s_metadataCache.GetOrGenerateAsync(
            symbol, document.Project.Language, ct).ConfigureAwait(false);
        if (filePath is null) return [];

        var memberLine = s_metadataCache.FindMemberLine(filePath, symbol);
        return [new LspLocation
        {
            Uri         = new Uri(filePath).AbsoluteUri,
            StartLine   = memberLine,
            StartColumn = 0,
        }];
    }

    public static async Task<IReadOnlyList<LspLocation>> GetReferencesAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await FindSymbolAsync(document, line, column, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var refs = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, ct)
            .ConfigureAwait(false);

        var results = new List<LspLocation>();
        foreach (var refSymbol in refs)
            foreach (var loc in refSymbol.Locations)
            {
                var span = loc.Location.GetLineSpan();
                if (!span.IsValid || span.Path is null) continue;
                results.Add(new LspLocation
                {
                    Uri         = new Uri(span.Path).AbsoluteUri,
                    StartLine   = span.StartLinePosition.Line,
                    StartColumn = span.StartLinePosition.Character,
                });
            }

        return results;
    }

    public static async Task<IReadOnlyList<LspLocation>> GetImplementationAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await FindSymbolAsync(document, line, column, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var impls = await SymbolFinder.FindImplementationsAsync(symbol, document.Project.Solution, cancellationToken: ct)
            .ConfigureAwait(false);

        return impls.SelectMany(s => MapLocations(s.Locations)).ToList();
    }

    public static async Task<IReadOnlyList<LspLocation>> GetTypeDefinitionAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await FindSymbolAsync(document, line, column, ct).ConfigureAwait(false);
        var typeSymbol = symbol switch
        {
            ILocalSymbol local       => local.Type,
            IParameterSymbol param   => param.Type,
            IFieldSymbol field       => field.Type,
            IPropertySymbol prop     => prop.Type,
            IMethodSymbol method     => method.ReturnType,
            IEventSymbol evt         => evt.Type,
            _                        => symbol as ITypeSymbol,
        };

        if (typeSymbol is null) return [];
        return MapLocations(typeSymbol.Locations);
    }

    internal static async Task<ISymbol?> FindSymbolAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var position = text.Lines.GetPosition(new LinePosition(line, column));
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel is null) return null;

        var symbolInfo = semanticModel.GetSymbolInfo(
            (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))!
                .FindToken(position).Parent!, ct);

        return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
    }

    private static IReadOnlyList<LspLocation> MapLocations(ImmutableArray<Location> locations)
    {
        var results = new List<LspLocation>();
        foreach (var loc in locations)
        {
            if (!loc.IsInSource) continue;
            var span = loc.GetLineSpan();
            if (span.Path is null) continue;
            results.Add(new LspLocation
            {
                Uri         = new Uri(span.Path).AbsoluteUri,
                StartLine   = span.StartLinePosition.Line,
                StartColumn = span.StartLinePosition.Character,
            });
        }
        return results;
    }
}
