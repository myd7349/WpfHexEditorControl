# WpfHexEditor.Plugins.DataInspector

**Project:** WpfHexEditor.Plugins.DataInspector
**Version:** 0.7.0
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Bottom

---

## Description

The Data Inspector plugin interprets the bytes at the current cursor position (or selection) as every common primitive type simultaneously and displays all results in a single scrollable list. It also renders a byte-frequency or Shannon-entropy chart of the active data scope.

---

## Features

- **40+ byte interpretations** at the caret: signed/unsigned integers (8/16/32/64-bit), floating-point (float, double), GUID, DateTime, Windows FILETIME, Unix timestamp, color (RGBA, ARGB, BGR), ASCII, UTF-8, UTF-16, boolean, bitmask, and more.
- **Byte-frequency chart** (`BarChartPanel`) — custom-rendered `FrameworkElement`; 256 bars, one per byte value (0x00–0xFF).
- **Entropy chart** — sliding-window Shannon entropy rendered as a color-coded canvas (green = low, red = high).
- **Three data scopes** switchable from the toolbar:
  - *Active view* — bytes currently visible in the hex editor viewport; updates on scroll (coalesced).
  - *Selection* — selected byte range; async load with progress overlay for ranges > 1 MB.
  - *Whole file* — up to a 4 MB stratified sample; loaded once per file, cached.
- **Zoom** on the frequency chart: 1x–16x, mouse-wheel zoom, drag rubber-band zoom to a byte range.
- **Chart position** configurable (Left / Right / Top / Bottom) and persisted to `DataInspectorOptions.json`.
- **Chart copy** to clipboard as a PNG bitmap.
- **Footer statistics**: entropy value, most-common byte, null-byte %, printable-ASCII %.
- **Auto-refresh** toggle; manual Refresh button always available regardless of setting.
- **Toolbar overflow** (5 groups, `ToolbarOverflowManager`): collapses Layout → Action → Zoom → Mode → Toggles.
- **Theme-aware**: bar color, background, grid lines, and text color update on global theme switch.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.DataInspector.Panel.DataInspectorPanel` |
| Default dock side | Bottom |
| Auto-hide | false |
| Preferred height | 260 |
| View menu entry | `View > Data Inspector` |

### Internal components

| Component | Responsibility |
|---|---|
| `DataInspectorPanel.xaml(.cs)` | Layout engine, zoom/scope logic, toolbar overflow wiring |
| `DataInspectorViewModel.cs` | 40+ interpretation rows, `UpdateBytes(byte[])` |
| `BarChartPanel.cs` | Custom `FrameworkElement` rendering 256 frequency bars |
| `DataInspectorOptions.cs` | Persisted settings (`%AppData%\WpfHexaEditor\Plugins\DataInspector.json`) |
| `DataInspectorPlugin.cs` | Plugin entry point; subscribes to `SelectionChanged`, `ViewportScrolled`, `FileOpened`, `ThemeChanged` |

### Architecture notes

- Pattern: **Observer** — `DataInspectorPlugin` subscribes to host events and forwards them to the panel.
- Circular dependency avoided: `BarChartPanel` has no reference to `DataInspectorViewModel`.
- Async reads use `CancellationTokenSource` — a new scope cancels the previous in-flight read.
- Whole-file chart data is cached (`_wholeFileChartLoaded` flag) and is not re-read on every `SelectionChanged`.

### Theme compliance

All brushes use `DynamicResource` tokens (`Panel_ToolbarButtonActiveBrush`, `PFP_PanelBackgroundBrush`, `DockMenuForegroundBrush`, `DockBorderBrush`). `RefreshTheme()` is called by the plugin on every theme-switch notification.
