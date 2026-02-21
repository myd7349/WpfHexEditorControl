//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Service for managing TBL templates (built-in and user-defined)
    /// </summary>
    public class TblTemplateService
    {
        private readonly string _userTemplatesPath;
        private List<TblTemplate> _cachedTemplates;

        public TblTemplateService()
        {
            _userTemplatesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WpfHexEditor",
                "TblTemplates"
            );

            EnsureUserTemplatesDirectory();
        }

        /// <summary>
        /// Get all templates (built-in + user)
        /// </summary>
        public List<TblTemplate> GetAllTemplates()
        {
            if (_cachedTemplates != null)
                return _cachedTemplates;

            var templates = new List<TblTemplate>();

            // Add built-in templates
            templates.AddRange(GetBuiltInTemplates());

            // Add user templates
            templates.AddRange(LoadUserTemplates());

            _cachedTemplates = templates;
            return templates;
        }

        /// <summary>
        /// Get templates by category
        /// </summary>
        public List<TblTemplate> GetTemplatesByCategory(string category)
        {
            return GetAllTemplates()
                .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Get all unique categories
        /// </summary>
        public List<string> GetCategories()
        {
            return GetAllTemplates()
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// Save a TBL as a user template
        /// </summary>
        public void SaveAsTemplate(TblStream tbl, string name, string description, string category)
        {
            var template = new TblTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                Category = category,
                Author = Environment.UserName,
                IsBuiltIn = false,
                CreatedDate = DateTime.Now,
                TblContent = ExportTblToString(tbl)
            };

            SaveUserTemplate(template);

            // Invalidate cache
            _cachedTemplates = null;
        }

        /// <summary>
        /// Delete a user template
        /// </summary>
        public bool DeleteTemplate(string templateId)
        {
            var filePath = Path.Combine(_userTemplatesPath, $"{templateId}.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _cachedTemplates = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get built-in templates
        /// </summary>
        private List<TblTemplate> GetBuiltInTemplates()
        {
            return new List<TblTemplate>
            {
                new TblTemplate
                {
                    Id = "ascii-standard",
                    Name = "ASCII Standard",
                    Description = "Standard ASCII table (0x20-0x7E)",
                    Category = "Standard",
                    DefaultType = DefaultCharacterTableType.Ascii,
                    IsBuiltIn = true
                },
                new TblTemplate
                {
                    Id = "ebcdic",
                    Name = "EBCDIC",
                    Description = "Extended Binary Coded Decimal Interchange Code",
                    Category = "Standard",
                    DefaultType = DefaultCharacterTableType.EbcdicWithSpecialChar,
                    IsBuiltIn = true
                },
                new TblTemplate
                {
                    Id = "nes-default",
                    Name = "NES Default",
                    Description = "Standard NES character encoding (shifted ASCII)",
                    Category = "Game Systems",
                    TblContent = GenerateNesDefaultTbl(),
                    IsBuiltIn = true
                },
                new TblTemplate
                {
                    Id = "snes-default",
                    Name = "SNES Default",
                    Description = "Standard SNES character encoding (ASCII direct)",
                    Category = "Game Systems",
                    TblContent = GenerateSnesDefaultTbl(),
                    IsBuiltIn = true
                },
                new TblTemplate
                {
                    Id = "latin1",
                    Name = "Latin-1 (ISO-8859-1)",
                    Description = "Western European character set",
                    Category = "Unicode",
                    TblContent = GenerateLatin1Tbl(),
                    IsBuiltIn = true
                },
                new TblTemplate
                {
                    Id = "katakana-halfwidth",
                    Name = "Japanese Katakana (Half-width)",
                    Description = "Half-width Katakana characters (U+FF61-U+FF9F)",
                    Category = "Unicode",
                    TblContent = GenerateKatakanaTbl(),
                    IsBuiltIn = true
                }
            };
        }

        /// <summary>
        /// Generate NES Default TBL (shifted ASCII)
        /// </summary>
        private string GenerateNesDefaultTbl()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# NES Default Character Table");
            sb.AppendLine("# Shifted ASCII encoding commonly used in NES games");
            sb.AppendLine();

            // Uppercase A-Z (0x00-0x19)
            for (int i = 0; i < 26; i++)
                sb.AppendLine($"{i:X2}={Convert.ToChar('A' + i)}");

            // Lowercase a-z (0x1A-0x33)
            for (int i = 0; i < 26; i++)
                sb.AppendLine($"{(i + 0x1A):X2}={Convert.ToChar('a' + i)}");

            // Digits 0-9 (0x34-0x3D)
            for (int i = 0; i < 10; i++)
                sb.AppendLine($"{(i + 0x34):X2}={Convert.ToChar('0' + i)}");

            // Common punctuation
            sb.AppendLine("3E= ");      // Space
            sb.AppendLine("3F=!");
            sb.AppendLine("40=?");
            sb.AppendLine("41=.");
            sb.AppendLine("42=,");
            sb.AppendLine("43=-");
            sb.AppendLine("44='");
            sb.AppendLine("45=\"");

            // Special characters
            sb.AppendLine("/50=<END>");
            sb.AppendLine("*51=<NL>");

            return sb.ToString();
        }

        /// <summary>
        /// Generate SNES Default TBL (ASCII direct + control codes)
        /// </summary>
        private string GenerateSnesDefaultTbl()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# SNES Default Character Table");
            sb.AppendLine("# Direct ASCII mapping with control codes");
            sb.AppendLine();

            // Direct ASCII mapping (0x20-0x7E)
            for (int i = 0x20; i <= 0x7E; i++)
                sb.AppendLine($"{i:X2}={Convert.ToChar(i)}");

            // Control codes
            sb.AppendLine();
            sb.AppendLine("# Control codes");
            sb.AppendLine("/00=<END>");
            sb.AppendLine("*0A=<NL>");
            sb.AppendLine("0D=<CR>");

            return sb.ToString();
        }

        /// <summary>
        /// Generate Latin-1 (ISO-8859-1) TBL
        /// </summary>
        private string GenerateLatin1Tbl()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Latin-1 (ISO-8859-1) Character Table");
            sb.AppendLine("# Western European character set");
            sb.AppendLine();

            // Standard ASCII (0x20-0x7E)
            for (int i = 0x20; i <= 0x7E; i++)
                sb.AppendLine($"{i:X2}={Convert.ToChar(i)}");

            // Extended Latin-1 (0xA0-0xFF)
            sb.AppendLine();
            sb.AppendLine("# Extended characters");
            for (int i = 0xA0; i <= 0xFF; i++)
                sb.AppendLine($"{i:X2}={Convert.ToChar(i)}");

            return sb.ToString();
        }

        /// <summary>
        /// Generate Japanese Katakana (half-width) TBL
        /// </summary>
        private string GenerateKatakanaTbl()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Japanese Katakana (Half-width) Character Table");
            sb.AppendLine("# Half-width Katakana range U+FF61 to U+FF9F");
            sb.AppendLine();

            // Half-width Katakana (mapped to 0xA1-0xDF)
            for (int i = 0; i < 63; i++)
            {
                char katakana = Convert.ToChar(0xFF61 + i);
                sb.AppendLine($"{(0xA1 + i):X2}={katakana}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Load user templates from disk
        /// </summary>
        private List<TblTemplate> LoadUserTemplates()
        {
            var templates = new List<TblTemplate>();

            if (!Directory.Exists(_userTemplatesPath))
                return templates;

            foreach (var file in Directory.GetFiles(_userTemplatesPath, "*.tbltemplate"))
            {
                try
                {
                    var template = LoadTemplateFromFile(file);
                    if (template != null)
                        templates.Add(template);
                }
                catch (Exception)
                {
                    // Skip invalid template files
                }
            }

            return templates;
        }

        /// <summary>
        /// Save a user template to disk
        /// </summary>
        private void SaveUserTemplate(TblTemplate template)
        {
            EnsureUserTemplatesDirectory();

            var filePath = Path.Combine(_userTemplatesPath, $"{template.Id}.tbltemplate");
            SaveTemplateToFile(template, filePath);
        }

        /// <summary>
        /// Load template from file (simple format)
        /// </summary>
        private TblTemplate LoadTemplateFromFile(string filePath)
        {
            var template = new TblTemplate();
            var tblContent = new StringBuilder();
            var inTblSection = false;

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (line.StartsWith("[TEMPLATE]"))
                    inTblSection = false;
                else if (line.StartsWith("[TBL]"))
                    inTblSection = true;
                else if (inTblSection)
                    tblContent.AppendLine(line);
                else if (line.StartsWith("Id="))
                    template.Id = line.Substring(3);
                else if (line.StartsWith("Name="))
                    template.Name = line.Substring(5);
                else if (line.StartsWith("Description="))
                    template.Description = line.Substring(12);
                else if (line.StartsWith("Author="))
                    template.Author = line.Substring(7);
                else if (line.StartsWith("Category="))
                    template.Category = line.Substring(9);
            }

            template.TblContent = tblContent.ToString();
            template.IsBuiltIn = false;
            return template;
        }

        /// <summary>
        /// Save template to file (simple format)
        /// </summary>
        private void SaveTemplateToFile(TblTemplate template, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[TEMPLATE]");
            sb.AppendLine($"Id={template.Id}");
            sb.AppendLine($"Name={template.Name}");
            sb.AppendLine($"Description={template.Description ?? ""}");
            sb.AppendLine($"Author={template.Author ?? ""}");
            sb.AppendLine($"Category={template.Category ?? "Custom"}");
            sb.AppendLine();
            sb.AppendLine("[TBL]");
            sb.Append(template.TblContent);

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Ensure user templates directory exists
        /// </summary>
        private void EnsureUserTemplatesDirectory()
        {
            if (!Directory.Exists(_userTemplatesPath))
                Directory.CreateDirectory(_userTemplatesPath);
        }

        /// <summary>
        /// Export TblStream to string format
        /// </summary>
        private string ExportTblToString(TblStream tbl)
        {
            var sb = new StringBuilder();

            foreach (var dte in tbl.GetAllEntries().OrderBy(d => d.Entry))
            {
                // Escape special characters in value
                string escapedValue = dte.Value
                    .Replace("\\", "\\\\")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");

                // Handle special types
                if (dte.Type == DteType.EndBlock)
                    sb.AppendLine($"/{dte.Entry}={escapedValue}");
                else if (dte.Type == DteType.EndLine)
                    sb.AppendLine($"*{dte.Entry}={escapedValue}");
                else
                    sb.AppendLine($"{dte.Entry}={escapedValue}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Invalidate template cache
        /// </summary>
        public void InvalidateCache()
        {
            _cachedTemplates = null;
        }
    }
}
