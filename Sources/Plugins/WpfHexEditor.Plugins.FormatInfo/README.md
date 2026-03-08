# WpfHexEditor.Plugins.FormatInfo

**Project:** WpfHexEditor.Plugins.FormatInfo
**Version:** 0.4.0
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Right

---

## Description

The Format Info plugin displays rich metadata about the file format detected by the hex editor's format-detection engine. When the editor raises `FormatDetected`, the plugin pushes the full `FormatDefinition` object to the `EnrichedFormatInfoPanel`, which presents format name, category, description, file extensions, MIME types, associated software, use cases, technical details, related formats, author/version, magic-byte detection info, and clickable web references.

---

## Features

- **Format header card**: name, category badge, description, priority badge (if high-confidence match).
- **Quality / completeness score** — proportional progress bar showing how complete the format definition is.
- **File extensions** list.
- **MIME types** list.
- **Associated software** (authoring applications).
- **Common use cases**.
- **Technical details card** — spec-level notes (block structure, endianness, version history).
- **Related formats** card.
- **Author / version card** — definition authorship metadata.
- **Detection info card** — magic-byte signature in hex, byte offset, and whether the signature is required.
- **References card**:
  - Specification documents list.
  - Clickable web hyperlinks (opens default browser via `Process.Start`).
- **Documentation level** badge (Basic / Standard / Full).
- **Automatic clear** on `FileOpened` — stale format info is removed before the new detection result arrives.
- **`SetFormat(FormatDefinition?)`** / **`ClearFormat()`** public API for the plugin or any other host component to push format data.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.FormatInfo.Panel.EnrichedFormatInfoPanel` |
| Default dock side | Right |
| Auto-hide | true |
| View menu entry | `View > Format Info` |

### Internal components

| Component | Responsibility |
|---|---|
| `EnrichedFormatInfoPanel.xaml(.cs)` | Card-based display; conditional visibility per card |
| `EnrichedFormatViewModel.cs` | Formats all string properties from `FormatDefinition` for binding |
| `FormatInfoPlugin.cs` | Plugin entry point; subscribes to `FormatDetected` + `FileOpened`; casts `RawFormatDefinition` to `FormatDefinition` (bundled-plugin privilege) |

### Architecture notes

- Pattern: **Observer** — plugin subscribes to `IHexEditorService.FormatDetected` and pushes data to the panel via `Dispatcher.BeginInvoke`.
- The `RawFormatDefinition` field on `FormatDetectedArgs` is populated by `HexEditorServiceImpl` and carries the full Core `FormatDefinition` object. This cast is a bundled-plugin privilege; external/sandboxed plugins must not rely on it.
- Each card section has its own `Visibility` toggle so empty optional sections are hidden automatically.
- Quality score bar width is computed proportionally from `ActualWidth` and updated on `OnRenderSizeChanged`.

### Theme compliance

All brushes use `DynamicResource` tokens from the active global theme. Hyperlink foreground is set explicitly to `#2196F3` (Material Blue) for readability across themes.
