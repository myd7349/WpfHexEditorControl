// Project      : WpfHexEditorControl
// File         : Services/SharpCompressArchiveReader.cs
// Description  : IArchiveReader implementation for 7z, RAR, TAR, GZ, BZ2, XZ
//                using the SharpCompress library (MIT, pure managed, no native DLL).
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using SharpCompress.Archives;
using SharpCompress.Common;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Reads 7z, RAR, TAR (+ GZ/BZ2/XZ compressed TAR) archives via SharpCompress.
/// </summary>
public sealed class SharpCompressArchiveReader : IArchiveReader
{
    private readonly IArchive _archive;
    private readonly List<ArchiveEntry> _entries;
    private bool _disposed;

    public ArchiveFormat Format      { get; }
    public string        ArchivePath { get; }
    public IReadOnlyList<ArchiveEntry> Entries => _entries;

    public SharpCompressArchiveReader(string archivePath)
    {
        ArchivePath = archivePath;
        _archive    = ArchiveFactory.Open(archivePath);
        Format      = DetectFormat(_archive);
        _entries    = [.. _archive.Entries.Select(MapEntry)];
    }

    // ── IArchiveReader ─────────────────────────────────────────────────────

    public Stream OpenEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var raw = FindEntry(entry) ?? throw new FileNotFoundException($"Entry not found: {entry.FullPath}");
        return raw.OpenEntryStream();
    }

    public async Task ExtractEntryAsync(ArchiveEntry entry, string destPath, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var raw = FindEntry(entry) ?? throw new FileNotFoundException($"Entry not found: {entry.FullPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        await using var src  = raw.OpenEntryStream();
        await using var dest = File.Create(destPath);
        await src.CopyToAsync(dest, ct).ConfigureAwait(false);
    }

    public async Task ExtractAllAsync(string destFolder, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fileEntries = _entries.Where(e => !e.IsDirectory).ToList();
        int total = fileEntries.Count;
        int done  = 0;
        foreach (var entry in fileEntries)
        {
            ct.ThrowIfCancellationRequested();
            var destPath = Path.Combine(destFolder, entry.FullPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            var raw = FindEntry(entry)!;
            await using var src  = raw.OpenEntryStream();
            await using var dest = File.Create(destPath);
            await src.CopyToAsync(dest, ct).ConfigureAwait(false);
            progress?.Report(++done / (double)total);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static ArchiveEntry MapEntry(IArchiveEntry e) => new(
        FullPath          : (e.Key ?? string.Empty).Replace('\\', '/'),
        IsDirectory       : e.IsDirectory,
        Size              : e.Size,
        CompressedSize    : e.CompressedSize,
        CompressionMethod : e.CompressionType.ToString(),
        LastModified      : e.LastModifiedTime,
        Crc               : e.Crc != 0 ? $"{e.Crc:X8}" : null);

    private IArchiveEntry? FindEntry(ArchiveEntry entry)
        => _archive.Entries.FirstOrDefault(e =>
            string.Equals(e.Key?.Replace('\\', '/'), entry.FullPath, StringComparison.OrdinalIgnoreCase));

    private static ArchiveFormat DetectFormat(IArchive archive) => archive.Type switch
    {
        ArchiveType.SevenZip => ArchiveFormat.SevenZip,
        ArchiveType.Rar      => ArchiveFormat.Rar,
        ArchiveType.Tar      => ArchiveFormat.Tar,
        ArchiveType.GZip     => ArchiveFormat.GZip,
        _                    => ArchiveFormat.Unknown,
    };

    public void Dispose()
    {
        if (_disposed) return;
        _archive.Dispose();
        _disposed = true;
    }
}
