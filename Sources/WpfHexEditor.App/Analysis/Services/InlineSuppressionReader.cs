// ==========================================================
// Project: WpfHexEditor.App
// File: Analysis/Services/InlineSuppressionReader.cs
// Description: Reads `// CodeAnalysis: suppress WH00xx [— reason]` markers
//              placed directly above a code construct. Filters out matching
//              diagnostics during analysis.
// Architecture Notes:
//     Stateless. Single regex pass per file.
// ==========================================================

using System.Text.RegularExpressions;
using WpfHexEditor.App.Analysis.Models;

namespace WpfHexEditor.App.Analysis.Services;

internal static class InlineSuppressionReader
{
    private static readonly Regex SuppressPattern =
        new(@"//\s*CodeAnalysis:\s*suppress\s+(WH\d{4})", RegexOptions.Compiled);

    /// <summary>Map of (filePath → set of (ruleId, line)). Line = the line of the // comment.</summary>
    public sealed record SuppressionMap(Dictionary<string, HashSet<(string Rule, int Line)>> Entries);

    internal static SuppressionMap Read(IReadOnlyList<string> filePaths)
    {
        var map = new Dictionary<string, HashSet<(string, int)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in filePaths)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(file);
                var set = new HashSet<(string, int)>();
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = SuppressPattern.Match(lines[i]);
                    if (match.Success)
                        set.Add((match.Groups[1].Value, i + 1));
                }
                if (set.Count > 0) map[file] = set;
            }
            catch { /* skip unreadable */ }
        }

        return new SuppressionMap(map);
    }

    internal static bool IsSuppressed(AnalysisDiagnostic d, SuppressionMap map)
    {
        if (!map.Entries.TryGetValue(d.FilePath, out var set)) return false;
        // Marker on the line above OR same line
        return set.Contains((d.Id, d.Line - 1)) || set.Contains((d.Id, d.Line));
    }
}
