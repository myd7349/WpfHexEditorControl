// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynCompletionProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Maps Roslyn CompletionService to LspCompletionItem DTOs.
// ==========================================================

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynCompletionProvider
{
    // Cache last completion results for resolve.
    [ThreadStatic] private static CompletionList? t_lastCompletionList;
    [ThreadStatic] private static Document? t_lastDocument;

    public static async Task<IReadOnlyList<LspCompletionItem>> GetCompletionsAsync(
        Document document, int line, int column, char? triggerChar, CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var position = text.Lines.GetPosition(new LinePosition(line, column));

        var service = CompletionService.GetService(document);
        if (service is null) return [];

        var trigger = triggerChar.HasValue
            ? CompletionTrigger.CreateInsertionTrigger(triggerChar.Value)
            : CompletionTrigger.Invoke;

        var completions = await service.GetCompletionsAsync(document, position, trigger, cancellationToken: ct)
            .ConfigureAwait(false);

        if (completions is null) return [];

        // Cache for resolve.
        t_lastCompletionList = completions;
        t_lastDocument = document;

        var results = new List<LspCompletionItem>(completions.ItemsList.Count);
        foreach (var item in completions.ItemsList)
        {
            results.Add(new LspCompletionItem
            {
                Label      = item.DisplayText,
                Kind       = MapCompletionKind(item.Tags),
                Detail     = item.InlineDescription,
                InsertText = item.DisplayText,
                CommitCharacters = null,
            });
        }

        return results;
    }

    public static async Task<LspCompletionItem?> ResolveAsync(
        Document document, LspCompletionItem item, CancellationToken ct)
    {
        // Use cached completion list from last GetCompletionsAsync call.
        var completions = t_lastCompletionList;
        var cachedDoc = t_lastDocument ?? document;

        var service = CompletionService.GetService(cachedDoc);
        if (service is null || completions is null) return item;

        var match = completions.ItemsList.FirstOrDefault(c => c.DisplayText == item.Label);
        if (match is null) return item;

        var description = await service.GetDescriptionAsync(cachedDoc, match, cancellationToken: ct)
            .ConfigureAwait(false);

        return new LspCompletionItem
        {
            Label         = item.Label,
            Kind          = item.Kind,
            Detail        = item.Detail,
            InsertText    = item.InsertText,
            Documentation = description?.Text,
        };
    }

    private static string MapCompletionKind(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case "Class":     return "class";
                case "Struct":    return "struct";
                case "Interface": return "interface";
                case "Enum":      return "enum";
                case "Method":    return "method";
                case "Property":  return "property";
                case "Field":     return "field";
                case "Event":     return "event";
                case "Namespace": return "module";
                case "Keyword":   return "keyword";
                case "Local":
                case "Parameter": return "variable";
                case "Constant":  return "constant";
                case "EnumMember":return "enumMember";
            }
        }
        return "text";
    }
}
