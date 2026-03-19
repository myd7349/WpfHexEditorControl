# WpfHexEditor.Plugins.XamlDesigner

**Type:** Plugin (`net8.0-windows`)
**Role:** IDE integration wrapper for the XAML Designer — registers all 9 dockable panels and wires designer lifecycle to the IDE.

---

## Responsibility

Thin plugin adapter that connects `WpfHexEditor.Editor.XamlDesigner` to the IDE:

- Registers all 9 designer panels with `IDockPanelRegistry`
- Subscribes to `IFocusContextService.FocusChanged` to wire panels to the active designer
- Adds View menu items (Show Outline, Properties, Toolbox, History, …)
- Adds a status bar item showing the currently selected canvas element (left, order=15)
- Handles keyboard shortcuts: `Ctrl+1/2/3`, `F7`, `Delete`
- Provides IDE Options page (`XamlDesignerOptionsPage`)

---

## Registered Panels (9)

| Panel | Side | Size |
|-------|------|------|
| XAML Outline | Left, auto-hide | 220px |
| XAML Properties (F4) | Right | 260px |
| XAML Toolbox | Left, auto-hide | 220px |
| Resource Browser | Right, auto-hide | 260px |
| Design Data | Bottom, auto-hide | 180px |
| Animation Timeline | Bottom, auto-hide | 180px |
| Design History | Right, auto-hide | 240px |
| Binding Inspector | Right, auto-hide | 280px |
| Live Visual Tree | Left, auto-hide | 240px |

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Editor.XamlDesigner` | Designer controls + panels |
| `WpfHexEditor.SDK` | Plugin contracts |
| `WpfHexEditor.Editor.Core` | Editor types |

---

## Design Patterns Used

Observer — subscribes to `IFocusContextService` to track the active designer instance and forward events to each panel.
