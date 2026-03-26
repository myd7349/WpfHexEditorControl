// ==========================================================
// Project: WpfHexEditor.Plugins.ParsedFields
// File: ParsedFieldsPlugin.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6, Claude (Anthropic)
// Created: 2026-03-07
// Description:
//     Plugin entry point for the Parsed Fields panel.
//     Uses IFormatParsingService (editor-agnostic) when available,
//     falling back to IHexEditorService for backward compatibility.
//
// Architecture Notes:
//     Pattern: Observer + Mediator
//     - When IFormatParsingService is available: panel wiring, format detection,
//       field selection, and bookmark navigation all go through the shared service.
//     - Fallback: legacy IHexEditorService.ConnectParsedFieldsPanel path.
//     - TemplateApplyRequestedEvent and GrammarAppliedEvent always use EventBus.
// ==========================================================

using WpfHexEditor.SDK.Commands;
using WpfHexEditor.Core.FormatDetection;
using WpfHexEditor.Core.Interfaces;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.Plugins.ParsedFields.Views;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;
using WpfHexEditor.SDK.Descriptors;
using WpfHexEditor.SDK.Events;
using WpfHexEditor.SDK.Models;

namespace WpfHexEditor.Plugins.ParsedFields;

/// <summary>
/// Plugin registering the Parsed Fields panel (Right dock).
/// Prefers <see cref="IFormatParsingService"/> for editor-agnostic wiring;
/// falls back to <see cref="IHexEditorService"/> legacy path when unavailable.
/// </summary>
public sealed class ParsedFieldsPlugin : IWpfHexEditorPlugin
{
    public string  Id      => "WpfHexEditor.Plugins.ParsedFields";
    public string  Name    => "Parsed Fields";
    public Version Version => new(0, 6, 1);

    public PluginCapabilities Capabilities => new()
    {
        AccessHexEditor  = true,
        AccessFileSystem = false,
        RegisterMenus    = true,
        WriteOutput      = false
    };

    private ParsedFieldsPanel? _panel;
    private IIDEHostContext?   _context;
    private IDisposable?       _templateSub;
    private IDisposable?       _grammarSub;
    private bool               _usesFormatParsingService;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public Task InitializeAsync(IIDEHostContext context, CancellationToken ct = default)
    {
        _context = context;
        _panel   = new ParsedFieldsPanel();

        // Register the panel (Right dock — same side as before).
        context.UIRegistry.RegisterPanel(
            "WpfHexEditor.Plugins.ParsedFields.Panel.ParsedFieldsPanel",
            _panel,
            Id,
            new PanelDescriptor
            {
                Title           = "Parsed Fields",
                DefaultDockSide = "Right",
                DefaultAutoHide = false,
                CanClose        = true,
                PreferredWidth  = 340
            });

        // Register View menu item so the user can show/hide this panel.
        context.UIRegistry.RegisterMenuItem(
            $"{Id}.Menu.Show",
            Id,
            new MenuItemDescriptor
            {
                Header     = "_Parsed Fields",
                ParentPath = "View",
                Group      = "Analysis",
                IconGlyph  = "\uE81E",
                Command    = new RelayCommand(_ => context.UIRegistry.ShowPanel(
                                 "WpfHexEditor.Plugins.ParsedFields.Panel.ParsedFieldsPanel"))
            });

        // ── Prefer IFormatParsingService (editor-agnostic) ───────────────
        var formatParsing = context.FormatParsing;
        if (formatParsing != null)
        {
            _usesFormatParsingService = true;

            // Connect panel to the shared service
            formatParsing.ConnectPanel(_panel);

            // Format detection events come from the service
            formatParsing.FormatDetected += OnServiceFormatDetected;
            formatParsing.Cleared += OnServiceCleared;
        }
        else
        {
            // ── Fallback: legacy IHexEditorService path ──────────────────
            _usesFormatParsingService = false;

            if (context.HexEditor.IsActive)
                context.HexEditor.ConnectParsedFieldsPanel(_panel);

            context.HexEditor.ActiveEditorChanged += OnActiveEditorChanged;
            context.HexEditor.FileOpened          += OnFileOpened;
            context.HexEditor.FormatDetected      += OnFormatDetected;
        }

        // Wire bookmark navigation (works with both paths).
        _panel.NavigateToOffsetRequested += OnNavigateToOffsetRequested;

        // Route TemplateApplyRequestedEvent to this panel (was handled by MainWindow).
        _templateSub = context.EventBus.Subscribe<TemplateApplyRequestedEvent>(OnTemplateApplyRequested);

        // Route GrammarAppliedEvent from SynalysisGrammarPlugin (issue #177).
        _grammarSub = context.EventBus.Subscribe<GrammarAppliedEvent>(OnGrammarApplied);

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _templateSub?.Dispose();
        _grammarSub?.Dispose();

        if (_context != null)
        {
            if (_usesFormatParsingService && _context.FormatParsing != null)
            {
                _context.FormatParsing.FormatDetected -= OnServiceFormatDetected;
                _context.FormatParsing.Cleared -= OnServiceCleared;
                _context.FormatParsing.DisconnectPanel();
            }
            else
            {
                _context.HexEditor.ActiveEditorChanged -= OnActiveEditorChanged;
                _context.HexEditor.FileOpened          -= OnFileOpened;
                _context.HexEditor.FormatDetected      -= OnFormatDetected;
                _context.HexEditor.DisconnectParsedFieldsPanel();
            }
        }

        if (_panel != null)
            _panel.NavigateToOffsetRequested -= OnNavigateToOffsetRequested;

        _panel   = null;
        _context = null;
        return Task.CompletedTask;
    }

    // ── IFormatParsingService event handlers ──────────────────────────────

    private void OnServiceFormatDetected(object? sender, WpfHexEditor.Core.Events.FormatDetectedEventArgs e)
    {
        if (_panel is null || !e.Success) return;
        _panel.Dispatcher.InvokeAsync(() => _panel.SetEnrichedFormat(e.Format));
    }

    private void OnServiceCleared(object? sender, EventArgs e)
    {
        _panel?.Dispatcher.InvokeAsync(() => _panel?.Clear());
    }

    // ── Legacy IHexEditorService event handlers ──────────────────────────

    private void OnNavigateToOffsetRequested(object? sender, long e)
    {
        if (_context?.HexEditor.IsActive == true)
            _context.HexEditor.NavigateTo(e);
    }

    private void OnActiveEditorChanged(object? sender, EventArgs e)
    {
        if (_panel is null || _context is null) return;
        _context.HexEditor.DisconnectParsedFieldsPanel();
        if (_context.HexEditor.IsActive)
            _context.HexEditor.ConnectParsedFieldsPanel(_panel);
    }

    private void OnFileOpened(object? sender, EventArgs e)
    {
        _panel?.Clear();
    }

    private void OnFormatDetected(object? sender, FormatDetectedArgs e)
    {
        if (_panel is null) return;
        var format = e.RawFormatDefinition as FormatDefinition;
        _panel.Dispatcher.InvokeAsync(() => _panel.SetEnrichedFormat(format));
    }

    // ── EventBus handlers (shared by both paths) ─────────────────────────

    private void OnTemplateApplyRequested(TemplateApplyRequestedEvent evt)
    {
        if (_panel is null || _context?.HexEditor.IsActive != true) return;

        _panel.Dispatcher.InvokeAsync(() =>
        {
            _panel.Clear();
            _panel.TotalFileSize = _context.HexEditor.FileSize;

            foreach (var block in evt.Blocks)
            {
                _panel.ParsedFields.Add(new ParsedFieldViewModel
                {
                    Name           = block.Name,
                    Offset         = block.Offset,
                    Length         = block.Length,
                    ValueType      = block.TypeHint     ?? "Unknown",
                    FormattedValue = block.DisplayValue ?? string.Empty
                });
            }

            _panel.RefreshView();
        });
    }

    private void OnGrammarApplied(GrammarAppliedEvent evt)
    {
        if (_panel is null || _context?.HexEditor.IsActive != true) return;

        _panel.Dispatcher.InvokeAsync(() =>
        {
            _panel.Clear();
            _panel.TotalFileSize = _context.HexEditor.FileSize;

            foreach (var field in evt.Fields)
            {
                _panel.ParsedFields.Add(new ParsedFieldViewModel
                {
                    Name           = field.Name,
                    Offset         = field.Offset,
                    Length         = field.Length,
                    FormattedValue = field.ValueDisplay,
                    ValueType      = field.Kind.ToString(),
                    Description    = field.Description,
                    Color          = string.IsNullOrEmpty(field.Color) ? null
                                     : (field.Color.StartsWith('#') ? field.Color : "#" + field.Color),
                    IndentLevel    = field.IndentLevel,
                    GroupName      = field.GroupName,
                    IsValid        = field.IsValid,
                });
            }

            _panel.FormatInfo = new FormatInfo
            {
                IsDetected  = true,
                Name        = evt.GrammarName,
                Description = $"Parsed via UFWB grammar — {evt.Fields.Count} fields",
            };

            _panel.RefreshView();
        });
    }
}
