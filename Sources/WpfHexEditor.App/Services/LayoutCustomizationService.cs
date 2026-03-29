// ==========================================================
// Project: WpfHexEditor.App
// File: Services/LayoutCustomizationService.cs
// Description:
//     Pure logic service for the Customize Layout feature.
//     Routes toggle/radio/mode changes to the host window and
//     manages layout mode state (Zen, Focused, Presentation).
//     No direct WPF dependencies — receives Action callbacks.
//
// Architecture Notes:
//     Instantiated once by MainWindow.Layout.cs. Holds the Zen
//     mode snapshot for state restore on exit.
// ==========================================================

using WpfHexEditor.Core.Options;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Snapshot of UI state captured before entering Zen Mode, used to restore on exit.
/// </summary>
internal sealed record ZenSnapshot(
    bool MenuBar,
    bool Toolbar,
    bool StatusBar,
    IReadOnlyList<(string Id, bool WasVisible)> Panels);

/// <summary>
/// Pure service coordinating layout customization actions.
/// </summary>
internal sealed class LayoutCustomizationService
{
    // ── Host callbacks ────────────────────────────────────────────────────

    private readonly Action<string, bool>    _applyToggle;     // (elementId, visible)
    private readonly Action<string, string>  _applyPosition;   // (groupId, value)
    private readonly Action<bool>            _applyFullScreen;
    private readonly Action<double>          _applyFontScale;  // presentation mode scale
    private readonly Func<IReadOnlyList<(string Id, bool IsVisible)>> _getPanelStates;

    // ── State ──────────────────────────────────────────────────────────────

    private ZenSnapshot? _zenSnapshot;
    private double _prePresentationFontScale = 1.0;

    public LayoutCustomizationService(
        Action<string, bool> applyToggle,
        Action<string, string> applyPosition,
        Action<bool> applyFullScreen,
        Action<double> applyFontScale,
        Func<IReadOnlyList<(string Id, bool IsVisible)>> getPanelStates)
    {
        _applyToggle    = applyToggle;
        _applyPosition  = applyPosition;
        _applyFullScreen = applyFullScreen;
        _applyFontScale = applyFontScale;
        _getPanelStates = getPanelStates;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Toggle dispatch
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Apply a visibility toggle for a UI element or panel.</summary>
    public void ApplyToggle(string elementId, bool visible, LayoutSettings settings)
    {
        switch (elementId)
        {
            case "menubar":   settings.ShowMenuBar   = visible; break;
            case "toolbar":   settings.ShowToolbar   = visible; break;
            case "statusbar": settings.ShowStatusBar = visible; break;
        }
        _applyToggle(elementId, visible);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Radio position dispatch
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Apply a layout position change.</summary>
    public void ApplyRadioChange(string groupId, string value, LayoutSettings settings)
    {
        switch (groupId)
        {
            case "toolbar-position":  settings.ToolbarPosition      = value; break;
            case "panel-dock-side":   settings.DefaultPanelDockSide = value; break;
            case "tab-position":      settings.TabBarPosition       = value; break;
        }
        _applyPosition(groupId, value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Layout modes
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Toggle a layout mode on/off.</summary>
    public void ApplyModeToggle(string modeId, LayoutSettings settings)
    {
        switch (modeId)
        {
            case "fullscreen":
                _applyFullScreen(true);
                break;

            case "zen":
                if (settings.IsZenMode)
                    ExitZenMode(settings);
                else
                    EnterZenMode(settings);
                break;

            case "focused":
                if (settings.IsFocusedMode)
                    ExitFocusedMode(settings);
                else
                    EnterFocusedMode(settings);
                break;

            case "presentation":
                if (settings.IsPresentationMode)
                    ExitPresentationMode(settings);
                else
                    EnterPresentationMode(settings);
                break;
        }
    }

    // ── Zen Mode ──────────────────────────────────────────────────────────

    private void EnterZenMode(LayoutSettings settings)
    {
        // Capture current state
        _zenSnapshot = new ZenSnapshot(
            settings.ShowMenuBar,
            settings.ShowToolbar,
            settings.ShowStatusBar,
            _getPanelStates());

        settings.IsZenMode = true;

        if (settings.ZenHideMenuBar)   { settings.ShowMenuBar   = false; _applyToggle("menubar",   false); }
        if (settings.ZenHideToolbar)   { settings.ShowToolbar   = false; _applyToggle("toolbar",   false); }
        if (settings.ZenHideStatusBar) { settings.ShowStatusBar = false; _applyToggle("statusbar", false); }

        if (settings.ZenHidePanels)
        {
            foreach (var (id, isVis) in _zenSnapshot.Panels)
            {
                if (isVis) _applyToggle(id, false);
            }
        }
    }

    private void ExitZenMode(LayoutSettings settings)
    {
        settings.IsZenMode = false;

        if (_zenSnapshot is null) return;

        // Restore from snapshot
        settings.ShowMenuBar   = _zenSnapshot.MenuBar;
        settings.ShowToolbar   = _zenSnapshot.Toolbar;
        settings.ShowStatusBar = _zenSnapshot.StatusBar;

        _applyToggle("menubar",   _zenSnapshot.MenuBar);
        _applyToggle("toolbar",   _zenSnapshot.Toolbar);
        _applyToggle("statusbar", _zenSnapshot.StatusBar);

        foreach (var (id, wasVis) in _zenSnapshot.Panels)
        {
            if (wasVis) _applyToggle(id, true);
        }

        _zenSnapshot = null;
    }

    // ── Focused Mode ──────────────────────────────────────────────────────

    private void EnterFocusedMode(LayoutSettings settings)
    {
        // Capture panel state if we don't have a zen snapshot
        if (_zenSnapshot is null)
        {
            _zenSnapshot = new ZenSnapshot(
                settings.ShowMenuBar,
                settings.ShowToolbar,
                settings.ShowStatusBar,
                _getPanelStates());
        }

        settings.IsFocusedMode = true;

        // Hide all panels but keep menu/toolbar/statusbar
        foreach (var (id, isVis) in _getPanelStates())
        {
            if (isVis) _applyToggle(id, false);
        }
    }

    private void ExitFocusedMode(LayoutSettings settings)
    {
        settings.IsFocusedMode = false;

        if (_zenSnapshot is null) return;

        foreach (var (id, wasVis) in _zenSnapshot.Panels)
        {
            if (wasVis) _applyToggle(id, true);
        }

        _zenSnapshot = null;
    }

    // ── Presentation Mode ─────────────────────────────────────────────────

    private void EnterPresentationMode(LayoutSettings settings)
    {
        _prePresentationFontScale = 1.0;
        settings.IsPresentationMode = true;
        _applyFontScale(settings.PresentationFontScale);
    }

    private void ExitPresentationMode(LayoutSettings settings)
    {
        settings.IsPresentationMode = false;
        _applyFontScale(_prePresentationFontScale);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Startup restore
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Apply persisted layout settings on app startup.</summary>
    public void RestoreFromSettings(LayoutSettings settings)
    {
        // Visibility toggles — only apply if non-default (hidden)
        if (!settings.ShowMenuBar)   _applyToggle("menubar",   false);
        if (!settings.ShowToolbar)   _applyToggle("toolbar",   false);
        if (!settings.ShowStatusBar) _applyToggle("statusbar", false);

        // Positions — only apply if changed from defaults to avoid triggering
        // DocumentTabHost.Rebind (deferred via Dispatcher) which races with tab activation.
        if (settings.ToolbarPosition != "Top")
            _applyPosition("toolbar-position", settings.ToolbarPosition);
        if (settings.DefaultPanelDockSide != "Right")
            _applyPosition("panel-dock-side", settings.DefaultPanelDockSide);
        if (settings.TabBarPosition != "Top")
            _applyPosition("tab-position", settings.TabBarPosition);
    }
}
