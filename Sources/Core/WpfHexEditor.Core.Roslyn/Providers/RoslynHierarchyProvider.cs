// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynHierarchyProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Call hierarchy and type hierarchy using Roslyn SymbolFinder.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynHierarchyProvider
{
    // ── Call Hierarchy ────────────────────────────────────────────────────────

    public static async Task<IReadOnlyList<LspCallHierarchyItem>> PrepareCallHierarchyAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await RoslynNavigationProvider.FindSymbolAsync(document, line, column, ct)
            .ConfigureAwait(false);
        if (symbol is null) return [];
        return [MapToCallItem(symbol)];
    }

    public static async Task<IReadOnlyList<LspIncomingCall>> GetIncomingCallsAsync(
        Solution solution, LspCallHierarchyItem item, CancellationToken ct)
    {
        var symbol = await FindSymbolByItemAsync(solution, item, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var callers = await SymbolFinder.FindCallersAsync(symbol, solution, ct).ConfigureAwait(false);
        var results = new List<LspIncomingCall>();

        foreach (var caller in callers)
        {
            if (!caller.IsDirect) continue;
            var ranges = caller.Locations
                .Where(l => l.IsInSource)
                .Select(l =>
                {
                    var s = l.GetLineSpan();
                    return (s.StartLinePosition.Line, s.StartLinePosition.Character,
                            s.EndLinePosition.Line, s.EndLinePosition.Character);
                }).ToList();

            if (ranges.Count > 0)
                results.Add(new LspIncomingCall
                {
                    From       = MapToCallItem(caller.CallingSymbol),
                    FromRanges = ranges,
                });
        }

        return results;
    }

    public static async Task<IReadOnlyList<LspOutgoingCall>> GetOutgoingCallsAsync(
        Solution solution, LspCallHierarchyItem item, CancellationToken ct)
    {
        var symbol = await FindSymbolByItemAsync(solution, item, ct).ConfigureAwait(false);
        if (symbol is not IMethodSymbol method) return [];

        // Find the method's syntax body and walk for invocations.
        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef is null) return [];

        var syntaxNode = await syntaxRef.GetSyntaxAsync(ct).ConfigureAwait(false);
        var document = solution.GetDocument(syntaxRef.SyntaxTree);
        if (document is null) return [];

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel is null) return [];

        // Collect all invocations and group by target method.
        var callsByTarget = new Dictionary<IMethodSymbol, List<(int, int, int, int)>>(SymbolEqualityComparer.Default);
        foreach (var descendant in syntaxNode.DescendantNodes())
        {
            var info = semanticModel.GetSymbolInfo(descendant, ct);
            if (info.Symbol is not IMethodSymbol targetMethod) continue;

            // Only count actual invocation expressions, not declarations.
            if (!descendant.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression) &&
                !descendant.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression))
                continue;

            var span = descendant.GetLocation().GetLineSpan();
            if (!span.IsValid) continue;

            if (!callsByTarget.TryGetValue(targetMethod, out var ranges))
            {
                ranges = [];
                callsByTarget[targetMethod] = ranges;
            }
            ranges.Add((span.StartLinePosition.Line, span.StartLinePosition.Character,
                        span.EndLinePosition.Line, span.EndLinePosition.Character));
        }

        var results = new List<LspOutgoingCall>();
        foreach (var (target, ranges) in callsByTarget)
        {
            results.Add(new LspOutgoingCall
            {
                To         = MapToCallItem(target),
                FromRanges = ranges,
            });
        }

        return results;
    }

    // ── Type Hierarchy ───────────────────────────────────────────────────────

    public static async Task<IReadOnlyList<LspTypeHierarchyItem>> PrepareTypeHierarchyAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await RoslynNavigationProvider.FindSymbolAsync(document, line, column, ct)
            .ConfigureAwait(false);
        if (symbol is not INamedTypeSymbol typeSymbol) return [];
        return [MapToTypeItem(typeSymbol)];
    }

    public static async Task<IReadOnlyList<LspTypeHierarchyItem>> GetSupertypesAsync(
        Solution solution, LspTypeHierarchyItem item, CancellationToken ct)
    {
        var symbol = await FindTypeByItemAsync(solution, item, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var results = new List<LspTypeHierarchyItem>();
        if (symbol.BaseType is not null && symbol.BaseType.SpecialType != SpecialType.System_Object)
            results.Add(MapToTypeItem(symbol.BaseType));

        foreach (var iface in symbol.Interfaces)
            results.Add(MapToTypeItem(iface));

        return results;
    }

    public static async Task<IReadOnlyList<LspTypeHierarchyItem>> GetSubtypesAsync(
        Solution solution, LspTypeHierarchyItem item, CancellationToken ct)
    {
        var symbol = await FindTypeByItemAsync(solution, item, ct).ConfigureAwait(false);
        if (symbol is null) return [];

        var derived = await SymbolFinder.FindDerivedClassesAsync(symbol, solution, cancellationToken: ct)
            .ConfigureAwait(false);

        return derived.Select(MapToTypeItem).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LspCallHierarchyItem MapToCallItem(ISymbol symbol)
    {
        var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var span = loc?.GetLineSpan();
        return new LspCallHierarchyItem
        {
            Name          = symbol.Name,
            Kind          = symbol is IMethodSymbol ? "method" : "function",
            Uri           = span?.Path is not null ? new Uri(span.Value.Path).AbsoluteUri : string.Empty,
            StartLine     = span?.StartLinePosition.Line ?? 0,
            StartColumn   = span?.StartLinePosition.Character ?? 0,
            ContainerName = symbol.ContainingType?.Name,
        };
    }

    private static LspTypeHierarchyItem MapToTypeItem(INamedTypeSymbol symbol)
    {
        var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var span = loc?.GetLineSpan();
        return new LspTypeHierarchyItem
        {
            Name          = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            Kind          = symbol.TypeKind == TypeKind.Interface ? "interface" : "class",
            Uri           = span?.Path is not null ? new Uri(span.Value.Path).AbsoluteUri : string.Empty,
            StartLine     = span?.StartLinePosition.Line ?? 0,
            StartColumn   = span?.StartLinePosition.Character ?? 0,
            ContainerName = symbol.ContainingNamespace?.ToDisplayString(),
        };
    }

    private static async Task<ISymbol?> FindSymbolByItemAsync(
        Solution solution, LspCallHierarchyItem item, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(item.Uri)) return null;
        var filePath = new Uri(item.Uri).LocalPath;
        var docId = solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
        if (docId is null) return null;
        var doc = solution.GetDocument(docId);
        if (doc is null) return null;
        return await RoslynNavigationProvider.FindSymbolAsync(doc, item.StartLine, item.StartColumn, ct)
            .ConfigureAwait(false);
    }

    private static async Task<INamedTypeSymbol?> FindTypeByItemAsync(
        Solution solution, LspTypeHierarchyItem item, CancellationToken ct)
    {
        var symbol = await FindSymbolByItemAsync(solution,
            new LspCallHierarchyItem
            {
                Name = item.Name, Kind = item.Kind, Uri = item.Uri,
                StartLine = item.StartLine, StartColumn = item.StartColumn,
                ContainerName = item.ContainerName,
            }, ct).ConfigureAwait(false);
        return symbol as INamedTypeSymbol;
    }
}
