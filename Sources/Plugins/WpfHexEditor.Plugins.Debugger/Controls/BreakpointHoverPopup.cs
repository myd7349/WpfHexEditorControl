// ==========================================================
// Project: WpfHexEditor.Plugins.Debugger
// File: Controls/BreakpointHoverPopup.cs
// Description:
//     VS-style hover popup shown when the user hovers a row in the BreakpointsPanel.
//     Layout: header / syntax-colored code preview / context-menu action list.
// Architecture:
//     Derives from Popup. All colours via SetResourceReference (ET_* tokens).
//     Syntax tokens use fixed VS Dark–inspired colors (same pattern as EndBlockHintPopup).
//     Grace-timer pattern (400 ms) for comfortable list hover interaction.
//     GoToSourceRequested / EditConditionRequested events delegate navigation to the panel.
// ==========================================================

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WpfHexEditor.Core.Debugger.Models;
using WpfHexEditor.Plugins.Debugger.Dialogs;
using WpfHexEditor.Plugins.Debugger.ViewModels;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.Plugins.Debugger.Controls;

/// <summary>
/// VS-style hover popup for inspecting and acting on a breakpoint row in the BreakpointExplorerPanel.
/// </summary>
internal sealed class BreakpointHoverPopup : Popup
{
    // ── Grace-timer ───────────────────────────────────────────────────────────

    private readonly DispatcherTimer _graceTimer;
    private bool _mouseInsidePopup;

    // ── Visual tree (dynamic) ─────────────────────────────────────────────────

    private readonly TextBlock   _locationText;
    private readonly StackPanel  _codePreviewStack;
    private readonly Border      _codePreviewBorder;
    private readonly Border      _codePreviewSep;
    private readonly TextBlock   _enableIconTb;
    private readonly TextBlock   _enableLabelTb;

    // ── State ─────────────────────────────────────────────────────────────────

    private IDebuggerService? _svc;
    private string            _filePath  = string.Empty;
    private int               _line;
    private bool              _isEnabled;
    private string            _condition = string.Empty;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when "Go to Source" is clicked. Args: filePath, lineNumber.</summary>
    internal event Action<string, int>? GoToSourceRequested;

    /// <summary>Fired when "Edit Condition" is clicked. Args: filePath, lineNumber.</summary>
    internal event Action<string, int>? EditConditionRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    internal BreakpointHoverPopup()
    {
        StaysOpen          = true;
        AllowsTransparency = true;
        Placement          = PlacementMode.Mouse;

        _graceTimer       = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _graceTimer.Tick += (_, _) => { _graceTimer.Stop(); IsOpen = false; };

        // ── Header ────────────────────────────────────────────────────────────
        var bpDot = new TextBlock
        {
            Text              = "\uEA39",
            FontFamily        = new FontFamily("Segoe MDL2 Assets"),
            FontSize          = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 5, 0),
        };
        bpDot.SetResourceReference(TextBlock.ForegroundProperty, "ET_AccentBrush");

        var titleText = new TextBlock
        {
            Text              = "Breakpoint",
            FontWeight        = FontWeights.SemiBold,
            FontSize          = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "ET_HeaderForeground");

        _locationText = new TextBlock
        {
            FontSize          = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(6, 0, 0, 0),
        };
        _locationText.SetResourceReference(TextBlock.ForegroundProperty, "ET_MetaForeground");

        var closeBtn = new TextBlock
        {
            Text              = "\uE711",
            FontFamily        = new FontFamily("Segoe MDL2 Assets"),
            FontSize          = 10,
            Cursor            = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(8, 0, 0, 0),
        };
        closeBtn.SetResourceReference(TextBlock.ForegroundProperty, "ET_MetaForeground");
        closeBtn.MouseLeftButtonUp += (_, _) => IsOpen = false;

        var headerStack = new StackPanel
        {
            Orientation       = Orientation.Horizontal,
            Margin            = new Thickness(10, 7, 10, 5),
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerStack.Children.Add(bpDot);
        headerStack.Children.Add(titleText);
        headerStack.Children.Add(_locationText);
        headerStack.Children.Add(new Border { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 20 });
        headerStack.Children.Add(closeBtn);

        var headerBorder = new Border { Padding = new Thickness(0) };
        headerBorder.SetResourceReference(Border.BackgroundProperty, "ET_HeaderBackground");
        headerBorder.Child = headerStack;

        // ── Separator ─────────────────────────────────────────────────────────
        var sep1 = new Border { Height = 1 };
        sep1.SetResourceReference(Border.BackgroundProperty, "ET_PopupBorderBrush");

        // ── Code preview (populated dynamically in Show()) ────────────────────
        _codePreviewStack  = new StackPanel { Margin = new Thickness(0) };
        _codePreviewBorder = new Border
        {
            CornerRadius = new CornerRadius(3),
            Padding      = new Thickness(8, 5, 8, 5),
            Margin       = new Thickness(8, 6, 8, 2),
            Visibility   = Visibility.Collapsed,
            Child        = _codePreviewStack,
        };
        _codePreviewBorder.SetResourceReference(Border.BackgroundProperty, "ET_HeaderBackground");

        _codePreviewSep = new Border { Height = 1, Visibility = Visibility.Collapsed };
        _codePreviewSep.SetResourceReference(Border.BackgroundProperty, "ET_PopupBorderBrush");

        // ── Context menu list ─────────────────────────────────────────────────
        _enableIconTb  = new TextBlock { FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12, Width = 20 };
        _enableLabelTb = new TextBlock { FontSize = 11, Margin = new Thickness(4, 0, 0, 0) };
        _enableIconTb.SetResourceReference(TextBlock.ForegroundProperty, "ET_HeaderForeground");
        _enableLabelTb.SetResourceReference(TextBlock.ForegroundProperty, "ET_HeaderForeground");

        var menuPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 2, 0, 4) };
        menuPanel.Children.Add(MakeMenuItem("\uE8B0", "Go to Source",    OnMenuGoToSource));
        menuPanel.Children.Add(MakeMenuSeparator());
        menuPanel.Children.Add(MakeMenuItemRaw(_enableIconTb, _enableLabelTb, OnMenuToggleEnabled));
        menuPanel.Children.Add(MakeMenuItem("\uE8AC", "Edit Condition",  OnMenuEditCondition));
        menuPanel.Children.Add(MakeMenuSeparator());
        menuPanel.Children.Add(MakeMenuItem("\uE8C8", "Copy Location",   OnMenuCopyLocation));
        menuPanel.Children.Add(MakeMenuSeparator());
        menuPanel.Children.Add(MakeMenuItem("\uE74D", "Delete Breakpoint", OnMenuDelete, isDestructive: true));

        var menuBorder = new Border();
        menuBorder.SetResourceReference(Border.BackgroundProperty, "ET_PopupBackground");
        menuBorder.Child = menuPanel;

        // ── Outer stack ───────────────────────────────────────────────────────
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(headerBorder);
        stack.Children.Add(sep1);
        stack.Children.Add(_codePreviewBorder);
        stack.Children.Add(_codePreviewSep);
        stack.Children.Add(menuBorder);

        // ── Outer border ──────────────────────────────────────────────────────
        var outerBorder = new Border
        {
            CornerRadius    = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            MinWidth        = 220,
            MaxWidth        = 480,
            Child           = stack,
            Effect = new DropShadowEffect
            {
                BlurRadius  = 8,
                ShadowDepth = 2,
                Opacity     = 0.35,
                Color       = Colors.Black,
            },
        };
        outerBorder.SetResourceReference(Border.BackgroundProperty,  "ET_PopupBackground");
        outerBorder.SetResourceReference(Border.BorderBrushProperty, "ET_PopupBorderBrush");

        Child = outerBorder;

        outerBorder.MouseEnter += (_, _) => { _mouseInsidePopup = true;  _graceTimer.Stop(); };
        outerBorder.MouseLeave += (_, _) => { _mouseInsidePopup = false; _graceTimer.Start(); };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { IsOpen = false; e.Handled = true; }
        };

        if (Application.Current is not null)
            Application.Current.Deactivated += OnApplicationDeactivated;
    }

    private void OnApplicationDeactivated(object? sender, EventArgs e)
        => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => IsOpen = false));

    // ── Code preview with syntax coloring ────────────────────────────────────

    private void PopulateCodePreview(string filePath, int line1)
    {
        _codePreviewStack.Children.Clear();

        string[] fileLines;
        try { fileLines = File.ReadAllLines(filePath); }
        catch { fileLines = []; }

        if (fileLines.Length == 0 || line1 < 1 || line1 > fileLines.Length)
        {
            _codePreviewBorder.Visibility = Visibility.Collapsed;
            _codePreviewSep.Visibility    = Visibility.Collapsed;
            return;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        int startLine = Math.Max(0, line1 - 2);
        int endLine   = Math.Min(fileLines.Length - 1, line1 - 1);

        for (int i = startLine; i <= endLine; i++)
        {
            bool isBpLine = i == line1 - 1;
            var tb = new TextBlock
            {
                FontFamily   = new FontFamily("Cascadia Code, Consolas, Courier New"),
                FontSize     = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight   = isBpLine ? FontWeights.SemiBold : FontWeights.Normal,
            };

            foreach (var (text, color, useAccent) in BasicSyntaxColorizer.Tokenize(fileLines[i], ext))
            {
                var run = new Run(text);
                if (useAccent)
                    run.SetResourceReference(TextElement.ForegroundProperty, "ET_HeaderForeground");
                else
                    run.Foreground = new SolidColorBrush(color);
                tb.Inlines.Add(run);
            }

            _codePreviewStack.Children.Add(tb);
        }

        _codePreviewBorder.Visibility = Visibility.Visible;
        _codePreviewSep.Visibility    = Visibility.Visible;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    internal void Show(BreakpointRowEx row, IDebuggerService svc)
    {
        _svc       = svc;
        _filePath  = row.FilePath;
        _line      = row.Line;
        _isEnabled = row.IsEnabled;
        _condition = row.Condition ?? string.Empty;

        _graceTimer.Stop();
        _mouseInsidePopup = false;

        _locationText.Text  = $"  ·  {Path.GetFileName(row.FilePath)} : {row.Line}";
        _enableIconTb.Text  = row.IsEnabled ? "\uE73E" : "\uE73F";
        _enableLabelTb.Text = row.IsEnabled ? "Disable" : "Enable";

        PopulateCodePreview(row.FilePath, row.Line);

        IsOpen = true;
    }

    internal void OnHostMouseLeft()
    {
        if (!IsOpen || _mouseInsidePopup) return;
        _graceTimer.Stop();
        _graceTimer.Start();
    }

    internal void Dispose()
    {
        if (Application.Current is not null)
            Application.Current.Deactivated -= OnApplicationDeactivated;
    }

    // ── Menu action handlers ──────────────────────────────────────────────────

    private void OnMenuGoToSource(object sender, RoutedEventArgs e)
    {
        IsOpen = false;
        GoToSourceRequested?.Invoke(_filePath, _line);
    }

    private void OnMenuToggleEnabled(object sender, RoutedEventArgs e)
    {
        if (_svc is null) return;
        _isEnabled = !_isEnabled;
        _ = _svc.UpdateBreakpointAsync(_filePath, _line, _condition.Length == 0 ? null : _condition, _isEnabled);
        _enableIconTb.Text  = _isEnabled ? "\uE73E" : "\uE73F";
        _enableLabelTb.Text = _isEnabled ? "Disable" : "Enable";
    }

    private void OnMenuEditCondition(object sender, RoutedEventArgs e)
    {
        IsOpen = false;

        // Open BreakpointConditionDialog directly (same plugin assembly — no event relay needed).
        var owner = Application.Current?.MainWindow;
        if (owner is null || _svc is null) return;

        var bp = _svc.Breakpoints.FirstOrDefault(
            b => string.Equals(b.FilePath, _filePath, StringComparison.OrdinalIgnoreCase)
                 && b.Line == _line);
        if (bp is null) return;

        var loc = new BreakpointLocation
        {
            FilePath          = bp.FilePath,
            Line              = bp.Line,
            Condition         = bp.Condition ?? string.Empty,
            IsEnabled         = bp.IsEnabled,
            ConditionKind     = bp.ConditionKind,
            ConditionMode     = bp.ConditionMode,
            HitCountOp        = bp.HitCountOp,
            HitCountTarget    = bp.HitCountTarget,
            FilterExpr        = bp.FilterExpr,
            HasAction         = bp.HasAction,
            LogMessage        = bp.LogMessage,
            ContinueExecution = bp.ContinueExecution,
            DisableOnceHit    = bp.DisableOnceHit,
            DependsOnBpKey    = bp.DependsOnBpKey,
        };

        var allLocs = _svc.Breakpoints.Select(b => new BreakpointLocation
        {
            FilePath  = b.FilePath,
            Line      = b.Line,
            Condition = b.Condition ?? string.Empty,
            IsEnabled = b.IsEnabled,
        }).ToList();

        var result = BreakpointConditionDialog.Show(owner, loc, allLocs);
        if (result is not null)
            _ = _svc.UpdateBreakpointSettingsAsync(_filePath, _line, result);

        // Keep the legacy event in case external consumers still rely on it.
        EditConditionRequested?.Invoke(_filePath, _line);
    }

    private void OnMenuCopyLocation(object sender, RoutedEventArgs e)
        => Clipboard.SetText($"{_filePath} : {_line}");

    private void OnMenuDelete(object sender, RoutedEventArgs e)
    {
        if (_svc is null) return;
        _ = _svc.DeleteBreakpointAsync(_filePath, _line);
        IsOpen = false;
    }

    // ── Menu item helpers ─────────────────────────────────────────────────────

    private static FrameworkElement MakeMenuItem(string glyph, string label,
        RoutedEventHandler handler, bool isDestructive = false)
    {
        var iconTb = new TextBlock
        {
            Text       = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize   = 12,
            Width      = 20,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var labelTb = new TextBlock
        {
            Text   = label,
            FontSize = 11,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var fgKey = isDestructive ? "ET_AccentBrush" : "ET_HeaderForeground";
        iconTb.SetResourceReference(TextBlock.ForegroundProperty,  fgKey);
        labelTb.SetResourceReference(TextBlock.ForegroundProperty, fgKey);

        return MakeMenuItemRaw(iconTb, labelTb, handler);
    }

    private static FrameworkElement MakeMenuItemRaw(TextBlock iconTb, TextBlock labelTb,
        RoutedEventHandler handler)
    {
        var row = new StackPanel
        {
            Orientation       = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(iconTb);
        row.Children.Add(labelTb);

        var bd = new Border
        {
            Padding           = new Thickness(10, 5, 16, 5),
            Cursor            = Cursors.Hand,
            Background        = Brushes.Transparent,
            Child             = row,
        };
        bd.MouseEnter       += (_, _) => bd.SetResourceReference(Border.BackgroundProperty, "ET_HeaderBackground");
        bd.MouseLeave       += (_, _) => bd.Background = Brushes.Transparent;
        bd.MouseLeftButtonUp += (s, e) => handler(s, e);
        return bd;
    }

    private static Border MakeMenuSeparator()
    {
        var sep = new Border { Height = 1, Margin = new Thickness(10, 2, 10, 2) };
        sep.SetResourceReference(Border.BackgroundProperty, "ET_PopupBorderBrush");
        return sep;
    }

    // ── BasicSyntaxColorizer ──────────────────────────────────────────────────

    /// <summary>
    /// Minimal left-to-right tokenizer for C# / JS / TS / Python code lines.
    /// Returns (text, color, useResourceRef) tuples — useResourceRef=true means use ET_HeaderForeground.
    /// Fixed colors mirror VS Dark theme syntax palette.
    /// </summary>
    private static class BasicSyntaxColorizer
    {
        private static readonly Color KeywordColor = Color.FromRgb(0x56, 0x9C, 0xD6); // blue
        private static readonly Color StringColor  = Color.FromRgb(0xD6, 0x9D, 0x85); // orange
        private static readonly Color CommentColor = Color.FromRgb(0x57, 0xA6, 0x4A); // green

        private static readonly HashSet<string> CsKeywords =
        [
            "abstract","as","base","bool","break","byte","case","catch","char","checked",
            "class","const","continue","decimal","default","delegate","do","double","else",
            "enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
            "long","namespace","new","null","object","operator","out","override","params",
            "private","protected","public","readonly","ref","return","sbyte","sealed",
            "short","sizeof","stackalloc","static","string","struct","switch","this",
            "throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
            "using","virtual","void","volatile","while","var","async","await","record",
            "init","with","not","and","or","nint","nuint","global","file","required",
        ];

        private static readonly HashSet<string> JsKeywords =
        [
            "abstract","arguments","await","boolean","break","byte","case","catch","char",
            "class","const","continue","debugger","default","delete","do","double","else",
            "enum","eval","export","extends","false","final","finally","float","for",
            "function","goto","if","implements","import","in","instanceof","int","interface",
            "let","long","native","new","null","package","private","protected","public",
            "return","short","static","super","switch","synchronized","this","throw","throws",
            "transient","true","try","typeof","undefined","var","void","volatile","while",
            "with","yield","async","of","from","type","declare","readonly","namespace",
        ];

        private static readonly HashSet<string> PyKeywords =
        [
            "False","None","True","and","as","assert","async","await","break","class",
            "continue","def","del","elif","else","except","finally","for","from","global",
            "if","import","in","is","lambda","nonlocal","not","or","pass","raise","return",
            "try","while","with","yield","self","cls",
        ];

        // Returns (text, color, useResourceRef=true means ET_HeaderForeground, fixed color otherwise)
        internal static IEnumerable<(string text, Color color, bool useRef)> Tokenize(string line, string ext)
        {
            var keywords = ext switch
            {
                ".cs"             => CsKeywords,
                ".js" or ".ts"    => JsKeywords,
                ".jsx" or ".tsx"  => JsKeywords,
                ".py"             => PyKeywords,
                _                 => null,
            };

            if (keywords is null)
            {
                yield return (line, default, true);
                yield break;
            }

            // Detect line-comment prefix
            var commentPrefix = ext switch
            {
                ".py" => "#",
                _     => "//",
            };

            int pos = 0;
            int len = line.Length;
            var buf = new System.Text.StringBuilder();

            void Flush(bool useRef, Color c)
            {
                if (buf.Length > 0)
                {
                    // placeholder — yielded below via local list
                }
            }

            var result = new List<(string, Color, bool)>();

            while (pos < len)
            {
                // Line comment
                if (line.AsSpan(pos).StartsWith(commentPrefix))
                {
                    result.Add((line[pos..], CommentColor, false));
                    break;
                }

                // String / char literal
                char ch = line[pos];
                if (ch == '"' || ch == '\'' || ch == '`')
                {
                    char quote = ch;
                    int start  = pos++;
                    while (pos < len)
                    {
                        if (line[pos] == '\\') { pos += 2; continue; }
                        if (line[pos] == quote) { pos++; break; }
                        pos++;
                    }
                    result.Add((line[start..pos], StringColor, false));
                    continue;
                }

                // Identifier / keyword
                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = pos;
                    while (pos < len && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_'))
                        pos++;
                    var word = line[start..pos];
                    if (keywords.Contains(word))
                        result.Add((word, KeywordColor, false));
                    else
                        result.Add((word, default, true));
                    continue;
                }

                // Single character — flush as default
                result.Add((ch.ToString(), default, true));
                pos++;
            }

            foreach (var item in result)
                yield return item;
        }
    }
}
