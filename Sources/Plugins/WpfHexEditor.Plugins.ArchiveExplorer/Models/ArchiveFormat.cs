// Project      : WpfHexEditorControl
// File         : Models/ArchiveFormat.cs
// Description  : Enum of supported archive formats.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

namespace WpfHexEditor.Plugins.ArchiveExplorer.Models;

/// <summary>Identifies the container format of an opened archive.</summary>
public enum ArchiveFormat
{
    Unknown,
    Zip,
    SevenZip,
    Rar,
    Tar,
    GZip,
    BZip2,
    Xz,
}
