//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using WpfHexaEditor.Core.CharacterTable;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Extended TBL entry with additional metadata for .tblx format
    /// </summary>
    public class TblxEntry
    {
        /// <summary>
        /// Hex entry (e.g., "41", "8283")
        /// </summary>
        public string Entry { get; set; }

        /// <summary>
        /// Character value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Entry type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Byte count
        /// </summary>
        public int ByteCount { get; set; }

        /// <summary>
        /// Category (e.g., "Letters", "Numbers", "Symbols", "Control Codes")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Description/Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Usage frequency (0-100, for analytics)
        /// </summary>
        public int? Frequency { get; set; }

        /// <summary>
        /// Is this a commonly used entry?
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Convert from DTE to TblxEntry
        /// </summary>
        public static TblxEntry FromDte(Dte dte, string category = null)
        {
            return new TblxEntry
            {
                Entry = dte.Entry,
                Value = dte.Value,
                Type = dte.Type.ToString(),
                ByteCount = dte.Entry.Length / 2,
                Comment = dte.Comment,
                Category = category
            };
        }

        /// <summary>
        /// Convert to DTE
        /// </summary>
        public Dte ToDte()
        {
            var dte = new Dte(Entry, Value);
            if (!string.IsNullOrWhiteSpace(Comment))
                dte.Comment = Comment;
            return dte;
        }
    }
}
