//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Controls/StructureEditor.Contributors.cs
// Description: IEditorToolbarContributor + IStatusBarContributor + IDiagnosticSource implementation.
//              Provides contextual toolbar buttons and status bar items to the IDE shell.
//              Publishes validation diagnostics to the IDE Error List panel.
//////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.IO;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.Core.Validation;

namespace WpfHexEditor.Editor.StructureEditor.Controls;

public sealed partial class StructureEditor : IEditorToolbarContributor, IStatusBarContributor, IDiagnosticSource
{
    // ── Toolbar ──────────────────────────────────────────────────────────────
    // NOTE: Save, Undo, Redo are global IDE commands (Ctrl+S/Z/Y delegate to
    //       ActiveDocumentEditor). Only domain-specific actions belong here.

    public ObservableCollection<EditorToolbarItem> ToolbarItems { get; } = [];

    private EditorToolbarItem? _tbAddBlock;
    private EditorToolbarItem? _tbCodeView;

    // Tracks whether the live code view split pane is open
    private bool _codeViewVisible;

    private const double CodeViewWidth = 360d;

    private void InitToolbarItems()
    {
        var tbValidate = new EditorToolbarItem
        {
            Icon    = "\uE73E",
            Tooltip = "Validate (Ctrl+Shift+V)",
            Command = new ViewModels.RelayCommand(() => _vm.TriggerValidationNow()),
        };
        _tbAddBlock = new EditorToolbarItem
        {
            Icon      = "\uE710",
            Tooltip   = "Add Block (Ctrl+N)",
            Command   = new ViewModels.RelayCommand(() => BlocksTabCtrl.RequestAddBlock()),
            IsEnabled = false,
        };
        _tbCodeView = new EditorToolbarItem
        {
            Icon    = "\uE943",   // Segoe MDL2 "Code" glyph
            Tooltip = "Toggle Live Code View",
            Command = new ViewModels.RelayCommand(ToggleCodeView),
        };

        ToolbarItems.Add(tbValidate);
        ToolbarItems.Add(new EditorToolbarItem { IsSeparator = true });
        ToolbarItems.Add(_tbAddBlock);
        ToolbarItems.Add(new EditorToolbarItem { IsSeparator = true });
        ToolbarItems.Add(_tbCodeView);
    }

    private void UpdateToolbarState()
    {
        if (_tbAddBlock is not null)
            _tbAddBlock.IsEnabled = MainTabs.SelectedIndex == 2; // Blocks tab
    }

    private void ToggleCodeView()
    {
        _codeViewVisible = !_codeViewVisible;

        if (_codeViewVisible)
        {
            SplitterCol.Width  = new System.Windows.GridLength(4);
            CodeViewCol.Width  = new System.Windows.GridLength(CodeViewWidth);
            // Push a fresh snapshot so the panel isn't blank on first open
            PushJsonToCodeView();
        }
        else
        {
            SplitterCol.Width  = new System.Windows.GridLength(0);
            CodeViewCol.Width  = new System.Windows.GridLength(0);
        }
    }

    // ── Diagnostics (IDE Error List) ─────────────────────────────────────────

    private List<DiagnosticEntry> _diagnostics = [];

    public IReadOnlyList<DiagnosticEntry> GetDiagnostics() => _diagnostics;

    public event EventHandler? DiagnosticsChanged;

    public string SourceLabel => "WHFMT";

    private void PublishDiagnostics()
    {
        var fileName = string.IsNullOrEmpty(_filePath) ? null : Path.GetFileName(_filePath);
        _diagnostics = _vm.ValidationSummary
            .Select(v => new DiagnosticEntry(
                Severity    : v.Severity == ValidationSeverity.Error
                                  ? DiagnosticSeverity.Error
                                  : DiagnosticSeverity.Warning,
                Code        : "WHFMT",
                Description : v.Message,
                ProjectName : "Format Definitions",
                FileName    : fileName,
                FilePath    : _filePath,
                Line        : v.Line,
                Column      : v.Column))
            .ToList();
        DiagnosticsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearDiagnostics()
    {
        _diagnostics = [];
        DiagnosticsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Status Bar ───────────────────────────────────────────────────────────

    public ObservableCollection<StatusBarItem> StatusBarItems { get; } = [];

    private StatusBarItem? _sbFormat;
    private StatusBarItem? _sbTab;
    private StatusBarItem? _sbBlocks;
    private StatusBarItem? _sbValidation;
    private StatusBarItem? _sbDirty;

    private void InitStatusBarItems()
    {
        _sbFormat = new StatusBarItem { Label = "Format", Value = "WHFMT" };
        _sbTab = new StatusBarItem { Label = "Tab", Value = "Metadata" };
        _sbBlocks = new StatusBarItem { Label = "Blocks", Value = "0" };
        _sbValidation = new StatusBarItem { Label = "Validation", Value = "—" };
        _sbDirty = new StatusBarItem { Label = "", Value = "", IsVisible = false };

        StatusBarItems.Add(_sbFormat);
        StatusBarItems.Add(_sbTab);
        StatusBarItems.Add(_sbBlocks);
        StatusBarItems.Add(_sbValidation);
        StatusBarItems.Add(_sbDirty);
    }

    public void RefreshStatusBarItems()
    {
        if (_sbTab is not null)
            _sbTab.Value = MainTabs.SelectedItem is System.Windows.Controls.TabItem ti
                ? ti.Header?.ToString() ?? "" : "";

        if (_sbBlocks is not null)
            _sbBlocks.Value = $"{_vm.Blocks.BlockTree.Count}";

        UpdateValidationStatus();
        UpdateDirtyStatus();
    }

    private void UpdateValidationStatus()
    {
        if (_sbValidation is null) return;
        _sbValidation.Value = _vm.ErrorCount > 0
            ? $"{_vm.ErrorCount} error(s), {_vm.WarningCount} warning(s)"
            : _vm.WarningCount > 0
                ? $"{_vm.WarningCount} warning(s)"
                : "Valid";
    }

    private void UpdateDirtyStatus()
    {
        if (_sbDirty is null) return;
        _sbDirty.Value = _vm.IsDirty ? "Modified" : "";
        _sbDirty.IsVisible = _vm.IsDirty;
    }
}
