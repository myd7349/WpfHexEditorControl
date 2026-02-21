//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using WpfHexaEditor.Core.CharacterTable;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Represents a conflict detected in TBL file
    /// </summary>
    public class TblConflict
    {
        /// <summary>
        /// Type of conflict
        /// </summary>
        public ConflictType Type { get; set; }

        /// <summary>
        /// Severity level
        /// </summary>
        public ConflictSeverity Severity { get; set; }

        /// <summary>
        /// List of conflicting entries
        /// </summary>
        public List<Dte> ConflictingEntries { get; set; } = new();

        /// <summary>
        /// Description of the conflict
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Suggested resolution
        /// </summary>
        public string Suggestion { get; set; }

        /// <summary>
        /// Get detailed message for display
        /// </summary>
        public string GetDetailedMessage()
        {
            if (ConflictingEntries.Count == 0)
                return Description;

            var entries = string.Join(", ", ConflictingEntries.ConvertAll(e => $"'{e.Entry}'"));
            return $"{Description} - Entries: {entries}";
        }

        /// <summary>
        /// Get conflicting entries as formatted text
        /// </summary>
        public string ConflictingEntriesText =>
            string.Join(", ", ConflictingEntries.ConvertAll(e => e.Entry));
    }
}
