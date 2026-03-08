# WpfHexEditor.Plugins.FileStatistics

**Project:** WpfHexEditor.Plugins.FileStatistics
**Version:** 0.5.0
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Bottom

---

## Description

The File Statistics plugin is a file health dashboard. Whenever a file is opened (or the active editor tab changes), it reads up to 1 MB of the file, computes byte-composition metrics and Shannon entropy on a background thread, then pushes a `FileStats` snapshot to the panel. The panel displays progress bars, a health score, a data-type classification, detected anomalies, and integrity indicators.

---

## Features

- **File info header**: file name, path, detected format name, analysis timestamp.
- **Byte composition** (progress bars with percentages):
  - Null bytes (0x00)
  - Printable ASCII (0x20–0x7E)
  - Other / binary bytes
- **Most-common byte** and **unique byte count** (out of 256).
- **Shannon entropy** score (0–8 bits/byte) with color-coded progress bar and textual interpretation:
  - < 1.0: highly structured / repetitive
  - 1.0–3.0: low entropy (sparse)
  - 3.0–5.5: medium entropy (mixed)
  - 5.5–7.0: high entropy (compressed/binary)
  - > 7.0: very high (likely compressed or encrypted)
- **Data-type classification**: Text, Binary, Compressed, Encrypted, Sparse, Image, Executable.
- **Health score** (0–100) with label (Good / Fair / Poor / Critical).
  - Deductions: >80% null bytes (-30), entropy >7.8 (-20), <10 unique bytes (-20).
- **Integrity indicators**: structure validity icon, checksum status.
- **Anomaly list** — flagged conditions such as high null-byte ratio or very high entropy.
- **Refresh button** — re-runs analysis on demand; also fires automatically on `FileOpened` and `ActiveEditorChanged`.
- **Toolbar overflow** (1 collapsible group: `TbgStatsRefresh`).
- **Async computation** — heavy statistics run on a `Task.Run` background thread; UI stays responsive.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.FileStatistics.Panel.FileStatisticsPanel` |
| Default dock side | Bottom |
| Auto-hide | false |
| Preferred height | 200 |
| View menu entry | `View > File Statistics` |

### Internal components

| Component | Responsibility |
|---|---|
| `FileStatisticsPanel.xaml(.cs)` | Dashboard display, progress bars, anomaly list, toolbar |
| `FileStatisticsPlugin.cs` | Plugin entry point; subscribes to `FileOpened` + `ActiveEditorChanged`; calls `ComputeStats` on background thread |
| `FileStats` | Plain data snapshot (all computed statistics) |
| `AnomalyInfo` | Title + description for a flagged condition |

### Architecture notes

- Pattern: **Observer** — plugin subscribes to host events and pushes `FileStats` snapshots; panel is a pure display component with no host dependencies.
- Sample cap: up to 1 MB read per analysis. `ReadBytes(0, readLen)` must be called on the UI thread (HexEditorControl API); the CPU-intensive computation then runs on `Task.Run`.
- `async/await` resumes on the UI `SynchronizationContext` — no explicit `Dispatcher.BeginInvoke` needed.

### Theme compliance

All brushes use `DynamicResource` tokens from the active global theme.
