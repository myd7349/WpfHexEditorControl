# WpfHexEditor.Plugins.SynalysisGrammar

**Type:** Plugin (`net8.0-windows`) | **Load Priority:** 45
**Role:** UFWB (Synalysis / Hexinator) grammar support — parses binary file structures and overlays colored field annotations on the hex view.

---

## Responsibility

- Loads and applies Synalysis `.grammar` (UFWB XML) files to the active hex editor
- Outputs colored background overlays (`CustomBackgroundBlock`) to the hex view
- Populates the Parsed Fields panel with structured field tree
- Provides a dockable Grammar Selector panel (right side, 340px)
- Resolves GitHub issue **#177**

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `SynalysisGrammarPlugin` | Plugin entry point; registers panels, menus, embedded grammars, auto-apply logic |
| `SynalysisGrammarService` | Async grammar application engine |
| `GrammarSelectorPanel` | Dockable panel for grammar selection + status |
| `GrammarSelectorViewModel` | UI state for grammar selection, progress, and results |
| `GrammarEntryViewModel` | Single grammar entry row (name, extension filter, enabled) |
| `GrammarStructureNodeViewModel` | Tree node for parsed field hierarchy |
| `SynalysisToBackgroundBlockBridge` | Converts parse results → `CustomBackgroundBlock` hex overlays |
| `SynalysisToFieldViewModelBridge` | Converts parse results → Parsed Fields panel rows |
| `GrammarExplorerOptions` | Options model (auto-apply, max depth, color scheme) |
| `GrammarExplorerOptionsPage` | IDE Options page integration |

---

## Features

- **10+ embedded grammars** loaded from `WpfHexEditor.Definitions`
- **Plugin-contributed grammars** via `IGrammarProvider` SDK extension point
- **Auto-apply** on file open / editor switch (configurable)
- **`GrammarAppliedEvent`** published via `IPluginEventBus` → consumed by Parsed Fields panel

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.SDK` | Plugin contracts, `IGrammarProvider`, UI registry |
| `WpfHexEditor.Core` | Synalysis parser engine |
| `WpfHexEditor.Editor.Core` | Editor types |
| `WpfHexEditor.Definitions` | Embedded `.grammar` files |

---

## Design Patterns Used

Facade + Mediator — `SynalysisGrammarPlugin` mediates between the parser, hex view overlays, and Parsed Fields panel.
