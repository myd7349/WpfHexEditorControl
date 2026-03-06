<div align="center">
  <a href="Images/Logo2026.png"><img src="Images/Logo2026.png" width="750" /></a>
  <br/><br/>

  <h3>⚡ The Fastest Wpf Hex Editor IDE for .NET ⚡</h3>

  [![NuGet Legacy V1](https://img.shields.io/nuget/v/WPFHexaEditor?color=blue&label=NuGet%20(Legacy%20V1)&logo=nuget)](https://www.nuget.org/packages/WPFHexaEditor/)
  [![.NET Multi-Target](https://img.shields.io/badge/.NET-net48%20%7C%20net8.0--windows-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/Platform-Windows%20WPF-0078D4?logo=windows)](https://github.com/abbaye/WpfHexEditorIDE)
  [![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
  [![Version](https://img.shields.io/badge/Version-0.2.0-blueviolet)](CHANGELOG.md)
  [![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
  [![Languages](https://img.shields.io/badge/Languages-19-success?logo=googletranslate&logoColor=white)](#-multilingual)
  [![Status](https://img.shields.io/badge/Status-Active%20Development-orange)](https://github.com/abbaye/WpfHexEditorIDE/commits/master)

  <br/>

  > 🚧 **Active Development** — New features, editors and panels are being added regularly. The IDE application is under active construction. Contributions and feedback welcome!

  <br/>

  <a href="Images/Sample2026-001.png"><img src="Images/Sample2026-001.png" alt="WPF HexEditor IDE" width="900"/></a>
  <br/>
  <sub><i>WpfHexEditor — Full IDE with VS-style docking, project system, and multiple editors</i></sub>

  <p>
    <a href="#-the-ide-application"><b>The IDE</b></a> •
    <a href="#-editors"><b>Editors</b></a> •
    <a href="#-controls--libraries"><b>Controls</b></a> •
    <a href="#-analysis--ide-panels"><b>Panels</b></a> •
    <a href="#-quick-start"><b>Quick Start</b></a> •
    <a href="#-documentation"><b>Docs</b></a> •
    <a href="#-whats-next"><b>What's Next</b></a> •
    <a href="CHANGELOG.md"><b>Changelog</b></a>
  </p>
</div>

---

## 🖥️ The IDE Application

**WpfHexEditor** is a full-featured binary analysis IDE for Windows, built entirely with WPF and .NET. It goes far beyond a simple hex editor — think Visual Studio for binary files.

<table>
<tr>
<td width="50%">

### 🏗️ Project System
- **Solution & Project** management (`.whsln` / `.whproj`)
- **Virtual & physical folders** (like VS Solution Explorer)
- **Format versioning** with `IFormatMigrator` + `MigrationPipeline`
- **Atomic upgrade** — `.v{N}.bak` backups before writing
- **Per-file state** persistence (bookmarks, scroll, caret, encoding)
- **`IItemLink`** — typed links between project items (`.bin` ↔ `.tbl`)

</td>
<td width="50%">

### 🪟 VS-Style Docking *(🔧 100% in-house engine)*
- **Float, dock, auto-hide** any panel
- **Colored tabs** with per-document customization (`TabSettingsDialog`)
- **8 built-in themes** (Dark, Light, VS2022Dark, DarkGlass, Minimal, Office, Cyberpunk, VisualStudio)
- **Tab placement** left / right / bottom per dock group

</td>
</tr>
<tr>
<td width="50%">

### 📋 IDE Infrastructure
- **WelcomePanel** — VS Start Page with quick actions, recent files/projects, live changelog
- **Format-aware editor routing** — `preferredEditor` per `.whfmt` definition (400+ formats)
- **IDocumentEditor** plugin contract — every editor is pluggable
- **Undo/Redo/Copy/Cut/Paste** unified via menu bindings
- **VS2022-style status bar** (edit mode, bytes/line, caret offset)
- **VS2026-style Options** — document tab, auto-save, live theme preview, ColorPicker

</td>
<td width="50%">

### 🔍 Binary Intelligence
- **400+ file format** auto-detection (`.whfmt` definitions)
- **Parsed Fields Panel** with structure overlay
- **Data Inspector** — 40+ type interpretations
- **Quick Search** (inline Ctrl+F) + **Advanced Search** (5 modes: Hex, Text, Regex, TBL, Wildcard)
- **19 languages** with instant switching *(partial — not all languages fully translated)*

</td>
</tr>
</table>

---

## 📝 Editors

WpfHexEditor uses a **plugin architecture** (`IDocumentEditor`) — every editor is a standalone, reusable component hosted in the docking system.

| Editor | Status | Progress | Description |
|--------|--------|----------|-------------|
| **Hex Editor** | ✅ Active | ~75% | Binary editing — insert/overwrite, 400+ format detection, search, bookmarks, TBL support |
| **Code Editor** | ✅ Active | ~65% | Multi-language editor — syntax highlighting, language registry (`.whlang`), `IEditorPersistable`, diagnostics |
| **TBL Editor** | ✅ Active | ~65% | Character table editor for custom encodings and ROM hacking |
| **JSON Editor** | ✅ Active | ~60% | JSON editing with real-time validation, syntax highlighting and Error Panel diagnostics |
| **Text Editor** | ✅ Active | ~50% | Text editing with syntax highlighting and encoding support |
| **Structure Editor** | 🔧 Early | ~20% | Binary structure editor — field mapping and overlay |
| **Image Viewer** | 🔧 Stub | ~5% | Binary image viewer (BMP, PNG, JPEG…) |
| **Audio Viewer** | 🔧 Stub | ~5% | Audio binary viewer (WAV, MP3…) |
| **Diff Viewer** | 🔧 Stub | ~5% | Side-by-side binary comparison with diff navigation |
| **Disassembly Viewer** | 🔧 Stub | ~5% | Binary disassembler (x86/x64) |
| **Entropy Viewer** | 🔧 Stub | ~5% | Entropy analysis and visualization |
| **Tile Editor** | 🔧 Stub | ~5% | Tile-based graphic editor for retro/game ROMs |
| **Script Editor** | 🔧 Stub | ~5% | Embedded script editor (automation & macros) |
| **Changeset Editor** | 🔧 Stub | ~5% | Visual changeset review and annotation |

> **Implementing a new editor?** See [IDocumentEditor contract](Sources/WpfHexEditor.Editor.Core/) and register via `EditorRegistry`.

---

## 🧩 Standalone Controls & Libraries

All controls are **independently reusable** — no IDE required. Drop any of them into your own WPF application with a simple project reference.

### UI Controls

| Control | Frameworks | Progress | Description |
|---------|-----------|----------|-------------|
| **[HexEditor](Sources/WpfHexEditor.HexEditor/)** | net48 · net8.0-windows | ~80% | Full-featured hex editor UserControl — MVVM, 16 services, insert/overwrite, search, bookmarks, TBL, 400+ format detection |
| **[HexBox](Sources/WpfHexEditor.HexBox/)** | net48 · net8.0-windows | ~80% | Lightweight hex input field — zero external dependencies, MVVM-ready |
| **[ColorPicker](Sources/WpfHexEditor.ColorPicker/)** | net48 · net8.0-windows | ~95% | Compact color picker UserControl with RGB/HSV/hex input — integrated in Options and TabSettingsDialog |
| **[BarChart](Sources/WpfHexEditor.BarChart/)** | net48 · net8.0-windows | ~60% | Byte frequency distribution chart (0x00–0xFF visualization) |
| **[Docking.Wpf](Sources/WpfHexEditor.Docking.Wpf/)** | net8.0-windows | ~70% | **Custom-built** VS-style docking engine — float, dock, auto-hide, colored tabs, 8 themes — 100% in-house, zero third-party dependency |

### Libraries & Infrastructure

| Library | Frameworks | Description |
|---------|-----------|-------------|
| **[Core](Sources/WpfHexEditor.Core/)** | net48 · net8.0-windows | ByteProvider, 16 services, data layer — the engine powering HexEditor |
| **[Editor.Core](Sources/WpfHexEditor.Editor.Core/)** | net48 · net8.0-windows | `IDocumentEditor` plugin contract, `EditorRegistry`, `IEditorPersistable`, shared interfaces |
| **[BinaryAnalysis](Sources/WpfHexEditor.BinaryAnalysis/)** | net8.0-windows | 400+ format detection engine, `.whfmt` definitions, DataInspector service |
| **[ProjectSystem](Sources/WpfHexEditor.ProjectSystem/)** | net8.0-windows | `.whsln` / `.whproj` format, `SolutionManager`, migration pipeline, file templates |
| **[Definitions](Sources/WpfHexEditor.Definitions/)** | net8.0-windows | 400+ embedded `.whfmt` format catalog, `EmbeddedFormatCatalog` singleton |
| **[Options](Sources/WpfHexEditor.Options/)** | net8.0-windows | `AppSettings` / `AppSettingsService`, 4 option pages (General, SolutionExplorer, CodeEditor, TextEditor) |

---

## 🗂️ IDE Panels

Panels connect to the active document automatically via the docking system.

| Panel | Progress | Description |
|-------|----------|-------------|
| **Welcome Panel** | ~80% | VS Start Page — quick actions, recent files/projects, live changelog from GitHub, theme-compliant |
| **Solution Explorer** | ~70% | Project tree with virtual & physical folders, Show All Files, drag & drop from Windows Explorer, context menus |
| **Parsed Fields Panel** | ~75% | 400+ format detection — parsed field list with type overlay and inline editing |
| **Data Inspector** | ~65% | 40+ byte interpretations at caret position (int, float, GUID, date, color, …) |
| **Structure Overlay** | ~55% | Visual field highlighting superimposed on the hex grid |
| **Properties Panel** | ~50% | Context-aware properties for the active document (F4) |
| **Error Panel** | ~70% | VS-style diagnostics panel with severity filtering — any `IDiagnosticSource` editor can push errors |
| **Output Panel** | ~65% | Session log, file operation messages and build feedback |
| **Options** | ~75% | VS2026-style settings document tab — theme, display, editing defaults, ColorPicker, auto-save |
| **Quick Search Bar** | ~60% | Inline Ctrl+F overlay (VSCode-style) — find next/prev, regex toggle, jump to Advanced |
| **Advanced Search** | ~50% | Full-featured search dialog — 5 modes: Hex, Text, Regex, TBL, Wildcard |
| **File Diff** | ~30% | Side-by-side binary comparison with diff navigation (F7/F8) |

---

## 📸 Screenshots

<table>
<tr>
<td width="50%" align="center">
  <b>🖥️ IDE Overview</b><br/>
  <sub>VS-style docking with Solution Explorer, HexEditor and ParsedFieldsPanel</sub><br/><br/>
  <a href="Images/App-IDE-Overview.png"><img src="Images/App-IDE-Overview.png" alt="IDE Overview"/></a>
</td>
<td width="50%" align="center">
  <b>🔬 Parsed Fields Panel</b><br/>
  <sub>400+ format auto-detection with structured field analysis</sub><br/><br/>
  <a href="Images/App-ParsedFields.png"><img src="Images/App-ParsedFields.png" alt="Parsed Fields"/></a>
</td>
</tr>
<tr>
<td width="50%" align="center">
  <b>📝 Multi-Editor Tabs</b><br/>
  <sub>HexEditor, TBL, JSON and Code editors side by side</sub><br/><br/>
  <a href="Images/App-Editors.png"><img src="Images/App-Editors.png" alt="Multiple Editors"/></a>
</td>
<td width="50%" align="center">
  <b>🗂️ Solution Explorer</b><br/>
  <sub>VS-style project tree with virtual and physical folders</sub><br/><br/>
  <a href="Images/App-SolutionExplorer.png"><img src="Images/App-SolutionExplorer.png" alt="Solution Explorer"/></a>
</td>
</tr>
</table>

<details>
<summary>📷 <b>More screenshots</b> — TBL Editor, Error List, Output, Light theme</summary>

<br/>

<table>
<tr>
<td width="50%" align="center">
  <b>📋 TBL Editor</b><br/>
  <sub>Custom character table editor for ROM hacking and encodings</sub><br/><br/>
  <a href="Images/App-TBLEditor.png"><img src="Images/App-TBLEditor.png" alt="TBL Editor"/></a>
</td>
<td width="50%" align="center">
  <b>🔴 Error Panel</b><br/>
  <sub>Diagnostics and validation errors from active editors</sub><br/><br/>
  <a href="Images/App-ErrorList.png"><img src="Images/App-ErrorList.png" alt="Error Panel"/></a>
</td>
</tr>
<tr>
<td width="50%" align="center">
  <b>📤 Output Panel</b><br/>
  <sub>Session log, messages and file operation feedback</sub><br/><br/>
  <a href="Images/App-Output.png"><img src="Images/App-Output.png" alt="Output Panel"/></a>
</td>
<td width="50%" align="center">
  <b>☀️ Light Theme</b><br/>
  <sub>8 built-in themes — Dark, Light, VS2022Dark, DarkGlass, and more</sub><br/><br/>
  <a href="Images/App-Theme-Light.png"><img src="Images/App-Theme-Light.png" alt="Light Theme"/></a>
</td>
</tr>
<tr>
<td colspan="2" align="center">
  <b>🎮 TBL Format Explained</b><br/>
  <sub>Custom character table format for game ROM editing</sub><br/><br/>
  <a href="Images/TBLExplain.png"><img src="Images/TBLExplain.png" alt="TBL Explained" width="600"/></a>
</td>
</tr>
</table>

</details>

---

## ⚡ Quick Start

### Run the IDE

```bash
git clone https://github.com/abbaye/WpfHexEditorIDE.git
```

Open `WpfHexEditorControl.sln` in Visual Studio 2022, set **WpfHexEditor.App** as startup project, and run.

### Embed the HexEditor in your WPF app

**Reference the V2 projects:**
```xml
<ProjectReference Include="..\WpfHexEditor.Core\WpfHexEditor.Core.csproj" />
<ProjectReference Include="..\WpfHexEditor.HexEditor\WpfHexEditor.HexEditor.csproj" />
```

**Add to your XAML:**
```xml
<Window xmlns:hex="clr-namespace:WpfHexEditor.HexEditor;assembly=WpfHexEditor.HexEditor">
  <hex:HexEditor FileName="data.bin" />
</Window>
```

> **NuGet:** A V1 legacy package (`WPFHexaEditor`) is still available on NuGet but is no longer maintained. V2 NuGet packaging is planned.

> **JetBrains Rider:** See the **[Rider Guide](docs/IDE/RIDER_GUIDE.md)** for IntelliSense tips.

**[Complete Tutorial →](GETTING_STARTED.md)**

---

## 🎯 Why Choose WPF HexEditor?

<table>
<tr>
<td width="33%">

### ⚡ Performance
- **99% faster** rendering (DrawingContext)
- **10-100x faster** search (LRU + SIMD + parallel)
- **80-90% less** memory (Span\<T\> + pooling)
- Handles **GB+ files** without freezing

</td>
<td width="33%">

### 🏗️ Clean Architecture
- **MVVM** with 16 specialized services
- **Partial classes** organized by feature
- **Plugin editors** via IDocumentEditor
- **100% testable**, zero UI in services

</td>
<td width="33%">

### 🖥️ Full IDE
- **Project system** (.whsln / .whproj)
- **VS-style docking** (no third-party lib)
- **8 themes** out of the box
- **5 functional editors** + 9 planned

</td>
</tr>
<tr>
<td width="33%">

### 🔍 Binary Intelligence
- **400+ formats** auto-detected
- **Format-aware editor routing** per file type
- **Parsed Fields** with type overlay
- **Data Inspector** 40+ interpretations

</td>
<td width="33%">

### 🌍 Multilingual
- **19 languages** defined *(partial translations)*
- **Instant switching** at runtime
- No restart required
- Extensible with new languages

</td>
<td width="33%">

### ✅ Production Ready
- **Insert Mode** bug fixed (#145)
- **Save data loss** resolved
- **Unlimited Undo/Redo**
- **Async** file operations with progress

</td>
</tr>
</table>

---

## 🔮 What's Next

> Planned features — subject to change. See [CHANGELOG.md](CHANGELOG.md) for details.

<table>
<tr>
<td width="50%">

### 🔌 Plugin System & SDK
- Public `WpfHexEditor.SDK` — versioned, open-source API
- Hot-load / Hot-unload plugins at runtime
- Process-level sandbox (`WpfHexEditor.PluginSandbox`)
- CPU/memory monitoring + slow-plugin warnings
- `.whix` package format + Plugin Installer tool
- Official panels as first-class plugin packages

</td>
<td width="50%">

### ⚙️ IDE Core Infrastructure
- Service Container / Dependency Injection
- Global CommandBus — all actions routed centrally
- Configurable Keyboard Shortcuts (per-plugin)
- Persistent preferences — global + per-workspace
- Integrated Terminal (PowerShell / Bash / CMD)

</td>
</tr>
<tr>
<td width="50%">

### 📝 CodeEditor — VS-Like Overkill
- Multi-caret, virtual scroll for >1 GB files
- Incremental syntax highlighting + code folding
- IntelliSense: autocomplete, snippets, quick-info
- Real-time diagnostics → Error Panel + EventBus
- AI-Assisted suggestions + auto-refactoring hints

</td>
<td width="50%">

### 🚀 Advanced IDE Modules
- **Integrated Debugger** — breakpoints, watches, step over/into
- **Git Integration** — inline gutter diff, commit/push/pull
- **Plugin Marketplace** — browse, install, update (sandboxed)
- **Advanced Refactoring** — rename symbol, extract method
- **Unit Testing Panel** — auto-detect NUnit/JUnit/PyTest
- **Code Analysis & Metrics** — dependency graphs, complexity
- **Multi-User Collaboration** — real-time pair programming
- **Internationalization** — EN/FR initial, plugin-extensible

</td>
</tr>
</table>

---

## 📚 Documentation

### User Guides

| Document | Description |
|----------|-------------|
| **[GETTING_STARTED.md](GETTING_STARTED.md)** | Run the IDE or embed the control — step by step |
| **[FEATURES.md](FEATURES.md)** | Complete feature list — IDE, editors, panels, controls |
| **[CHANGELOG.md](CHANGELOG.md)** | Version history and what's new |
| **[MIGRATION.md](docs/migration/MIGRATION.md)** | V1 → V2 migration guide (zero code changes required) |

### Developer Reference

| Document | Description |
|----------|-------------|
| **[Architecture Overview](docs/architecture/Overview.md)** | System architecture, services, MVVM patterns |
| **[Core Systems](docs/architecture/core-systems/)** | ByteProvider, position mapping, undo/redo, rendering |
| **[Data Flow](docs/architecture/data-flow/)** | File, edit, search and save operation sequences |
| **[API Reference](docs/api-reference/)** | Full API documentation with code examples |
| **[Rider Guide](docs/IDE/RIDER_GUIDE.md)** | JetBrains Rider setup and tips |

---

## 🔧 Supported Frameworks

| Framework | Version | Notes |
|-----------|---------|-------|
| **.NET Framework** | 4.8 | Full support — LRU cache, parallel search |
| **.NET** | 8.0-windows | Maximum performance — Span\<T\>, SIMD, PGO |

**Recommendation:** Use .NET 8.0 for best performance (SIMD vectorization, Profile-Guided Optimization).

---

## 🌍 Multilingual

19 languages defined with instant runtime switching (no restart).

> ⚠️ **Partial translations** — not all languages are fully translated yet. English and French are the most complete. Contributions welcome!

English · French · Spanish · German · Italian · Japanese · Korean · Dutch · Polish · Portuguese · Russian · Swedish · Turkish · Chinese · Arabic · Hindi · and more

---

## ⭐ Support This Project

WPF HexEditor is **100% free and open source** (Apache 2.0) — free for personal and commercial use.

This project is developed in **free time** by passionate contributors. If you find it useful:

- ⭐ **Star this repository** — helps others discover the project
- 🍴 **Fork and contribute** — pull requests are always welcome
- 💬 **Share feedback** — report bugs or suggest features via [GitHub Issues](https://github.com/abbaye/WpfHexEditorIDE/issues)

**Every star motivates us to keep building! 🙏**

---

## 🤝 Contributing

We welcome contributions! The codebase is well-organized and documented:

- **[Architecture Guide](docs/architecture/Overview.md)** — service-based design and patterns
- **[CONTRIBUTING.md](CONTRIBUTING.md)** — branch conventions, standards, how to submit a PR
- **Partial class structure** — every feature in its own file, easy to navigate
- **[Sample App](Sources/WpfHexEditor.App/)** — real integration to learn from

---

## 📝 License

**Apache License 2.0** — free for personal and commercial use. See [LICENSE](LICENSE) for details.

---

## 📧 Contact & Support

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/abbaye/WpfHexEditorIDE/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/abbaye/WpfHexEditorIDE/discussions)
- 📧 **Email**: derektremblay666@gmail.com

---

<div align="center">
  <br/>
  <p>
    <b>✨ WPF HexEditor ✨</b><br/>
    <sub>A powerful, actively developed hex editor IDE for .NET</sub>
  </p>
  <p>
    <sub>Created by Derek Tremblay (abbaye)<br/>
    Contributors: Claude Sonnet 4.6, Claude Opus 4.6<br/>
    V1 Contributors: ehsan69h, Janus Tida</sub>
  </p>
  <p>
    <sub>Coded with ❤️ for the community! 😊🤟 (with a touch of AI magic ✨)</sub>
  </p>
  <br/>

  **[🚀 Quick Start](#-quick-start)** •
  **[📖 Tutorial](GETTING_STARTED.md)** •
  **[📊 Features](FEATURES.md)** •
  **[📝 Changelog](CHANGELOG.md)** •
  **[🔮 What's Next](#-whats-next)** •
  **[⭐ Star Us](https://github.com/abbaye/WpfHexEditorIDE)**

  <br/>
</div>
