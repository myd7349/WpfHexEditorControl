# WpfHexEditor.App

**Type:** Executable (`net8.0-windows`)
**Role:** Main IDE host — entry point and orchestration shell for the entire WpfHexEditor IDE.

---

## Responsibility

`WpfHexEditor.App` is the root startup project. It:

- Bootstraps the WPF application and loads the global theme
- Creates and wires the docking engine, editor registry, and all built-in panels
- Discovers and loads plugins via `WpfPluginHost`
- Bridges every subsystem (build, terminal, solution explorer, output, errors) to a shared `IDEHostContext` used by plugins
- Manages the document lifecycle (open, close, dirty state, save, reload)
- Exposes the build system UI (ConfigurationManager, startup project, build output/error adapters)

---

## Architecture

### Partial Class Decomposition

`MainWindow` is split into 5 files, each owning a distinct concern:

| File | Concern |
|------|---------|
| `MainWindow.xaml.cs` | UI bootstrap, docking engine, editor lifecycle, keyboard shortcuts |
| `MainWindow.DocumentModel.cs` | DocumentManager, title/dirty propagation, auto-serialize timer |
| `MainWindow.Build.cs` | Build system wiring, configurations, startup project, build commands |
| `MainWindow.FileChangeBar.cs` | External file-change detection and reload pipeline |
| `MainWindow.PluginSystem.cs` | Plugin discovery/load, service adapter wiring, IDE EventBus, Plugin Manager tab |

### Service Adapter Pattern

Every IDE subsystem is exposed to plugins through a typed adapter implementing an SDK interface. Plugins never reference `MainWindow` directly.

| Adapter | SDK Interface | Subsystem |
|---------|--------------|-----------|
| `DockingAdapter` | `IDockingAdapter` | Docking engine — panels/tabs |
| `MenuAdapter` | `IMenuAdapter` | Main menu contributions |
| `StatusBarAdapter` | `IStatusBarAdapter` | Status bar items |
| `HexEditorServiceImpl` | `IHexEditorService` | Active hex editor proxy |
| `DocumentHostService` | `IDocumentHostService` | Open file / navigate to line |
| `OutputServiceImpl` | `IOutputService` | Output panel channels |
| `ErrorPanelServiceImpl` | `IErrorPanelService` | Diagnostics / error list |
| `ThemeServiceImpl` | `IThemeService` | Theme switching |
| `TerminalServiceImpl` | `ITerminalService` | Terminal sessions |
| `SolutionExplorerServiceImpl` | `ISolutionExplorerService` | Solution tree navigation |

### Null / Stub Services

`NullCodeEditorService` and `NullParsedFieldService` are no-op implementations returned when no relevant editor is active, preventing null-reference errors in plugin code.

---

## File Structure

```
WpfHexEditor.App/
├── App.xaml / App.xaml.cs                  — WPF entry point, CLI arg parsing, theme init
├── MainWindow.xaml / .cs                   — IDE shell layout + partial orchestration
├── MainWindow.DocumentModel.cs             — Document lifecycle
├── MainWindow.Build.cs                     — Build system
├── MainWindow.FileChangeBar.cs             — File monitor
├── MainWindow.PluginSystem.cs              — Plugin system
├── OutputLogger.cs                         — Static logging facade → OutputPanel
│
├── Build/
│   ├── BuildErrorListAdapter.cs            — Routes build diagnostics → ErrorPanel
│   ├── BuildOutputAdapter.cs               — Routes build output → OutputPanel
│   ├── BuildStatusBarAdapter.cs            — Updates status bar during builds
│   └── ConfigurationManagerDialog.xaml/.cs — Add/edit build configurations
│
├── Controls/
│   ├── DocumentTabHeader.xaml/.cs          — Tab header with dirty indicator (●)
│   ├── DocumentInfoBar.xaml/.cs            — Orange reload/conflict warning bar
│   ├── OutputPanel.xaml/.cs                — Multi-channel log UI
│   ├── WelcomePanel.xaml/.cs               — VS Start Page with recent files
│   ├── PluginQuickStatusPopup.xaml/.cs     — Plugin load/unload toast
│   ├── EditorToolbarItemTemplateSelector   — Dynamic editor toolbar DataTemplate selector
│   └── TblItemTemplateSelector             — TBL dropdown DataTemplate selector
│
├── Dialogs/
│   ├── GoToOffsetDialog.xaml/.cs           — Ctrl+G jump to byte offset
│   ├── SaveChangesDialog.xaml/.cs          — Save/Discard/Cancel on close
│   ├── PasteConflictDialog.xaml/.cs        — Paste size conflict resolver
│   ├── ImportEmbeddedFormatDialog.xaml/.cs — Import .whfmt format definitions
│   ├── ImportEmbeddedSyntaxDialog.xaml/.cs — Import .whsyntax syntax definitions
│   ├── SolutionPropertyPagesDialog.xaml/.cs — Multi-page solution properties
│   └── SolutionPropertyPages/
│       ├── BuildDependenciesPage.cs        — Project build dependency order
│       ├── ConfigurationPropertiesPage.cs  — Per-configuration build settings
│       ├── SourceFilesPage.cs              — Included/excluded source files
│       └── StartupProjectsPage.cs          — F5 startup project selection
│
├── Models/
│   └── TblSelectionItem.cs                 — TBL selection dropdown VM
│
├── Services/                               — All SDK adapter implementations (see above)
│
├── Themes/
│   └── DialogStyles.xaml                   — Dialog button styles + orange InfoBar styles
│
└── ViewModels/
    └── PluginQuickStatusViewModel.cs       — Plugin toast notification state
```

---

## Startup Flow

```
App.OnStartup()
  └─ Parse --open <path> or bare file association arg
  └─ Load global theme (WpfHexEditor.Shell Dark theme)
  └─ Create MainWindow

MainWindow.OnLoaded()
  ├─ Restore docking layout from %AppData%\WpfHexEditor\layout.json
  ├─ Create all singleton panels (SolutionExplorer, Output, Errors, Terminal, …)
  ├─ InitDocumentManager() — subscribe to title/dirty events
  ├─ InitializePluginSystemAsync()
  │    ├─ Build all service adapters
  │    ├─ Assemble IDEHostContext
  │    ├─ DiscoverPluginsAsync() — scan Plugins folder
  │    ├─ LoadAllAsync() — init each plugin in priority order
  │    ├─ RestoreSession() or open startup file
  │    └─ Fire IDEInitializedEvent
  └─ Start auto-serialize timer (Tracked document mode)
```

---

## Document Lifecycle

```
Open file
  └─ Determine editor type (HexEditor / CodeEditor / TextEditor / …)
  └─ Create editor control + DockItem (ContentId = "doc-{uuid}")
  └─ DocumentManager.Register(contentId, editor)

Tab activated
  └─ Update _connectedHexEditor
  └─ Notify StatusBar, PropertyPanel, FocusContextService
  └─ SyncActiveDocument(contentId)

Close / shutdown
  └─ CheckDirtyDocuments() → SaveChangesDialog if unsaved
  └─ ShutdownThenCloseAsync()
        ├─ AutoSaveLayout()
        ├─ PluginHost.UnloadAll()
        └─ Application.Shutdown()
```

---

## Build System Integration

```
Solution loaded
  └─ InitializeBuildSystemAsync()
        ├─ BuildSystem + ConfigurationManager
        ├─ BuildOutputAdapter → OutputPanel (Build channel)
        ├─ BuildErrorListAdapter → ErrorPanel
        ├─ BuildStatusBarAdapter → StatusBar
        └─ StartupProjectRunner

Build command (F6 / Ctrl+Shift+B)
  └─ ClearDiagnostics()
  └─ BuildSystem.BuildSolutionAsync()
        └─ publishes BuildStarted/OutputLine/Progress/Succeeded/Failed events
```

---

## Well-Known Content IDs

| Content ID | Panel / Document |
|-----------|-----------------|
| `panel-solution-explorer` | Solution Explorer |
| `panel-errors` | Error List |
| `panel-terminal` | Integrated Terminal |
| `plugin-manager` | Plugin Manager document tab |
| `doc-{uuid}` | Any open document |
| `doc-projprops-{name}` | Solution/project property pages |
| `doc-nuget-solution-{name}` | Solution-level NuGet manager |
| `doc-nuget-{name}` | Project-level NuGet manager |

---

## Theme & Style

- Global theme loaded from `WpfHexEditor.Shell` (Dark by default; switchable at runtime)
- Key brush tokens: `DockWindowBackgroundBrush`, `DockMenuBackgroundBrush`, `DockAccentBrush`, `DockTabActiveBrush`
- Custom styles in `Themes/DialogStyles.xaml`: `InfoBarButtonStyle` (flat buttons on orange banner), `TitleBarButtonStyle`

---

## Key Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Shell` | Docking engine + 8 themes |
| `WpfHexEditor.Editor.Core` | `IDocumentEditor`, `DocumentManager` |
| `WpfHexEditor.PluginHost` | Plugin discovery + loading |
| `WpfHexEditor.BuildSystem` | Build orchestration engine |
| `WpfHexEditor.ProjectSystem` | Solution / project model |
| `WpfHexEditor.Panels.IDE` | Solution Explorer, Properties panels |
| `WpfHexEditor.Terminal` | Integrated terminal |
| All 14 Editor modules | Pluggable editor controls |

---

## Design Patterns Used

| Pattern | Where |
|---------|-------|
| **Adapter** | All service adapters (DockingAdapter, MenuAdapter, etc.) |
| **Partial class** | MainWindow split across 5 domain files |
| **Facade** | OutputLogger, DocumentHostService |
| **Observer** | DocumentManager events → MainWindow handlers |
| **Null Object** | NullCodeEditorService, NullParsedFieldService |
| **Template Selector** | EditorToolbarItemTemplateSelector, TblItemTemplateSelector |
| **Singleton** | All built-in panels, OutputPanel, _pluginHost |
