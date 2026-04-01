// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Providers/RoslynRenameProvider.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Workspace-wide rename using Roslyn Renamer API.
// ==========================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Core.Roslyn.Providers;

internal static class RoslynRenameProvider
{
    public static async Task<LspWorkspaceEdit?> RenameAsync(
        Document document, int line, int column, string newName, CancellationToken ct)
    {
        var symbol = await RoslynNavigationProvider.FindSymbolAsync(document, line, column, ct)
            .ConfigureAwait(false);
        if (symbol is null) return null;

        var newSolution = await Renamer.RenameSymbolAsync(
            document.Project.Solution, symbol, new SymbolRenameOptions(), newName, ct)
            .ConfigureAwait(false);

        return await RoslynCodeActionProvider.MapSolutionChangesAsync(
            document.Project.Solution, newSolution, ct)
            .ConfigureAwait(false);
    }
}
