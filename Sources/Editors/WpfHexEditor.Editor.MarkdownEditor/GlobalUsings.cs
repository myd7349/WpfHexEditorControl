// ==========================================================
// Project: WpfHexEditor.Editor.MarkdownEditor
// File: GlobalUsings.cs
// Description:
//     Resolves type ambiguities introduced by UseWindowsForms + UseWPF
//     being active simultaneously. WPF types take precedence globally;
//     WinForms types are accessed via explicit aliases where needed
//     (e.g., WinFormsWebView2 in MarkdownPreviewPane.xaml.cs).
// ==========================================================

global using Brush               = System.Windows.Media.Brush;
global using Color               = System.Windows.Media.Color;
global using UserControl         = System.Windows.Controls.UserControl;
global using KeyEventArgs        = System.Windows.Input.KeyEventArgs;
global using Clipboard           = System.Windows.Clipboard;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
