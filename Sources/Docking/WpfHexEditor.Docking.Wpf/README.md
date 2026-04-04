# WpfHexEditor.Docking.Wpf

WPF docking framework built on top of `WpfHexEditor.Docking.Core`.
Provides VS Code-style panel/document hosting, drag-and-drop, theme switching and JSON layout persistence.

**License:** GNU Affero General Public License v3.0
**Target:** net8.0-windows (WPF required)

---

## Features

- VS Code-like chrome (WindowChrome + WindowStyle=None)
- Panel docking: Left / Right / Top / Bottom / Center (tabbed)
- Document host with split-view support
- Floating windows (undock any panel)
- Auto-hide panels (collapse to edge bar, expand on hover)
- Drag-and-drop with overlay drop targets
- Runtime theme switching (Dark / Office Light via DynamicResource)
- JSON layout persistence (DockLayoutSerializer)
- Full UI Automation / MSAA accessibility support

---

## Quick start

Register your panel content factory and restore the saved layout:

    DockWorkspace.ContentFactory = new MyContentFactory();
    await DockWorkspace.LoadLayoutAsync(layoutPath);

---

## Dependencies

| Package                     | Version |
|-----------------------------|---------|
| WpfHexEditor.Docking.Core   | >= 1.0.0 |
| WpfHexEditor.Editor.Core    | >= 1.0.0 |
| WpfHexEditor.ProgressBar    | >= 1.0.0 |

---

## Repository

https://github.com/abbaye/WpfHexEditorIDE
