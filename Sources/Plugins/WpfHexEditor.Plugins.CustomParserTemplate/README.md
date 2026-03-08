# WpfHexEditor.Plugins.CustomParserTemplate

**Project:** WpfHexEditor.Plugins.CustomParserTemplate
**Version:** 0.4.0
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Right (auto-hide)

---

## Description

The Custom Parser Template plugin provides a visual editor for authoring binary parser templates. A template is a named list of field blocks, each specifying a byte offset, length, value type, color tag, and description. When the user clicks "Apply Template", the blocks are mapped to SDK `ParsedBlockInfo` records and broadcast via `IPluginEventBus`. The `ParsedFieldsPlugin` subscribes to this event and populates the Parsed Fields panel — the two plugins are fully decoupled (no direct reference between them).

---

## Features

- **Template library** — templates are persisted as JSON files in `%AppData%\WpfHexaEditor\CustomTemplates\`. The panel loads all `*.json` files at startup.
- **Template editor** — edit name, description, and target file extensions (comma-separated list) in the toolbar area.
- **Field block editor** — editable `DataGrid` with columns: Name, Offset, Length, ValueType, Color, Description.
- **17 supported value types**: uint8, uint16, uint32, uint64, int8, int16, int32, int64, float, double, string, ascii, utf8, utf16, hex, binary, boolean.
- **New / Delete template** — create a new template with a timestamped name or delete the selected one (with confirmation).
- **Add / Remove block** — add a default `uint32` block or remove the selected block from the DataGrid.
- **Save** — writes the current template to its `%AppData%` JSON file with indented formatting.
- **Import / Export** — load a template from any `.json` file or export the current template to a chosen location.
- **Apply template** — maps blocks to `ParsedBlockInfo` and publishes `TemplateApplyRequestedEvent` on `IPluginEventBus`; the `ParsedFieldsPlugin` receives this and fills the Parsed Fields panel.
- **Mediator pattern** — this plugin has no `ProjectReference` to `ParsedFieldsPlugin`; communication is entirely through the EventBus.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.CustomParserTemplate.Panel.CustomParserTemplatePanel` |
| Default dock side | Right |
| Auto-hide | true |
| View menu entry | `View > Custom Parser Template` |

### Internal components

| Component | Responsibility |
|---|---|
| `CustomParserTemplatePanel.xaml(.cs)` | Template list, field DataGrid, all toolbar handlers |
| `CustomParserTemplatePlugin.cs` | Plugin entry point; wires `TemplateApplyRequested` event → EventBus publish |
| `CustomTemplate` | Observable template model (name, description, extensions, blocks list) |
| `CustomBlock` | Observable field block model (name, offset, length, type, color, description) |
| `TemplateApplyEventArgs` | Event args carrying the applied `CustomTemplate` |

### Architecture notes

- Pattern: **Mediator (EventBus)** — `CustomParserTemplatePlugin` publishes `TemplateApplyRequestedEvent`; `ParsedFieldsPlugin` subscribes. Neither plugin knows about the other.
- Templates directory is created with `Directory.CreateDirectory` at startup — no manual setup required.
- JSON serialization uses `System.Text.Json` with `WriteIndented = true` and `WhenWritingNull` ignore policy.
- `FilePath` is decorated with `[JsonIgnore]` to keep it out of the serialized template files.

### Theme compliance

All brushes use `DynamicResource` tokens from the active global theme. No hardcoded colors.
