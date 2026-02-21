//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Metadata for .tblx extended format
    /// </summary>
    public class TblxMetadata
    {
        /// <summary>
        /// Format version (e.g., "1.0")
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Table name/title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the character table
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Author name
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Game information
        /// </summary>
        public GameInfo Game { get; set; }

        /// <summary>
        /// Character encoding name (e.g., "Shift-JIS", "ASCII Extended")
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// Categories for organizing entries
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Tags for search and filtering
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Custom properties (key-value pairs)
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Validation rules
        /// </summary>
        public ValidationRules Validation { get; set; }
    }

    /// <summary>
    /// Game information for .tblx metadata
    /// </summary>
    public class GameInfo
    {
        /// <summary>
        /// Game title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Game platform (e.g., "NES", "SNES", "GB", "GBA")
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Game region (e.g., "USA", "Japan", "Europe")
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Game version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Release year
        /// </summary>
        public int? ReleaseYear { get; set; }

        /// <summary>
        /// Developer/Publisher
        /// </summary>
        public string Developer { get; set; }
    }

    /// <summary>
    /// Validation rules for .tblx entries
    /// </summary>
    public class ValidationRules
    {
        /// <summary>
        /// Minimum byte length allowed
        /// </summary>
        public int? MinByteLength { get; set; }

        /// <summary>
        /// Maximum byte length allowed
        /// </summary>
        public int? MaxByteLength { get; set; }

        /// <summary>
        /// Allowed byte ranges (e.g., "00-7F", "80-FF")
        /// </summary>
        public List<string> AllowedRanges { get; set; }

        /// <summary>
        /// Forbidden byte values
        /// </summary>
        public List<string> ForbiddenValues { get; set; }

        /// <summary>
        /// Require unique hex entries
        /// </summary>
        public bool RequireUniqueEntries { get; set; } = true;

        /// <summary>
        /// Allow multi-byte sequences
        /// </summary>
        public bool AllowMultiByte { get; set; } = true;

        /// <summary>
        /// Maximum multi-byte length (default: 8 bytes)
        /// </summary>
        public int MaxMultiByteLength { get; set; } = 8;
    }
}
