// Project      : WpfHexEditorControl
// File         : Models/ArchiveEntry.cs
// Description  : Flat, format-agnostic record representing one entry in an archive.
//                Decoupled from both System.IO.Compression.ZipArchiveEntry and
//                SharpCompress.Archives.IArchiveEntry — IArchiveReader implementations
//                map their native types to this record.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

namespace WpfHexEditor.Plugins.ArchiveExplorer.Models;

/// <summary>
/// Immutable snapshot of an archive entry, independent of the underlying library.
/// </summary>
public sealed record ArchiveEntry(
    string FullPath,
    bool IsDirectory,
    long Size,
    long CompressedSize,
    string? CompressionMethod,
    DateTime? LastModified,
    string? Crc);
