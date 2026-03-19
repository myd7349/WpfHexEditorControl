# WpfHexEditor.Sample.HexEditor

**Type:** Standalone WPF Application (`net8.0-windows`)
**Role:** Full-featured standalone hex editor demonstration with multi-language UI, themes, and sample files.

---

## Purpose

The primary showcase application for the `WpfHexEditor.HexEditor` control. Demonstrates:

- Full hex editor UI with toolbar, menu, status bar
- Multi-language support: `fr-CA`, `pl-PL`, `pt-BR`, `ru-RU`, `zh-CN`
- Runtime theme switching (Dark / Office Light)
- Settings panel, search command center
- Custom background block demo (colored byte-range overlays)
- Embedded `CodeEditor` demo in a dialog
- Sample TBL character table files for multi-byte encoding

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `App` | Application entry point |
| `MainWindow` | Window chrome and event wiring |
| `ModernMainWindowViewModel` | Window-level state and commands |
| `ThemeManager` | Runtime theme switching |
| `DynamicResourceManager` | Dynamic resource loading per language |
| `LocalizedResourceDictionary` | Localization support (5 languages) |
| `SettingsPanelViewModel` / `SettingsPanel` | Options configuration UI |
| `SearchCommandCenterViewModel` / `SearchCommandCenter` | Search/replace UI |
| `CustomBackgroundDemo` | Colored byte-range overlay demo |
| `CodeEditorDemoWindow` | Embedded CodeEditor in a dialog |
| `Converters` | Value converters for data binding |

---

## Sample Files

- `SampleFiles/TBL/` — Sample TBL character table files for multi-byte encoding demonstration

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Core` | Core byte operations |
| `WpfHexEditor.HexEditor` | Hex editor control |
| `WpfHexEditor.Editor.CodeEditor` | Embedded code editor dialog |
| `WpfHexEditor.Panels.IDE` | Solution Explorer, Properties panels |
| `WpfHexEditor.Panels.FileOps` | File operations panels |
