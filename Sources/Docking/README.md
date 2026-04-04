# WpfHexEditor Docking System

VS Code-style docking framework for WPF applications — panels, documents, drag-and-drop, auto-hide, floating windows and JSON layout persistence.

**License:** GNU Affero General Public License v3.0  
**Target frameworks:** `net8.0` / `net8.0-windows`

---

## Solution structure

```
WpfHexEditor.Docking.slnx
│
├── src/
│   ├── WpfHexEditor.Docking.Core   (net8.0)          Platform-agnostic layout engine
│   └── WpfHexEditor.Docking.Wpf    (net8.0-windows)  WPF implementation
│
├── dependencies/
│   ├── WpfHexEditor.ProgressBar    (net8.0-windows)
│   └── WpfHexEditor.Editor.Core    (net8.0-windows)
│
└── tests/
    └── WpfHexEditor.Docking.Tests  (net8.0, xUnit)
```

---

## Architecture

```
Consumer (App / Shell)
        │
        ▼
WpfHexEditor.Docking.Wpf          <- WPF controls, drag-and-drop, themes
  DockWorkspace                   Entry point for the host window
  DockControl                     Main docking surface
  DockDragManager                 Drag-and-drop overlay logic
  FloatingWindow                  Undocked panel host
  AutoHideBar                     Edge bar for collapsed panels
        │
        ▼
WpfHexEditor.Docking.Core         <- Pure logic, no WPF dependency
  DockEngine      <- HEART        All Dock / Split / Undock operations
  DockLayoutRoot                  Root of the layout tree
  DockItem                        Single panel or document
  DockGroupNode                   Tab-strip of DockItems
  DockSplitNode                   Horizontal or vertical split
  DocumentHostNode                Main document area
  DockCommandStack                Undo / redo
  DockLayoutSerializer            JSON / XML persistence
```

---

## Build

```powershell
dotnet build WpfHexEditor.Docking.slnx -c Release
```

## Test

```powershell
dotnet test WpfHexEditor.Docking.slnx -c Release
```

## Pack (NuGet)

```powershell
dotnet pack WpfHexEditor.Docking.slnx -c Release --output ./nupkgs
```

Produces two independent packages:

| Package                      | Description                                    |
|------------------------------|------------------------------------------------|
| `WpfHexEditor.Docking.Core`  | Platform-agnostic engine (net8.0)              |
| `WpfHexEditor.Docking.Wpf`   | WPF implementation — pulls Core automatically  |

## Publish

```powershell
dotnet nuget push ./nupkgs/WpfHexEditor.Docking.Core.1.0.0.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ./nupkgs/WpfHexEditor.Docking.Wpf.1.0.0.nupkg  --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
```

---

## Per-project documentation

- [WpfHexEditor.Docking.Core — README](WpfHexEditor.Docking.Core/README.md)
- [WpfHexEditor.Docking.Wpf  — README](WpfHexEditor.Docking.Wpf/README.md)

---

## Repository

https://github.com/abbaye/WpfHexEditorIDE
