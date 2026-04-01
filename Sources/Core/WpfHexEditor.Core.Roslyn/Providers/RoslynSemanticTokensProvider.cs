// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynSemanticTokensProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Semantic token classification using Roslyn Classifier API.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynSemanticTokensProvider
{
    public static async Task<LspSemanticTokensResult?> GetTokensAsync(
        Document document, CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var spans = await Classifier.GetClassifiedSpansAsync(
            document, TextSpan.FromBounds(0, text.Length), ct).ConfigureAwait(false);

        var tokens = new List<LspSemanticToken>();
        foreach (var span in spans)
        {
            var tokenType = MapClassification(span.ClassificationType);
            if (tokenType is null) continue;

            var startPos = text.Lines.GetLinePosition(span.TextSpan.Start);
            tokens.Add(new LspSemanticToken
            {
                Line      = startPos.Line,
                Column    = startPos.Character,
                Length    = span.TextSpan.Length,
                TokenType = tokenType,
                Modifiers = [],
            });
        }

        return new LspSemanticTokensResult { Tokens = tokens };
    }

    private static string? MapClassification(string classification) => classification switch
    {
        ClassificationTypeNames.ClassName         => "type",
        ClassificationTypeNames.StructName        => "type",
        ClassificationTypeNames.InterfaceName     => "type",
        ClassificationTypeNames.EnumName          => "type",
        ClassificationTypeNames.DelegateName      => "type",
        ClassificationTypeNames.RecordClassName    => "type",
        ClassificationTypeNames.RecordStructName   => "type",
        ClassificationTypeNames.TypeParameterName  => "typeParameter",
        ClassificationTypeNames.MethodName         => "method",
        ClassificationTypeNames.ExtensionMethodName => "method",
        ClassificationTypeNames.PropertyName       => "property",
        ClassificationTypeNames.FieldName          => "field",
        ClassificationTypeNames.ConstantName       => "variable",
        ClassificationTypeNames.EnumMemberName     => "enumMember",
        ClassificationTypeNames.EventName          => "event",
        ClassificationTypeNames.LocalName          => "variable",
        ClassificationTypeNames.ParameterName      => "parameter",
        ClassificationTypeNames.NamespaceName       => "namespace",
        ClassificationTypeNames.LabelName           => "variable",
        ClassificationTypeNames.Keyword             => "keyword",
        ClassificationTypeNames.ControlKeyword      => "keyword",
        ClassificationTypeNames.StringLiteral       => "string",
        ClassificationTypeNames.VerbatimStringLiteral => "string",
        ClassificationTypeNames.NumericLiteral      => "number",
        ClassificationTypeNames.Operator            => "operator",
        ClassificationTypeNames.Comment             => "comment",
        ClassificationTypeNames.XmlDocCommentText   => "comment",
        ClassificationTypeNames.PreprocessorKeyword => "macro",
        _                                           => null,
    };
}
