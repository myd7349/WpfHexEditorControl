# WpfHexEditor.Panels.IDE

Core IDE dockable panels — Error List, Properties, Solution Explorer, and Plugin Monitor — all styled VS-like with toolbar overflow support.

**License:** GNU Affero General Public License v3.0
**Target:** net8.0-windows · WPF

---

## Architecture / Modules

```
WpfHexEditor.Panels.IDE/
├── Panels/
│   ├── ErrorPanel.xaml(.cs)
│   ├── PropertiesPanel.xaml(.cs)
│   ├── SolutionExplorerPanel.xaml(.cs)
│   ├── PluginMonitoringPanel.xaml(.cs)
│   ├── SparklineControl.cs
│   └── ViewModels/
│       ├── PluginMonitoringViewModel.cs
│       ├── PluginMiniChartViewModel.cs
│       ├── SolutionExplorerViewModel.cs
│       └── SolutionExplorerNodeVm.cs
└── Services/
```

### Panels

- **`ErrorPanel.xaml` / `.cs`** — VS2022-style error/warning/message list.
  - Aggregates `DiagnosticEntry` instances from multiple `IDiagnosticSource` providers.
  - Filterable (Errors / Warnings / Messages toggle buttons) and sortable by column click.
  - Implements `IErrorPanel`; exposes `Scope` (`ErrorPanelScope.Solution` / `ActiveDocument`).
  - `CollectionViewSource` with sort direction cycling on column header click.

- **`PropertiesPanel.xaml` / `.cs`** — VS-style Properties panel (F4).
  - Binds to an `IPropertyProvider`; renders categorized, optionally-editable property rows.
  - `PropertyEntryDataTemplateSelector` picks the correct `DataTemplate` per entry type (text, color swatch, file path, enum, boolean).
  - Toolbar: Sort alphabetically / by category, Copy, Refresh. Context menu: copy value, open path in Explorer.
  - `ToolbarOverflowManager` with 1 group (`TbgPropActions`).

- **`SolutionExplorerPanel.xaml` / `.cs`** — project workspace tree.
  - Backed by `SolutionExplorerViewModel` + `SolutionExplorerNodeVm` hierarchy.
  - Sort modes: None, Name, Type, DateModified, Size.
  - Filter modes: All, Binary, Text, Image, Language.
  - Real-time search box filtering. "Show All Files" toggle.
  - Drag-and-drop from Windows Explorer; expand-state persistence.

- **`PluginMonitoringPanel.xaml` / `.cs`** — live plugin diagnostics.
  - Backed by `PluginMonitoringViewModel` (Observer pattern, subscribes to `WpfPluginHost` lifecycle events).
  - Global CPU % and managed heap time-series chart (Canvas + Polyline, no third-party charting library).
  - Per-plugin sparkline mini-charts (`SparklineControl`).
  - Interactive permissions editor: per-plugin permission flags toggleable at runtime.
  - Configurable alert thresholds with `PluginAlertEngine`. CSV/JSON export via `PluginDiagnosticsExporter`.
  - Live event log: Loaded, Unloaded, Crashed, Slow events with timestamps.
  - Chart position configurable: Top / Bottom / Left / Right (`MonitorChartsPosition`).

### ViewModels

- **`PluginMonitoringViewModel`** — CPU estimation: `processCpu × (plugin.AvgExecMs / sumAllAvgExecMs)`. Sampling tick drives chart history, alert evaluation, and `EventLog` updates on the UI thread.
- **`PluginMiniChartViewModel`** — per-plugin sparkline data and threshold-based color assignment.
- **`SolutionExplorerViewModel`** — builds/synchronizes tree from `ISolution`; manages sort/filter/search state.
- **`SolutionExplorerNodeVm`** — recursive node ViewModel for Solution, SolutionFolder, Project, VirtualFolder, and ProjectItem nodes.

### Auxiliary

- **`SparklineControl`** — custom `FrameworkElement` rendering a miniature line chart via `DrawingContext`; used by Plugin Monitor per-plugin rows.

---

## Theme Compliance

All panels use global IDE theme brushes (`DockMenuBackgroundBrush`, `DockForegroundBrush`, `DockAccentBrush`, `DockBorderBrush`) via `DynamicResource`, ensuring instant update on theme switch.
