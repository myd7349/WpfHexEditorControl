# WpfHexEditor.Plugins.FileComparison

**Project:** WpfHexEditor.Plugins.FileComparison
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Bottom

---

## Description

The File Comparison plugin provides a side-by-side structural diff of two binary files. Each file is parsed using the automatic format detection engine (400+ format definitions); the extracted named fields are then compared by name and value. Changed, added, and removed fields are color-coded. Summary statistics show the number of matching, added, modified, and removed fields.

---

## Features

- **Structural diff** — files are compared at the field level (not raw byte level); each named field is matched by name and its decoded string value is compared.
- **Format auto-detection** — `FormatDetectionService` with JSON format definitions from `FormatDefinitions/` is applied to each file independently.
- **Multi-candidate resolution** — when more than one format matches a file, a selection dialog (up to 5 candidates) lets the user choose the correct format.
- **Side-by-side display** — two synchronized `ListView` columns, one per file.
- **Diff status coloring** — each field row is color-coded: Unchanged, Added, Removed, or Modified.
- **Summary statistics**: matching field count, added, modified, and removed fields.
- **Supported primitive value types** for field decoding: uint8, uint16, uint32, int16, int32, string/ASCII, UTF-8, and raw hex fallback.
- **ViewModel** (`FileComparisonPanelViewModel`) tracks file names, diff counts, comparison status, and a loading flag.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.FileComparison.Panel.FileComparisonPanel` |
| Default dock side | Bottom |
| View menu entry | `View > File Comparison` |

### Internal components

| Component | Responsibility |
|---|---|
| `FileComparisonPanel.xaml(.cs)` | File picker buttons, side-by-side ListViews, diff logic, statistics labels |
| `FileComparisonPanelViewModel.cs` | File names, diff counts, comparison status flag |
| `ParsedField` | Local field model (name, value, type, offset, length) |
| `DiffField` | Observable diff row model with `DiffStatus` (Unchanged / Added / Removed / Modified) |
| `FormatSelectionDialog` | Dialog shown when multiple format candidates are detected |

### Architecture notes

- Pattern: **MVVM + Strategy** — field decoding is a switch expression over the `valueType` string; the format detection pipeline is injected via `FormatDetectionService`.
- Diff algorithm: `O(n)` dictionary lookup (file 1 fields indexed by name), not an LCS diff — sufficient for named-field comparison where order may vary by format.
- Metadata-typed blocks (no byte offset) are decoded from the `variables` dictionary rather than from the raw byte buffer.

### Theme compliance

All brushes use `DynamicResource` tokens from the active global theme.
