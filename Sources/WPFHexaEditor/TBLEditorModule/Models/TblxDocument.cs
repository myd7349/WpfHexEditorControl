//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using WpfHexaEditor.Core.CharacterTable;

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Complete .tblx document structure
    /// </summary>
    public class TblxDocument
    {
        /// <summary>
        /// File format identifier (always "tblx")
        /// </summary>
        public string Format { get; set; } = "tblx";

        /// <summary>
        /// Metadata
        /// </summary>
        public TblxMetadata Metadata { get; set; } = new TblxMetadata();

        /// <summary>
        /// Entries with extended metadata
        /// </summary>
        public List<TblxEntry> Entries { get; set; } = new List<TblxEntry>();

        /// <summary>
        /// Convert from TblStream to TblxDocument
        /// </summary>
        public static TblxDocument FromTblStream(TblStream tbl, TblxMetadata metadata = null)
        {
            var doc = new TblxDocument
            {
                Metadata = metadata ?? new TblxMetadata(),
                Entries = tbl.GetAllEntries()
                    .Select(dte => TblxEntry.FromDte(dte))
                    .ToList()
            };

            return doc;
        }

        /// <summary>
        /// Convert to TblStream
        /// </summary>
        public TblStream ToTblStream()
        {
            var tbl = new TblStream();

            foreach (var entry in Entries)
            {
                var dte = entry.ToDte();
                tbl.Add(dte);
            }

            return tbl;
        }

        /// <summary>
        /// Get entries by category
        /// </summary>
        public List<TblxEntry> GetEntriesByCategory(string category)
        {
            return Entries.Where(e => e.Category == category).ToList();
        }

        /// <summary>
        /// Get all unique categories
        /// </summary>
        public List<string> GetCategories()
        {
            return Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Category))
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
