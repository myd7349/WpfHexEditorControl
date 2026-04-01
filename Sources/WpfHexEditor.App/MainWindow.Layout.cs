// ==========================================================
// Project: WpfHexEditor.App
// File: MainWindow.Layout.cs
// Description:
//     Partial class of MainWindow handling the Customize Layout
//     feature: opens the popup, applies toggle/radio/mode changes,
//     restores persisted layout preferences, and manages
//     Ctrl+K chord keybindings for layout modes.
//
// Architecture Notes:
//     Uses LayoutCustomizationService for pure logic.
//     All WPF interaction stays in this file.
// ==========================================================

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfHexEditor.App.Dialogs;
using WpfHexEditor.App.Services;
using WpfHexEditor.Core.Options;

namespace WpfHexEditor.App;

public partial class MainWindow
{
    // ── Fields ─────────────────────────────────────────────────────────────

    private LayoutCustomizationService? _layoutService;
    private CustomizeLayoutPopup?       _customizeLayoutPopup;

    // Chord keybinding state (Ctrl+K → next key)
    private bool            _awaitingChord;
    private DispatcherTimer? _chordTimer;

    // Full-screen restore state
    private WindowState? _preFullScreenState;
    private WindowStyle? _preFullScreenStyle;

    // ── Initialization ────────────────────────────────────────────────────

    /// <summary>
    /// Creates the layout service and hooks chord keybinding handler.
    /// Called from <see cref="OnLoaded"/> after layout is loaded.
    /// </summary>
    private void InitLayoutCustomization()
    {
        var settings = AppSettingsService.Instance.Current.Layout;

        _layoutService = new LayoutCustomizationService(
            applyToggle:    ApplyLayoutElement,
            applyPosition:  ApplyLayoutPosition,
            applyFullScreen: _ => OnToggleFullScreen(),
            applyFontScale: ApplyFontScale,
            getPanelStates: () => _dockingAdapter?.GetAllKnownPanelStates()
                                    .Select(p => (p.Id, p.IsVisible)).ToList()
                                  ?? new List<(string Id, bool IsVisible)>());

        // Chord keybinding timer (1.5s timeout)
        _chordTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _chordTimer.Tick += (_, _) => { _awaitingChord = false; _chordTimer.Stop(); };

        // Hook chord handler
        PreviewKeyDown += OnLayoutChordKeyDown;

        // NOTE: RestoreFromSettings is called separately via RestoreLayoutPreferences()
        // AFTER TryRestoreSession() to avoid disrupting tab activation during startup.
    }

    /// <summary>
    /// Applies persisted layout preferences (visibility, positions, modes).
    /// Must be called after TryRestoreSession() so tab rebind doesn't break activation.
    /// </summary>
    private void RestoreLayoutPreferences()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        _layoutService?.RestoreFromSettings(settings);
    }

    // ── Command handlers ──────────────────────────────────────────────────

    /// <summary>Click handler for the title bar Customize Layout button.</summary>
    private void OnCustomizeLayoutButtonClick(object sender, RoutedEventArgs e)
        => OnCustomizeLayout();

    /// <summary>CommandBinding handler for Ctrl+Shift+L.</summary>
    private void OnCustomizeLayoutCommand(object sender, ExecutedRoutedEventArgs e)
        => OnCustomizeLayout();

    private void OnCustomizeLayout()
    {
        if (_customizeLayoutPopup is { IsVisible: true })
        {
            _customizeLayoutPopup.Close();
            _customizeLayoutPopup = null;
            return;
        }

        // Anchor below TitleBarSearchButton (same DPI-aware logic as Command Palette)
        Point? anchor = null;
        double? paletteWidth = null;
        if (TitleBarSearchButton is { IsLoaded: true })
        {
            var physPt = TitleBarSearchButton.PointToScreen(
                new Point(
                    TitleBarSearchButton.ActualWidth / 2,
                    TitleBarSearchButton.ActualHeight));

            var src  = PresentationSource.FromVisual(this);
            var dpiX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            var dpiY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            anchor   = new Point(physPt.X / dpiX, physPt.Y / dpiY);

            // Match Command Palette width
            var cpSettings = AppSettingsService.Instance.Current.CommandPalette;
            paletteWidth = Math.Clamp(cpSettings.WindowWidth, 400, 900);
        }

        var settings = AppSettingsService.Instance.Current.Layout;
        var panelStates = _dockingAdapter?.GetAllKnownPanelStates()
                          ?? Array.Empty<(string Id, string Title, bool IsVisible)>();

        _customizeLayoutPopup = new CustomizeLayoutPopup(
            settings:          settings,
            owner:             this,
            anchor:            anchor,
            panelStates:       panelStates,
            resolveGesture:    id => _keyBindingService.ResolveGesture(id),
            onToggle:          (id, vis) => _layoutService!.ApplyToggle(id, vis, settings),
            onRadioChange:     (gid, val) => _layoutService!.ApplyRadioChange(gid, val, settings),
            onModeToggle:      mid => _layoutService!.ApplyModeToggle(mid, settings),
            onSettingsChanged: () => AppSettingsService.Instance.Save(),
            overrideWidth:     paletteWidth);

        _customizeLayoutPopup.Show();
    }

    private void OnToggleFullScreen()
    {
        if (_preFullScreenState.HasValue)
        {
            // Exit full screen
            WindowStyle = _preFullScreenStyle!.Value;
            WindowState = _preFullScreenState.Value;
            _preFullScreenState = null;
            _preFullScreenStyle = null;
        }
        else
        {
            // Enter full screen
            _preFullScreenState = WindowState;
            _preFullScreenStyle = WindowStyle;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
    }

    private void OnToggleZenMode()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        _layoutService?.ApplyModeToggle("zen", settings);
        AppSettingsService.Instance.Save();
    }

    private void OnToggleFocusedMode()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        _layoutService?.ApplyModeToggle("focused", settings);
        AppSettingsService.Instance.Save();
    }

    private void OnTogglePresentationMode()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        _layoutService?.ApplyModeToggle("presentation", settings);
        AppSettingsService.Instance.Save();
    }

    private void OnToggleMenuBar()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        settings.ShowMenuBar = !settings.ShowMenuBar;
        ApplyLayoutElement("menubar", settings.ShowMenuBar);
        AppSettingsService.Instance.Save();
    }

    private void OnToggleToolbar()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        settings.ShowToolbar = !settings.ShowToolbar;
        ApplyLayoutElement("toolbar", settings.ShowToolbar);
        AppSettingsService.Instance.Save();
    }

    private void OnToggleStatusBar()
    {
        var settings = AppSettingsService.Instance.Current.Layout;
        settings.ShowStatusBar = !settings.ShowStatusBar;
        ApplyLayoutElement("statusbar", settings.ShowStatusBar);
        AppSettingsService.Instance.Save();
    }

    // ── Apply layout changes ──────────────────────────────────────────────

    private void ApplyLayoutElement(string elementId, bool visible)
    {
        var vis = visible ? Visibility.Visible : Visibility.Collapsed;

        switch (elementId)
        {
            case "menubar":
                MainMenuBar.Visibility = vis;
                break;

            case "toolbar":
                MainToolBarPanel.Visibility = vis;
                if (MainToolBarPanel.Parent is FrameworkElement toolbarBorder)
                    toolbarBorder.Visibility = vis;
                break;

            case "statusbar":
                AppStatusBar.Visibility = vis;
                break;

            default:
                // Dock panel toggle
                if (visible)
                    _dockingAdapter?.ShowDockablePanel(elementId);
                else
                    _dockingAdapter?.HideDockablePanel(elementId);
                break;
        }
    }

    private void ApplyLayoutPosition(string groupId, string value)
    {
        switch (groupId)
        {
            case "toolbar-position":
                ApplyToolbarPosition(value);
                break;

            case "panel-dock-side":
                // Override default dock side for newly opened panels
                if (_dockingAdapter != null)
                    _dockingAdapter.DefaultDockSideOverride = value;
                break;

            case "tab-position":
                // Change document tab strip placement via DocumentTabBarSettings
                if (DockHost.TabBarSettings is { } tabSettings)
                {
                    tabSettings.TabPlacement = value == "Bottom"
                        ? WpfHexEditor.Docking.Core.DocumentTabPlacement.Bottom
                        : WpfHexEditor.Docking.Core.DocumentTabPlacement.Top;
                }
                break;
        }
    }

    /// <summary>
    /// Moves the toolbar between Top (row 1) and Bottom (row 3, above status bar at row 4).
    /// </summary>
    private void ApplyToolbarPosition(string position)
    {
        if (MainToolBarPanel.Parent is not FrameworkElement toolbarBorder) return;
        Grid.SetRow(toolbarBorder, position == "Bottom" ? 3 : 1);
    }

    private void ApplyFontScale(double scale)
    {
        // Apply font scale to all open editors
        foreach (var doc in _documentManager.OpenDocuments)
        {
            var editor = doc.AssociatedEditor;
            if (editor is WpfHexEditor.Editor.CodeEditor.Controls.CodeEditorSplitHost codeHost)
            {
                codeHost.PrimaryEditor.ZoomLevel = scale;
                codeHost.SecondaryEditor.ZoomLevel = scale;
            }
            else if (editor is WpfHexEditor.Editor.TextEditor.Controls.TextEditor textEd)
            {
                textEd.ZoomLevel = scale;
            }
        }
    }

    // ── Chord keybindings (Ctrl+K → Z/F/P) ───────────────────────────────

    private void OnLayoutChordKeyDown(object sender, KeyEventArgs e)
    {
        if (_awaitingChord)
        {
            _awaitingChord = false;
            _chordTimer?.Stop();

            switch (e.Key)
            {
                case Key.Z: OnToggleZenMode();          e.Handled = true; break;
                case Key.F: OnToggleFocusedMode();      e.Handled = true; break;
                case Key.P: OnTogglePresentationMode();  e.Handled = true; break;
            }
            return;
        }

        // Detect Ctrl+K (chord start)
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _awaitingChord = true;
            _chordTimer?.Stop();
            _chordTimer?.Start();
            e.Handled = true;
        }
    }
}
