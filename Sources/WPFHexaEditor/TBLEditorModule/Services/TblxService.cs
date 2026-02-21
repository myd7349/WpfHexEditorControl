//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Service for .tblx extended format operations
    /// </summary>
    public class TblxService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        #region Load/Import

        /// <summary>
        /// Load .tblx file
        /// </summary>
        public TblxDocument LoadFromFile(string filePath)
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                return LoadFromString(jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load .tblx file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load .tblx from JSON string
        /// </summary>
        public TblxDocument LoadFromString(string jsonContent)
        {
            try
            {
                var doc = JsonSerializer.Deserialize<TblxDocument>(jsonContent, _jsonOptions);

                // Validate format identifier
                if (doc?.Format != "tblx")
                    throw new Exception("Invalid .tblx format: missing or incorrect format identifier");

                return doc;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Invalid .tblx JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Import .tblx into TblStream
        /// </summary>
        public TblImportResult ImportToTblStream(string filePath)
        {
            var result = new TblImportResult { DetectedFormat = TblFileFormat.Tblx };

            try
            {
                var doc = LoadFromFile(filePath);

                // Convert entries to DTE list
                foreach (var entry in doc.Entries)
                {
                    try
                    {
                        var dte = entry.ToDte();
                        if (dte.IsValid)
                        {
                            result.Entries.Add(dte);
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                            result.Warnings.Add($"Skipped invalid entry: {entry.Entry}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.SkippedCount++;
                        result.Warnings.Add($"Entry {entry.Entry}: {ex.Message}");
                    }
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        #endregion

        #region Save/Export

        /// <summary>
        /// Save .tblx file
        /// </summary>
        public void SaveToFile(TblxDocument document, string filePath)
        {
            try
            {
                // Update modified date
                document.Metadata.ModifiedDate = DateTime.Now;

                var jsonContent = SaveToString(document);
                File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save .tblx file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save .tblx to JSON string
        /// </summary>
        public string SaveToString(TblxDocument document)
        {
            try
            {
                return JsonSerializer.Serialize(document, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to serialize .tblx: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export TblStream to .tblx file
        /// </summary>
        public void ExportFromTblStream(TblStream tbl, string filePath, TblxMetadata metadata = null)
        {
            try
            {
                var doc = TblxDocument.FromTblStream(tbl, metadata);

                // Set creation date if not specified
                if (doc.Metadata.CreatedDate == null)
                    doc.Metadata.CreatedDate = DateTime.Now;

                SaveToFile(doc, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export .tblx: {ex.Message}", ex);
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create a new .tblx document with metadata
        /// </summary>
        public TblxDocument CreateNew(TblxMetadata metadata = null)
        {
            var doc = new TblxDocument
            {
                Metadata = metadata ?? new TblxMetadata
                {
                    CreatedDate = DateTime.Now,
                    Version = "1.0"
                }
            };

            return doc;
        }

        /// <summary>
        /// Validate .tblx document
        /// </summary>
        public TblValidationResult Validate(TblxDocument document)
        {
            var result = new TblValidationResult { IsValid = true };

            // Check format identifier
            if (document.Format != "tblx")
            {
                result.IsValid = false;
                result.Errors.Add("Invalid format identifier. Expected 'tblx'.");
            }

            // Check metadata
            if (document.Metadata == null)
            {
                result.IsValid = false;
                result.Errors.Add("Metadata is required.");
            }

            // Validate entries
            if (document.Entries == null || document.Entries.Count == 0)
            {
                result.Warnings.Add("Document contains no entries.");
            }
            else
            {
                var validationService = new TblValidationService();
                foreach (var entry in document.Entries)
                {
                    var entryResult = validationService.ValidateEntry(entry.Entry, entry.Value);
                    if (!entryResult.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Entry {entry.Entry}: {entryResult.ErrorMessage}");
                    }
                }
            }

            // Validate against rules if specified
            if (document.Metadata.Validation != null)
            {
                ValidateAgainstRules(document, result);
            }

            return result;
        }

        private void ValidateAgainstRules(TblxDocument document, TblValidationResult result)
        {
            var rules = document.Metadata.Validation;

            foreach (var entry in document.Entries)
            {
                int byteLength = entry.Entry.Length / 2;

                // Check byte length constraints
                if (rules.MinByteLength.HasValue && byteLength < rules.MinByteLength.Value)
                {
                    result.Warnings.Add($"Entry {entry.Entry}: Byte length {byteLength} is below minimum {rules.MinByteLength.Value}");
                }

                if (rules.MaxByteLength.HasValue && byteLength > rules.MaxByteLength.Value)
                {
                    result.Errors.Add($"Entry {entry.Entry}: Byte length {byteLength} exceeds maximum {rules.MaxByteLength.Value}");
                    result.IsValid = false;
                }

                // Check multi-byte constraints
                if (!rules.AllowMultiByte && byteLength > 1)
                {
                    result.Errors.Add($"Entry {entry.Entry}: Multi-byte entries are not allowed");
                    result.IsValid = false;
                }

                if (byteLength > rules.MaxMultiByteLength)
                {
                    result.Errors.Add($"Entry {entry.Entry}: Multi-byte length {byteLength} exceeds maximum {rules.MaxMultiByteLength}");
                    result.IsValid = false;
                }
            }
        }

        #endregion
    }
}
