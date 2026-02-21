//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Fast search and filter service with hash-based indexing
    /// </summary>
    public class TblSearchService
    {
        // O(1) lookup indexes
        private Dictionary<string, List<TblEntryViewModel>> _entryIndex;
        private Dictionary<DteType, List<TblEntryViewModel>> _typeIndex;
        private Dictionary<char, List<TblEntryViewModel>> _valueFirstCharIndex;

        /// <summary>
        /// Build search indexes from entries
        /// </summary>
        public async Task BuildIndexAsync(IEnumerable<TblEntryViewModel> entries)
        {
            await Task.Run(() => BuildIndex(entries));
        }

        /// <summary>
        /// Build search indexes (synchronous)
        /// </summary>
        public void BuildIndex(IEnumerable<TblEntryViewModel> entries)
        {
            var entryList = entries.ToList();

            // Index by entry hex (case-insensitive)
            _entryIndex = entryList
                .GroupBy(e => e.Entry.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());

            // Index by type
            _typeIndex = entryList
                .GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Index by first character of value
            _valueFirstCharIndex = entryList
                .Where(e => !string.IsNullOrEmpty(e.Value))
                .GroupBy(e => e.Value[0])
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Search by entry hex value
        /// </summary>
        public List<TblEntryViewModel> SearchByEntry(string hexPattern)
        {
            if (string.IsNullOrWhiteSpace(hexPattern) || _entryIndex == null)
                return new List<TblEntryViewModel>();

            var upper = hexPattern.ToUpperInvariant();

            // Exact match
            if (_entryIndex.TryGetValue(upper, out var exact))
                return new List<TblEntryViewModel>(exact);

            // Prefix match
            return _entryIndex
                .Where(kvp => kvp.Key.StartsWith(upper))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }

        /// <summary>
        /// Search by value (character)
        /// </summary>
        public List<TblEntryViewModel> SearchByValue(string text, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(text) || _valueFirstCharIndex == null)
                return new List<TblEntryViewModel>();

            // Fast path: search by first character
            if (text.Length == 1)
            {
                var firstChar = caseSensitive ? text[0] : char.ToLowerInvariant(text[0]);
                if (_valueFirstCharIndex.TryGetValue(firstChar, out var matches))
                    return new List<TblEntryViewModel>(matches);
            }

            // Full text search
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return _entryIndex.Values
                .SelectMany(list => list)
                .Where(e => e.Value.Contains(text, comparison))
                .ToList();
        }

        /// <summary>
        /// Filter by type
        /// </summary>
        public List<TblEntryViewModel> FilterByType(DteType type)
        {
            if (_typeIndex == null)
                return new List<TblEntryViewModel>();

            return _typeIndex.TryGetValue(type, out var matches)
                ? new List<TblEntryViewModel>(matches)
                : new List<TblEntryViewModel>();
        }

        /// <summary>
        /// Filter by byte length
        /// </summary>
        public List<TblEntryViewModel> FilterByByteLength(int byteLength, IEnumerable<TblEntryViewModel> entries)
        {
            return entries.Where(e => e.ByteLength == byteLength).ToList();
        }

        /// <summary>
        /// Apply multiple filters
        /// </summary>
        public List<TblEntryViewModel> ApplyFilters(
            IEnumerable<TblEntryViewModel> entries,
            string searchText = null,
            DteType? filterType = null,
            bool showConflictsOnly = false)
        {
            var results = entries.AsEnumerable();

            // Apply text search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                results = results.Where(e =>
                    e.Entry.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    e.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            // Apply type filter
            if (filterType.HasValue)
            {
                results = results.Where(e => e.Type == filterType.Value);
            }

            // Apply conflict filter
            if (showConflictsOnly)
            {
                results = results.Where(e => e.HasConflict);
            }

            return results.ToList();
        }
    }
}
