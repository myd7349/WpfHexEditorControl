// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynHoverProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Hover information using Roslyn symbol display + XML doc comments.
// ==========================================================

using Microsoft.CodeAnalysis;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynHoverProvider
{
    private static readonly SymbolDisplayFormat s_displayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions:
            SymbolDisplayMemberOptions.IncludeType |
            SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeContainingType |
            SymbolDisplayMemberOptions.IncludeAccessibility |
            SymbolDisplayMemberOptions.IncludeModifiers,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeType |
            SymbolDisplayParameterOptions.IncludeName |
            SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static async Task<LspHoverResult?> GetHoverAsync(
        Document document, int line, int column, CancellationToken ct)
    {
        var symbol = await RoslynNavigationProvider.FindSymbolAsync(document, line, column, ct)
            .ConfigureAwait(false);
        if (symbol is null) return null;

        var display = symbol.ToDisplayString(s_displayFormat);
        var xmlDoc = symbol.GetDocumentationCommentXml(cancellationToken: ct);

        var markdown = $"```csharp\n{display}\n```";
        if (!string.IsNullOrWhiteSpace(xmlDoc))
        {
            var summary = ExtractSummary(xmlDoc);
            if (summary is not null)
                markdown += $"\n\n{summary}";
        }

        return new LspHoverResult { Contents = markdown };
    }

    private static string? ExtractSummary(string xml)
    {
        const string startTag = "<summary>";
        const string endTag = "</summary>";
        var start = xml.IndexOf(startTag, StringComparison.Ordinal);
        var end = xml.IndexOf(endTag, StringComparison.Ordinal);
        if (start < 0 || end < 0) return null;
        start += startTag.Length;
        return xml[start..end].Trim();
    }
}
