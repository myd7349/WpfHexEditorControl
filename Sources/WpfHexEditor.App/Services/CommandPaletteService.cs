//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : CommandPaletteService.cs
// Description  : Catalog and fuzzy-search engine for the Command Palette.
//                Aggregates built-in entries and plugin-contributed menu items.
// Architecture : Pure service; no WPF dependency. Consumed by CommandPaletteWindow.
//////////////////////////////////////////////

using WpfHexEditor.App.Models;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Manages the full command catalog and provides fuzzy-filtered results for the Command Palette.
/// </summary>
public sealed class CommandPaletteService
{
    private readonly List<CommandPaletteEntry> _allEntries;

    public CommandPaletteService(IEnumerable<CommandPaletteEntry> entries)
    {
        _allEntries = entries
            .OrderBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>All entries sorted by Category then Name.</summary>
    public IReadOnlyList<CommandPaletteEntry> AllEntries => _allEntries;

    /// <summary>
    /// Returns up to 50 entries matching <paramref name="query"/> using a three-tier fuzzy score:
    /// prefix (1000), substring (500), scattered subsequence (100). Empty query returns all entries.
    /// </summary>
    public IReadOnlyList<CommandPaletteEntry> Filter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allEntries;

        var q = query.Trim();
        var scored = new List<(CommandPaletteEntry Entry, int Score)>(_allEntries.Count);

        foreach (var entry in _allEntries)
        {
            var score = FuzzyScore(entry.Name, q);
            if (score < 0)
                score = FuzzyScore(entry.Category, q);
            if (score >= 0)
                scored.Add((entry, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Select(x => x.Entry)
            .ToList();
    }

    // Returns -1 if no match; otherwise a positive score (higher = better match).
    private static int FuzzyScore(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack)) return -1;

        var h = haystack.AsSpan();
        var n = needle.AsSpan();

        // Tier 1: prefix match
        if (h.StartsWith(n, StringComparison.OrdinalIgnoreCase))
            return 1000 - haystack.Length;

        // Tier 2: substring match
        var idx = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return 500 - idx;

        // Tier 3: scattered subsequence
        var gaps = CountSubsequenceGaps(h, n);
        if (gaps >= 0)
            return 100 - gaps;

        return -1;
    }

    // Returns the total gap count if needle is a subsequence of haystack (case-insensitive),
    // or -1 if it is not.
    private static int CountSubsequenceGaps(ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle)
    {
        var hi = 0;
        var ni = 0;
        var gaps = 0;
        var lastMatch = -1;

        while (ni < needle.Length && hi < haystack.Length)
        {
            if (char.ToUpperInvariant(haystack[hi]) == char.ToUpperInvariant(needle[ni]))
            {
                if (lastMatch >= 0)
                    gaps += hi - lastMatch - 1;
                lastMatch = hi;
                ni++;
            }
            hi++;
        }

        return ni == needle.Length ? gaps : -1;
    }
}
