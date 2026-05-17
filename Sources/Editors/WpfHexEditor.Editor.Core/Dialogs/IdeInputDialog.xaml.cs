// ==========================================================
// Project: WpfHexEditor.Editor.Core
// File: Dialogs/IdeInputDialog.xaml.cs
// Description:
//     Themed single-line input dialog. Drop-in replacement for
//     ad-hoc Window + TextBox patterns throughout the IDE.
//
// Architecture Notes:
//     Extends ThemedDialog (VS-style chrome, DynamicResource brushes).
//     Static Show() mirrors IdeMessageBox.Show() for easy call-site use.
//     Supports an optional icon glyph (Segoe MDL2 Assets), a watermark
//     placeholder, a secondary hint line, and a custom confirm label.
//     Lives in Editor.Core so all plugins/editors can reference it
//     without a dependency on WpfHexEditor.App.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHexEditor.Editor.Core.Views;

namespace WpfHexEditor.Editor.Core.Dialogs;

/// <summary>
/// Themed modal input dialog. Use <see cref="Show"/> instead of raw
/// <c>Window + TextBox</c> ad-hoc dialogs.
/// </summary>
public sealed partial class IdeInputDialog : ThemedDialog
{
    // -- Result -------------------------------------------------------------
    public string? Value { get; private set; }

    // -- Constructor --------------------------------------------------------
    private IdeInputDialog() => InitializeComponent();

    // -- Static entry points ------------------------------------------------

    /// <summary>
    /// Shows a themed input dialog. Must be called on the UI thread.
    /// </summary>
    /// <param name="prompt">Label shown above the text box.</param>
    /// <param name="title">Window title bar text.</param>
    /// <param name="defaultValue">Pre-filled value in the text box.</param>
    /// <param name="placeholder">Watermark hint shown when the box is empty.</param>
    /// <param name="hint">Secondary line below the text box (e.g. "Powered by Claude API").</param>
    /// <param name="confirmLabel">Label for the primary button (default: "OK").</param>
    /// <param name="iconGlyph">Optional Segoe MDL2 Assets glyph char for the prompt icon.</param>
    /// <param name="iconColor">Brush for the icon glyph; ignored when iconGlyph is null.</param>
    /// <param name="owner">Owner window; falls back to Application.MainWindow.</param>
    /// <returns>The entered string, or <c>null</c> if the user cancelled.</returns>
    public static string? Show(
        string   prompt,
        string   title        = "",
        string   defaultValue = "",
        string?  placeholder  = null,
        string?  hint         = null,
        string   confirmLabel = "OK",
        string?  iconGlyph    = null,
        Brush?   iconColor    = null,
        Window?  owner        = null)
    {
        var dlg = Build(prompt, title, defaultValue, placeholder, hint, confirmLabel, iconGlyph, iconColor, owner);
        dlg.ShowDialog();
        return dlg.Value;
    }

    // -- Builder ------------------------------------------------------------

    private static IdeInputDialog Build(
        string   prompt,
        string   title,
        string   defaultValue,
        string?  placeholder,
        string?  hint,
        string   confirmLabel,
        string?  iconGlyph,
        Brush?   iconColor,
        Window?  owner)
    {
        var dlg = new IdeInputDialog
        {
            Title = title,
            Owner = owner ?? TryGetMainWindow(),
        };

        dlg.PromptText.Text = prompt;
        dlg.InputBox.Text   = defaultValue;
        dlg.InputBox.SelectAll();

        if (iconGlyph is not null)
        {
            dlg.IconGlyph.Text       = iconGlyph;
            dlg.IconGlyph.Foreground = iconColor ?? Brushes.White;
            dlg.IconGlyph.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(placeholder))
        {
            dlg.WatermarkText.Text       = placeholder;
            dlg.WatermarkText.Visibility = string.IsNullOrEmpty(defaultValue)
                ? Visibility.Visible
                : Visibility.Collapsed;

            dlg.InputBox.TextChanged += (_, _) =>
                dlg.WatermarkText.Visibility = string.IsNullOrEmpty(dlg.InputBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        if (!string.IsNullOrEmpty(hint))
        {
            dlg.HintText.Text       = hint;
            dlg.HintText.Visibility = Visibility.Visible;
        }

        dlg.AddButton(confirmLabel, isPrimary: true,  isDefault: true,  isCancel: false, onConfirm: () => dlg.Value = dlg.InputBox.Text);
        dlg.AddButton("Cancel",    isPrimary: false, isDefault: false, isCancel: true,  onConfirm: null);

        dlg.Loaded += (_, _) =>
        {
            dlg.InputBox.Focus();
            dlg.InputBox.SelectAll();
            dlg.InputBorder.BorderBrush = (Brush?)dlg.TryFindResource("DockBorderBrush")
                                          ?? new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        };

        dlg.InputBox.GotFocus  += (_, _) => dlg.InputBorder.BorderBrush =
            new SolidColorBrush(Color.FromRgb(0x6B, 0x3F, 0xA0));
        dlg.InputBox.LostFocus += (_, _) => dlg.InputBorder.BorderBrush =
            (Brush?)dlg.TryFindResource("DockBorderBrush")
            ?? new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));

        return dlg;
    }

    private static Window? TryGetMainWindow()
    {
        try { return Application.Current?.MainWindow; }
        catch { return null; }
    }

    // -- Buttons ------------------------------------------------------------

    private void AddButton(string label, bool isPrimary, bool isDefault, bool isCancel = false, Action? onConfirm = null)
    {
        var btn = new Button
        {
            Content   = label,
            IsDefault = isDefault,
            IsCancel  = isCancel,
        };

        if (isPrimary)
        {
            btn.Background  = new SolidColorBrush(Color.FromRgb(0x6B, 0x3F, 0xA0));
            btn.Foreground  = Brushes.White;
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x7B, 0x4F, 0xB0));
        }

        btn.Click += (_, _) =>
        {
            onConfirm?.Invoke();
            DialogResult = isPrimary;
            Close();
        };

        ButtonPanel.Children.Add(btn);
    }
}
