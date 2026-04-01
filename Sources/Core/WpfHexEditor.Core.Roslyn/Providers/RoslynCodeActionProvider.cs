// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynCodeActionProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Code actions (quick fixes + refactorings) via Roslyn CodeFix/Refactoring APIs.
//     Uses MEF-discovered providers from Microsoft.CodeAnalysis.*.Features packages.
// ==========================================================

using System.Collections.Concurrent;
using System.Composition.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynCodeActionProvider
{
    // Cache MEF-discovered providers per language (they're stateless).
    private static readonly ConcurrentDictionary<string, IReadOnlyList<CodeFixProvider>> s_codeFixCache = new();
    private static readonly ConcurrentDictionary<string, IReadOnlyList<CodeRefactoringProvider>> s_refactoringCache = new();

    public static async Task<IReadOnlyList<LspCodeAction>> GetCodeActionsAsync(
        Document document,
        int startLine, int startColumn,
        int endLine, int endColumn,
        CancellationToken ct)
    {
        var text = await document.GetTextAsync(ct).ConfigureAwait(false);
        var start = text.Lines.GetPosition(new LinePosition(startLine, startColumn));
        var end = text.Lines.GetPosition(new LinePosition(endLine, endColumn));
        var span = TextSpan.FromBounds(start, end);

        var results = new List<LspCodeAction>();

        // Code fixes for diagnostics in range.
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel is not null)
        {
            var diagnostics = semanticModel.GetDiagnostics(span, ct);
            var providers = GetCodeFixProviders(document.Project.Language);

            foreach (var diag in diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Hidden) continue;

                foreach (var provider in providers)
                {
                    if (!provider.FixableDiagnosticIds.Contains(diag.Id)) continue;

                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(document, diag,
                        (a, _) => actions.Add(a), ct);

                    try { await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false); }
                    catch { continue; }

                    foreach (var action in actions)
                    {
                        var mapped = await MapActionAsync(action, document.Project.Solution, "quickfix", ct)
                            .ConfigureAwait(false);
                        if (mapped is not null) results.Add(mapped);
                    }
                }
            }
        }

        // Code refactorings for selection range.
        var refactoringProviders = GetCodeRefactoringProviders(document.Project.Language);
        foreach (var provider in refactoringProviders)
        {
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span,
                a => actions.Add(a), ct);

            try { await provider.ComputeRefactoringsAsync(context).ConfigureAwait(false); }
            catch { continue; }

            foreach (var action in actions)
            {
                var mapped = await MapActionAsync(action, document.Project.Solution, "refactor", ct)
                    .ConfigureAwait(false);
                if (mapped is not null) results.Add(mapped);
            }
        }

        return results;
    }

    private static async Task<LspCodeAction?> MapActionAsync(
        CodeAction action, Solution solution, string kind, CancellationToken ct)
    {
        LspWorkspaceEdit? edit = null;

        try
        {
            var operations = await action.GetOperationsAsync(ct).ConfigureAwait(false);
            var applyOp = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
            if (applyOp is not null)
                edit = await MapSolutionChangesAsync(solution, applyOp.ChangedSolution, ct)
                    .ConfigureAwait(false);
        }
        catch { return null; }

        return new LspCodeAction
        {
            Title       = action.Title,
            Kind        = kind,
            IsPreferred = action.Priority == CodeActionPriority.High,
            Edit        = edit,
        };
    }

    internal static async Task<LspWorkspaceEdit> MapSolutionChangesAsync(
        Solution oldSolution, Solution newSolution, CancellationToken ct)
    {
        var changes = new Dictionary<string, IReadOnlyList<LspTextEdit>>();
        var solutionChanges = newSolution.GetChanges(oldSolution);

        foreach (var projectChanges in solutionChanges.GetProjectChanges())
        {
            foreach (var docId in projectChanges.GetChangedDocuments())
            {
                var oldDoc = oldSolution.GetDocument(docId);
                var newDoc = newSolution.GetDocument(docId);
                if (oldDoc is null || newDoc is null) continue;

                var textChanges = await newDoc.GetTextChangesAsync(oldDoc, ct).ConfigureAwait(false);
                var oldText = await oldDoc.GetTextAsync(ct).ConfigureAwait(false);

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

                if (edits.Count > 0 && oldDoc.FilePath is not null)
                    changes[new Uri(oldDoc.FilePath).AbsoluteUri] = edits;
            }
        }

        return new LspWorkspaceEdit { Changes = changes };
    }

    private static IReadOnlyList<CodeFixProvider> GetCodeFixProviders(string language)
    {
        return s_codeFixCache.GetOrAdd(language, lang =>
        {
            try
            {
                var assemblies = MefHostServices.DefaultAssemblies;
                var providers = new List<CodeFixProvider>();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        foreach (var type in asm.GetTypes())
                        {
                            if (type.IsAbstract || !typeof(CodeFixProvider).IsAssignableFrom(type)) continue;
                            var attrs = type.GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false);
                            foreach (ExportCodeFixProviderAttribute attr in attrs)
                            {
                                if (attr.Languages.Contains(lang))
                                {
                                    try { providers.Add((CodeFixProvider)Activator.CreateInstance(type)!); }
                                    catch { /* skip providers that fail to construct */ }
                                    break;
                                }
                            }
                        }
                    }
                    catch { /* skip assemblies that fail type enumeration */ }
                }
                return providers;
            }
            catch { return []; }
        });
    }

    private static IReadOnlyList<CodeRefactoringProvider> GetCodeRefactoringProviders(string language)
    {
        return s_refactoringCache.GetOrAdd(language, lang =>
        {
            try
            {
                var assemblies = MefHostServices.DefaultAssemblies;
                var providers = new List<CodeRefactoringProvider>();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        foreach (var type in asm.GetTypes())
                        {
                            if (type.IsAbstract || !typeof(CodeRefactoringProvider).IsAssignableFrom(type)) continue;
                            var attrs = type.GetCustomAttributes(typeof(ExportCodeRefactoringProviderAttribute), false);
                            foreach (ExportCodeRefactoringProviderAttribute attr in attrs)
                            {
                                if (attr.Languages.Contains(lang))
                                {
                                    try { providers.Add((CodeRefactoringProvider)Activator.CreateInstance(type)!); }
                                    catch { /* skip providers that fail to construct */ }
                                    break;
                                }
                            }
                        }
                    }
                    catch { /* skip assemblies that fail type enumeration */ }
                }
                return providers;
            }
            catch { return []; }
        });
    }
}
