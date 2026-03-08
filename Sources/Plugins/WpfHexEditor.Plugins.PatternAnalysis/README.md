# WpfHexEditor.Plugins.PatternAnalysis

**Project:** WpfHexEditor.Plugins.PatternAnalysis
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Bottom

---

## Description

The Pattern Analysis plugin inspects a byte range selected in the hex editor and produces four analysis cards: Shannon entropy, a byte-distribution histogram, detected structural patterns, and anomaly warnings. All heavy computation (entropy calculation, byte distribution, pattern detection, anomaly detection) runs on a background thread to keep the UI responsive.

---

## Features

- **Shannon entropy card** — value (0–8 bits/byte), color-coded horizontal bar (green = low, yellow = medium, red = high), textual interpretation.
- **Byte-distribution histogram card** — Canvas with 256 bars (one per byte value), most-frequent byte, unique byte count. Bars are drawn at `DispatcherPriority.Normal` after the Canvas is measured.
- **Patterns card** — detects and lists:
  - Null-byte dominance (>30% null bytes).
  - Most-repeated 4-byte sequence (packed `uint` key, zero-allocation inner loop).
  - ASCII text dominance (>70% printable ASCII).
  - 4-byte alignment.
- **Anomalies card** — flags:
  - Extremely skewed distribution (single byte >90% of data).
  - Very high entropy (> 7.5 bits — possible encryption).
  - Very low entropy (< 2.0 bits — repetitive/padding data).
- **Analyze button** — raises `AnalysisRequested`; the plugin entry point responds by calling `AnalyzeAsync(byte[])` with the current selection.
- **Async analysis** — `Task.Run` moves all CPU work off the UI thread; results are applied back on the UI `SynchronizationContext`.
- **Status text** — displays byte count during analysis and completion confirmation.
- **Toolbar overflow** (1 collapsible group: `TbgPatternRefresh`).

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.PatternAnalysis.Panel.PatternAnalysisPanel` |
| Default dock side | Bottom |
| View menu entry | `View > Pattern Analysis` |

### Internal components

| Component | Responsibility |
|---|---|
| `PatternAnalysisPanel.xaml(.cs)` | Four analysis cards, histogram Canvas, async `AnalyzeAsync`, toolbar |
| `PatternAnalysisPanelViewModel.cs` | ViewModel with observable state for card visibility and status |
| `PatternInfo` | Icon, pattern label, description (used for the Patterns card list) |
| `AnomalyInfo` | Title + description (used for the Anomalies card list) |

### Architecture notes

- Pattern: **Observer** — panel raises `AnalysisRequested`; plugin entry point reads the selection and calls `AnalyzeAsync(byte[])`.
- Zero-allocation pattern key: 4-byte sequences are packed into a `uint` (little-endian) to avoid allocating a `byte[]` per iteration — prevents GC pressure on large files.
- Histogram drawing is deferred via `Canvas.Loaded` if the Canvas has not been measured yet at the time the result arrives.
- All four analysis algorithms are pure static functions; the async wrapper captures only the result tuple, not `this`.

### Theme compliance

Entropy bar brush colors use named `DynamicResource` keys (`LowEntropyBrush`, `MediumEntropyBrush`, `HighEntropyBrush`). Histogram bar fill uses a fixed Material Blue (`#4A90E2`) which is legible on both light and dark themes.
