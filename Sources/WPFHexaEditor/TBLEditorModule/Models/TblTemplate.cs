//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using WpfHexaEditor.Core.CharacterTable;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Template for pre-defined TBL character tables
    /// </summary>
    public class TblTemplate
    {
        /// <summary>
        /// Unique identifier (e.g., "nes-default", "snes-default")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name (e.g., "NES Default")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the template
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Author of the template
        /// </summary>
        public string Author { get; set; } = "Built-in";

        /// <summary>
        /// Category (e.g., "Standard", "Game Systems", "Unicode", "Custom")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Default character table type (for built-in templates)
        /// </summary>
        public DefaultCharacterTableType? DefaultType { get; set; }

        /// <summary>
        /// Raw TBL file content
        /// </summary>
        public string TblContent { get; set; }

        /// <summary>
        /// Whether this is a built-in template
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Load template into a new TBLStream
        /// </summary>
        public TblStream Load()
        {
            if (DefaultType.HasValue)
            {
                // Use built-in default
                return TblStream.CreateDefaultTbl(DefaultType.Value);
            }
            else if (!string.IsNullOrEmpty(TblContent))
            {
                // Load from TBL string
                var tbl = new TblStream();
                tbl.Load(TblContent);
                return tbl;
            }

            return null;
        }
    }
}
