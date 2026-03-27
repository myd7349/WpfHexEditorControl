//////////////////////////////////////////////
// Project: WpfHexEditor.App
// File: Services/EditorSettingsService.cs
// Description:
//     Applies per-editor-type user settings to newly created editor instances.
//     Extracted from MainWindow.ApplyEditorSettings to reduce code-behind.
// Architecture:
//     Static utility — reads from AppSettingsService, applies to IDocumentEditor.
//////////////////////////////////////////////

using WpfHexEditor.Core.Options;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.Core;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Applies user settings (scroll speed, zoom, syntax colors, etc.) to editor instances.
/// </summary>
internal static class EditorSettingsService
{
    /// <summary>
    /// Applies per-editor-type user settings to a newly created editor.
    /// Called once per editor instance from CreateSmartFileEditorContent.
    /// </summary>
    public static void Apply(IDocumentEditor editor)
    {
        var settings = AppSettingsService.Instance.Current;

        switch (editor)
        {
            case WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor ce:
                ApplyCodeEditor(ce, settings.CodeEditorDefaults);
                break;
            case WpfHexEditor.Editor.TextEditor.Controls.TextEditor te:
                te.MouseWheelSpeed   = settings.TextEditorDefaults.MouseWheelSpeed;
                te.ZoomLevel         = settings.TextEditorDefaults.DefaultZoom;
                te.IsWordWrapEnabled = settings.TextEditorDefaults.WordWrap;
                if (te.RefreshTimeStatusBarItem is { } teRt)
                    teRt.IsVisible = settings.TextEditorDefaults.ShowRefreshRateInStatusBar;
                break;
            case WpfHexEditor.HexEditor.HexEditor hexEd:
                hexEd.ShowRefreshTimeInStatusBar = settings.HexEditorDefaults.ShowRefreshRateInStatusBar;
                break;
        }

        // CodeEditorSplitHost wraps two CodeEditor instances
        if (editor is WpfHexEditor.Editor.CodeEditor.Controls.CodeEditorSplitHost splitHost)
        {
            if (splitHost.RefreshTimeStatusBarItem is { } shRt)
                shRt.IsVisible = settings.CodeEditorDefaults.ShowRefreshRateInStatusBar;
            splitHost.ShowMinimap = settings.CodeEditorDefaults.ShowMinimap;
            splitHost.MinimapRenderCharacters = settings.CodeEditorDefaults.MinimapRenderCharacters;
            splitHost.MinimapVerticalSize = (WpfHexEditor.Editor.CodeEditor.Controls.MinimapVerticalSize)settings.CodeEditorDefaults.MinimapVerticalSize;
            splitHost.MinimapSliderMode = (WpfHexEditor.Editor.CodeEditor.Controls.MinimapSliderMode)settings.CodeEditorDefaults.MinimapSliderMode;
            splitHost.MinimapSide = (WpfHexEditor.Editor.CodeEditor.Controls.MinimapSide)settings.CodeEditorDefaults.MinimapSide;
        }
    }

    /// <summary>
    /// Applies all HexEditor-specific default settings.
    /// </summary>
    public static void ApplyHexDefaults(WpfHexEditor.HexEditor.HexEditor hex)
    {
        var d = AppSettingsService.Instance.Current.HexEditorDefaults;

        hex.BytePerLine           = d.BytePerLine;
        hex.ShowOffset            = d.ShowOffset;
        hex.ShowAscii             = d.ShowAscii;
        hex.DataStringVisual      = d.DataStringVisual;
        hex.OffSetStringVisual    = d.OffSetStringVisual;
        hex.ByteGrouping          = d.ByteGrouping;
        hex.ByteSpacerPositioning = d.ByteSpacerPositioning;

        hex.EditMode              = d.DefaultEditMode;
        hex.AllowZoom             = d.AllowZoom;
        hex.MouseWheelSpeed       = d.MouseWheelSpeed;
        hex.AllowFileDrop         = d.AllowFileDrop;

        hex.ByteSize                    = d.ByteSize;
        hex.ByteOrder                   = d.ByteOrder;
        hex.DefaultCopyToClipboardMode  = d.DefaultCopyToClipboardMode;
        hex.ByteSpacerVisualStyle       = d.ByteSpacerVisualStyle;

        hex.ShowBookmarkMarkers     = d.ShowBookmarkMarkers;
        hex.ShowModifiedMarkers     = d.ShowModifiedMarkers;
        hex.ShowInsertedMarkers     = d.ShowInsertedMarkers;
        hex.ShowDeletedMarkers      = d.ShowDeletedMarkers;
        hex.ShowSearchResultMarkers = d.ShowSearchResultMarkers;

        hex.ShowRefreshTimeInStatusBar = d.ShowRefreshRateInStatusBar;
        hex.ShowStatusMessage          = d.ShowStatusMessage;
        hex.ShowFileSizeInStatusBar    = d.ShowFileSizeInStatusBar;
        hex.ShowSelectionInStatusBar   = d.ShowSelectionInStatusBar;
        hex.ShowPositionInStatusBar    = d.ShowPositionInStatusBar;
        hex.ShowEditModeInStatusBar    = d.ShowEditModeInStatusBar;
        hex.ShowBytesPerLineInStatusBar = d.ShowBytesPerLineInStatusBar;

        hex.AllowAutoHighLightSelectionByte      = d.AllowAutoHighLightSelectionByte;
        hex.AllowAutoSelectSameByteAtDoubleClick = d.AllowAutoSelectSameByteAtDoubleClick;
        hex.AllowContextMenu                     = d.AllowContextMenu;
        hex.AllowDeleteByte                      = d.AllowDeleteByte;
        hex.AllowExtend                          = d.AllowExtend;
        hex.FileDroppingConfirmation             = d.FileDroppingConfirmation;
        hex.PreloadByteInEditorMode              = d.PreloadByteInEditorMode;

        hex.ByteToolTipDisplayMode               = d.ByteToolTipDisplayMode;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ApplyCodeEditor(
        WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor ce,
        CodeEditorDefaultSettings s)
    {
        ce.MouseWheelSpeed         = s.MouseWheelSpeed;
        ce.ZoomLevel               = s.DefaultZoom;
        ce.FoldToggleOnDoubleClick = s.FoldToggleOnDoubleClick;
        ce.IsWordWrapEnabled       = s.WordWrap;
        ce.ShowInlineHints         = s.ShowInlineHints;
        ce.InlineHintsVisibleKinds = s.InlineHintsVisibleKinds == 0
            ? WpfHexEditor.Editor.Core.InlineHintsSymbolKinds.All
            : (WpfHexEditor.Editor.Core.InlineHintsSymbolKinds)s.InlineHintsVisibleKinds;
        ce.ShowEndOfBlockHint          = s.EndOfBlockHintEnabled;
        ce.ShowBreakpointLineHighlight = s.BreakpointLineHighlightEnabled;
        ce.EndOfBlockHintDelayMs       = s.EndOfBlockHintDelayMs;
        ce.EnableAutoClosingBrackets = s.AutoClosingBrackets;
        ce.EnableAutoClosingQuotes   = s.AutoClosingQuotes;
        ce.SkipOverClosingChar       = s.SkipOverClosingChar;
        ce.WrapSelectionInPairs      = s.WrapSelectionInPairs;

        var ss = s.StickyScroll;
        ce.ApplyStickyScrollSettings(
            ss.Enabled, ss.MaxLines, ss.SyntaxHighlight,
            ss.ClickToNavigate, ss.Opacity, ss.MinScopeLines);

        ApplySyntaxColorOverrides(ce, s);

        if (ce.RefreshTimeStatusBarItem is { } rt)
            rt.IsVisible = s.ShowRefreshRateInStatusBar;
    }

    private static void ApplySyntaxColorOverrides(
        WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor editor,
        CodeEditorDefaultSettings ce)
    {
        Apply(SyntaxTokenKind.Keyword,    ce.KeywordColor);
        Apply(SyntaxTokenKind.String,     ce.StringColor);
        Apply(SyntaxTokenKind.Number,     ce.NumberColor);
        Apply(SyntaxTokenKind.Comment,    ce.CommentColor);
        Apply(SyntaxTokenKind.Type,       ce.TypeColor);
        Apply(SyntaxTokenKind.Identifier, ce.IdentifierColor);
        Apply(SyntaxTokenKind.Operator,   ce.OperatorColor);
        Apply(SyntaxTokenKind.Bracket,    ce.BracketColor);
        Apply(SyntaxTokenKind.Attribute,  ce.AttributeColor);

        void Apply(SyntaxTokenKind k, string? hex)
            => editor.SetSyntaxColorOverride(k, TryParseHexColor(hex, out var c) ? c : null);
    }

    private static bool TryParseHexColor(string? hex, out System.Windows.Media.Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        try
        {
            color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex.Trim());
            return true;
        }
        catch { return false; }
    }
}
