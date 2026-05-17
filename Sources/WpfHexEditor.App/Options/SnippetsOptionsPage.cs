//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : Options/SnippetsOptionsPage.cs
// Description  : Options page for viewing and editing user-defined code
//                snippets persisted via UserSnippetStore.
// Architecture : Code-behind-only UserControl implementing IOptionsPage.
//                Phase 2 adds: syntax-highlighted body editor, variable
//                picker chips, live preview panel, conflict detection,
//                and import/export.
//////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.App.Options.Snippets;
using WpfHexEditor.App.Properties;
using WpfHexEditor.Core.Options;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.CodeEditor.Snippets;
using WpfHexEditor.Editor.Core.Dialogs;
using static WpfHexEditor.Editor.CodeEditor.Snippets.SnippetBodyTokenizer;

namespace WpfHexEditor.App.Options;

/// <summary>IDE options page — user-defined Code Snippets.</summary>
public sealed class SnippetsOptionsPage : UserControl, IOptionsPage
{
    private const string DefaultTrigger  = "new";
    private const string DefaultBody     = CursorMarker;
    private const string GlobalLanguage  = "*";

    private readonly UserSnippetStore   _store;
    private readonly DataGrid           _grid;
    private readonly SnippetBodyHighlightBox _bodyEditor;
    private readonly SnippetVariablePicker   _varPicker;
    private readonly SnippetPreviewPanel     _previewPanel;
    private readonly Border                  _conflictBanner;
    private readonly TextBlock               _conflictText;
    private readonly ObservableCollection<StoredSnippet> _rows = [];
    private string _lastPersistedSignature = string.Empty;

    public event EventHandler? Changed;

    public SnippetsOptionsPage() : this(new UserSnippetStore()) { }

    public SnippetsOptionsPage(UserSnippetStore store)
    {
        _store = store;

        var toolbar = BuildToolbar();
        _grid       = BuildGrid();

        (var bodyLabel, _bodyEditor, _varPicker, _previewPanel) = BuildBodySection();
        (_conflictBanner, _conflictText) = BuildConflictBanner();

        var root = new StackPanel { Margin = new Thickness(8) };
        root.Children.Add(toolbar);
        root.Children.Add(_grid);
        root.Children.Add(bodyLabel);
        root.Children.Add(_bodyEditor);
        root.Children.Add(_varPicker);
        root.Children.Add(_previewPanel);
        root.Children.Add(_conflictBanner);

        Content = root;
        ReloadRows();
    }

    private (TextBlock label, SnippetBodyHighlightBox editor,
             SnippetVariablePicker picker, SnippetPreviewPanel preview)
        BuildBodySection()
    {
        var label = new TextBlock
        {
            Text       = CodeEditorResources.Snippets_Page_BodyLabel,
            Margin     = new Thickness(0, 8, 0, 4),
            FontWeight = FontWeights.SemiBold,
        };
        var editor = new SnippetBodyHighlightBox();
        editor.TextChanged += (_, _) => OnBodyTextChanged();

        var picker = new SnippetVariablePicker();
        picker.VariableChosen += varName => editor.InsertAtCaret($"${{{varName}}}");

        return (label, editor, picker, new SnippetPreviewPanel());
    }

    private static (Border banner, TextBlock text) BuildConflictBanner()
    {
        var text   = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var banner = new Border
        {
            Visibility      = Visibility.Collapsed,
            Margin          = new Thickness(0, 4, 0, 0),
            Padding         = new Thickness(8, 4, 8, 4),
            BorderThickness = new Thickness(1),
            Child           = text,
        };
        banner.SetResourceReference(Border.BackgroundProperty,   "Opt_WarnBrush");
        banner.SetResourceReference(Border.BorderBrushProperty,  "Opt_WarnBorder");
        return (banner, text);
    }

    // ── IOptionsPage ─────────────────────────────────────────────────────────

    public void Load(AppSettings settings) => ReloadRows();
    public void Flush(AppSettings settings) { }

    // ── Toolbar ──────────────────────────────────────────────────────────────

    private StackPanel BuildToolbar()
    {
        var addBtn    = MakeButton(CodeEditorResources.Snippets_Page_Add,    OnAdd);
        var removeBtn = MakeButton(CodeEditorResources.Snippets_Page_Remove, OnRemove);
        var importBtn = MakeButton(CodeEditorResources.Snippets_Page_Import, OnImport);
        var exportBtn = MakeButton(CodeEditorResources.Snippets_Page_Export, OnExport);

        var sep = new Separator { Width = 12, Opacity = 0 };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        toolbar.Children.Add(addBtn);
        toolbar.Children.Add(removeBtn);
        toolbar.Children.Add(sep);
        toolbar.Children.Add(importBtn);
        toolbar.Children.Add(exportBtn);
        return toolbar;
    }

    private static Button MakeButton(string label, Action onClick)
    {
        var btn = new Button
        {
            Content = label,
            Margin  = new Thickness(0, 0, 6, 0),
            Padding = new Thickness(10, 3, 10, 3),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    // ── Grid ─────────────────────────────────────────────────────────────────

    private DataGrid BuildGrid()
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows      = false,
            CanUserDeleteRows   = false,
            HeadersVisibility   = DataGridHeadersVisibility.Column,
            SelectionMode       = DataGridSelectionMode.Single,
            ItemsSource         = _rows,
            Height              = 220,
        };
        grid.Columns.Add(MakeTextColumn(CodeEditorResources.Snippets_Page_ColLanguage,    nameof(StoredSnippet.LanguageId),  100));
        grid.Columns.Add(MakeTextColumn(CodeEditorResources.Snippets_Page_ColTrigger,     nameof(StoredSnippet.Trigger),     120));
        grid.Columns.Add(MakeTextColumn(CodeEditorResources.Snippets_Page_ColDescription, nameof(StoredSnippet.Description), 260));
        grid.SelectionChanged += OnSelectionChanged;
        grid.CellEditEnding   += (_, _) => Persist();
        return grid;
    }

    private static DataGridTextColumn MakeTextColumn(string header, string binding, double width) => new()
    {
        Header  = header,
        Binding = new System.Windows.Data.Binding(binding)
            { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
        Width   = width,
    };

    // ── Event handlers ───────────────────────────────────────────────────────

    private void OnAdd()
    {
        var snippet = new StoredSnippet
        {
            LanguageId  = GlobalLanguage,
            Trigger     = DefaultTrigger,
            Description = CodeEditorResources.Snippets_Page_DefaultDescription,
            Body        = DefaultBody,
        };
        _rows.Add(snippet);
        _grid.SelectedItem = snippet;
        _bodyEditor.Text   = snippet.Body;
        Persist();
    }

    private void OnRemove()
    {
        if (_grid.SelectedItem is not StoredSnippet selected) return;
        _rows.Remove(selected);
        _bodyEditor.Text = string.Empty;
        Persist();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_grid.SelectedItem is not StoredSnippet s) return;
        _bodyEditor.Text = s.Body;
        _previewPanel.Refresh(s.Body);
    }

    private void OnBodyTextChanged()
    {
        if (_grid.SelectedItem is StoredSnippet selected)
            selected.Body = _bodyEditor.Text ?? string.Empty;
        _previewPanel.Refresh(_bodyEditor.Text ?? string.Empty);
        UpdateConflictBanner();
    }

    private void OnImport()
    {
        var owner = Window.GetWindow(this);
        var (ok, imported, error) = SnippetImportExport.TryImport(owner!);
        if (!ok)
        {
            if (error is not null)
                IdeMessageBox.Show(string.Format(CodeEditorResources.Snippets_Page_ImportError, error),
                    CodeEditorResources.Snippets_Page_ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning, owner);
            return;
        }

        var merge = IdeMessageBox.Show(
            CodeEditorResources.Snippets_Page_ImportMergePrompt,
            CodeEditorResources.Snippets_Page_ImportMergeTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Question, owner);

        ApplyImport(imported, replaceAll: merge == MessageBoxResult.Yes);
        IdeMessageBox.Show(string.Format(CodeEditorResources.Snippets_Page_ImportSuccess, imported.Count),
            CodeEditorResources.Snippets_Page_ImportTitle, MessageBoxButton.OK, MessageBoxImage.Information, owner);
    }

    private void ApplyImport(System.Collections.Generic.IReadOnlyList<StoredSnippet> imported, bool replaceAll)
    {
        if (replaceAll)
        {
            _rows.Clear();
            foreach (var s in imported) _rows.Add(s);
        }
        else
        {
            foreach (var s in imported)
                if (!_rows.Any(r => UserSnippetStore.SameKey(r, s)))
                    _rows.Add(s);
        }
        Persist();
    }

    private void OnExport()
    {
        var owner = Window.GetWindow(this);
        var (ok, error) = SnippetImportExport.TryExport(_rows, owner!);
        if (!ok && error is not null)
            IdeMessageBox.Show(string.Format(CodeEditorResources.Snippets_Page_ExportError, error),
                CodeEditorResources.Snippets_Page_ExportTitle, MessageBoxButton.OK, MessageBoxImage.Warning, owner);
    }

    // ── Persistence & helpers ────────────────────────────────────────────────

    private void Persist()
    {
        if (_grid.SelectedItem is StoredSnippet selected)
            selected.Body = _bodyEditor.Text ?? string.Empty;

        var signature = ComputeSignature(_rows);
        if (signature == _lastPersistedSignature) return;

        _store.ReplaceAll(_rows);
        _lastPersistedSignature = signature;
        UpdateConflictBanner();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateConflictBanner()
    {
        var conflicts = SnippetConflictDetector.DetectConflicts(_rows);
        if (conflicts.Count == 0)
        {
            _conflictBanner.Visibility = Visibility.Collapsed;
            return;
        }
        _conflictText.Text = string.Format(
            CodeEditorResources.Snippets_Page_ConflictWarning, conflicts.Count);
        _conflictBanner.Visibility = Visibility.Visible;
    }

    private void ReloadRows()
    {
        _rows.Clear();
        foreach (var s in _store.GetAll())
            _rows.Add(UserSnippetStore.Clone(s));
        _lastPersistedSignature = ComputeSignature(_rows);
    }

    private static string ComputeSignature(IReadOnlyList<StoredSnippet> rows)
    {
        if (rows.Count == 0) return "0";
        var sb = new System.Text.StringBuilder(rows.Count * 32);
        foreach (var s in rows)
        {
            sb.Append(s.LanguageId).Append('\x01')
              .Append(s.Trigger).Append('\x01')
              .Append(s.Description).Append('\x01')
              .Append(s.Body).Append('\x01');
        }
        return sb.ToString();
    }
}
