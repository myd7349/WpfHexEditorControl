// ==========================================================
// Project: WpfHexEditor.Plugins.SynalysisGrammar
// File: Commands/GrammarApplyCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — apply a grammar by key.
// ==========================================================

using WpfHexEditor.Core.SynalysisGrammar;
using WpfHexEditor.Plugins.SynalysisGrammar.Services;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.SynalysisGrammar.Commands;

internal sealed class GrammarApplyCommand(
    SynalysisGrammarService service,
    SynalysisGrammarRepository repository) : PluginTerminalCommandBase
{
    public override string CommandName => "grammar-apply";
    public override string Description => "Apply a Synalysis grammar by key or partial name.";
    public override string Usage       => "grammar-apply <key-or-name>";

    protected override async Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        if (!RequireArgs(1, args, output, Usage)) return 1;

        var term = string.Join(" ", args);

        // Try exact key first, then partial name match.
        var all   = repository.GetAll();
        var entry = all.FirstOrDefault(e =>
            string.Equals(e.Key,  term, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name, term, StringComparison.OrdinalIgnoreCase)) ??
            all.FirstOrDefault(e =>
                e.Key.Contains(term,  StringComparison.OrdinalIgnoreCase) ||
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            output.WriteError($"Grammar not found: '{term}'. Use 'grammar-list' to see available grammars.");
            return 1;
        }

        output.WriteInfo($"Applying grammar: {entry.Name}…");
        await service.ApplyByKeyAsync(entry.Key, ct).ConfigureAwait(false);
        output.WriteInfo($"Grammar applied: {entry.Name}");
        return 0;
    }
}
