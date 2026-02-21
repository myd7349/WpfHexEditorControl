//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfHexaEditor.TBLEditorModule.Models;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Service for validating TBL entries with synchronous and async support
    /// </summary>
    public class TblValidationService
    {
        /// <summary>
        /// Validate a single entry (fast synchronous validation)
        /// </summary>
        public TblValidationResult ValidateEntry(string entry, string value)
        {
            // Check if entry is valid hex
            if (!IsValidHexEntry(entry))
                return TblValidationResult.Error("Invalid hex format");

            // Check length
            if (!HasValidLength(entry))
                return TblValidationResult.Error($"Length must be 2-16 characters, got {entry?.Length ?? 0}");

            // Check value is not empty
            if (!IsValidValue(value))
                return TblValidationResult.Error("Value cannot be empty");

            return TblValidationResult.Success();
        }

        /// <summary>
        /// Check if hex entry is valid format
        /// </summary>
        public bool IsValidHexEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
                return false;

            // Must be even length
            if (entry.Length % 2 != 0)
                return false;

            // All chars must be hex digits
            return Regex.IsMatch(entry, "^[0-9A-Fa-f]+$");
        }

        /// <summary>
        /// Check if entry has valid length (2-16 chars for 1-8 bytes)
        /// </summary>
        public bool HasValidLength(string entry)
        {
            if (string.IsNullOrEmpty(entry))
                return false;

            return entry.Length >= 2 && entry.Length <= 16 && entry.Length % 2 == 0;
        }

        /// <summary>
        /// Check if value is valid
        /// </summary>
        public bool IsValidValue(string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Check for duplicate entry in list
        /// </summary>
        public bool HasDuplicateEntry(string entry, IEnumerable<TblEntryViewModel> existingEntries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                return false;

            var upperEntry = entry.ToUpperInvariant();
            return existingEntries.Any(e => e.Entry.ToUpperInvariant() == upperEntry);
        }

        /// <summary>
        /// Validate all entries asynchronously
        /// </summary>
        public async Task<Dictionary<TblEntryViewModel, TblValidationResult>> ValidateAllAsync(
            IEnumerable<TblEntryViewModel> entries,
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<TblEntryViewModel, TblValidationResult>();

            await Task.Run(() =>
            {
                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = ValidateEntry(entry.Entry, entry.Value);
                    results[entry] = result;

                    // Update entry's validation state
                    entry.IsValid = result.IsValid;
                    entry.ValidationError = result.ErrorMessage;
                }
            }, cancellationToken);

            return results;
        }
    }
}
