//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Controls/StructureEditor.Contributors.cs
// Description: IEditorToolbarContributor + IStatusBarContributor implementation.
//              Provides contextual toolbar buttons and status bar items to the IDE shell.
//////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using WpfHexEditor.Editor.Core;

namespace WpfHexEditor.Editor.StructureEditor.Controls;

public sealed partial class StructureEditor : IEditorToolbarContributor, IStatusBarContributor
{
    // ── Toolbar ──────────────────────────────────────────────────────────────

    public ObservableCollection<EditorToolbarItem> ToolbarItems { get; } = [];

    private EditorToolbarItem? _tbSave;
    private EditorToolbarItem? _tbUndo;
    private EditorToolbarItem? _tbRedo;

    private void InitToolbarItems()
    {
        _tbSave = new EditorToolbarItem
        {
            Icon = "\uE74E", Tooltip = "Save (Ctrl+S)",
            Command = SaveCommand,
        };
        var tbValidate = new EditorToolbarItem
        {
            Icon = "\uE73E", Tooltip = "Validate",
            Command = new ViewModels.RelayCommand(() => _vm.TriggerValidationNow()),
        };
        _tbUndo = new EditorToolbarItem
        {
            Icon = "\uE7A7", Tooltip = "Undo (Ctrl+Z)",
            Command = UndoCommand, IsEnabled = false,
        };
        _tbRedo = new EditorToolbarItem
        {
            Icon = "\uE7A6", Tooltip = "Redo (Ctrl+Y)",
            Command = RedoCommand, IsEnabled = false,
        };

        ToolbarItems.Add(_tbSave);
        ToolbarItems.Add(new EditorToolbarItem { IsSeparator = true });
        ToolbarItems.Add(tbValidate);
        ToolbarItems.Add(new EditorToolbarItem { IsSeparator = true });
        ToolbarItems.Add(_tbUndo);
        ToolbarItems.Add(_tbRedo);
    }

    private void UpdateToolbarState()
    {
        if (_tbSave is not null) _tbSave.IsEnabled = _vm.IsDirty;
        if (_tbUndo is not null) _tbUndo.IsEnabled = _vm.UndoRedo.CanUndo;
        if (_tbRedo is not null) _tbRedo.IsEnabled = _vm.UndoRedo.CanRedo;
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
