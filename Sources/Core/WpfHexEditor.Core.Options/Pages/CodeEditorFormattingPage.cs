// ==========================================================
// Project: WpfHexEditor.Core.Options
// File: Pages/CodeEditorFormattingPage.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-04-02
// Updated: 2026-04-04
// Description:
//     Dedicated options page for Code Editor formatting overrides.
//     Three-state checkboxes: checked = force on, unchecked = force off,
//     indeterminate = inherit the language .whfmt default.
//     Two-column layout: left = options, right = FormattingPreviewPanel.
//     Checkbox MouseOver triggers FormattingRuleTooltip (whfmt-driven Before/After).
//
// Architecture Notes:
//     Code-only UserControl implementing IOptionsPage.
//     Reads/writes AppSettings.CodeEditorDefaults formatting override properties.
//     Registered in OptionsPageRegistry under "Code Editor" / "Formatting".
//     IPreviewColorizer injected from App layer via constructor (null = no preview).
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfHexEditor.Core.Options.Preview;
using WpfHexEditor.Core.ProjectSystem.Languages;

namespace WpfHexEditor.Core.Options.Pages;

/// <summary>
/// IDE options page — Code Editor › Formatting.
/// </summary>
public sealed class CodeEditorFormattingPage : UserControl, IOptionsPage
{
    public event EventHandler? Changed;

    // â”€â”€ Checkboxes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private readonly CheckBox _formatOnSave;
    private readonly CheckBox _trimTrailing;
    private readonly CheckBox _insertFinalNewline;
    private readonly CheckBox _spaceAfterKeywords;
    private readonly CheckBox _spaceAroundOperators;
    private readonly CheckBox _spaceAfterComma;
    private readonly CheckBox _indentCaseLabels;
    private readonly CheckBox _organizeImports;

    // Smart editing
    private readonly CheckBox _autoBrackets;
    private readonly CheckBox _autoQuotes;
    private readonly CheckBox _skipOverClose;
    private readonly CheckBox _wrapSelection;

    // â”€â”€ Preview â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private readonly FormattingPreviewPanel? _preview;

    // ruleId â†’ tooltip (lazy-initialised on first MouseEnter)
    private readonly Dictionary<string, FormattingRuleTooltip> _tooltips = new();

    // Map: CheckBox â†’ (ruleId, display label) for tooltip wiring
    private readonly List<(CheckBox Box, string RuleId, string Label)> _ruleMap = [];

    private readonly IPreviewColorizer? _colorizer;
    private bool _loading;

    // â”€â”€ Construction â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <param name="colorizer">
    /// Optional colorizer for the live preview and tooltips.
    /// When null the right column shows a placeholder message.
    /// </param>
    /// <param name="formatter">
    /// Optional formatter applied to the snippet before colorising.
    /// When null the raw snippet is displayed as-is.
    /// </param>
    public CodeEditorFormattingPage(IPreviewColorizer? colorizer = null,
                                    IPreviewFormatter? formatter = null)
    {
        _colorizer = colorizer;

        // â”€â”€ Root: two-column Grid â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 280 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });   // gutter
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 260 });

        // â”€â”€ Left: options scroll â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var leftScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(16)
        };
        Grid.SetColumn(leftScroll, 0);

        var stack = new StackPanel { Orientation = Orientation.Vertical };

        // Page header
        var header = new TextBlock
        {
            Text       = "Code Editor \u2014 Formatting",
            FontSize   = 16,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 6)
        };
        stack.Children.Add(header);

        // Subtitle
        var subtitle = new TextBlock
        {
            Text         = "These settings override the language .whfmt defaults.\nLeave checkboxes at their indeterminate state (â€”) to inherit the language rules.",
            TextWrapping = TextWrapping.Wrap,
            FontSize     = 11,
            Opacity      = 0.65,
            Margin       = new Thickness(0, 0, 0, 16)
        };
        stack.Children.Add(subtitle);

        // â”€â”€ General â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        stack.Children.Add(MakeSectionHeader("GENERAL"));

        _formatOnSave = MakeCheckBox("Format on Save (Ctrl+S)", false, "formatOnSave", "Format on Save");
        stack.Children.Add(_formatOnSave);

        // â”€â”€ Whitespace â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        stack.Children.Add(MakeSectionHeader("WHITESPACE"));

        _trimTrailing = MakeThreeStateCheckBox("Trim trailing whitespace",
            "Remove trailing spaces and tabs on each line",
            "trimTrailingWhitespace", "Trim trailing whitespace");

        _insertFinalNewline = MakeThreeStateCheckBox("Insert final newline",
            "Ensure the file ends with exactly one newline",
            "insertFinalNewline", "Insert final newline");

        stack.Children.Add(_trimTrailing);
        stack.Children.Add(_insertFinalNewline);

        // â”€â”€ Spacing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        stack.Children.Add(MakeSectionHeader("SPACING"));

        _spaceAfterKeywords = MakeThreeStateCheckBox(
            "Space after keywords (if, for, while, switch, catch)",
            "Insert a space between control keywords and opening paren: if (...) vs if(...)",
            "spaceAfterKeywords", "Space after keywords");

        _spaceAroundOperators = MakeThreeStateCheckBox(
            "Space around binary operators (+, -, *, /, ==, !=, &&, ||)",
            "Insert spaces around binary operators: a + b vs a+b",
            "spaceAroundBinaryOperators", "Space around binary operators");

        _spaceAfterComma = MakeThreeStateCheckBox("Space after commas",
            "Insert a space after commas: (a, b, c) vs (a,b,c)",
            "spaceAfterComma", "Space after commas");

        stack.Children.Add(_spaceAfterKeywords);
        stack.Children.Add(_spaceAroundOperators);
        stack.Children.Add(_spaceAfterComma);

        // â”€â”€ Structure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        stack.Children.Add(MakeSectionHeader("STRUCTURE"));

        _indentCaseLabels = MakeThreeStateCheckBox("Indent case/when labels inside switch",
            "Add one indent level to case and default labels",
            "indentCaseLabels", "Indent case labels");

        _organizeImports = MakeThreeStateCheckBox("Organize imports on format",
            "Sort using/import directives alphabetically when formatting",
            "organizeImports", "Organize imports");

        stack.Children.Add(_indentCaseLabels);
        stack.Children.Add(_organizeImports);

        // â”€â”€ Smart Editing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        stack.Children.Add(MakeSectionHeader("SMART EDITING"));

        _autoBrackets  = MakeCheckBox("Auto-close brackets, braces and parentheses", false, null, null);
        _autoQuotes    = MakeCheckBox("Auto-close quotes", false, null, null);
        _skipOverClose = MakeCheckBox("Skip over closing character (avoids duplicate closing bracket/quote)", false, null, null);
        _wrapSelection = MakeCheckBox("Wrap selection in pairs (type opening bracket/quote over a selection)", false, null, null);

        stack.Children.Add(_autoBrackets);
        stack.Children.Add(_autoQuotes);
        stack.Children.Add(_skipOverClose);
        stack.Children.Add(_wrapSelection);

        leftScroll.Content = stack;
        root.Children.Add(leftScroll);

        // â”€â”€ Right: preview panel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (_colorizer is not null)
        {
            _preview = new FormattingPreviewPanel();
            Grid.SetColumn(_preview, 2);
            root.Children.Add(_preview);

            // Initialize with all languages that have a previewSnippet or previewSamples
            var langs = LanguageRegistry.Instance.AllLanguages()
                .Where(l => l.PreviewSnippet is not null || l.PreviewSamples.Count > 0)
                .OrderBy(l => l.Name)
                .ToList();
            _preview.Initialize(langs, _colorizer, formatter);
        }
        else
        {
            // Placeholder when no colorizer is available
            var placeholder = new TextBlock
            {
                Text              = "Preview not available\n(colorizer not injected)",
                TextWrapping      = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                FontStyle         = FontStyles.Italic,
                Opacity           = 0.45,
                FontSize          = 11,
                Margin            = new Thickness(16),
            };
            placeholder.SetResourceReference(TextBlock.ForegroundProperty, "DockMenuForegroundBrush");
            Grid.SetColumn(placeholder, 2);
            root.Children.Add(placeholder);
        }

        Content = root;
    }

    // â”€â”€ IOptionsPage â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void Load(AppSettings s)
    {
        _loading = true;
        try
        {
            var ce = s.CodeEditorDefaults;
            _formatOnSave.IsChecked         = ce.FormatOnSave;
            _trimTrailing.IsChecked         = ce.TrimTrailingWhitespace;
            _insertFinalNewline.IsChecked   = ce.InsertFinalNewline;
            _spaceAfterKeywords.IsChecked   = ce.SpaceAfterKeywords;
            _spaceAroundOperators.IsChecked = ce.SpaceAroundBinaryOperators;
            _spaceAfterComma.IsChecked      = ce.SpaceAfterComma;
            _indentCaseLabels.IsChecked     = ce.IndentCaseLabels;
            _organizeImports.IsChecked      = ce.OrganizeImports;
            _autoBrackets.IsChecked         = ce.AutoClosingBrackets;
            _autoQuotes.IsChecked           = ce.AutoClosingQuotes;
            _skipOverClose.IsChecked        = ce.SkipOverClosingChar;
            _wrapSelection.IsChecked        = ce.WrapSelectionInPairs;
        }
        finally { _loading = false; }

        _preview?.Refresh(BuildOverrides());
    }

    public void Flush(AppSettings s)
    {
        var ce = s.CodeEditorDefaults;
        ce.FormatOnSave               = _formatOnSave.IsChecked == true;
        ce.TrimTrailingWhitespace     = _trimTrailing.IsChecked;
        ce.InsertFinalNewline         = _insertFinalNewline.IsChecked;
        ce.SpaceAfterKeywords         = _spaceAfterKeywords.IsChecked;
        ce.SpaceAroundBinaryOperators = _spaceAroundOperators.IsChecked;
        ce.SpaceAfterComma            = _spaceAfterComma.IsChecked;
        ce.IndentCaseLabels           = _indentCaseLabels.IsChecked;
        ce.OrganizeImports            = _organizeImports.IsChecked;
        ce.AutoClosingBrackets        = _autoBrackets.IsChecked  == true;
        ce.AutoClosingQuotes          = _autoQuotes.IsChecked    == true;
        ce.SkipOverClosingChar        = _skipOverClose.IsChecked == true;
        ce.WrapSelectionInPairs       = _wrapSelection.IsChecked == true;
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnChanged(object? sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Changed?.Invoke(this, EventArgs.Empty);
        _preview?.Refresh(BuildOverrides());
    }

    private FormattingOverrides BuildOverrides() => new()
    {
        TrimTrailingWhitespace     = _trimTrailing.IsChecked,
        InsertFinalNewline         = _insertFinalNewline.IsChecked,
        SpaceAfterKeywords         = _spaceAfterKeywords.IsChecked,
        SpaceAroundBinaryOperators = _spaceAroundOperators.IsChecked,
        SpaceAfterComma            = _spaceAfterComma.IsChecked,
        IndentCaseLabels           = _indentCaseLabels.IsChecked,
        OrganizeImports            = _organizeImports.IsChecked,
    };

    private CheckBox MakeCheckBox(string label, bool isThreeState,
                                  string? ruleId, string? ruleDisplayName)
    {
        var cb = new CheckBox
        {
            Content      = label,
            IsThreeState = isThreeState,
            Margin       = new Thickness(0, 4, 0, 4),
        };
        cb.Checked       += OnChanged;
        cb.Unchecked     += OnChanged;
        if (isThreeState)
            cb.Indeterminate += OnChanged;

        // Wire tooltip if this checkbox maps to a formatting rule
        if (ruleId is not null && ruleDisplayName is not null && _colorizer is not null)
            WireTooltip(cb, ruleId, ruleDisplayName);

        return cb;
    }

    private CheckBox MakeThreeStateCheckBox(string label, string tooltip,
                                             string ruleId, string ruleDisplayName)
    {
        var cb = new CheckBox
        {
            Content      = label,
            IsThreeState = true,
            ToolTip      = tooltip + "\nIndeterminate = use .whfmt language default.",
            Margin       = new Thickness(0, 4, 0, 4),
        };
        cb.Checked       += OnChanged;
        cb.Unchecked     += OnChanged;
        cb.Indeterminate += OnChanged;

        if (_colorizer is not null)
            WireTooltip(cb, ruleId, ruleDisplayName);

        return cb;
    }

    /// <summary>
    /// Attaches a <see cref="FormattingRuleTooltip"/> shown in a Popup on MouseEnter.
    /// The tooltip is lazy-created and refreshed with the current language on each show.
    /// </summary>
    private void WireTooltip(CheckBox cb, string ruleId, string displayName)
    {
        _ruleMap.Add((cb, ruleId, displayName));

        cb.MouseEnter += (_, _) =>
        {
            if (_colorizer is null) return;

            // Lazy-create the tooltip for this rule
            if (!_tooltips.TryGetValue(ruleId, out var tip))
            {
                tip = new FormattingRuleTooltip(ruleId, displayName);
                _tooltips[ruleId] = tip;
            }

            // Resolve the current language from the preview panel's selection
            var langId  = _preview?.SelectedLanguageId;
            var langDef = langId is not null
                ? LanguageRegistry.Instance.FindById(langId)
                : null;

            tip.Refresh(langDef, _colorizer);

            // Show in a Popup anchored to the checkbox
            cb.ToolTip = tip;
            ToolTipService.SetShowDuration(cb, 30_000);
            ToolTipService.SetInitialShowDelay(cb, 300);
        };
    }

    private static TextBlock MakeSectionHeader(string text)
    {
        var tb = new TextBlock
        {
            Text       = text,
            FontSize   = 10,
            FontWeight = FontWeights.Bold,
            Margin     = new Thickness(0, 14, 0, 4),
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "CP_SecondaryTextBrush");
        return tb;
    }
}
