// Project      : WpfHexEditorControl
// File         : Services/IArchiveReader.cs
// Description  : Abstraction over archive reading backends (ZIP via BCL,
//                7z/RAR/TAR/GZ via SharpCompress). Callers depend only on
//                this interface — concrete implementations are hidden.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Read-only access to an archive's contents.
/// Implementations must be thread-safe for read operations once constructed.
/// </summary>
public interface IArchiveReader : IDisposable
{
    /// <summary>The detected format of this archive.</summary>
    ArchiveFormat Format { get; }

    /// <summary>Full path of the archive file on disk.</summary>
    string ArchivePath { get; }

    /// <summary>Flat list of all entries (files and directories).</summary>
    IReadOnlyList<ArchiveEntry> Entries { get; }

    /// <summary>Opens a readable stream for the given entry.</summary>
    Stream OpenEntry(ArchiveEntry entry);

    /// <summary>Extracts a single entry to <paramref name="destPath"/>.</summary>
    Task ExtractEntryAsync(ArchiveEntry entry, string destPath, CancellationToken ct = default);

    /// <summary>
    /// Extracts all entries to <paramref name="destFolder"/>.
    /// <paramref name="progress"/> receives values in [0.0, 1.0].
    /// </summary>
    Task ExtractAllAsync(string destFolder, IProgress<double>? progress = null, CancellationToken ct = default);
}
