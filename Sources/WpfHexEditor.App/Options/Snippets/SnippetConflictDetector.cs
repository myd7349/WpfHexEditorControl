// Project      : WpfHexEditor.App
// File         : Options/Snippets/SnippetConflictDetector.cs
// Description  : Detects trigger collisions among user-defined snippets.
// Architecture : Pure logic — no WPF, no IO. Called from SnippetsOptionsPage.

using System.Collections.Generic;
using System.Linq;
using WpfHexEditor.Editor.CodeEditor.Snippets;

namespace WpfHexEditor.App.Options.Snippets;

/// <summary>A group of snippets sharing the same trigger + language.</summary>
public readonly record struct SnippetConflict(string LanguageId, string Trigger, int Count);

/// <summary>
/// Scans a snippet list and returns every (Trigger, LanguageId) pair that
/// appears more than once.
/// </summary>
public static class SnippetConflictDetector
{
    public static IReadOnlyList<SnippetConflict> DetectConflicts(
        IReadOnlyList<StoredSnippet> snippets)
    {
        return snippets
            .GroupBy(s => (s.LanguageId, s.Trigger), StringComparer_LanguageTrigger.Instance)
            .Where(g => g.Count() > 1)
            .Select(g => new SnippetConflict(g.Key.LanguageId, g.Key.Trigger, g.Count()))
            .ToList();
    }
}

file sealed class StringComparer_LanguageTrigger
    : IEqualityComparer<(string LanguageId, string Trigger)>
{
    public static readonly StringComparer_LanguageTrigger Instance = new();

    public bool Equals((string LanguageId, string Trigger) x,
                       (string LanguageId, string Trigger) y)
        => StringComparer.OrdinalIgnoreCase.Equals(x.LanguageId, y.LanguageId)
        && StringComparer.OrdinalIgnoreCase.Equals(x.Trigger,    y.Trigger);

    public int GetHashCode((string LanguageId, string Trigger) obj)
        => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.LanguageId),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Trigger));
}
