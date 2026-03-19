# WpfHexEditor.Sample.Docking

**Type:** Standalone WPF Application (`net8.0-windows`)
**Role:** Minimal demonstration of the `WpfHexEditor.Shell` docking framework with VS Code–like chrome.

---

## Purpose

Shows how to build a fully dockable WPF application from scratch using the Shell framework. Covers:

- VS Code–like window chrome (`WindowChrome` + `WindowStyle=None`)
- Panel/document registration via `ContentFactory` (lazy loading)
- Default layout: Explorer (left) + Welcome document (center) + Output & Properties (bottom)
- Layout persistence to `%AppData%\WpfHexEditor\Samples\Docking\layout.json`
- Runtime Dark / Office Light theme switching via View menu

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `App` | Application entry + `App.SwitchTheme(Uri)` |
| `MainWindow` | DockControl setup, ContentFactory, layout persistence, theme menu sync |
| `ExplorerPanel` | Demo left panel |
| `OutputPanel` | Demo bottom panel |
| `PropertiesPanel` | Demo right panel |
| `WelcomePanel` | Demo center document |

---

## Architecture Notes

- `ContentFactory` pattern enables lazy panel creation on first activation
- Layout accepted only when ALL four sample panels are present (guards stale JSON)
- All content IDs prefixed `"sample-"` to avoid collisions with IDE panels
- Theme brush keys: `DockWindowBackgroundBrush`, `DockMenuBackgroundBrush`

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Shell` | Docking engine + 8 themes |
