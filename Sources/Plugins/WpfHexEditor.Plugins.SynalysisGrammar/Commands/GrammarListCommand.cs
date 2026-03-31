// ==========================================================
// Project: WpfHexEditor.Plugins.SynalysisGrammar
// File: Commands/GrammarListCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — list all available grammars.
// ==========================================================

using WpfHexEditor.Core.SynalysisGrammar;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.SynalysisGrammar.Commands;

internal sealed class GrammarListCommand(SynalysisGrammarRepository repository) : PluginTerminalCommandBase
{
    public override string CommandName => "grammar-list";
    public override string Description => "List all available Synalysis grammars.";
    public override string Usage       => "grammar-list [filter]";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        var filter = args.Length > 0 ? args[0] : null;
        var all    = repository.GetAll();

        var grammars = filter is null
            ? all
            : all.Where(e =>
                e.Key.Contains(filter,  StringComparison.OrdinalIgnoreCase) ||
                e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (grammars.Count == 0)
        {
            output.WriteWarning(filter is null ? "No grammars registered." : $"No grammars matching '{filter}'.");
            return Task.FromResult(0);
        }

        output.WriteInfo($"{grammars.Count} grammar{(grammars.Count == 1 ? "" : "s")}{(filter is null ? "" : $" matching '{filter}'")}:");
        output.WriteLine($"  {"Key",-40} Name");
        output.WriteLine($"  {"───",-40} ────");
        foreach (var g in grammars)
            output.WriteLine($"  {g.Key,-40} {g.Name}");

        return Task.FromResult(0);
    }
}
