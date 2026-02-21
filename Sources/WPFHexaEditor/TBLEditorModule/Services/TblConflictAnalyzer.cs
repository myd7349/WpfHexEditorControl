//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Conflict analyzer using Trie algorithm for prefix detection
    /// </summary>
    public class TblConflictAnalyzer
    {
        private class TrieNode
        {
            public Dictionary<char, TrieNode> Children = new();
            public TblEntryViewModel Entry;
            public bool IsTerminal => Entry != null;
        }

        /// <summary>
        /// Analyze TBL for conflicts asynchronously
        /// </summary>
        public async Task<List<TblConflict>> AnalyzeConflictsAsync(
            IEnumerable<TblEntryViewModel> entries,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() => AnalyzeConflicts(entries, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Analyze TBL for conflicts (synchronous)
        /// </summary>
        public List<TblConflict> AnalyzeConflicts(
            IEnumerable<TblEntryViewModel> entries,
            CancellationToken cancellationToken)
        {
            var conflicts = new List<TblConflict>();
            var stopwatch = Stopwatch.StartNew();

            var entryList = entries.ToList();

            // Build Trie
            var trie = BuildTrie(entryList);

            // Detect prefix conflicts
            conflicts.AddRange(DetectPrefixConflicts(trie, cancellationToken));

            // Detect duplicates
            conflicts.AddRange(DetectDuplicates(entryList, cancellationToken));

            stopwatch.Stop();
            Debug.WriteLine($"Conflict analysis completed in {stopwatch.ElapsedMilliseconds}ms, found {conflicts.Count} conflicts");

            return conflicts;
        }

        /// <summary>
        /// Check conflicts for a single new entry
        /// </summary>
        public List<TblConflict> CheckEntryConflicts(
            string newEntry,
            IEnumerable<TblEntryViewModel> existingEntries)
        {
            var conflicts = new List<TblConflict>();
            var upper = newEntry.ToUpperInvariant();

            foreach (var existing in existingEntries)
            {
                var existingUpper = existing.Entry.ToUpperInvariant();

                // Check if new entry is prefix of existing
                if (existingUpper.StartsWith(upper) && existingUpper != upper)
                {
                    conflicts.Add(new TblConflict
                    {
                        Type = ConflictType.PrefixConflict,
                        Severity = ConflictSeverity.Warning,
                        ConflictingEntries = new List<Dte> { existing.ToDto() },
                        Description = $"Entry '{upper}' is prefix of '{existingUpper}'",
                        Suggestion = "Remove shorter entry or use different byte values"
                    });
                }

                // Check if existing is prefix of new entry
                if (upper.StartsWith(existingUpper) && upper != existingUpper)
                {
                    conflicts.Add(new TblConflict
                    {
                        Type = ConflictType.PrefixConflict,
                        Severity = ConflictSeverity.Warning,
                        ConflictingEntries = new List<Dte> { existing.ToDto() },
                        Description = $"Entry '{existingUpper}' is prefix of '{upper}'",
                        Suggestion = "Remove shorter entry or use different byte values"
                    });
                }

                // Check exact duplicate
                if (upper == existingUpper)
                {
                    conflicts.Add(new TblConflict
                    {
                        Type = ConflictType.Duplicate,
                        Severity = ConflictSeverity.Error,
                        ConflictingEntries = new List<Dte> { existing.ToDto() },
                        Description = $"Duplicate entry '{upper}'",
                        Suggestion = "Remove duplicate or merge values"
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Build Trie from entries
        /// </summary>
        private TrieNode BuildTrie(List<TblEntryViewModel> entries)
        {
            var root = new TrieNode();

            foreach (var entry in entries)
            {
                var current = root;
                foreach (char c in entry.Entry.ToUpperInvariant())
                {
                    if (!current.Children.ContainsKey(c))
                        current.Children[c] = new TrieNode();
                    current = current.Children[c];
                }
                current.Entry = entry;
            }

            return root;
        }

        /// <summary>
        /// Detect prefix conflicts using DFS
        /// </summary>
        private List<TblConflict> DetectPrefixConflicts(TrieNode root, CancellationToken cancellationToken)
        {
            var conflicts = new List<TblConflict>();

            void DFS(TrieNode node)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (node.IsTerminal && node.Children.Count > 0)
                {
                    // This entry has longer entries starting with it
                    var longerEntries = new List<Dte>();
                    CollectTerminals(node, longerEntries);

                    if (longerEntries.Count > 0)
                    {
                        conflicts.Add(new TblConflict
                        {
                            Type = ConflictType.PrefixConflict,
                            Severity = ConflictSeverity.Warning,
                            ConflictingEntries = new List<Dte> { node.Entry.ToDto() }.Concat(longerEntries).ToList(),
                            Description = $"Entry '{node.Entry.Entry}' is prefix of {longerEntries.Count} longer entries",
                            Suggestion = "Remove shorter entry or use different byte values"
                        });
                    }
                }

                foreach (var child in node.Children.Values)
                {
                    DFS(child);
                }
            }

            DFS(root);
            return conflicts;
        }

        /// <summary>
        /// Collect all terminal nodes (entries) from this node down
        /// </summary>
        private void CollectTerminals(TrieNode node, List<Dte> terminals)
        {
            foreach (var child in node.Children.Values)
            {
                if (child.IsTerminal)
                    terminals.Add(child.Entry.ToDto());

                CollectTerminals(child, terminals);
            }
        }

        /// <summary>
        /// Detect exact duplicates
        /// </summary>
        private List<TblConflict> DetectDuplicates(List<TblEntryViewModel> entries, CancellationToken cancellationToken)
        {
            var conflicts = new List<TblConflict>();
            var seen = new Dictionary<string, TblEntryViewModel>();

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = entry.Entry.ToUpperInvariant();
                if (seen.TryGetValue(key, out var existing))
                {
                    conflicts.Add(new TblConflict
                    {
                        Type = ConflictType.Duplicate,
                        Severity = ConflictSeverity.Error,
                        ConflictingEntries = new List<Dte> { existing.ToDto(), entry.ToDto() },
                        Description = $"Duplicate entry '{key}'",
                        Suggestion = "Remove duplicate or merge values"
                    });
                }
                else
                {
                    seen[key] = entry;
                }
            }

            return conflicts;
        }
    }
}
