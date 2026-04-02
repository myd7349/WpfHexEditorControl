// Project      : WpfHexEditorControl
// File         : Services/ZipArchiveReader.cs
// Description  : IArchiveReader implementation for ZIP archives using the .NET BCL
//                (System.IO.Compression.ZipFile). Zero extra NuGet dependency.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using System.IO.Compression;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Reads ZIP archives (including DOCX, XLSX, JAR, EPUB, NuGet packages …)
/// using <see cref="System.IO.Compression.ZipFile"/>.
/// </summary>
public sealed class ZipArchiveReader : IArchiveReader
{
    private readonly ZipArchive _zip;
    private readonly List<ArchiveEntry> _entries;
    private bool _disposed;

    public ArchiveFormat Format      => ArchiveFormat.Zip;
    public string        ArchivePath { get; }
    public IReadOnlyList<ArchiveEntry> Entries => _entries;

    public ZipArchiveReader(string archivePath)
    {
        ArchivePath = archivePath;
        var stream  = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _zip        = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        _entries    = [.. _zip.Entries.Select(MapEntry)];
    }

    // ── IArchiveReader ─────────────────────────────────────────────────────

    public Stream OpenEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var zipEntry = FindZipEntry(entry) ?? throw new FileNotFoundException($"Entry not found: {entry.FullPath}");
        return zipEntry.Open();
    }

    public async Task ExtractEntryAsync(ArchiveEntry entry, string destPath, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var zipEntry = FindZipEntry(entry) ?? throw new FileNotFoundException($"Entry not found: {entry.FullPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        await using var src  = zipEntry.Open();
        await using var dest = File.Create(destPath);
        await src.CopyToAsync(dest, ct).ConfigureAwait(false);
    }

    public async Task ExtractAllAsync(string destFolder, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fileEntries = _zip.Entries.Where(e => !e.FullName.EndsWith('/')).ToList();
        int total = fileEntries.Count;
        int done  = 0;
        foreach (var zipEntry in fileEntries)
        {
            ct.ThrowIfCancellationRequested();
            var destPath = Path.Combine(destFolder, zipEntry.FullName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            zipEntry.ExtractToFile(destPath, overwrite: true);
            progress?.Report(++done / (double)total);
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static ArchiveEntry MapEntry(ZipArchiveEntry e) => new(
        FullPath          : e.FullName.Replace('\\', '/'),
        IsDirectory       : e.FullName.EndsWith('/'),
        Size              : e.Length,
        CompressedSize    : e.CompressedLength,
        CompressionMethod : "Deflate",
        LastModified      : e.LastWriteTime.UtcDateTime,
        Crc               : $"{e.Crc32:X8}");

    private ZipArchiveEntry? FindZipEntry(ArchiveEntry entry)
    {
        var path = entry.FullPath;
        return _zip.GetEntry(path)
            ?? _zip.GetEntry(path.TrimEnd('/'))
            ?? _zip.GetEntry(path.Replace('/', '\\'))
            ?? _zip.GetEntry(path.Replace('/', '\\').TrimEnd('\\'));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _zip.Dispose();
        _disposed = true;
    }
}
