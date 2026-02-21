//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Options for TBL generation from text
    /// </summary>
    public class TblGenerationOptions
    {
        /// <summary>
        /// Sample text to analyze
        /// </summary>
        public string SampleText { get; set; }

        /// <summary>
        /// Whether search is case-sensitive
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Start position in file to search
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        /// End position in file to search (0 = end of file)
        /// </summary>
        public long EndPosition { get; set; }

        /// <summary>
        /// Minimum number of matches required for a proposal
        /// </summary>
        public int MinMatches { get; set; } = 1;

        /// <summary>
        /// Maximum number of proposals to show
        /// </summary>
        public int MaxProposalsToShow { get; set; } = 10;

        /// <summary>
        /// Merge strategy for existing entries
        /// </summary>
        public MergeStrategy MergeStrategy { get; set; } = MergeStrategy.Skip;
    }

    /// <summary>
    /// Strategy for merging generated entries with existing TBL
    /// </summary>
    public enum MergeStrategy
    {
        /// <summary>
        /// Skip conflicting entries (keep existing)
        /// </summary>
        Skip,

        /// <summary>
        /// Overwrite conflicting entries (replace existing)
        /// </summary>
        Overwrite,

        /// <summary>
        /// Ask user for each conflict
        /// </summary>
        Ask
    }
}
