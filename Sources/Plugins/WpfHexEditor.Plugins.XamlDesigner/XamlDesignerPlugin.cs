// ==========================================================
// Project: WpfHexEditor.Plugins.XamlDesigner
// File: XamlDesignerPlugin.cs
// Author: Derek Tremblay
// Created: 2026-03-16
// Description:
//     Official plugin entry point for the XAML Designer.
//     Implements IWpfHexEditorPlugin + IPluginWithOptions.
//     Registers:
//       - XamlOutlinePanel        (Left, AutoHide, width=220)
//       - PropertyInspectorPanel  (Right, width=260)
//       - DesignHistoryPanel      (Right, auto-hide, width=240)
//       - Status bar item         (Left, order=15)
//       - View menu items: "XAML _Outline", "XAML _Properties", "Design _History"
//     Subscribes to FocusContext.FocusChanged to wire the active
//     XamlDesignerSplitHost's SelectedElementChanged event to both
//     side panels and the status bar item on each document switch.
//     On each host switch: DesignHistoryPanel.ViewModel.Manager is updated and
//     JumpRequested is wired to host.JumpToHistoryEntry.
//
// Architecture Notes:
//     Pattern: Observer — subscribes to IFocusContextService.FocusChanged.
//     All UI is constructed and registered on the calling thread (UI thread).
//     UIRegistry.UnregisterAllForPlugin is called automatically by PluginHost on unload.
// ==========================================================

using System.Windows;
using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.XamlDesigner.Controls;
using WpfHexEditor.Editor.XamlDesigner.Panels;
using WpfHexEditor.Editor.XamlDesigner.ViewModels;
using WpfHexEditor.Plugins.XamlDesigner.Options;
using WpfHexEditor.SDK.Commands;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Focus;
using WpfHexEditor.SDK.Descriptors;
using WpfHexEditor.SDK.Models;

namespace WpfHexEditor.Plugins.XamlDesigner;

/// <summary>
/// Entry point for the official XAML Designer plugin.
/// Registers the Outline and Property Inspector side panels, wires them to
/// the active <see cref="XamlDesignerSplitHost"/> via FocusContext events.
/// </summary>
public sealed class XamlDesignerPlugin : IWpfHexEditorPlugin, IPluginWithOptions
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public string  Id      => "WpfHexEditor.Plugins.XamlDesigner";
    public string  Name    => "XAML Designer";
    public Version Version => new(0, 1, 0);

    public PluginCapabilities Capabilities => new()
    {
        AccessHexEditor  = false,
        AccessFileSystem = true,
        RegisterMenus    = true,
        WriteOutput      = false,
        AccessSettings   = true
    };

    // ── UI ID constants ───────────────────────────────────────────────────────

    private const string OutlinePanelUiId    = "WpfHexEditor.Plugins.XamlDesigner.Panel.Outline";
    private const string PropertiesPanelUiId = "WpfHexEditor.Plugins.XamlDesigner.Panel.Properties";
    private const string ToolboxPanelUiId    = "WpfHexEditor.Plugins.XamlDesigner.Panel.Toolbox";
    private const string ResourcePanelUiId   = "WpfHexEditor.Plugins.XamlDesigner.Panel.Resource";
    private const string DesignDataPanelUiId = "WpfHexEditor.Plugins.XamlDesigner.Panel.DesignData";
    private const string AnimationPanelUiId  = "WpfHexEditor.Plugins.XamlDesigner.Panel.Animation";
    private const string HistoryPanelUiId    = "WpfHexEditor.Plugins.XamlDesigner.Panel.History";
    private const string StatusBarElementId  = "WpfHexEditor.Plugins.XamlDesigner.StatusBar.Element";

    // ── State ─────────────────────────────────────────────────────────────────

    private XamlOutlinePanel?          _outlinePanel;
    private PropertyInspectorPanel?    _propertiesPanel;
    private XamlToolboxPanel?          _toolboxPanel;
    private ResourceBrowserPanel?      _resourcePanel;
    private DesignDataPanel?           _designDataPanel;
    private AnimationTimelinePanel?    _animationPanel;
    private DesignHistoryPanel?        _historyPanel;
    private IIDEHostContext?           _context;
    private XamlDesignerOptionsPage?   _optionsPage;
    private StatusBarItemDescriptor?   _sbElement;

    // Track the currently wired host to properly unwire on document switch.
    private XamlDesignerSplitHost?     _wiredHost;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public Task InitializeAsync(IIDEHostContext context, CancellationToken ct = default)
    {
        _context = context;

        // Build panels.
        _outlinePanel    = new XamlOutlinePanel();
        _propertiesPanel = new PropertyInspectorPanel();
        _toolboxPanel    = new XamlToolboxPanel();
        _resourcePanel   = new ResourceBrowserPanel();
        _designDataPanel = new DesignDataPanel();
        _animationPanel  = new AnimationTimelinePanel();
        _historyPanel    = new DesignHistoryPanel();

        // Register the XAML Outline panel (left side, auto-hide).
        context.UIRegistry.RegisterPanel(
            OutlinePanelUiId,
            _outlinePanel,
            Id,
            new PanelDescriptor
            {
                Title           = "XAML Outline",
                DefaultDockSide = "Left",
                DefaultAutoHide = true,
                CanClose        = true,
                PreferredWidth  = 220
            });

        // Register the Property Inspector panel (right side).
        context.UIRegistry.RegisterPanel(
            PropertiesPanelUiId,
            _propertiesPanel,
            Id,
            new PanelDescriptor
            {
                Title           = "XAML Properties",
                DefaultDockSide = "Right",
                DefaultAutoHide = false,
                CanClose        = true,
                PreferredWidth  = 260
            });

        // Register the XAML Toolbox panel (left side, auto-hide).
        context.UIRegistry.RegisterPanel(
            ToolboxPanelUiId,
            _toolboxPanel,
            Id,
            new PanelDescriptor
            {
                Title           = "XAML Toolbox",
                DefaultDockSide = "Left",
                DefaultAutoHide = true,
                CanClose        = true,
                PreferredWidth  = 220
            });

        // Register the Resource Browser panel (right side, auto-hide).
        context.UIRegistry.RegisterPanel(
            ResourcePanelUiId,
            _resourcePanel,
            Id,
            new PanelDescriptor
            {
                Title           = "Resource Browser",
                DefaultDockSide = "Right",
                DefaultAutoHide = true,
                CanClose        = true,
                PreferredWidth  = 260
            });

        // Register the Design Data panel (bottom).
        context.UIRegistry.RegisterPanel(
            DesignDataPanelUiId,
            _designDataPanel,
            Id,
            new PanelDescriptor
            {
                Title            = "Design Data",
                DefaultDockSide  = "Bottom",
                DefaultAutoHide  = false,
                CanClose         = true,
                PreferredHeight  = 180
            });

        // Register the Animation Timeline panel (bottom).
        context.UIRegistry.RegisterPanel(
            AnimationPanelUiId,
            _animationPanel,
            Id,
            new PanelDescriptor
            {
                Title            = "Animation Timeline",
                DefaultDockSide  = "Bottom",
                DefaultAutoHide  = false,
                CanClose         = true,
                PreferredHeight  = 180
            });

        // Register the Design History panel (right side, auto-hide).
        context.UIRegistry.RegisterPanel(
            HistoryPanelUiId,
            _historyPanel,
            Id,
            new PanelDescriptor
            {
                Title           = "Design History",
                DefaultDockSide = "Right",
                DefaultAutoHide = true,
                CanClose        = true,
                PreferredWidth  = 240
            });

        // Register status bar item (left, order=15).
        _sbElement = new StatusBarItemDescriptor
        {
            Text      = "",
            Alignment = StatusBarAlignment.Left,
            Order     = 15,
            ToolTip   = "Selected element in the active XAML designer"
        };
        context.UIRegistry.RegisterStatusBarItem(StatusBarElementId, Id, _sbElement);

        // Register menu items.
        RegisterMenuItems(context);

        // Subscribe to document focus changes so panels sync to the active designer.
        context.FocusContext.FocusChanged += OnFocusChanged;

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        if (_context is not null)
            _context.FocusContext.FocusChanged -= OnFocusChanged;

        UnwireCurrentHost();

        _outlinePanel    = null;
        _propertiesPanel = null;
        _toolboxPanel    = null;
        _resourcePanel   = null;
        _designDataPanel = null;
        _animationPanel  = null;
        _historyPanel    = null;
        _context         = null;
        _optionsPage     = null;
        _sbElement       = null;
        _wiredHost       = null;

        return Task.CompletedTask;
    }

    // ── Focus tracking ────────────────────────────────────────────────────────

    private void OnFocusChanged(object? sender, FocusChangedEventArgs e)
    {
        // Resolve the active XamlDesignerSplitHost from the newly active document.
        var host = ResolveActiveHost(e.ActiveDocument);

        if (ReferenceEquals(host, _wiredHost))
            return; // Same designer — no rewiring needed.

        UnwireCurrentHost();
        _wiredHost = host;

        if (host is null)
        {
            // No XAML designer active — clear the side panels.
            _outlinePanel?.ViewModel?.RebuildTree(null);
            if (_propertiesPanel?.ViewModel is not null)
                _propertiesPanel.ViewModel.SelectedObject = null;
            _propertiesPanel?.SetElementName(null);
            if (_historyPanel is not null)
                _historyPanel.ViewModel.Manager = null;
            UpdateStatusBar(null);
            return;
        }

        // Wire the new host: outline + property inspector + status bar.
        host.SelectedElementChanged        += OnSelectedElementChanged;
        host.Document.XamlChanged         += OnXamlChanged;
        host.FocusPropertiesPanelRequested += OnFocusPropertiesRequested;

        // Wire the history panel to the new host's undo manager.
        if (_historyPanel is not null)
        {
            _historyPanel.ViewModel.Manager = host.UndoManager;
            _historyPanel.JumpRequested     += OnHistoryPanelJumpRequested;
        }

        _outlinePanel?.ViewModel?.RebuildTree(host.Document.ParsedRoot);
        UpdateSidePanels(host);
    }

    private void UnwireCurrentHost()
    {
        if (_wiredHost is null) return;
        _wiredHost.SelectedElementChanged        -= OnSelectedElementChanged;
        _wiredHost.Document.XamlChanged         -= OnXamlChanged;
        _wiredHost.FocusPropertiesPanelRequested -= OnFocusPropertiesRequested;

        // Detach history panel from the outgoing host.
        if (_historyPanel is not null)
        {
            _historyPanel.JumpRequested     -= OnHistoryPanelJumpRequested;
            _historyPanel.ViewModel.Manager  = null;
        }

        _wiredHost = null;
    }

    private void OnHistoryPanelJumpRequested(object? sender, JumpToEntryEventArgs e)
        => _wiredHost?.JumpToHistoryEntry(e.UndoCount, e.RedoCount);

    private void OnFocusPropertiesRequested(object? sender, EventArgs e)
    {
        if (_context is null) return;
        _context.UIRegistry.FocusPanel(PropertiesPanelUiId);
    }

    private void OnXamlChanged(object? sender, EventArgs e)
    {
        if (_wiredHost is null) return;
        _outlinePanel?.ViewModel?.RebuildTree(_wiredHost.Document.ParsedRoot);
    }

    private void OnSelectedElementChanged(object? sender, EventArgs e)
    {
        if (_wiredHost is null) return;
        UpdateSidePanels(_wiredHost);
    }

    private void UpdateSidePanels(XamlDesignerSplitHost host)
    {
        var selectedUi = host.Canvas?.SelectedElement;
        var dep = selectedUi as System.Windows.DependencyObject;

        if (_propertiesPanel?.ViewModel is not null)
            _propertiesPanel.ViewModel.SelectedObject = dep;

        var elementName = dep?.GetType().Name ?? string.Empty;
        _propertiesPanel?.SetElementName(elementName);
        UpdateStatusBar(elementName);
    }

    private void UpdateStatusBar(string? elementName)
    {
        if (_sbElement is null) return;
        _sbElement.Text = string.IsNullOrEmpty(elementName)
            ? string.Empty
            : $"⬚ {elementName}";
    }

    // ── Helper: resolve XamlDesignerSplitHost from IDocument ─────────────────

    private XamlDesignerSplitHost? ResolveActiveHost(IDocument? doc)
    {
        if (doc is null || _context is null) return null;

        var model = _context.DocumentHost.Documents.OpenDocuments
            .FirstOrDefault(d => d.ContentId == doc.ContentId);

        return model?.AssociatedEditor as XamlDesignerSplitHost;
    }

    // ── Menu items ────────────────────────────────────────────────────────────

    private void RegisterMenuItems(IIDEHostContext context)
    {
        // View > XAML Outline
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleOutline",
            Id,
            new MenuItemDescriptor
            {
                Header     = "XAML _Outline",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE8A5",
                ToolTip    = "Show or hide the XAML Outline panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(OutlinePanelUiId))
            });

        // View > XAML Properties
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleProperties",
            Id,
            new MenuItemDescriptor
            {
                Header     = "XAML _Properties",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE946",
                ToolTip    = "Show or hide the XAML Property Inspector panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(PropertiesPanelUiId))
            });

        // View > XAML Toolbox
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleToolbox",
            Id,
            new MenuItemDescriptor
            {
                Header     = "XAML _Toolbox",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE7A6",
                ToolTip    = "Show or hide the XAML Toolbox panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(ToolboxPanelUiId))
            });

        // View > Resource Browser
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleResourceBrowser",
            Id,
            new MenuItemDescriptor
            {
                Header     = "_Resource Browser",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE8B9",
                ToolTip    = "Show or hide the Resource Browser panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(ResourcePanelUiId))
            });

        // View > Design Data
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleDesignData",
            Id,
            new MenuItemDescriptor
            {
                Header     = "_Design Data",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE9D9",
                ToolTip    = "Show or hide the Design Data panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(DesignDataPanelUiId))
            });

        // View > Animation Timeline
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleAnimation",
            Id,
            new MenuItemDescriptor
            {
                Header     = "_Animation Timeline",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE916",
                ToolTip    = "Show or hide the Animation Timeline panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(AnimationPanelUiId))
            });

        // View > Design History
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.ToggleHistory",
            Id,
            new MenuItemDescriptor
            {
                Header     = "Design _History",
                ParentPath = "View",
                Group      = "Panels",
                IconGlyph  = "\uE81C",
                ToolTip    = "Show or hide the Design History panel",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(HistoryPanelUiId))
            });
    }

    // ── IPluginWithOptions ────────────────────────────────────────────────────

    public FrameworkElement CreateOptionsPage()
    {
        _optionsPage = new XamlDesignerOptionsPage();
        _optionsPage.Load();
        return _optionsPage;
    }

    public void SaveOptions()
    {
        _optionsPage?.Save();
    }

    public void LoadOptions()
    {
        XamlDesignerOptions.Invalidate();
        _optionsPage?.Load();
    }
}
