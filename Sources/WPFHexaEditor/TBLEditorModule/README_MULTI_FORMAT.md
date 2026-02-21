# TBL Multi-Format Support

Complete documentation for TBL character table import/export in multiple formats (.tbl, .tblx, .csv, .json).

## Table of Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Quick Start](#quick-start)
- [Format Specifications](#format-specifications)
- [API Documentation](#api-documentation)
- [UI Integration](#ui-integration)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

---

## Overview

The WPF Hex Editor TBL module supports bidirectional import/export in 4 different formats:

| Format | Extension | Description | Use Case |
|--------|-----------|-------------|----------|
| **TBL** | `.tbl` | Standard Thingy format | ROM hacking, traditional tools |
| **TBLX** | `.tblx` | Extended JSON format | Metadata, categories, validation |
| **CSV** | `.csv` | Spreadsheet format | Excel editing, bulk operations |
| **JSON** | `.json` | Standard JSON format | Programmatic access, web apps |

**Key Features:**
- ✅ Bidirectional import/export (load ↔ save)
- ✅ Auto-format detection based on file extension
- ✅ Comprehensive error handling with skip-on-error
- ✅ Configurable import/export options
- ✅ No external dependencies (custom CSV parser)
- ✅ Full .NET Framework 4.8 and .NET 8.0 compatibility

---

## Supported Formats

### 1. TBL Format (Standard)

**Extension:** `.tbl`

**Description:** Traditional Thingy table format used in ROM hacking community.

**Format:**
```
# Comments start with #
41=A
42=B
8283=Á
/00=<END>
*0A=<LN>
```

**Features:**
- Single-byte and multi-byte entries (1-8 bytes)
- End block markers (`/XX`)
- End line markers (`*XX`)
- Comments (`#`)
- UTF-8 encoding with BOM support
- Escape sequences (`\n`, `\r`, `\t`)
- Raw hex format `[$XX]` for unmapped bytes

---

### 2. TBLX Format (Extended)

**Extension:** `.tblx`

**Description:** JSON-based extended format with rich metadata, categories, and validation rules.

**Format:**
```json
{
  "format": "tblx",
  "metadata": {
    "version": "1.0",
    "name": "Super Mario Bros (USA)",
    "description": "Character table for SMB NES ROM",
    "author": "John Doe",
    "createdDate": "2026-02-20T10:30:00Z",
    "modifiedDate": "2026-02-20T15:45:00Z",
    "game": {
      "title": "Super Mario Bros",
      "platform": "NES",
      "region": "USA",
      "developer": "Nintendo",
      "releaseYear": 1985
    },
    "encoding": "NES Custom ASCII",
    "categories": ["Letters", "Numbers", "Symbols"],
    "tags": ["nes", "nintendo", "mario"],
    "validation": {
      "minByteLength": 1,
      "maxByteLength": 3,
      "requireUniqueEntries": true,
      "allowMultiByte": true,
      "maxMultiByteLength": 8
    }
  },
  "entries": [
    {
      "entry": "00",
      "value": "A",
      "type": "Ascii",
      "byteCount": 1,
      "category": "Letters",
      "comment": "Uppercase A",
      "frequency": 85,
      "isFavorite": true
    }
  ]
}
```

**Metadata Fields:**
- **version**: Format version (default: "1.0")
- **name**: Table name/title
- **description**: Detailed description
- **author**: Creator name
- **createdDate**: ISO 8601 timestamp
- **modifiedDate**: Last modification timestamp
- **game**: Game information object
  - title, platform, region, version, releaseYear, developer
- **encoding**: Character encoding name (e.g., "Shift-JIS", "ASCII Extended")
- **categories**: Array of category names
- **tags**: Array of searchable tags
- **customProperties**: Dictionary of key-value pairs
- **validation**: Validation rules object
  - minByteLength, maxByteLength
  - allowedRanges, forbiddenValues
  - requireUniqueEntries, allowMultiByte
  - maxMultiByteLength

**Entry Fields:**
- **entry**: Hex value (required)
- **value**: Character string (required)
- **type**: Entry type string (Ascii, DualTitleEncoding, etc.)
- **byteCount**: Number of bytes
- **category**: Category name
- **comment**: Description
- **frequency**: Usage frequency (0-100)
- **isFavorite**: Boolean flag

---

### 3. CSV Format

**Extension:** `.csv`

**Description:** Spreadsheet-compatible format for easy editing in Excel/LibreOffice.

**Format:**
```csv
Hex,Character,Type,ByteCount,Comment
41,A,Ascii,1,Uppercase A
42,B,Ascii,1,Uppercase B
8283,Á,DualTitleEncoding,2,Accented A
```

**Import Options:**
- **Delimiter**: Comma (default), semicolon, tab
- **HasHeader**: Include header row (default: true)
- **AutoDetectType**: Auto-detect DteType from hex length (default: true)
- **SkipInvalidRows**: Skip invalid rows instead of failing (default: true)
- **Encoding**: UTF-8 (default), ASCII, etc.

**Export Options:**
- **IncludeType**: Include Type column (default: true)
- **IncludeByteCount**: Include ByteCount column (default: true)
- **IncludeComment**: Include Comment column (default: true)
- **Delimiter**: CSV delimiter (default: ",")
- **QuoteStrings**: Quote string values (default: true)
- **Encoding**: Output encoding (default: UTF-8)

---

### 4. JSON Format

**Extension:** `.json`

**Description:** Standard JSON format for programmatic access.

**Format (Simple Array):**
```json
[
  {
    "hex": "41",
    "value": "A",
    "type": "Ascii",
    "byteCount": 1
  },
  {
    "hex": "8283",
    "value": "Á",
    "type": "DualTitleEncoding",
    "byteCount": 2
  }
]
```

**Format (Object with Entries):**
```json
{
  "entries": [
    { "hex": "41", "value": "A" },
    { "hex": "42", "value": "B" }
  ]
}
```

**Import Options:**
- **AutoDetectType**: Auto-detect DteType (default: true)
- **SkipInvalidEntries**: Skip invalid entries (default: true)
- **HexPropertyName**: Property name for hex value (default: "hex")
- **ValuePropertyName**: Property name for character value (default: "value")

**Export Options:**
- **IncludeType**: Include type property (default: true)
- **IncludeByteCount**: Include byte count (default: true)
- **IncludeComment**: Include comment (default: true)
- **Indented**: Pretty print (default: true)
- **HexPropertyName**: Hex property name (default: "hex")
- **ValuePropertyName**: Value property name (default: "value")
- **IncludeMetadata**: Wrap in metadata object (default: false)
- **Metadata**: JsonMetadata object

---

## Quick Start

### Using TBL Editor UI

1. **Import:**
   - Click **Import** button in toolbar
   - Select file (.csv, .json, .tblx)
   - Auto-detection handles format
   - Review import results

2. **Export:**
   - Click **Export** button
   - Choose format (.csv, .json, .tblx)
   - File saved with auto-generated metadata (for .tblx)

### Using TBLStream API

```csharp
// Import from any format
var tbl = new TblStream();
var result = tbl.LoadFromFile("table.csv");

if (result.Success)
{
    Console.WriteLine($"Imported {result.ImportedCount} entries");
}

// Export to any format
tbl.SaveToFile("output.json");
tbl.SaveToFile("output.tblx");
tbl.SaveToFile("output.csv");
```

### Using Import/Export Services

```csharp
// CSV Import
var importService = new TblImportService();
var csvOptions = new CsvImportOptions
{
    Delimiter = ";",
    HasHeader = true,
    SkipInvalidRows = true
};
var result = importService.ImportFromCsv("table.csv", csvOptions);

// JSON Export
var exportService = new TblExportService();
var jsonOptions = new JsonExportOptions
{
    Indented = true,
    IncludeMetadata = true,
    Metadata = new JsonMetadata
    {
        Version = "1.0",
        Description = "My character table"
    }
};
exportService.ExportToJsonFile(entries, "output.json", jsonOptions);

// TBLX operations
var tblxService = new TblxService();
var doc = tblxService.LoadFromFile("table.tblx");
doc.Metadata.Author = "Updated Author";
tblxService.SaveToFile(doc, "updated.tblx");
```

---

## Format Specifications

### Auto-Detection Logic

File format is detected based on extension:

```csharp
TblFileFormat DetectFileFormat(string filePath)
{
    var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

    return extension switch
    {
        ".tbl"  => TblFileFormat.Tbl,
        ".tblx" => TblFileFormat.Tblx,
        ".csv"  => TblFileFormat.Csv,
        ".json" => TblFileFormat.Json,
        _       => TblFileFormat.Tbl // Default
    };
}
```

### CSV Parsing Details

**Custom CSV Parser Features:**
- Quote handling: `"value with, comma"`
- Escape sequences: `\n`, `\r`, `\t`
- Header detection
- Delimiter auto-detection (comma, semicolon, tab)
- Column mapping by name (case-insensitive)

**Supported Column Names:**
- Hex: `Hex`, `Entry`
- Value: `Value`, `Character`, `Character(s)`
- Type: `Type`
- ByteCount: `ByteCount`
- Comment: `Comment`

### JSON Parsing Details

**Supported Structures:**
1. Simple array: `[{hex, value}, ...]`
2. Object with entries: `{entries: [{hex, value}, ...]}`
3. Extended format: See .tblx specification

**Property Name Variations:**
- Hex: `hex`, `entry`, `Entry`
- Value: `value`, `Value`, `character`
- Comment: `comment`, `Comment`

---

## API Documentation

### TblImportService

```csharp
public class TblImportService
{
    // Auto-detect format
    TblImportResult ImportFromFile(string filePath)

    // CSV import
    TblImportResult ImportFromCsv(string filePath, CsvImportOptions options = null)
    TblImportResult ImportFromCsvString(string csvContent, CsvImportOptions options = null)

    // JSON import
    TblImportResult ImportFromJson(string filePath, JsonImportOptions options = null)
    TblImportResult ImportFromJsonString(string jsonContent, JsonImportOptions options = null)

    // TBLX import
    TblImportResult ImportFromTblx(string filePath)
}
```

### TblExportService

```csharp
public class TblExportService
{
    // Auto-detect format
    void ExportToFile(IEnumerable<Dte> entries, string filePath,
        CsvExportOptions csvOptions = null,
        JsonExportOptions jsonOptions = null,
        TblxMetadata tblxMetadata = null)

    // CSV export
    void ExportToCsvFile(IEnumerable<Dte> entries, string filePath, CsvExportOptions options = null)
    string ExportToCsv(IEnumerable<Dte> entries, CsvExportOptions options = null)

    // JSON export
    void ExportToJsonFile(IEnumerable<Dte> entries, string filePath, JsonExportOptions options = null)
    string ExportToJson(IEnumerable<Dte> entries, JsonExportOptions options = null)

    // TBLX export
    void ExportToTblxFile(IEnumerable<Dte> entries, string filePath, TblxMetadata metadata = null)

    // TBL export
    void ExportToTblFile(IEnumerable<Dte> entries, string filePath)
    string ExportToTbl(IEnumerable<Dte> entries)
}
```

### TblxService

```csharp
public class TblxService
{
    // Load/Import
    TblxDocument LoadFromFile(string filePath)
    TblxDocument LoadFromString(string jsonContent)
    TblImportResult ImportToTblStream(string filePath)

    // Save/Export
    void SaveToFile(TblxDocument document, string filePath)
    string SaveToString(TblxDocument document)
    void ExportFromTblStream(TblStream tbl, string filePath, TblxMetadata metadata = null)

    // Utilities
    TblxDocument CreateNew(TblxMetadata metadata = null)
    TblValidationResult Validate(TblxDocument document)
}
```

### TBLStream Multi-Format Methods

```csharp
public sealed class TblStream
{
    // Load
    TblImportResult LoadFromFile(string filePath)  // Auto-detect
    TblImportResult LoadFromCsv(string filePath, CsvImportOptions options = null)
    TblImportResult LoadFromJson(string filePath, JsonImportOptions options = null)
    TblImportResult LoadFromTblx(string filePath)

    // Save
    void SaveToFile(string filePath, CsvExportOptions csvOptions = null,
        JsonExportOptions jsonOptions = null, TblxMetadata tblxMetadata = null)
    void SaveToCsv(string filePath, CsvExportOptions options = null)
    void SaveToJson(string filePath, JsonExportOptions options = null)
    void SaveToTblx(string filePath, TblxMetadata metadata = null)

    // Export to string
    string ExportToCsvString(CsvExportOptions options = null)
    string ExportToJsonString(JsonExportOptions options = null)

    // Utilities
    static TblFileFormat DetectFileFormat(string filePath)
}
```

---

## UI Integration

### TBL Editor Dialog

**Toolbar Buttons:**
- **Import** (📥): Import from .csv, .json, .tblx
- **Export** (📤): Export to .csv, .json, .tblx

**File Dialogs:**

Import Filter:
```
All Supported Formats|*.tbl;*.tblx;*.csv;*.json|
TBL Files (*.tbl)|*.tbl|
Extended TBL Files (*.tblx)|*.tblx|
CSV Files (*.csv)|*.csv|
JSON Files (*.json)|*.json|
All Files (*.*)|*.*
```

Export Filter:
```
TBL Files (*.tbl)|*.tbl|
Extended TBL Files (*.tblx)|*.tblx|
CSV Files (*.csv)|*.csv|
JSON Files (*.json)|*.json|
All Files (*.*)|*.*
```

### Workflow

1. **Import CSV from Excel:**
   - Export character table from Excel as CSV
   - Click Import in TBL Editor
   - Select CSV file
   - Entries auto-loaded into editor
   - Edit as needed
   - Save as .tbl

2. **Export to JSON for Web App:**
   - Open existing .tbl in TBL Editor
   - Click Export
   - Choose .json format
   - Use JSON in web application

3. **Create TBLX with Metadata:**
   - Open .tbl file
   - Click Export
   - Choose .tblx format
   - Auto-generates metadata (author, date)
   - Add categories/tags later by editing JSON

---

## Examples

### Example 1: Import CSV from Spreadsheet

**Input CSV (table.csv):**
```csv
Hex,Character,Type,Comment
00,A,Ascii,Letter A
01,B,Ascii,Letter B
02,C,Ascii,Letter C
8283,Á,DualTitleEncoding,Accented A
```

**Code:**
```csharp
var tbl = new TblStream();
var result = tbl.LoadFromCsv("table.csv");

if (result.Success)
{
    Console.WriteLine($"Loaded {result.ImportedCount} entries");
    tbl.SaveToFile("output.tbl");
}
```

### Example 2: Export to JSON with Metadata

**Code:**
```csharp
var tbl = new TblStream("game.tbl");
tbl.Load();

var metadata = new JsonMetadata
{
    Version = "1.0",
    Description = "Super Mario Bros character table",
    Author = "John Doe",
    CreatedDate = DateTime.Now.ToString("O")
};

var options = new JsonExportOptions
{
    Indented = true,
    IncludeMetadata = true,
    Metadata = metadata,
    IncludeComment = true
};

var exportService = new TblExportService();
exportService.ExportToJsonFile(tbl.GetAllEntries(), "game.json", options);
```

### Example 3: Create TBLX with Categories

**Code:**
```csharp
var tblxService = new TblxService();
var doc = tblxService.CreateNew();

doc.Metadata.Name = "NES Game Table";
doc.Metadata.Game = new GameInfo
{
    Title = "Super Mario Bros",
    Platform = "NES",
    Region = "USA",
    ReleaseYear = 1985
};

doc.Entries.Add(new TblxEntry
{
    Entry = "00",
    Value = "A",
    Category = "Letters",
    Comment = "Uppercase A"
});

doc.Entries.Add(new TblxEntry
{
    Entry = "8283",
    Value = "Á",
    Category = "Special",
    Comment = "Accented letter"
});

tblxService.SaveToFile(doc, "game.tblx");
```

### Example 4: Batch Convert TBL to CSV

**Code:**
```csharp
var tblFiles = Directory.GetFiles(@"C:\ROMs\Tables", "*.tbl");
var exportService = new TblExportService();

foreach (var tblFile in tblFiles)
{
    var tbl = new TblStream(tblFile);
    tbl.Load();

    var csvFile = Path.ChangeExtension(tblFile, ".csv");
    exportService.ExportToCsvFile(tbl.GetAllEntries(), csvFile);

    Console.WriteLine($"Converted: {Path.GetFileName(tblFile)} → {Path.GetFileName(csvFile)}");
}
```

---

## Troubleshooting

### Import Errors

**Error: "Unsupported file format"**
- **Cause:** File extension not recognized
- **Solution:** Use .tbl, .tblx, .csv, or .json extension

**Error: "Invalid hex entry"**
- **Cause:** Hex value has odd length or invalid characters
- **Solution:** Ensure hex values have even length (2, 4, 6, 8...)

**Error: "Required column not found"**
- **Cause:** CSV missing "Hex" or "Value" columns
- **Solution:** Add header row with required columns

**Warning: "X entries skipped"**
- **Cause:** Invalid entries encountered
- **Solution:** Review skipped entries in import result, fix source file

### Export Errors

**Error: "Failed to export to .tblx file"**
- **Cause:** Invalid metadata or entries
- **Solution:** Validate entries before export

**Error: "Access denied"**
- **Cause:** File locked by another process
- **Solution:** Close file in other applications

### Performance Issues

**Slow import of large CSV (10,000+ entries)**
- **Solution:** Use SkipInvalidRows = false to fail fast on errors

**Large JSON file size**
- **Solution:** Set Indented = false for compact output

---

## Best Practices

### File Format Selection

- **Use .tbl**: For ROM hacking tools compatibility
- **Use .tblx**: For projects needing metadata and categories
- **Use .csv**: For bulk editing in Excel
- **Use .json**: For programmatic access, web apps

### TBLX Metadata

**Always include:**
- name, description, author
- createdDate (auto-generated)

**Optional but recommended:**
- game information (for ROM hacking projects)
- categories (for large tables >100 entries)
- validation rules (for quality control)

### CSV Editing

**Tips:**
- Use UTF-8 encoding in Excel (Save As → CSV UTF-8)
- Quote values containing commas: `"Hello, World"`
- Test import with small sample first

### Error Handling

**Always check import results:**
```csharp
var result = tbl.LoadFromFile("table.csv");

if (!result.Success)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error}");
}
else if (result.Warnings.Count > 0)
{
    Console.WriteLine($"Imported with {result.SkippedCount} warnings");
}
```

---

## Version History

### Version 2.6.0 (2026-02-20)

**Phase 1: Core Support**
- TblFileFormat enum
- TblImportResult model
- CSV/JSON import/export options
- TblImportService, TblExportService
- Custom CSV parser (no dependencies)
- JSON parsing with System.Text.Json
- Multi-format support in TBLStream

**Phase 2: Extended Format**
- .tblx format with JSON metadata
- TblxMetadata, TblxEntry, TblxDocument models
- TblxService for .tblx operations
- GameInfo, ValidationRules
- Document-level validation

**Phase 3: UI Integration**
- Import/Export buttons in TBL Editor
- File dialog filters
- Auto-format detection
- Import result feedback
- Export with auto-metadata

---

## License

Apache 2.0 - 2016-2026
Author: Derek Tremblay (derektremblay666@gmail.com)
Contributors: Claude Sonnet 4.5

---

## Support

For issues, feature requests, or questions:
- GitHub: https://github.com/abbaye/WpfHexEditorControl
- Issues: https://github.com/abbaye/WpfHexEditorControl/issues

