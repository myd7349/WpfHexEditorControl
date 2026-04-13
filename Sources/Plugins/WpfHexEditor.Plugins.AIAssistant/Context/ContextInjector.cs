// ==========================================================
// Project: WpfHexEditor.Plugins.AIAssistant
// File: ContextInjector.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Resolves @mention tokens into context blocks using IDE services.
//     Supports @file, @selection, @errors, @solution, @hex.
// ==========================================================
using System.IO;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.Plugins.AIAssistant.Context;

public sealed class ContextInjector
{
    private readonly IIDEHostContext _context;

    public ContextInjector(IIDEHostContext context) => _context = context;

    public async Task<ContextPayload> BuildContextAsync(string rawInput, CancellationToken ct = default)
    {
        var mentions = MentionParser.Parse(rawInput);
        var payload = new ContextPayload { CleanedText = MentionParser.RemoveMentions(rawInput) };

        foreach (var mention in mentions)
        {
            var block = mention.Kind switch
            {
                ContextKind.File => await ResolveFileAsync(mention.Argument, ct),
                ContextKind.Selection => ResolveSelection(),
                ContextKind.Errors => ResolveErrors(),
                ContextKind.Solution => ResolveSolution(),
                ContextKind.Hex => ResolveHex(),
                _ => null
            };

            if (block is not null)
                payload.Blocks.Add(block);
        }

        return payload;
    }

    private async Task<ContextBlock?> ResolveFileAsync(string? filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = _context.CodeEditor?.CurrentFilePath;
            if (string.IsNullOrEmpty(filePath)) return null;
        }

        if (!File.Exists(filePath)) return null;

        var info = new FileInfo(filePath);
        if (info.Length > 65536)
            return new ContextBlock($"File: {Path.GetFileName(filePath)} (truncated to 64KB)",
                (await File.ReadAllTextAsync(filePath, ct))[..65536], ContextKind.File);

        var content = await File.ReadAllTextAsync(filePath, ct);
        return new ContextBlock($"File: {Path.GetFileName(filePath)}", content, ContextKind.File);
    }

    private ContextBlock? ResolveSelection()
    {
        var text = _context.CodeEditor?.GetSelectedText();
        if (string.IsNullOrEmpty(text)) return null;

        var lang = _context.CodeEditor?.CurrentLanguage ?? "text";
        return new ContextBlock($"Selected code ({lang})", text, ContextKind.Selection);
    }

    private ContextBlock? ResolveErrors()
    {
        var errors = _context.ErrorPanel.GetRecentErrors(10);
        if (errors.Count == 0) return null;

        var content = string.Join(Environment.NewLine, errors);
        return new ContextBlock("Build Errors", content, ContextKind.Errors);
    }

    private ContextBlock? ResolveSolution()
    {
        if (!_context.SolutionExplorer.HasActiveSolution) return null;

        var paths = _context.SolutionExplorer.GetSolutionFilePaths();
        if (paths.Count == 0) return null;

        var name = _context.SolutionExplorer.ActiveSolutionName ?? "Solution";
        var content = string.Join(Environment.NewLine, paths);
        return new ContextBlock($"Solution: {name}", content, ContextKind.Solution);
    }

    private ContextBlock? ResolveHex()
    {
        if (!_context.HexEditor.IsActive) return null;

        var bytes = _context.HexEditor.GetSelectedBytes();
        if (bytes.Length == 0) return null;

        var hex = BitConverter.ToString(bytes).Replace("-", " ");
        return new ContextBlock(
            $"Hex Selection (offset 0x{_context.HexEditor.SelectionStart:X}, {bytes.Length} bytes)",
            hex,
            ContextKind.Hex);
    }
}
