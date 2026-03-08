# WpfHexEditor.Plugins.ParsedFields

**Project:** WpfHexEditor.Plugins.ParsedFields
**Version:** 0.5.0
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Right

---

## Description

The Parsed Fields plugin displays the structured field list that results from automatic format detection (400+ binary formats) applied to the open file. Each field shows its name, byte offset, length, type, and decoded value. Fields can be sorted, filtered, bookmarked, and edited inline. The panel also presents format-level metadata (insight chips) and integrates bidirectionally with the hex editor (field selection scrolls the editor; editor selection highlights the corresponding field).

---

## Features

- **Automatic format detection** — subscribes to `FileOpened`; the hex editor's 400+ format definitions produce a `ParsedFieldViewModel` list immediately after a file is opened.
- **Inline value editing** — double-click a field value to edit it directly in the panel; changes are written back to the hex editor buffer.
- **Sorting** — 5 sort modes: by offset (default), name A→Z, name Z→A, type, size.
- **Search / filter** — live text filter narrows the field list; result count is shown in the toolbar.
- **Bookmarks** — star individual fields; "Bookmarks only" toggle filters to starred fields.
- **Insight chips** — summary badges (format name, field count, encoding, version) shown at the top of the panel.
- **Metadata section** — separate list for non-positional metadata fields (author, title, timestamps, etc.).
- **Bidirectional sync** with the hex editor:
  - Selecting a field scrolls and highlights the corresponding bytes.
  - Moving the caret in the editor highlights the matching field row.
- **Format candidate selection** — when multiple formats match, a dialog lets the user pick the most appropriate one.
- **Type overlay** — field type label shown as a colored chip next to the value.
- **Toolbar overflow** (groups managed by `ToolbarOverflowManager`).
- **EventBus integration** — receives `TemplateApplyRequestedEvent` from `CustomParserTemplatePlugin` to populate fields from a user-defined schema without a direct dependency.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.ParsedFields.Panel.ParsedFieldsPanel` |
| Default dock side | Right |
| Auto-hide | false |
| Preferred width | 340 |
| View menu entry | `View > Parsed Fields` |

### Internal components

| Component | Responsibility |
|---|---|
| `ParsedFieldsPanel.xaml(.cs)` | ListView, filter, sort, inline edit, insight chips |
| `ParsedFieldsPlugin.cs` | Plugin entry point; wires `HexEditor.ConnectParsedFieldsPanel()`, `ActiveEditorChanged`, `FileOpened`, EventBus subscription |

### Architecture notes

- Pattern: **Observer + Mediator** — the plugin subscribes to `IHexEditorService` events and routes `TemplateApplyRequestedEvent` from the EventBus.
- The five bidirectional `HexEditor ↔ ParsedFieldsPanel` events (`FieldSelected`, `RefreshRequested`, `FormatterChanged`, `FieldValueEdited`, `FormatCandidateSelected`) are auto-wired by `HexEditorControl.ParsedFieldsPanelProperty` (dependency property).
- The panel implements `IParsedFieldsPanel` — any host that holds this interface can reconnect it on tab switches without knowing the concrete type.

### Theme compliance

All brushes use `DynamicResource` tokens from the active theme. No hardcoded colors.
