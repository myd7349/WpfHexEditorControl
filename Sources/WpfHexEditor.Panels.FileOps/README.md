# WpfHexEditor.Panels.FileOps

File operation dockable panels — Archive Structure tree viewer and side-by-side File Comparison diff panel.

**License:** GNU Affero General Public License v3.0
**Target:** net8.0-windows · WPF

---

## Architecture / Modules

```
WpfHexEditor.Panels.FileOps/
├── Panels/
│   ├── ArchiveStructurePanel.xaml(.cs)
│   ├── FileComparisonPanel.xaml(.cs)
│   └── Dialogs/
└── WpfHexEditor.Panels.FileOps.csproj
```

### Panels

- **`ArchiveStructurePanel.xaml` / `.cs`** — tree view of archive contents (ZIP, RAR, 7Z, etc.).
  - `LoadArchive(ArchiveNode root)` — binds the root node to the `TreeView` and updates the summary bar.
  - Summary bar: file count, folder count, total size, compression ratio (displayed only when compressed size > 0).
  - `ArchiveNode` — recursive tree node: `Name`, `Size`, `CompressedSize`, `IsFolder`, `Children`.
  - `ArchiveStats` — aggregated statistics computed by recursive walk: TotalFiles, TotalFolders, TotalSize, CompressedSize.
  - Empty state: displays "No archive loaded" when no root is provided.

- **`FileComparisonPanel.xaml` / `.cs`** — side-by-side binary/structured file diff.
  - Two-pane layout (File 1 / File 2), each loaded via `OpenFileDialog`.
  - Loads structured field data using `WpfHexEditor.Core.FormatDetection` + `ParsedField` pipeline.
  - `UpdateComparison()` — aligns field lists and produces `ComparisonRow` records with a `DiffKind` discriminator.
  - Diff kinds: `Equal`, `Modified`, `OnlyInFile1`, `OnlyInFile2`.
  - Status bar: match count and difference count.

### Data Models

- **`ArchiveNode`** — recursive tree node (name, sizes, folder flag, children list).
- **`ArchiveStats`** — aggregate statistics struct.
- **`ComparisonRow`** — record pairing a field from each file with a `DiffKind` value.

### Dialogs (`Dialogs/`)

Supporting dialogs for file selection and comparison configuration.

---

## Dependencies

| Project | Used for |
|---|---|
| `WpfHexEditor.Core` | Format detection, `ParsedField` model |
| `WpfHexEditor.HexEditor` | Raw byte access via `GetCopyData` |
| `WpfHexEditor.Editor.Core` | `IPropertyProvider`, editor event contracts |

---

## Theme Compliance

Both panels bind background, foreground, and border colors via `DynamicResource` to global theme brushes. `TreeView` item templates use `DockAccentBrush` for selected-state highlight.
