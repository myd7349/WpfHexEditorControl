// Project      : WpfHexEditorControl
// File         : Views/Dialogs/ArchivePropertiesDialog.xaml.cs
// Description  : Properties dialog showing metadata for one archive entry.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Windows;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Views.Dialogs;

public partial class ArchivePropertiesDialog : WpfHexEditor.Editor.Core.Views.ThemedDialog
{
    public ArchivePropertiesDialog(ArchiveNode node)
    {
        InitializeComponent();

        EntryNameText.Text  = node.Name;
        FullPathText.Text   = node.FullPath;
        SizeText.Text       = FormatSize(node.Size);
        CompressedText.Text = FormatSize(node.CompressedSize);
        MethodText.Text     = node.CompressionMethod ?? "—";
        ModifiedText.Text   = node.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
        CrcText.Text        = node.Crc ?? "—";

        if (node.Size > 0)
        {
            var ratio = 1.0 - (double)node.CompressedSize / node.Size;
            RatioText.Text = $"{ratio:P1}";
        }
        else
        {
            RatioText.Text = "—";
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private static string FormatSize(long bytes) => bytes switch
    {
        0                     => "—",
        < 1024L               => $"{bytes} B",
        < 1024L * 1024        => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:F1} MB",
        _                     => $"{bytes / 1024.0 / 1024 / 1024:F2} GB",
    };
}
