//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Result of merging TBL entries
    /// </summary>
    public class TblMergeResult
    {
        /// <summary>
        /// Entries that were added
        /// </summary>
        public List<string> Added { get; set; } = new();

        /// <summary>
        /// Entries that were skipped due to conflicts
        /// </summary>
        public List<string> Skipped { get; set; } = new();

        /// <summary>
        /// Entries that were overwritten
        /// </summary>
        public List<string> Overwritten { get; set; } = new();

        /// <summary>
        /// Conflicts that require user decision
        /// </summary>
        public List<TblMergeConflict> Conflicts { get; set; } = new();

        /// <summary>
        /// Get summary message
        /// </summary>
        public string GetSummary() =>
            $"Added: {Added.Count}, Skipped: {Skipped.Count}, Overwritten: {Overwritten.Count}, Conflicts: {Conflicts.Count}";
    }

    /// <summary>
    /// Represents a conflict during merge
    /// </summary>
    public class TblMergeConflict
    {
        /// <summary>
        /// Hex key that conflicts
        /// </summary>
        public string HexKey { get; set; }

        /// <summary>
        /// Existing value
        /// </summary>
        public string ExistingValue { get; set; }

        /// <summary>
        /// New value
        /// </summary>
        public string NewValue { get; set; }
    }
}
