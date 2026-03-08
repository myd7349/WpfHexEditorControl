# WpfHexEditor.Plugins.ArchiveStructure

**Project:** WpfHexEditor.Plugins.ArchiveStructure
**License:** GNU Affero General Public License v3.0
**Platform:** net8.0-windows (WPF)
**Default dock:** Left

---

## Description

The Archive Structure plugin presents the hierarchical contents of archive files (ZIP, RAR, 7z, and similar formats) in a dockable tree panel. It shows files and folders with sizes, compression ratios, CRC status, and compression method. Aggregate statistics (total files, folders, uncompressed and compressed sizes, compression ratio) are displayed in the panel header.

---

## Features

- **Hierarchical tree view** of archive entries — folders and files with expand/collapse.
- **Per-entry metadata**: name, size, compressed size, CRC, compression method.
- **Aggregate statistics** displayed in header: file count, folder count, total size, compression ratio.
- **File type icons** mapped by extension (text, image, archive, executable, audio/video, generic).
- **CRC error indicator** — entries with a failed checksum display a warning icon.
- **Entry details dialog** — right-click or toolbar button shows full entry details.
- **Expand All / Collapse All** tree controls.
- **Extract action** — placeholder that informs the user an archive library is required (extraction is deferred to a future integration).
- **ViewModel** (`ArchiveStructurePanelViewModel`) exposes `RootNodes`, file/folder counts, total size, and compression ratio as observable properties for MVVM binding.

---

## Panel

| Property | Value |
|---|---|
| Panel ID | `WpfHexEditor.Plugins.ArchiveStructure.Panel.ArchiveStructurePanel` |
| Default dock side | Left |
| View menu entry | `View > Archive Structure` |

### Internal components

| Component | Responsibility |
|---|---|
| `ArchiveStructurePanel.xaml(.cs)` | TreeView rendering, stat labels, toolbar handlers |
| `ArchiveStructurePanelViewModel.cs` | Observable root nodes, aggregate statistics |
| `ArchiveNode` | Observable tree node model (name, size, compressed size, CRC, method, children) |
| `ArchiveStats` | Plain stat accumulator used during tree traversal |

### Architecture notes

- Pattern: **MVVM** — ViewModel holds the observable state; panel code-behind handles only UI events.
- Tree expansion is animated via the standard WPF `TreeView`; `IsExpanded` is an observable property on `ArchiveNode`.
- Statistics are computed by a recursive `CalculateStatsRecursive` traversal at load time, not incrementally.

### Theme compliance

All brushes use `DynamicResource` tokens from the active global theme. No hardcoded colors.
