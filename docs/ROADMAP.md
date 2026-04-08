# 🗺️ WpfHexEditor — Roadmap

This document tracks all planned and in-progress features for the WpfHexEditor IDE.
Features already shipped are in [CHANGELOG.md](CHANGELOG.md).

> **Legend:** 🔧 In Progress · 🔜 Planned · ✅ Done (see CHANGELOG)
>
> 📅 *Last revised: 2026-04-06 (audit — corrected feature status)*

---

## 🔧 In Progress

| Feature # | Title | Description | Progress |
|-----------|-------|-------------|----------|
| #81 | **Plugin Sandbox** | Out-of-process isolation via HWND embedding + full IPC bridge (menus, toolbar, events). HWND parenting, Job Object resource control, IPC HexEditor event bridge done. Auto-isolation engine done. IDE EventBus IPC bridge done. Plugin signing + signature validation done (ADR-SB-01). Remaining: gRPC migration, hot-reload from sandbox. | ~75% |
| #84 | **Code Editor — VS-Like Advanced** | Full feature set shipped: navigation bar, 55+ language definitions (incl. F# + VB.NET), URL hover/click, find/replace, split view, `IEditorPersistable`, Ctrl+Click cross-file nav, LSP multi-location popup, Alt+Click rect selection, drag-to-move, `#region` colorization, shared `UndoEngine`, data-driven folding (4 strategies), comment-aware brace matching, 454 `.whfmt` definitions, gutter diagnostics, multi-caret, diagnostics integration, full LSP suite, word wrap, **auto-close brackets/quotes + skip-over + wrap-selection** (#163 ✅), **end-of-block hover hint** ✅, **word highlight** ✅, **sticky scroll with line numbers** (#160 ✅), **Find All References + dockable panel** (#157 ✅), **bracket pair colorization** (#162 ✅), **color swatch preview** (#168 ✅), **format-on-save** (#159 partial ✅), **Ctrl+Click links & emails** (ADR-CTRL-LINK-01 ✅ v0.6.4.6), **Roslyn semantic inline hints upgrade** (ADR-ROSLYN-INLINEHINTS-01 ✅ v0.6.4.6 — `IReferenceCountProvider`, whfmt-driven `CanProvide`), **full LSP code formatting** (`CodeFormattingService` + `CodeFormatter` + `RoslynFormattingProvider` ✅), **code minimap** (`MinimapControl` ✅ #161), **smart indentation** (`SmartIndentService` ✅ #164), **column ruler guides** (`CE_RulerBrush` + options wired ✅ #165), **expand/collapse all folds** (`FoldingEngine.CollapseAll/ExpandAll` ✅ #167), **inline gutter change markers** (`ChangeMarkerGutterControl` + `GutterChangeTracker` ✅ #166 — `CE_GutterAdded/Modified/Deleted` × 18 themes, save-point hash diff), **peek definition** (#158 ✅ — `InlinePeekHost`, `Alt+F12`), **LSP inlay hints layer** (`LspInlayHintsLayer` ✅ — parameter name pills, `CE_InlayHint*` tokens), **LSP declaration hints layer** (`LspDeclarationHintsLayer` ✅ — Run|Debug above test methods, reference counts), **incremental document sync** (`IIncrementalSyncClient` ✅ — `TextDocumentSyncKind=2`). Remaining: inline value hints (debug-session variable overlay, via #85 LSP). | ~99% |
| #101–103 | **MSBuild & VS Solution Support** | Open `.sln` files; build/rebuild/clean via MSBuild API; output routed to Build channel; severity coloring; auto-focus; project templates; error list navigation; incremental build with dirty tracking (`IIncrementalBuildTracker`, FSW per project, `BuildDirtyAsync()`); orange dirty dot in Solution Explorer (`Ctrl+Alt+F7`); **parallel project builds** (`SemaphoreSlim`-gated `Task.WhenAll`, `MaxParallelProjects` from `AppSettings`, `Interlocked`-safe counters); **build progress bar** in status bar ✅; **VB.NET project template write** (`VbNetConsoleAppTemplate` ✅). Remaining: VB.NET item group editing, nested solution folders. | ~97% |
| #104–106 | **Assembly Explorer + Decompilation** | .NET PE tree, C# decompilation → `CodeEditorSplitHost` (syntax-highlighted, read-only) ✅, ILSpy backend, VB.NET decompilation ✅, CFG Canvas, Assembly Diff, Assembly Search, XRef View, Decompile Cache, Ctrl+Click external symbol decompilation via `FindAssemblyPath` (AppDomain + runtime + NuGet) + `CSharpSkeletonEmitter` pipeline; **ECMA-335 token→offset** (`PeOffsetResolver` ✅); **hex sync** (`AssemblyHexSyncService` ✅). Remaining: plugin panel improvements, PDB source-link matching. | ~92% |
| #107 | **Document Model** | `IDocumentBuffer` / `DocumentBuffer` (thread-safe, Dispatcher-marshalled); `IBufferAwareEditor` implemented by `CodeEditor`, `TextEditor`, `MarkdownEditorHost`, `CodeEditorSplitHost`, `XamlDesignerSplitHost`, **`HexEditor`** (`HexEditor.BufferAware.cs` partial — `ByteModified` debounce 300ms, `OpenStream(MemoryStream)` reload, 10MB cap); `DocumentManager` buffer lifecycle; `LspBufferBridge` (300 ms debounce → `DidChange`); `LspDocumentBridgeService`; **HexEditor block undo/redo** (ADR-UNDO-01 ✅ — paste/cut/delete as single undo step, `UndoGroup` composite, coalescence, VS-style history dropdown). Remaining: multi-editor collaboration, undo/redo unification across editors (CodeEditor ↔ HexEditor shared history). | ~70% |
| #85–86 | **LSP Engine / SmartComplete** | Full JSON-RPC LSP client (`LspClientImpl`); `ServerCapabilities` parse; 10 LSP providers: completion (LSP-first + local fallback), hover, signature help, code actions (`Ctrl+.`), rename (`F2`), inlay hints, code lens, semantic tokens, breadcrumb bar, workspace symbols (`Ctrl+T`); `LspDocumentSync` (DidOpen/DidChange/DidClose); `LspStatusBarAdapter`; `LspServersOptionsPage`; F# (`fsautocomplete`) + VB.NET server entries ✅ (Roslyn in-process for C#/VB.NET — ADR-ROSLYN-01); 30 new tokens × 18 themes; **call hierarchy** (`LspCallHierarchyProvider` + `CallHierarchyPanel` ✅ Shift+Alt+H); **linked editing** (`LspLinkedEditingProvider` ✅), **incremental document sync** (`IIncrementalSyncClient`, `TextDocumentSyncKind=2` ✅ Phase 3), **pull-diagnostics interface** (`IDiagnosticsModeClient`, `UsesPullDiagnostics` ✅ Phase 3), **LSP inlay hints** (`LspInlayHintsLayer` ✅), **LSP declaration hints** (`LspDeclarationHintsLayer` ✅). Remaining: inline value hints. | ~96% |

---

## 🔜 Planned — IDE Core & Build

| Feature # | Title | Description |
|-----------|-------|-------------|
| #36 | **Service Container / DI** | `ServiceContainer` singleton with `FileService`, `EditorService`, `PanelService`, `PluginService`, `EventBus`, `TerminalService`; Singleton/Scoped/Transient lifecycle. |
| #37 | **Global CommandBus** | All IDE actions (menus, toolbar, terminal, plugins) routed through `CommandBus`; every command has Id, Handler, CanExecute context, and Category. |
| #38 | **Keyboard Shortcuts & Bindings** | `KeyBindingService`, configurable gestures per command, conflict detection, plugin-extensible, export/import. |
| #39 | **User Preferences Persistence** | `ConfigurationManager`, per-section schemas, plugin config API, export/import, cross-session persistence. |
| #40 | **Centralized Logging & Diagnostics** | `LogService` (Info/Warning/Error/Debug), `DiagnosticService` (perf metrics), `LogSink` abstraction, Output + Error Panel integration. |
| #77 | **Workspace System & State Serialization** | Plugin lazy loading fully done (ADR-LAZY-01 ✅ v0.6.4.6 — manifest stubs, single-click activation, panel persistence across restarts). Multi-workspace management with project-specific settings — remaining. |
| #78 | **Command System (Internal IDE Commands)** | Central command registry for all IDE actions accessible via menu, terminal, keyboard shortcuts, and scripts. |
| #79 | **Scripting / Automation Engine** | Execute `.hxscript` files via terminal to automate IDE workflows, plugin actions, and batch operations. |
| #82 | **Service Registry & Dependency Injection** | Central DI container exposing all IDE services to plugins uniformly via `IServiceRegistry`. |
| #83 | **Options Document / IDE Settings** | Unified options panel for editor behavior, themes, plugins, and workspace config with live preview. |
| #87 | **Workspace Templates** | Pre-configured project templates for common file structures and development workflows. |
| #100 | **IDE Localization Engine** | Full i18n support for IDE UI (currently English only). HexEditor control already supports 19 languages. EN/FR initial; plugin-provided translations; dynamic switching. |
| #101 | **`.sln` Parser** | *(In Progress — see above)* Open and parse existing Visual Studio 2019/2022 `.sln` files via `VsSolutionLoaderPlugin`. Remaining: nested solution folders, shared projects, full project graph. |
| #102 | **C# / VB.NET Project Support** | *(In Progress)* `.csproj` parsing via MSBuild Locator. Remaining: VB.NET, write support, item group editing. |
| #103 | **MSBuild API Integration** | *(In Progress — see above)* Build/rebuild/clean done. Remaining: error list navigation with file/line jump, incremental builds, parallel project builds. |

---

## 🔜 Planned — Ultimate IDE Architecture (VS-Level)

Cette section présente les concepts VS-level de l’IDE, en se concentrant uniquement sur les fonctionnalités **non encore incluses** dans la roadmap actuelle.

| Feature # | Title | Description | Remarque |
|-----------|-------|-------------|----------|
| #153 | **Visual Scripting / Graph View** | Node-based visual representation for binary structures, memory flow, or data pipelines; plugin-extensible graphs for analysis and automation. | Nouveau concept, pas encore dans la roadmap |
| #154 | **Cross-Platform Ready Core** | WPF/.NET 8 foundation; potential interop with WinForms; future-proof design for possible Linux/macOS port with Avalonia/MAUI. | Nouveau concept, pas encore dans la roadmap |

---

## 🔜 Planned — Editors & Code Intelligence

| Feature # | Title | Description |
|-----------|-------|-------------|
| #84 | **Code Editor — VS-Like Advanced** | *(In Progress ~96% — see above)* |
| #85 | **LSP Engine** | *(In Progress ~93% — see above)* Remaining: inline value hints, LSP 3.18 pull-diagnostics (`textDocument/diagnostic`). Note: Roslyn replaces OmniSharp for C#/VB.NET (ADR-ROSLYN-01). |
| #179 | **Document Structure Panel** | ✅ **Done v0.6.4.6** — `WpfHexEditor.Plugins.DocumentStructure`; 8 providers (LSP/SourceOutline/JSON/XML/Markdown/INI/Binary/Folding); `DocumentStructureProviderResolver`; `IDocumentStructureProvider` SDK extension point; 18 `DS_*` theme tokens × 18 themes. |
| #180 | **Tab Groups** | ✅ **Done v0.6.4.6** — `ITabGroupService` SDK contract + `TabGroupService`; split horizontal/vertical; move between groups; 16 `TG_*` theme tokens; keyboard shortcuts; settings page; 77 integration tests. |
| #86 | **SmartComplete v4 (LSP)** | Advanced autocomplete, signature help, quick-info, multi-caret editing, virtual scroll for >1 GB files. |
| #88 | **Dynamic Snippets** | `SnippetsManager` with context-aware snippets, dynamic variables (`CurrentLine`, `FileName`, `CursorPosition`); user/plugin/language-scoped. |
| #89 | **AI-Assisted Code Suggestions** | AI Assistant plugin shipped (v0.6.4.3) with 5 providers, 25 MCP tools, streaming chat, inline apply. Remaining: `AICompletionEngine` (inline ghost-text completions), `AIRefactoringAssistant`, plugin-extensible AI rules. |
| #94 | **Advanced Refactoring** | Rename symbol (workspace-wide), extract method/class, inline variable, move file between projects; AI-assisted suggestions. |
| #96 | **Code Analysis & Metrics** | Cyclomatic complexity, code duplication detection, dependency graphs; dedicated panel with filter/sort. |
| #106 | **.NET Decompilation via ILSpy** | C# skeleton view + full IL disassembly per method; "Go to Metadata Token" navigation; decompiled source in Code Editor tab. |
| #157 | **Go-to-References / Find All References** | ✅ **Done v0.6.3.7** — `Shift+F12`; `WorkspaceFileCache` (thread-safe solution file index); `InlineHintsService` (regex reference counting); dockable `FindReferencesPanel` with scope filter + search debounce; `F8`/`Shift+F8` navigation. |
| #158 | **Peek Definition** | ✅ **Done** — `Alt+F12`; `InlinePeekHost` (DrawingVisual overlay, title bar, resize handle, keyboard nav, Esc to close); `ShowPeekDefinitionAsync` + `OpenInlinePeekAsync` in `CodeEditor.LSP.cs`; command registered in `MainWindow.Commands.cs`. |
| #159 | **Code Formatting** | ✅ **Done** — Format-on-save toggle (`FormatOnSave` DP), full LSP `textDocument/formatting` + range formatting (`CodeFormattingService`, `RoslynFormattingProvider`, `BasicIndentFormatter` fallback), `Ctrl+K Ctrl+D` document format. |
| #160 | **Sticky Scroll** | ✅ **Done v0.6.3.7** — Scope header pinned at top while scrolling; line numbers in gutter; syntax-highlighted rows; 6 options; `StickyScrollHeader` with allocation-free `OnRender` (resources cached in `Update()`); click to navigate. |
| #161 | **Code Minimap** | ✅ **Done** — `MinimapControl` (GlyphRun canvas, click-to-scroll, viewport slider, diagnostic overlay, context menu, `CE_Minimap*` tokens). |
| #162 | **Bracket Pair Colorization** | ✅ **Done v0.6.4.2** — `BracketPairColorizationEnabled` DP on `CodeEditor`; `BracketDepthColorizer` pipeline; synthetic bracket `RegexHighlightRule` generated from `language.BracketPairs`; `CE_Bracket_1/2/3/4` tokens × 18 themes; `InvalidateAllCache()` on highlighter change; option wired in settings. |
| #163 | **Auto-close Brackets & Quotes** | ✅ **Done v0.6.3.7** — Auto-insert matching `)` `]` `}` `"` `'` on type; skip-over on duplicate type; wrap selection in pair; 4 options (`AutoClosingBrackets`, `AutoClosingQuotes`, `SkipOverClosingChar`, `WrapSelectionInPairs`). |
| #164 | **Smart Indentation** | ✅ **Done** — `SmartIndentService` (3 strategies: CopyIndent, BraceIndent, ColonIndent); respects `.whfmt` indent rules. |
| #165 | **Column Ruler Guides** | ✅ **Done** — Configurable vertical ruler lines at user-defined columns; `CE_RulerBrush` token; per-language defaults via `.whfmt`; wired in options. |
| #166 | **Inline Gutter Change Markers** | ✅ **Done** — `ChangeMarkerGutterControl` (4px strip, `DrawingContext`); `GutterChangeTracker` (O(n) hash diff vs save-point, 800ms debounce); `LineChangeKind` enum; `ShowChangeMarkers` DP; `CE_GutterAdded`/`CE_GutterModified`/`CE_GutterDeleted` × 18 themes. |
| #167 | **Expand / Collapse All Folds** | ✅ **Done** — `FoldingEngine.CollapseAll/ExpandAll`; `Ctrl+M Ctrl+L` = expand all; `Ctrl+M Ctrl+O` = collapse all to definitions; `Ctrl+M Ctrl+M` = toggle current region. |
| #168 | **Color Swatch Inline Preview** | ✅ **Done v0.6.4.2** — `ColorSwatchPreviewEnabled` DP on `CodeEditor`; detects CSS/XAML/C# color literals (e.g. `#FF5733`); renders 12×12 inline swatch; option wired in settings. |
| #169 | **TextEditor Phase 2** | Multi-caret (`Ctrl+Alt+Click`, `Ctrl+D`); column/block select (Shift+Alt+drag); LSP integration (hover, completion); advanced encoding auto-detection; text statistics panel. |
| #170 | **ImageViewer Phase 2** | Inline color picker (click pixel); histogram panel (R/G/B/A channels); batch export (resize/convert/reformat); pixel inspector overlay; animated GIF frame viewer. |
| #171 | **DiffViewer Phase 2** | 3-way merge UI; chunk-level accept/reject staging; word-level diff mode; unified diff view; export as `.patch` file; integrated ChangesetEditor bridge. |
| #172 | **StructureEditor Phase 2** | Live binary preview sync (change field → hex view updates); complex type support (arrays, unions, conditionals, bitfields); template validation engine; import/export template packages. |
| #173 | **EntropyViewer Phase 2** | Named section labels (overlay on chart); chart export (PNG/SVG); multi-file overlay comparison; zoom/pan on entropy graph; region selection → jump to hex offset. |
| #174 | **AudioViewer — Full Implementation** | Waveform rendering (DrawingVisual); playback controls (play/pause/stop/seek); spectrogram view; audio metadata panel (sample rate, channels, bitrate, duration); format-aware (WAV/MP3/FLAC/OGG). |
| #175 | **TileEditor — Full Implementation** | Tile grid picker; palette editor (256-color + alpha); sprite sheet layout (N×M grid); pixel-level editing; export as PNG/BMP; zoom 1×–16×; ROM-format import (2bpp/4bpp/8bpp). |
| #176 | **JsonEditor — Full Implementation** | Dedicated JSON editor (not CodeEditor fallback); collapsible tree view with virtual scroll; JSON Schema validation + inline error markers; JSON path navigator (`$.foo.bar`); format/minify/sort-keys; side-by-side JSON diff. |
| #177 | **ScriptEditor Phase 2** | Step-debugger for `.hxscript` (breakpoints, watch, call stack); REPL panel (interactive HxScript console); snippet templates; syntax error underline + gutter marker; HxScript language server (LSP). |
| #178 | **DisassemblyViewer Phase 2** | x86/x64/ARM/WASM instruction decoding; jump arrows between branch targets; symbol table overlay (resolved function names); address navigation bar; cross-reference to hex view; export as `.asm`. |
| #155 | **Visual XAML Editor — Phase 2** | Core designer shipped in v0.6.0; overkill 10-phase improvement (constraint adorner, gradient editor, binding path picker, perf overlay, responsive breakpoint bar) shipped in v0.6.3. Remaining: trigger & animation timeline editor (beyond stub), data-binding wizard, "Go to Definition" for resource keys, multi-resolution DPI preview, export as standalone `.xaml`. |
| #156 | **Class Diagram Plugin** | Full-featured class diagram editor shipped in v0.6.3: regex-based C#/VB.NET source analysis, canvas with 6 docking panels, 36 CD_* theme tokens, Solution Explorer context menus ("View Class Diagram", "Generate for Project/Solution"), plugin menu registration fix (View menu grouping) and double-separator fix. Remaining: live Roslyn-backed analysis, export to SVG/PNG. |

---

## 🔜 Planned — Assembly & Binary

| Feature # | Title | Description |
|-----------|-------|-------------|
| #81 | **Plugin Sandbox (gRPC Migration + Hot-Reload)** | HWND embedding + IPC bridge done (v0.3.0). Remaining: gRPC transport migration, collectible `AssemblyLoadContext` hot-reload from sandbox, plugin restart-less upgrade flow. |
| #97 | **Large File Optimization** | `VirtualizationEngine`, `LazyParser`, multi-core SmartComplete adapter; virtualized display for >1 GB files, incremental parsing. |

---

## 🔜 Planned — DevOps & Collaboration

| Feature # | Title | Description |
|-----------|-------|-------------|
| #41 | **Plugin Marketplace** | `MarketplaceManager` — browse, install, update from online registry; signed `.whxplugin` packages. |
| #42 | **Plugin Security & Sandboxing** | Permission declarations at install time, integrity verification, AppDomain isolation. |
| #43 | **Plugin Auto-Update** | `UpdateService` / `UpdateChecker`, rollback support, scheduled checks for IDE + plugins. |
| #44 | **Integrated Debugger** | `DebuggerService` (StartDebug, StepInto/Over/Out, Evaluate), `BreakpointsManager`, `WatchPanel`, `CallStackPanel`. |
| #90 | **Debugger — Multi-Project** | Multi-project debug sessions via EventBus; supports scripts, plugins, and workspace projects. |
| #91 | **Git Integration** | `GitManager`, `GitPanel` (commit/push/pull/branch); inline gutter diff; `GitEventAdapter` for file-change notifications. |
| #93 | **Plugin Installer / Marketplace UI** | Plugin search UI, download/update manager, sandbox enforcement at install time. |
| #95 | **Unit Testing Panel** | ✅ `WpfHexEditor.Plugins.UnitTesting` plugin — `DotnetTestRunner` (`dotnet test --logger trx --no-build`), `TrxParser` (ECMA TRX XML), `UnitTestingViewModel` (pass/fail/skip counters, `ObservableCollection<TestResultRow>`), dockable `UnitTestingPanel` (Run/Stop/Clear toolbar, color-coded outcome glyphs, duration ms, virtualized ListView); auto-run on `BuildSucceededEvent`; test project auto-detection (xunit/nunit/mstest/Microsoft.NET.Test.Sdk keywords in .csproj). | ✅ Done |
| #98 | **Multi-User Collaboration** | Multi-cursor real-time editing, document sync, contextual chat/comments per line. |

---

## 🔜 Planned — UX & Infrastructure

| Feature # | Title | Description |
|-----------|-------|-------------|
| #99 | **Advanced UI/UX** | `NotificationManager`, `WorkspaceLayoutAdapter`; contextual inline notifications, layout persistence per workspace, full docking for all panels. |

---

## 🔜 Planned — Quality, Tooling & Operations

| Feature # | Title | Description |
|-----------|-------|-------------|
| #45 | **Plugin & Script Testing** | `TestRunnerService` — automated functional, compatibility, security and performance tests for plugins and scripts before execution or installation. |
| #46 | **Integrated Documentation & Contextual Help** | In-IDE documentation browser and context-sensitive help for all components (menus, commands, panels, plugins, APIs). |
| #47 | **Notification & Alert System** | Centralized `NotificationService` — inline toast alerts, priority levels, EventBus integration, plugin-extensible notification hooks. |
| #48 | **Monitoring & Analytics** | IDE health dashboard — plugin CPU/RAM usage, event frequency, command stats, latency tracking; exportable reports. |
| #49 | **Export / Import Projects & Configurations** | Full export/import of workspace configs, settings, plugin lists, templates and keybindings for portability and backup. |
| #54 | **Auto-Save & Restore** | Automatic session backup at configurable intervals; full restore on crash or abnormal exit without data loss. |
| #58 | **Reporting & Dashboards** | Dedicated report panel — code quality metrics, plugin health, build history, test results; filterable and exportable. |
| #63 | **Smart Session Backup & Auto-Resume** | Intelligent session snapshot (open files, cursors, layout, unsaved changes); seamless resume after restart or crash. |
| #65 | **Interactive Tutorials & Onboarding** | Step-by-step interactive onboarding for new users; contextual tutorial overlays for major features; plugin authors guide. |
| #67 | **Global Backup & Config Versioning** | Version-controlled snapshots of all IDE configurations, plugin manifests and workspace templates; rollback support. |
| #69 | **Dependency Monitoring & Plugin Compatibility** | Detect plugin dependency conflicts, SDK version mismatches, and incompatible combinations; alerts before install/update. |
| #71 | **CI Integration for Plugins & Scripts** | Trigger automated plugin test suites and script validation from CI pipelines; webhook support; result reporting via Output Panel. |

---

## 🔜 Planned — Distribution & Web Presence

| Feature # | Title | Description |
|-----------|-------|-------------|
| #108 | **Official Website** | Public project website — landing page, feature showcase, screenshots, documentation browser, changelog, download links and plugin registry. |
| #109 | **Installable Package** | Self-contained installer for the IDE — MSI / MSIX / WinGet package; auto-update channel; no .NET SDK required for end users; optional silent install for enterprise. |

---

## 🔜 Planned — Binary Analysis & Reverse Engineering

| Feature # | Title | Description |
|-----------|-------|-------------|
| #110 | **String Extraction Panel** | Dockable panel scanning the active binary for ASCII, Unicode, and custom-encoding strings with minimum-length filter and one-click jump to offset in hex view. |
| #111 | **Cryptographic Hash Inspector** | Compute MD5, SHA-1, SHA-256, SHA-512, CRC-32, and Adler-32 for the full file or any byte-range selection in real time, with copy-to-clipboard and reference-hash comparison. |
| #112 | **Embedded File Carver** | Detect and extract embedded PE files, ZIP archives, PNG images, and other magic-byte signatures from within a binary; carved blobs open as virtual child documents. |
| #113 | **YARA Rule Engine** | Author, compile, and run YARA rules directly in the IDE; matches highlighted in hex view and listed in a results panel with offset, length, and rule name. |
| #114 | **PE Import / Export Table Analyzer** | Parse PE32/PE64 IAT and Export Directory; display resolved DLL and function names side-by-side with raw offsets; flag suspicious ordinal-only imports. |
| #115 | **Binary Patch Recorder** | Record every byte modification as a named, annotated patch; export as binary diff, IDA script, or human-readable `.patch` file for reproducible patching. |
| #116 | **Obfuscation & Packing Detector** | Heuristic analysis of section entropy, suspicious imports, and known packer watermarks (UPX, Themida, MPRESS); produces a risk score and actionable report. |
| #117 | **Memory Snapshot Viewer** | Load Windows mini-dump (`.dmp`) or Linux core-dump files; display memory regions, thread stacks, loaded modules, and heap blocks in the hex view with structured overlays. |
| #139 | Grammar File Support | Add support for loading and parsing .syn grammar files from Synalysis; enable syntax highlighting, structured parsing, and integration with memory/code analysis workflows. |

---

## 🔜 Planned — Security & Forensics

| Feature # | Title | Description |
|-----------|-------|-------------|
| #118 | **File Signature Database** | Integrated, user-extensible magic-byte catalog that auto-identifies 500+ file types on open; shows detected format, MIME type, and confidence in the status bar. |
| #119 | **Byte Frequency & Bigram Heatmap** | 256×256 bigram dot-plot and 256-bucket byte-frequency histogram for any selection or full file — visually distinguishes encrypted, compressed, text, and binary payloads. |
| #120 | **XOR / ROT Cipher Decoder** | Brute-force single-byte XOR keys and ROT offsets over a selection; score candidates by ASCII printability; display top results with one-click "apply to selection". |
| #121 | **Audit Log & Forensic Session Journal** | Record every user action (open, edit, navigate, export) with timestamps and byte-range context into a tamper-evident journal, exportable as signed HTML or JSON report. |
| #122 | **Certificate & ASN.1 Inspector** | Parse DER/PEM X.509 certificates, PKCS#7/12 containers, and raw ASN.1 structures embedded in any binary; tree-view with field names, OIDs, validity dates, and key parameters. |
| #123 | **Vulnerability Pattern Scanner** | Curated byte-pattern rules (stack cookies, heap metadata, safe SEH markers) that flag potential vulnerability indicators in native binaries directly in the hex view. |

---

## 🔜 Planned — Developer Productivity

| Feature # | Title | Description |
|-----------|-------|-------------|
| #124 | **Hex Bookmarks & Named Regions** | Persistent named bookmarks at absolute or relative offsets with groups, colors, and comments; importable/exportable as JSON; visible in a dedicated Bookmarks Panel. |
| #125 | **Regex Search over Hex & ASCII** | Full regex search across hex byte patterns (e.g. `\xDE\xAD.{2}\xBE\xEF`) and decoded ASCII simultaneously, with match highlighting, results list, and replace support. |
| #126 | **Byte-Range Calculator** | Inline calculator panel for arithmetic, bitwise, and shift operations on raw byte values; accepts hex/decimal/binary input; shows two's-complement and IEEE-754 float interpretations. |
| #127 | **Column / Block Selection** | Rectangular block selection across hex and ASCII panes; paste, fill, or export selected columns as independent byte arrays — essential for fixed-width record formats. |
| #128 | **Binary Template Marketplace** | Community-driven repository of binary template definitions; browse, search, install, and auto-update format templates (PE, ELF, ZIP, PNG, MP4, etc.) from within the IDE. |
| #129 | **Multi-File Hex Session Tabs** | True independent hex editor sessions per tab with separate cursors, selections, undo stacks, and bookmarks — enabling parallel analysis of multiple binaries. |
| #130 | **Changeset Review Panel** | Diff-style view of all pending unsaved byte modifications across the file: offset, original value, new value, age, and per-change accept/reject controls before save. |

---

## 🔜 Planned — IDE Infrastructure

| Feature # | Title | Description |
|-----------|-------|-------------|
| #131 | **Plugin Dependency Graph Panel** | Interactive directed-graph visualization of all loaded plugins, their service dependencies, and SDK version constraints; detects cycles and orphaned dependencies. |
| #132 | **Theme Designer & Live Preview** | In-IDE XAML brush editor to create, fork, and export custom themes; changes applied live to all dockable panels; themes packaged as `.whtheme` files. |
| #133 | **Command Palette (Ctrl+Shift+P)** | ✅ Done v0.6.3.6 — VS Code-style fuzzy-search palette, 9 modes (`>`/`@`/`:`/`#`/`%`/`?`/Tab), frequency boost, context boost, file search, content grep. |
| #134 | **Extension Point Debugger** | Developer panel listing every SDK extension point (menus, toolbar, statusbar, eventbus) registered by each plugin with live invocation counts and last-call stack. |
| #135 | **Workspace File Watcher** | Monitor all open project files for external changes (from other processes or Git operations); prompt to reload, diff, or merge changes without closing the editor tab. |
| #138 | **In-IDE Plugin Development** | Develop, build, hot-reload, and live-test SDK plugins directly inside the IDE — `PluginProjectTemplate` scaffolding, MSBuild integration (#103), collectible `AssemblyLoadContext` hot-reload, lightweight sandbox, full SDK SmartComplete, Plugin Dev Log panel, and `.whxplugin` packaging; no external toolchain required. |

---

## 🔜 Planned — Network & Protocol Analysis

| Feature # | Title | Description |
|-----------|-------|-------------|
| #136 | **PCAP / Network Capture Viewer** | Load `.pcap` and `.pcapng` files; display packet list, layer breakdown (Ethernet/IP/TCP/UDP/TLS), and raw payload bytes in the hex view. |
| #137 | **Protocol Dissector Plugin API** | Plugin contract allowing third parties to register custom protocol dissectors; dissected fields appear in the ParsedFields panel and hex view overlays. |

---

## 🔜 Planned — IDE Infrastructure Phase 2

| Feature # | Title | Description |
|-----------|-------|-------------|
| #179 | **Quick File Open (Ctrl+P)** | VS Code-style fuzzy file picker across entire solution; recent files first; preview on hover; open at line (`filename:42`); filter by extension; excludes `.gitignore` patterns. |
| #180 | **Terminal Phase 2** | Full ANSI / Xterm-compatible emulation (256-color, SGR codes, mouse events); terminal split panes (horizontal/vertical); SSH / WSL session integration; persistent command history across restarts; REPL hosting (Python/Node/Ruby); macro recording & playback. |
| #181 | **Build System Phase 2** | Parallel project builds (MSBuild `-m` flag); build cancel button (kills MSBuild process); per-project build duration profiling in Output panel; pre/post-build event steps UI; distributed build cache (optional); cross-platform build (dotnet CLI fallback). |
| #182 | **Docking Phase 2** | Persistent floating window positions (saved on close, restored on next session); keyboard panel switcher (`Ctrl+Tab`-like cycle across all panels); multi-item tabbed float windows (group panels in a single float). |
| #183 | **EventBus Phase 2** | Async event delivery (non-blocking handlers via `Task`); per-event filtering / routing rules; cross-process plugin events via IPC; plugin-publishable custom events via SDK; event delivery metrics (latency, queue depth) in Monitoring panel. |
| #184 | **Project System Phase 2** | Lazy project loading (defer heavy projects until first open); solution-wide find & replace (`Ctrl+Shift+H`); project dependency graph view; `.gitignore` support in folder loader; bulk rename/move items; nested project groups (>1 level). |
| #185 | **Settings Phase 2** | Per-workspace settings override (`.whsettings` in project root); named settings profiles (switch between Debug/Release/Presentation modes); settings import/export UI; settings keyword search; keyboard shortcut conflict detection + resolution dialog. |

---

## 🔜 Planned — Plugins Phase 2

| Feature # | Title | Description |
|-----------|-------|-------------|
| #186 | **AssemblyExplorer Phase 2** | Full ILSpy backend wiring (replaces Skeleton fallback) ✅ partial; **ECMA-335 token→offset** (`PeOffsetResolver` ✅); **hex sync** (`AssemblyHexSyncService` ✅). Remaining: PDB symbol + source-link matching UI, plugin panel improvements. |
| #187 | **ArchiveStructure Phase 2** | In-place archive editing (add/remove/rename entries without re-pack); multi-format packer (TAR/GZ/XZ/BR); compression ratio analysis per entry; drag-and-drop files into archive panel. |
| #188 | **FileComparison Phase 2** | Diff algorithm choice (LCS / Myers / Histogram — selectable in toolbar); binary patch export (`.bdiff` / `xdelta` format); unified diff view mode; 3-way merge (base + mine + theirs). |
| #189 | **DiagnosticTools Phase 2** | Memory leak heuristic (detect growing gen2 objects across GC cycles); GC pressure analyzer (alert on LOH allocations); thread deadlock detector (wait graph); flame graph for CPU samples. |

---

## 🔜 Planned — LSP & Language Intelligence Phase 3

| Feature # | Title | Description |
|-----------|-------|-------------|
| #190 | **LSP Phase 3** | **Call hierarchy** (`LspCallHierarchyProvider` + `CallHierarchyPanel` ✅ Shift+Alt+H); **linked editing** (`LspLinkedEditingProvider` ✅). Remaining: inline value hints (debug scope expression eval), LSP 3.18 pull-diagnostics (`textDocument/diagnostic`), type hierarchy (`Ctrl+F12`). |
| #191 | **Core Source Analysis (Roslyn)** | C# incremental Roslyn parser replacing regex-based `SourceOutlineEngine`; VB.NET / F# parser support; cross-language symbol resolution; semantic model for LSP providers; `#if` / `#pragma` conditional compilation region handling. |
| #192 | **F# Full Language Support** | `FSharp.whfmt` syntax definition (keywords, computation expressions `async { }` / `seq { }` / `task { }`, active patterns, discriminated union arms, F# operators `\|>` / `>>` / `<-`); `LanguageRegistry` registration for `.fs` / `.fsx` / `.fsi`; `LspServerRegistry` auto-detect `fsautocomplete` (Ionide) with PATH check + install hint; Assembly Explorer F# decompilation language mapping; `DocumentManager` extension map update. See **DevPlan #9**. |
| #193 | **VB.NET Full Language Support** | `VBNet.whfmt` syntax definition (VB.NET keywords, `End Class` / `End Sub` / `End Function` block detection, `#Region` / `#End Region` named folding, `'` line comments, `'''` XML-doc comments, `&H` / `&B` / `&O` number literals, attribute `<…>` patterns); `LanguageRegistry` registration for `.vb`; OmniSharp `vbnet` language ID registration; Assembly Explorer VB.NET language mapping. See **DevPlan #9**. |

---

## ✅ Recently Shipped

| Feature | Version / Release |
|---------|-------------------|
| **AI Assistant Plugin** — 5 providers (Anthropic/OpenAI/Gemini/Ollama/Claude Code CLI), 25 MCP tools, streaming chat, inline apply, @mentions, command palette (`Ctrl+Shift+A`), conversation history, prompt presets, auto-fallback to CLI, 17 `CA_*` tokens × 18 themes | [0.6.4.3] — 2026-04-02 |
| **Roslyn Integration** — In-process `RoslynLanguageClient` replacing OmniSharp for C#/VB.NET; non-blocking load + LSP status bar follows active document | [0.6.4.3] — 2026-04-02 |
| **LSP Client Engine** — full JSON-RPC LSP client; 10 providers (completion, hover, sig-help, code actions, rename, inlay hints, code lens, semantic tokens, breadcrumb bar, workspace symbols); `LspDocumentBridgeService`; `LspStatusBarAdapter`; 30 tokens × 18 themes | [0.6.3.6] — 2026-03-23 |
| **Command Palette** — `Ctrl+Shift+P`, 9 search modes (`>`/`@`/`:`/`#`/`%`/`?`/Tab), fuzzy scoring, frequency boost, context boost, `CommandPaletteSettings` 14 props, `CommandPaletteOptionsPage` | [0.6.3.6] — 2026-03-23 |
| **Command System Central** — `WpfHexEditor.Commands` project; `CommandRegistry` (~45 commands); `KeyBindingService`; `KeyboardShortcutsPage`; TitleBar launcher; `TB_Search*` tokens × 18 themes | [0.6.3.6] — 2026-03-23 |
| **Diagnostic Tools Plugin** — `ProcessMonitor` (CPU/mem 500 ms), `EventCounterReader` (EventPipe), heap snapshot, 4-tab panel, `CpuGraphControl`/`MemoryGraphControl`; 8 `DT_*` tokens × 18 themes | [0.6.3.6] — 2026-03-23 |
| **Document Model Phase 1** — `IDocumentBuffer`/`DocumentBuffer`; `IBufferAwareEditor`; `LspBufferBridge`; `LspDocumentBridgeService`; wired for CodeEditor, TextEditor, MarkdownEditorHost, XamlDesignerSplitHost | [0.6.3.6] — 2026-03-23 |
| **IDE EventBus 100% coverage** — 39 typed events (Phase 1–3); `ProcessLaunchedEvent`/`ProcessExitedEvent`; `ParseCompletedEvent`; `CodeEditorCursorMovedEvent`/`FoldingChangedEvent`/`SelectionChangedEvent`; `BuildProgressUpdatedEvent` | [0.6.3.6] — 2026-03-23 |
| **Incremental Build** — `IIncrementalBuildTracker`/`IncrementalBuildTracker` (FSW per project); `BuildDirtyAsync()`; `Ctrl+Alt+F7`; orange dirty dot in Solution Explorer | [0.6.3.6] — 2026-03-23 |
| **Code Editor Gutter Diagnostics + Multi-caret** — `CE_GutterError`/`CE_GutterWarning` tokens; scrollbar markers; `Ctrl+Alt+Click` multi-caret; `Ctrl+D` SelectNextOccurrence; secondary carets at 60% opacity | [0.6.3.6] — 2026-03-23 |
| **Dockable Search Panel** — `Ctrl+Shift+F`; `ISearchable`/`ISearchPanel` contracts; `RegexSearchEngine`; `SearchPanelViewModel`; `HexEditor`/`CodeEditor`/`TblEditor` integration; 14 `SP_*` tokens × 18 themes | [0.6.3.6] — 2026-03-23 |
| **DI Infrastructure** — `AppServiceCollection` (MEF → `Microsoft.Extensions.DependencyInjection 8.0`); `MainWindowServiceArgs` record; `BuildServiceAdapters()` | [0.6.3.6] — 2026-03-23 |
| **Plugin Sandbox Signing** — `ValidateSignature()`; `[SECURITY]` prefix on mismatch; `SIGNED`/`unsigned` per-plugin log (ADR-SB-01) | [0.6.3.6] — 2026-03-23 |
| **Class Diagram Overkill (10 phases)** — DSL v2, Force-Directed + Hierarchical + Swimlane layouts, canvas adorners, A* orthogonal + Bézier routing, Roslyn incremental analysis, PlantUML/XMI/Interactive SVG export, minimap, spatial index, live sync; 46 `CD_*` tokens | [0.6.3.6] — 2026-03-23 |
| **XAML Designer Phase 3** — `ConstraintAdorner`, `ResponsiveBreakpointBar`, `GradientEditorAdorner`, `BindingPathPickerPopup`, `PerformanceOverlayAdorner` (`Ctrl+Shift+F9`); 9 panels migrated to plugin project; 40 `XD_*` tokens | [0.6.3.6] — 2026-03-23 |
| **Markdown Editor Full IDE Integration** — `MD.whfmt` routing, lazy mermaid.js, image paste, outline panel, context menus (`MD_*` tokens), adaptive debounce, off-thread word count | [0.6.3.6] — 2026-03-23 |
| **Code Editor Word Wrap** — `IsWordWrapEnabled` toggle; prefix-sum wrap map; `RenderTextContentWrapped` / `RenderSelectionWrapped`; H-scrollbar auto-hides | [0.6.3.6] — 2026-03-23 |
| **Docking Sprint 3** — M2.1 incremental visual tree; M2.4 WeakEvent subscriptions; M3.3 undo/redo layout (`Ctrl+Shift+Z/Y`) | [0.6.3.6] — 2026-03-23 |
| **Shell Split** — `WpfHexEditor.Docking.Wpf` extracted from `WpfHexEditor.Shell`; Shell is now theme-XAML-only; pack URIs updated (ADR-055) | [0.6.3.6] — 2026-03-23 |
| **Theme Persistence + Default Layout** — `ApplyTheme()` persists `ActiveThemeName`; `LoadDefaultLayoutFromResource()` reads embedded `defaultLayout.json` (ADR-052) | [0.6.3.6] — 2026-03-23 |
| **Visual XAML Designer** — live WPF canvas, bidirectional canvas↔code sync (~95%), move/resize/rotate handles, property inspector (F4), multi-select + alignment guides, snap grid, `#region` colorization, error card overlay, 4 split layouts (`Ctrl+Shift+L`), zoom/pan, `DesignHistoryPanel`, Toolbox/ResourceBrowser/DesignData/Animation panels | [0.6.0] — 2026-03-18 |
| **Shared `UndoEngine`** — `WpfHexEditor.Editor.Core.Undo`; coalescing (500 ms window), transactions (`BeginTransaction`/`CommitTransaction`), save-point tracking (`MarkSaved`/`IsAtSavePoint`), `Ctrl+Shift+Z` redo, dynamic "Undo (N)" headers; replaces both editors' custom stacks | [0.6.0] — 2026-03-18 |
| **Rectangular Selection + Drag-and-Drop** — Alt+Click block selection, text drag-to-move, rect block drag-to-move; single-rect rendering (no row seams); `IUndoEntry` migration for TextEditor | [0.6.0] — 2026-03-18 |
| **NuGet Solution Manager** — solution-level panel (Browse/Installed/Consolidate/Updates); right-click solution node; `doc-nuget-solution-` content ID | [0.6.0] — 2026-03-18 |
| **Inline Hints per-language gates + 29 new `.whlang` definitions** — 55+ total embedded language definitions; per-language Ctrl+Click and Inline Hints activation | [0.6.0] — 2026-03-18 |
| **Shell rename** — `WpfHexEditor.Docking.Wpf` → `WpfHexEditor.Shell`; namespace, assembly name and Pack URI updated | [0.6.0] — 2026-03-18 |
| **Ctrl+Click Navigation** — workspace cross-file declaration scan, multi-location `ReferencesPopup`, OmniSharp `MetadataUri` passthrough, external symbol decompilation via `AssemblyAnalysisEngine` + `CSharpSkeletonEmitter` → read-only Code Editor tab | [0.5.8] — 2026-03-17 |
| **Search highlight fix** — `ComputeVisualX` for tab-aware X alignment + `_lineYLookup` for Inline Hints-aware Y in `RenderFindResults` | [0.5.8] — 2026-03-17 |
| **Code Editor Navigation Bar** — VS-like types/members combos, Segoe MDL2 icons, `CaretMoved` event, auto-scroll to declaration | [0.5.2] — 2026-03-16 |
| **Assembly Explorer Expansion** — ILSpy backend, VB.NET, CFG Canvas, Assembly Diff, Assembly Search, XRef View, Decompile Cache, options page | [0.5.2] — 2026-03-16 |
| **NuGet support** — `NuGetV3Client`, `CsprojPackageWriter`, `NuGetPackageViewModel` in ProjectSystem | [0.5.2] — 2026-03-16 |
| **Workspace Templates** — `TemplateManager`, `ProjectScaffolder`, 3 built-in JSON templates, `NewProjectDialog` | [0.5.2] — 2026-03-16 |
| **Build system refactoring** — `MSBuildAdapter` error surfacing, async fix, Locator DLL copy; `MSBuildLogger`/`NuGetRestoreStep` removed | [0.5.2] — 2026-03-16 |
| **Themes: `CE_SelectionInactive`** brush token — Code Editor inactive selection, all 8 themes | [0.5.2] — 2026-03-16 |
| **Source Outline Navigation** — lazy type/member tree in Solution Explorer for `.cs`/`.xaml` files; `SourceOutlineEngine` BCL-only parser; `LoadingNode` placeholder + async expand | [0.5.0] — 2026-03-16 |
| **Assembly Explorer v0.2.1** — Decompile to C# → Code Editor tab with syntax highlighting; Extract to Project (`AssemblyCodeExtractService` + `ProjectPickerDialog`); Collapse All; Close All Assemblies | [0.5.0] — 2026-03-16 |
| **Code Editor — full syntax highlighting** — all 26 `.whlang` definitions; live `TryFindResource` brush resolution; foreground base pass; hover-only URL underline with tooltip; split host + document model cleanup | [0.5.0] — 2026-03-16 |
| **Build Output Channel** — `OutputServiceImpl.Write("Build",…)` now routes to Build channel; auto-focus via `OutputLogger.FocusChannel`; severity coloring (info/warn/error/success); empty-solution guard | [0.5.0] — 2026-03-16 |
| **VS Solution Loader** — `VsSolutionLoaderPlugin` with dynamic file-dialog filter + external solution routing; 4 project templates (Console, ClassLib, WPF, AspNet API) | [0.5.0] — 2026-03-16 |
| **LSP Infrastructure** — `WpfHexEditor.LSP`: `RefactoringEngine`, `RenameRefactoring`, `ExtractMethodRefactoring`, `CodeFormatter`, `SymbolTableManager`, `CommandIntegration` mediator (#85–86 foundation) | [0.5.0] — 2026-03-16 |
| **Docking: Tab Overflow Panel** — `TabOverflowButton` + `TabOverflowPanel` improvements; correct positioning and theme-aware context menu | [0.5.0] — 2026-03-16 |
| **IDE EventBus** — `WpfHexEditor.Events`, `IIDEEventBus`, 10 typed events, IPC bridge for sandbox plugins, options page (#80) | [0.4.0] — 2026-03-16 |
| **Plugin Lazy Loading** — `PluginActivationConfig`, `PluginActivationService`, `Dormant` state, file-extension/command triggers (#77 partial) | [0.4.0] — 2026-03-16 |
| **Capability Registry** — `IPluginCapabilityRegistry`, `PluginFeature` constants, manifest `features` field | [0.4.0] — 2026-03-16 |
| **Extension Points** — `IFileAnalyzerExtension`, `IHexViewOverlayExtension`, `IBinaryParserExtension`, `IDecompilerExtension`, `ExtensionPointCatalog` | [0.4.0] — 2026-03-16 |
| **Plugin Dependency Graph** — versioned constraints, topological ordering, cascading unload/reload | [0.4.0] — 2026-03-16 |
| **ALC Isolation Diagnostics** — assembly count, conflict detection, weak-reference GC verification | [0.4.0] — 2026-03-16 |
| **Plugin Sandbox — HWND embedding + IPC menu/toolbar/event bridges** (#81 Phase 9-12) | [0.3.0] — 2026-03-15 |
| **Auto-isolation decision engine + PluginMigrationPolicy** (#81) | [0.3.0] — 2026-03-15 |
| **SandboxJobObject** — per-plugin CPU/RAM resource limits (#81) | [0.3.0] — 2026-03-15 |
| Multi-tab terminal sessions + macro recording (#92) | [0.3.0] — 2026-03-15 |
| Assembly Explorer stub — PEReader pipeline, IDE menu, statusbar (#104 Phase 1) | [0.3.0] — 2026-03-15 |
| Plugin system (SDK, PluginHost, 9 first-party plugins, monitoring) | [0.3.0] — 2026-03-15 |
| VS-style docking engine (100% in-house) | [2.7.0] — 2026-02 |
| Project system (`.whsln` / `.whproj`) | [2.7.0] — 2026-02 |
| Insert Mode fix, save reliability, unlimited undo/redo | [2.5.0] — 2026-02 |

> Full history → [CHANGELOG.md](CHANGELOG.md)
