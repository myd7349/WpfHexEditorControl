//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////
// Project: WpfHexEditor.Editor.StructureEditor
// File: ViewModels/BlocksViewModel.cs
// Description: VM for the Blocks tab — tree management, selection, add/remove/reorder.
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfHexEditor.Core.FormatDetection;
using WpfHexEditor.Core.ViewModels;

namespace WpfHexEditor.Editor.StructureEditor.ViewModels;

internal sealed class BlocksViewModel : ViewModelBase
{
    internal event EventHandler? Changed;

    private BlockViewModel? _selectedBlock;

    public ObservableCollection<BlockViewModel> BlockTree { get; } = [];

    public BlockViewModel? SelectedBlock
    {
        get => _selectedBlock;
        set => SetField(ref _selectedBlock, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand RemoveCommand    => new RelayCommand(RemoveSelected,    () => SelectedBlock is not null);
    public ICommand MoveUpCommand    => new RelayCommand(MoveUp,            () => CanMoveUp());
    public ICommand MoveDownCommand  => new RelayCommand(MoveDown,          () => CanMoveDown());
    public ICommand DuplicateCommand => new RelayCommand(DuplicateSelected, () => SelectedBlock is not null);
    public ICommand CopyBlockCommand  => new RelayCommand(CopyBlock,  () => SelectedBlock is not null);
    public ICommand PasteBlockCommand => new RelayCommand(PasteBlock, CanPasteBlock);

    // ── Filter ───────────────────────────────────────────────────────────────

    private string _filterText = "";

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (!SetField(ref _filterText, value)) return;
            OnPropertyChanged(nameof(FilteredBlockTree));
        }
    }

    /// <summary>Returns blocks matching the current filter (case-insensitive on name, description, and children).</summary>
    public IEnumerable<BlockViewModel> FilteredBlockTree =>
        string.IsNullOrWhiteSpace(_filterText)
            ? BlockTree
            : BlockTree.Where(b => b.MatchesFilter(_filterText));

    // ── Load / Save ───────────────────────────────────────────────────────────

    internal void LoadFrom(List<BlockDefinition>? blocks)
    {
        BlockTree.Clear();
        foreach (var b in blocks ?? [])
        {
            var vm = CreateViewModel(b);
            BlockTree.Add(vm);
        }
    }

    internal List<BlockDefinition> BuildBlocks() =>
        [..BlockTree.Select(vm => vm.Build())];

    // ── Add (called from AddBlockDialog result) ───────────────────────────────

    internal void AddBlock(string blockType, string name)
    {
        var b = new BlockDefinition
        {
            Type  = blockType,
            Name  = name,
            Color = StructureEditorConstants.DefaultBlockColor,
        };
        var vm = CreateViewModel(b);
        BlockTree.Add(vm);
        SelectedBlock = vm;
        RaiseChanged();
    }

    // ── Toolbar operations ────────────────────────────────────────────────────

    private void RemoveSelected()
    {
        if (SelectedBlock is null) return;
        BlockTree.Remove(SelectedBlock);
        SelectedBlock = BlockTree.Count > 0 ? BlockTree[0] : null;
        RaiseChanged();
    }

    private void MoveUp()
    {
        if (SelectedBlock is null) return;
        var idx = BlockTree.IndexOf(SelectedBlock);
        if (idx <= 0) return;
        BlockTree.Move(idx, idx - 1);
        RaiseChanged();
    }

    private void MoveDown()
    {
        if (SelectedBlock is null) return;
        var idx = BlockTree.IndexOf(SelectedBlock);
        if (idx < 0 || idx >= BlockTree.Count - 1) return;
        BlockTree.Move(idx, idx + 1);
        RaiseChanged();
    }

    private void DuplicateSelected()
    {
        if (SelectedBlock is null) return;
        var json = SelectedBlock.ToRawJson();
        var dup  = new BlockViewModel();
        dup.LoadFromRawJson(json);
        dup.Name = dup.Name + " (copy)";
        var idx = BlockTree.IndexOf(SelectedBlock);
        WireViewModel(dup);
        BlockTree.Insert(idx + 1, dup);
        SelectedBlock = dup;
        RaiseChanged();
    }

    private bool CanMoveUp()
    {
        if (SelectedBlock is null) return false;
        return BlockTree.IndexOf(SelectedBlock) > 0;
    }

    private bool CanMoveDown()
    {
        if (SelectedBlock is null) return false;
        var idx = BlockTree.IndexOf(SelectedBlock);
        return idx >= 0 && idx < BlockTree.Count - 1;
    }

    // ── Copy / Paste ─────────────────────────────────────────────────────────

    private const string ClipboardPrefix = "WHFMT_BLOCK:";

    private void CopyBlock()
    {
        if (SelectedBlock is null) return;
        Clipboard.SetText(ClipboardPrefix + SelectedBlock.ToRawJson());
    }

    private void PasteBlock()
    {
        var text = Clipboard.GetText();
        if (!text.StartsWith(ClipboardPrefix, StringComparison.Ordinal)) return;
        var json = text[ClipboardPrefix.Length..];
        var vm = new BlockViewModel();
        vm.LoadFromRawJson(json);
        WireViewModel(vm);
        var idx = SelectedBlock is not null ? BlockTree.IndexOf(SelectedBlock) + 1 : BlockTree.Count;
        BlockTree.Insert(idx, vm);
        SelectedBlock = vm;
        RaiseChanged();
    }

    private static bool CanPasteBlock()
    {
        try { return Clipboard.GetText().StartsWith(ClipboardPrefix, StringComparison.Ordinal); }
        catch { return false; }
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────────────

    /// <summary>Moves <paramref name="source"/> to <paramref name="targetIndex"/> in the flat block list.</summary>
    internal void MoveBlock(BlockViewModel source, int targetIndex)
    {
        var idx = BlockTree.IndexOf(source);
        if (idx < 0 || idx == targetIndex) return;
        BlockTree.Move(idx, Math.Clamp(targetIndex, 0, BlockTree.Count - 1));
        RaiseChanged();
    }

    // ── Variable cross-reference ──────────────────────────────────────────────

    /// <summary>All distinct variable names referenced by any block in the tree.</summary>
    public IEnumerable<string> ReferencedVariableNames =>
        BlockTree.SelectMany(b => b.GetReferencedVariables())
                 .Distinct(StringComparer.Ordinal);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private BlockViewModel CreateViewModel(BlockDefinition b)
    {
        var vm = new BlockViewModel();
        vm.LoadFrom(b);
        WireViewModel(vm);
        return vm;
    }

    private void WireViewModel(BlockViewModel vm) =>
        vm.Changed += (_, _) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
