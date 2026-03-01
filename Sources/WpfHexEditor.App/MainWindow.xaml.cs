//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5, Claude Sonnet 4.6, Claude Opus 4.6
//////////////////////////////////////////////

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using HexEditorControl = WpfHexEditor.HexEditor.HexEditor;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;
using WpfHexEditor.Docking.Core.Serialization;
using WpfHexEditor.Docking.Wpf;
using WpfHexEditor.App.Controls;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.WindowPanels.Panels;

namespace WpfHexEditor.App;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // ─── INotifyPropertyChanged ────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ─── Constants ─────────────────────────────────────────────────────
    private static readonly string LayoutFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexEditor", "Sample.Docking", "layout.json");

    private const string ParsedFieldsPanelContentId = "panel-parsed-fields";

    // ─── Fields ────────────────────────────────────────────────────────
    private DockLayoutRoot _layout = null!;
    private DockEngine _engine = null!;
    private int _documentCounter;
    private bool _isLocked;

    // Content cache: ContentId → created UIElement (enables ParsedFields sync)
    private readonly Dictionary<string, UIElement> _contentCache = new();

    // ParsedFieldsPanel (persistent singleton — not recreated per tab)
    private ParsedFieldsPanel? _parsedFieldsPanel;
    private HexEditorControl? _connectedHexEditor;

    // ─── Bindable properties (XAML menu/statusbar bindings) ─────────────
    private IDocumentEditor? _activeDocumentEditor;
    public IDocumentEditor? ActiveDocumentEditor
    {
        get => _activeDocumentEditor;
        private set
        {
            if (_activeDocumentEditor != null)
            {
                _activeDocumentEditor.TitleChanged    -= OnEditorTitleChanged;
                _activeDocumentEditor.ModifiedChanged -= OnEditorModifiedChanged;
                _activeDocumentEditor.StatusMessage   -= OnEditorStatusMessage;
            }
            _activeDocumentEditor = value;
            if (_activeDocumentEditor != null)
            {
                _activeDocumentEditor.TitleChanged    += OnEditorTitleChanged;
                _activeDocumentEditor.ModifiedChanged += OnEditorModifiedChanged;
                _activeDocumentEditor.StatusMessage   += OnEditorStatusMessage;
            }
            OnPropertyChanged();
        }
    }

    private HexEditorControl? _activeHexEditor;
    public HexEditorControl? ActiveHexEditor
    {
        get => _activeHexEditor;
        private set { _activeHexEditor = value; OnPropertyChanged(); }
    }

    // ─── Constructor ───────────────────────────────────────────────────
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadSavedLayoutOrDefault();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        AutoSaveLayout();
    }

    // ─── Layout persistence ────────────────────────────────────────────

    private void LoadSavedLayoutOrDefault()
    {
        if (File.Exists(LayoutFilePath))
        {
            try
            {
                var layout = DockLayoutSerializer.Deserialize(File.ReadAllText(LayoutFilePath));
                RestoreWindowState(layout);
                ApplyLayout(layout);
                EnsureParsedFieldsPanel();
                OutputLogger.Info($"Layout restored from: {LayoutFilePath}");
                return;
            }
            catch (Exception ex)
            {
                OutputLogger.Error($"Failed to restore layout: {ex.Message}");
            }
        }

        OutputLogger.Info("No saved layout found, using defaults.");
        SetupDefaultLayout();
    }

    /// <summary>
    /// Restores the main window position, size, and state from the saved layout.
    /// Must be called BEFORE <see cref="ApplyLayout"/> so that panel pixel sizes
    /// resolve against the correct window dimensions.
    /// </summary>
    private void RestoreWindowState(DockLayoutRoot layout)
    {
        if (layout.WindowWidth is > 0 && layout.WindowHeight is > 0)
        {
            Left = layout.WindowLeft ?? Left;
            Top = layout.WindowTop ?? Top;
            Width = layout.WindowWidth.Value;
            Height = layout.WindowHeight.Value;
        }

        if (layout.WindowState is 2) // Maximized
            WindowState = WindowState.Maximized;
    }

    private void AutoSaveLayout()
    {
        try
        {
            DockHost.SyncLayoutSizes();

            // Persist window state and restore bounds so the window reopens
            // in the same position/size/state (including maximized).
            _layout.WindowState = (int)WindowState;
            var rb = RestoreBounds; // WPF provides normal-state bounds even when maximized
            if (rb != Rect.Empty)
            {
                _layout.WindowLeft = rb.Left;
                _layout.WindowTop = rb.Top;
                _layout.WindowWidth = rb.Width;
                _layout.WindowHeight = rb.Height;
            }
            else
            {
                _layout.WindowLeft = Left;
                _layout.WindowTop = Top;
                _layout.WindowWidth = Width;
                _layout.WindowHeight = Height;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LayoutFilePath)!);
            File.WriteAllText(LayoutFilePath, DockLayoutSerializer.Serialize(_layout));
            OutputLogger.Info($"Layout auto-saved to: {LayoutFilePath}");
        }
        catch (Exception ex)
        {
            OutputLogger.Error($"Failed to auto-save layout: {ex.Message}");
        }
    }

    // ─── Layout helpers ────────────────────────────────────────────────

    private void SetupDefaultLayout()
    {
        // Ensure ParsedFieldsPanel singleton is created before any HexEditor content
        _parsedFieldsPanel ??= new ParsedFieldsPanel();

        var layout = new DockLayoutRoot();
        var engine = new DockEngine(layout);

        layout.MainDocumentHost.AddItem(new DockItem { Title = "Welcome", ContentId = "doc-welcome" });

        var solutionExplorer = new DockItem { Title = "Solution Explorer", ContentId = "panel-solution-explorer" };
        engine.Dock(solutionExplorer, layout.MainDocumentHost, DockDirection.Left);

        var output = new DockItem { Title = "Output", ContentId = "panel-output" };
        engine.Dock(output, layout.MainDocumentHost, DockDirection.Bottom);

        var parsedFields = new DockItem
        {
            Title = "Parsed Fields",
            ContentId = ParsedFieldsPanelContentId,
            CanClose = false
        };
        engine.Dock(parsedFields, layout.MainDocumentHost, DockDirection.Right);

        ApplyLayout(layout, engine);
        OutputLogger.Info("Default layout applied.");
    }

    /// <summary>
    /// Wires a layout to the DockControl. Used by SetupDefaultLayout, LoadSavedLayoutOrDefault
    /// and OnLoadLayout. Manages the single subscription to TabCloseRequested and ActiveItemChanged,
    /// and synchronizes the document counter.
    /// </summary>
    private void ApplyLayout(DockLayoutRoot layout, DockEngine? engine = null)
    {
        // Avoid duplicates if ApplyLayout is called multiple times (Reset, Load…)
        DockHost.TabCloseRequested   -= OnTabCloseRequested;
        DockHost.ActiveItemChanged   -= OnActiveDocumentChanged;

        _layout = layout;
        _engine = engine ?? new DockEngine(_layout);
        _isLocked = false;
        LockMenuItem.IsChecked = false;

        DockHost.ContentFactory = CreateContentForItem;
        DockHost.TabCloseRequested   += OnTabCloseRequested;
        DockHost.ActiveItemChanged   += OnActiveDocumentChanged;
        DockHost.Layout = _layout;

        SyncDocumentCounter();
        UpdateStatusBar();
    }

    /// <summary>
    /// If the ParsedFields panel is missing from a restored layout, dock it programmatically.
    /// Also ensures the panel instance is created eagerly so HexEditors created during
    /// layout restore can connect to it before ActiveItemChanged fires.
    /// </summary>
    private void EnsureParsedFieldsPanel()
    {
        // Create the singleton instance eagerly — needed so CreateHexEditorContent
        // can connect it before the first OpenFile() call.
        _parsedFieldsPanel ??= new ParsedFieldsPanel();

        if (_layout.FindItemByContentId(ParsedFieldsPanelContentId) == null)
        {
            var item = new DockItem
            {
                Title = "Parsed Fields",
                ContentId = ParsedFieldsPanelContentId,
                CanClose = false
            };
            _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Right);
            DockHost.RebuildVisualTree();
            OutputLogger.Info("ParsedFields panel added to restored layout.");
        }
    }

    /// <summary>
    /// Synchronizes <see cref="_documentCounter"/> with existing ContentIds after
    /// a restore, to avoid ContentId collisions when creating new documents.
    /// </summary>
    private void SyncDocumentCounter()
    {
        var allItems = _layout.GetAllGroups()
            .SelectMany(g => g.Items)
            .Concat(_layout.FloatingItems)
            .Concat(_layout.AutoHideItems);

        var max = 0;
        foreach (var item in allItems)
        {
            var id = item.ContentId;
            if (id.StartsWith("doc-hex-") && int.TryParse(id["doc-hex-".Length..], out var n1))
                max = Math.Max(max, n1);
            else if (id.StartsWith("doc-") && int.TryParse(id["doc-".Length..], out var n2))
                max = Math.Max(max, n2);
        }

        _documentCounter = max;
    }

    // ─── Content factory (with cache) ──────────────────────────────────

    private object CreateContentForItem(DockItem item)
    {
        if (_contentCache.TryGetValue(item.ContentId, out var cached))
            return cached;
        var content = BuildContentForItem(item);
        _contentCache[item.ContentId] = content;
        return content;
    }

    private UIElement BuildContentForItem(DockItem item) =>
        item.ContentId switch
        {
            "panel-solution-explorer" => CreateSolutionExplorerContent(),
            "panel-output"            => CreateOutputContent(),
            ParsedFieldsPanelContentId => CreateParsedFieldsContent(),
            "panel-properties"        => CreatePropertiesContent(),
            _ when item.ContentId.StartsWith("doc-hex-") => CreateHexEditorContent(
                item.Metadata.TryGetValue("FilePath", out var fp) ? fp : null),
            _ => CreateDocumentContent(item)
        };

    private static UIElement CreateSolutionExplorerContent()
    {
        var treeView = new TreeView { Background = System.Windows.Media.Brushes.Transparent };
        var root = new TreeViewItem { Header = "Solution 'WpfHexEditor'" };
        root.Items.Add(new TreeViewItem { Header = "WpfHexEditor.Docking.Core" });
        root.Items.Add(new TreeViewItem { Header = "WpfHexEditor.Docking.Wpf" });
        root.Items.Add(new TreeViewItem { Header = "WpfHexEditor.Sample.Docking" });
        root.Items.Add(new TreeViewItem { Header = "WpfHexEditorCore" });
        root.IsExpanded = true;
        treeView.Items.Add(root);
        return treeView;
    }

    private static UIElement CreatePropertiesContent()
    {
        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock { Text = "Properties", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(new TextBlock { Text = "Name:" });
        panel.Children.Add(new TextBox { Text = "DockItem", Margin = new Thickness(0, 2, 0, 8) });
        panel.Children.Add(new TextBlock { Text = "Type:" });
        panel.Children.Add(new TextBox { Text = "Panel", Margin = new Thickness(0, 2, 0, 8) });
        panel.Children.Add(new TextBlock { Text = "CanClose:" });
        panel.Children.Add(new CheckBox { IsChecked = true, Margin = new Thickness(0, 2, 0, 8) });
        return panel;
    }

    private static UIElement CreateOutputContent() => new OutputPanel();

    private UIElement CreateParsedFieldsContent()
    {
        _parsedFieldsPanel ??= new ParsedFieldsPanel();
        return _parsedFieldsPanel;
    }

    private UIElement CreateHexEditorContent(string? filePath)
    {
        var hexEditor = new HexEditorControl();

        // Hide HexEditor's own status bar — the App's status bar handles display
        hexEditor.ShowStatusBar = false;

        // Early-connect: if no HexEditor is connected yet (e.g. on layout restore
        // before ActiveItemChanged fires) connect this one immediately so format
        // detection can populate the ParsedFieldsPanel during OpenFile().
        if (_parsedFieldsPanel != null && _connectedHexEditor == null)
        {
            _connectedHexEditor = hexEditor;
            hexEditor.ConnectParsedFieldsPanel(_parsedFieldsPanel);
            ActiveDocumentEditor = hexEditor as IDocumentEditor;
            ActiveHexEditor = hexEditor;
        }

        // No FilePath metadata → "New Hex Document" with random sample data
        if (filePath is null)
        {
            var data = new byte[1024];
            new Random().NextBytes(data);
            var tempFile = Path.Combine(Path.GetTempPath(), $"hexedit-sample-{Guid.NewGuid():N}.bin");
            File.WriteAllBytes(tempFile, data);
            hexEditor.OpenFile(tempFile);
            OutputLogger.Debug($"New hex document created (temp: {tempFile})");
            return hexEditor;
        }

        // FilePath exists → "Open File" or layout restore
        if (File.Exists(filePath))
        {
            hexEditor.OpenFile(filePath);
            OutputLogger.Info($"Opened: {filePath}");
            return hexEditor;
        }

        // File no longer exists → log error, show placeholder
        OutputLogger.Error($"File not found: {filePath}");
        return new TextBlock
        {
            Text = $"File not found:\n{filePath}",
            Foreground = System.Windows.Media.Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 14
        };
    }

    private static UIElement CreateDocumentContent(DockItem item)
    {
        var textBox = new TextBox
        {
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            AcceptsTab = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Text = $"This is document: {item.Title}\n\nEdit this text...",
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.LightGray,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 13,
            BorderThickness = new Thickness(0)
        };
        return textBox;
    }

    // ─── Active document tracking ───────────────────────────────────────

    private void OnActiveDocumentChanged(DockItem item)
    {
        if (!_contentCache.TryGetValue(item.ContentId, out var content))
            return;

        var hex = content as HexEditorControl;

        // Disconnect previous HexEditor from ParsedFieldsPanel
        if (hex != _connectedHexEditor)
        {
            _connectedHexEditor?.DisconnectParsedFieldsPanel();

            _connectedHexEditor = hex;
            if (_connectedHexEditor != null)
                _connectedHexEditor.ConnectParsedFieldsPanel(_parsedFieldsPanel);
            else
                _parsedFieldsPanel?.Clear();
        }

        // Update IDocumentEditor tracking
        ActiveDocumentEditor = hex as IDocumentEditor;
        ActiveHexEditor = hex;

        // If no IDocumentEditor status available, clear the status text
        if (ActiveDocumentEditor == null)
            StatusText.Text = "Ready";
    }

    // ─── IDocumentEditor event handlers ────────────────────────────────

    private void OnEditorTitleChanged(object? sender, string newTitle)
    {
        // Update the DockItem.Title for the active document's tab
        var contentId = _contentCache
            .FirstOrDefault(kv => ReferenceEquals(kv.Value, sender as UIElement)).Key;
        if (contentId != null)
        {
            var dockItem = _layout.FindItemByContentId(contentId);
            if (dockItem != null)
                dockItem.Title = newTitle;
        }
    }

    private void OnEditorModifiedChanged(object? sender, EventArgs e)
    {
        // Tab title already updated via TitleChanged — nothing extra needed here
    }

    private void OnEditorStatusMessage(object? sender, string message)
    {
        // Route editor status messages to the App's status bar
        StatusText.Text = message;
    }

    // ─── Tab / panel management ────────────────────────────────────────

    private void OnTabCloseRequested(DockItem item)
    {
        if (_isLocked) return;

        // Cleanup before closing
        if (_contentCache.TryGetValue(item.ContentId, out var ctrl))
        {
            if (ctrl is HexEditorControl hex)
            {
                if (ReferenceEquals(hex, _connectedHexEditor))
                {
                    hex.DisconnectParsedFieldsPanel();
                    _connectedHexEditor = null;
                    _parsedFieldsPanel?.Clear();
                }
                // Clear active editor binding if this was the active tab
                if (ReferenceEquals(hex, ActiveHexEditor))
                {
                    ActiveDocumentEditor = null;
                    ActiveHexEditor = null;
                    StatusText.Text = "Ready";
                }
            }
            _contentCache.Remove(item.ContentId);
        }

        try
        {
            _engine.Close(item);
            DockHost.RebuildVisualTree();
            UpdateStatusBar();
            OutputLogger.Info($"Closed tab: {item.Title} ({item.ContentId})");
        }
        catch (InvalidOperationException ex)
        {
            OutputLogger.Warn($"Cannot close '{item.Title}': {ex.Message}");
        }
    }

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            _documentCounter++;
            var item = new DockItem
            {
                Title = System.IO.Path.GetFileName(dialog.FileName),
                ContentId = $"doc-hex-{_documentCounter}",
                Metadata = { ["FilePath"] = dialog.FileName }
            };
            _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Center);
            DockHost.RebuildVisualTree();
            UpdateStatusBar();
            OutputLogger.Info($"Open file: {dialog.FileName}");
        }
    }

    private void OnNewDocument(object sender, RoutedEventArgs e)
    {
        _documentCounter++;
        var item = new DockItem
        {
            Title = $"Document {_documentCounter}",
            ContentId = $"doc-{_documentCounter}"
        };
        _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Center);
        DockHost.RebuildVisualTree();
        UpdateStatusBar();
        OutputLogger.Debug($"Created document: {item.Title}");
    }

    private void OnNewHexDocument(object sender, RoutedEventArgs e)
    {
        _documentCounter++;
        var item = new DockItem
        {
            Title = $"Hex Editor {_documentCounter}",
            ContentId = $"doc-hex-{_documentCounter}"
        };
        _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Center);
        DockHost.RebuildVisualTree();
        UpdateStatusBar();
        OutputLogger.Debug($"Created hex document: {item.Title}");
    }

    private void OnShowProperties(object sender, RoutedEventArgs e)
    {
        ShowOrCreatePanel("Properties", "panel-properties", DockDirection.Right);
    }

    private void OnShowSolutionExplorer(object sender, RoutedEventArgs e)
    {
        ShowOrCreatePanel("Solution Explorer", "panel-solution-explorer", DockDirection.Left);
    }

    private void OnShowOutput(object sender, RoutedEventArgs e)
    {
        ShowOrCreatePanel("Output", "panel-output", DockDirection.Bottom);
    }

    private void OnShowParsedFields(object sender, RoutedEventArgs e)
    {
        ShowOrCreatePanel("Parsed Fields", ParsedFieldsPanelContentId, DockDirection.Right);
    }

    private void ShowOrCreatePanel(string title, string contentId, DockDirection direction)
    {
        var existing = _layout.FindItemByContentId(contentId);
        if (existing is not null)
        {
            existing.Owner?.ActiveItem?.Equals(existing);
            return;
        }

        var item = new DockItem { Title = title, ContentId = contentId };
        _engine.Dock(item, _layout.MainDocumentHost, direction);
        DockHost.RebuildVisualTree();
        UpdateStatusBar();
    }

    // ─── Menu: Layout ──────────────────────────────────────────────────

    private void OnSaveLayout(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "dock-layout.json"
        };

        if (dialog.ShowDialog() == true)
        {
            DockHost.SyncLayoutSizes();

            _layout.WindowState = (int)WindowState;
            var rb = RestoreBounds;
            if (rb != Rect.Empty)
            {
                _layout.WindowLeft = rb.Left;
                _layout.WindowTop = rb.Top;
                _layout.WindowWidth = rb.Width;
                _layout.WindowHeight = rb.Height;
            }

            File.WriteAllText(dialog.FileName, DockLayoutSerializer.Serialize(_layout));
            OutputLogger.Info($"Layout saved to: {dialog.FileName}");
        }
    }

    private void OnLoadLayout(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var layout = DockLayoutSerializer.Deserialize(File.ReadAllText(dialog.FileName));
                _contentCache.Clear();
                ApplyLayout(layout);
                EnsureParsedFieldsPanel();
                OutputLogger.Info($"Layout loaded from: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                OutputLogger.Error($"Failed to load layout: {ex.Message}");
                MessageBox.Show($"Failed to load layout:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnResetLayout(object sender, RoutedEventArgs e)
    {
        _contentCache.Clear();
        SetupDefaultLayout();
        OutputLogger.Info("Layout reset to default.");
    }

    // ─── Menu: other ───────────────────────────────────────────────────

    private void OnToggleLock(object sender, RoutedEventArgs e)
    {
        _isLocked = LockMenuItem.IsChecked;
        _layout.MainDocumentHost.LockMode = _isLocked ? DockLockMode.Full : DockLockMode.None;

        foreach (var group in _layout.GetAllGroups())
            group.LockMode = _isLocked ? DockLockMode.Full : DockLockMode.None;

        OutputLogger.Info(_isLocked ? "Layout locked." : "Layout unlocked.");
        UpdateStatusBar();
    }

    private void ApplyTheme(string themeFile, string themeName)
    {
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary { Source = new Uri($"pack://application:,,,/WpfHexEditor.Docking.Wpf;component/Themes/{themeFile}") });
        SyncAllHexEditorThemes();
        OutputLogger.Info($"Theme changed to {themeName}.");
    }

    private void OnDarkTheme(object sender, RoutedEventArgs e) => ApplyTheme("DarkTheme.xaml", "Dark");
    private void OnLightTheme(object sender, RoutedEventArgs e) => ApplyTheme("Generic.xaml", "Light");
    private void OnVS2022DarkTheme(object sender, RoutedEventArgs e) => ApplyTheme("VS2022DarkTheme.xaml", "VS2022 Dark");
    private void OnDarkGlassTheme(object sender, RoutedEventArgs e) => ApplyTheme("DarkGlassTheme.xaml", "Dark Glass");
    private void OnVisualStudioTheme(object sender, RoutedEventArgs e) => ApplyTheme("VisualStudioTheme.xaml", "Visual Studio");
    private void OnCyberpunkTheme(object sender, RoutedEventArgs e) => ApplyTheme("CyberpunkTheme.xaml", "Cyberpunk");
    private void OnMinimalTheme(object sender, RoutedEventArgs e) => ApplyTheme("MinimalTheme.xaml", "Minimal");
    private void OnOfficeTheme(object sender, RoutedEventArgs e) => ApplyTheme("OfficeTheme.xaml", "Office");

    /// <summary>
    /// Re-applies theme colors to all HexEditor instances in the docking layout.
    /// </summary>
    private void SyncAllHexEditorThemes()
    {
        foreach (var editor in FindVisualChildren<HexEditorControl>(this))
            editor.ApplyThemeFromResources();
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                yield return t;
            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ─── Title bar ───────────────────────────────────────────────────

    private void OnMinimize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnCloseWindow(object sender, RoutedEventArgs e) => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Toggle maximize ↔ restore icon (Segoe MDL2 Assets)
        MaxRestoreButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE739";

        // WM_GETMINMAXINFO hook constrains maximize to the working area (respects taskbar),
        // so no margin compensation is needed in either state.
        RootGrid.Margin = new Thickness(0);
    }

    // ─── WM_GETMINMAXINFO — maximize respects taskbar ────────────────

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] private struct MINMAXINFO
    {
        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                // WindowStyle.None causes Windows to maximize over the taskbar.
                // Override ptMaxSize/ptMaxPosition to constrain to the working area.
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                var m   = source.CompositionTarget.TransformToDevice; // DIP → physical px
                var wa  = SystemParameters.WorkArea;                  // in DIPs
                mmi.ptMaxPosition.x = (int)(wa.Left   * m.M11);
                mmi.ptMaxPosition.y = (int)(wa.Top    * m.M22);
                mmi.ptMaxSize.x     = (int)(wa.Width  * m.M11);
                mmi.ptMaxSize.y     = (int)(wa.Height * m.M22);
                Marshal.StructureToPtr(mmi, lParam, true);
            }
        }
        return IntPtr.Zero;
    }

    // ─── Status bar ────────────────────────────────────────────────────

    private void UpdateStatusBar()
    {
        var panelCount = _layout.GetAllGroups().Sum(g => g.Items.Count);
        var lockState = _isLocked ? " [LOCKED]" : "";
        PanelCountText.Text = $"Panels: {panelCount}{lockState}";
    }
}
