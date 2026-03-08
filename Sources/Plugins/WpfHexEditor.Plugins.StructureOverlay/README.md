# WpfHexEditor.Plugins.StructureOverlay

**Project:** WpfHexEditor.Plugins.StructureOverlay
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Right

---

## Description

The Structure Overlay plugin superimposes named field regions onto the hex grid of the active editor. Users can load a JSON format definition or define a custom structure manually. Each overlay structure is shown as a tree of named fields with their byte offset and length; clicking a field highlights the corresponding bytes in the hex editor.

---

## Features

- **Visual field highlighting** — selecting a field node fires `OnFieldSelectedForHighlight`; the hex editor renders the field range with a colored overlay on the hex grid.
- **Load from JSON format definition** — parses a `FormatDefinition` JSON file and maps its blocks to `OverlayField` objects via `StructureOverlayService`.
- **Custom overlay builder** — toolbar button creates a default 4-field structure (Header, Version, Flags, Data Length) as a starting point for manual editing.
- **TreeView display** — overlays are shown as a two-level tree: structure → fields. Each field shows name, type, offset, and length.
- **Clear all** — with confirmation dialog, removes all overlays and notifies the host.
- **Event API** for host integration:
  - `OnOverlayAdded` — fired when a new structure is parsed and added.
  - `OnAllOverlaysCleared` — fired after a full clear.
  - `OnFieldSelectedForHighlight` — fired when a field is selected for hex-grid highlighting.
  - `OnStructureSelectedForHighlight` — fired when a top-level structure is selected.
- **Toolbar overflow** (1 collapsible group: `TbgStructureAdd`).
- **Memory-efficient** — raw file bytes are released immediately after overlay parsing; the overlay model stores only offsets and lengths, not byte content.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.StructureOverlay.Panel.StructureOverlayPanel` |
| Default dock side | Right |
| View menu entry | `View > Structure Overlay` |

### Internal components

| Component | Responsibility |
|---|---|
| `StructureOverlayPanel.xaml(.cs)` | TreeView display, toolbar, file dialog for JSON load |
| `StructureOverlayViewModel.cs` | Observable collection of `OverlayStructure`; selected item tracking |
| `StructureOverlayPlugin.cs` | Plugin entry point; connects panel events to `IHexEditorService` |
| `StructureOverlayService.cs` | Parses `FormatDefinition` JSON → `OverlayStructure` model |
| `IStructureOverlayPanel` | Interface used by the host to call `UpdateFileBytes`, `AddOverlayFromFormat`, `ClearAllOverlays` |

### Architecture notes

- Pattern: **Observer** — panel fires events; the host (plugin entry point) relays them to the hex editor service.
- Raw bytes passed via `UpdateFileBytes(byte[])` are held only until `AddOverlayFromFormat` completes, then released via `ReleaseFileBytes()` to avoid retaining large buffers.

### Theme compliance

All brushes use `DynamicResource` tokens (`PFP_*` and `Dock*` theme keys).
