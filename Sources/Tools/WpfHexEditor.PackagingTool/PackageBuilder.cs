//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO.Compression;
using System.Text.Json;

namespace WpfHexEditor.PackagingTool;

/// <summary>
/// Orchestrates the build of a .whxplugin package:
/// 1. Finalises manifest (injects SHA-256 + size).
/// 2. Optional RSA signing (Phase 5).
/// 3. Packs all plugin files into a .whxplugin ZIP archive.
/// </summary>
public sealed class PackageBuilder
{
    private readonly ManifestFinalizer _finalizer = new();

    public async Task BuildAsync(string inputDir, string outputPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");

        var manifestPath = Path.Combine(inputDir, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json not found in input directory.", manifestPath);

        // Resolve assembly path from manifest
        using var buildDoc = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false));
        var assemblyFile = buildDoc.RootElement.TryGetProperty("assembly", out var asm) &&
                           asm.TryGetProperty("file", out var asmFile)
                           ? asmFile.GetString()
                           : null;
        if (assemblyFile is null)
            throw new InvalidOperationException("manifest.json is missing assembly.file.");

        var assemblyPath = Path.Combine(inputDir, assemblyFile);
        var distManifest = await _finalizer.FinalizeAsync(manifestPath, assemblyPath, ct).ConfigureAwait(false);

        // Write distribution manifest to temp
        var tmpManifest = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tmpManifest,
                JsonSerializer.SerializeToUtf8Bytes(distManifest.RootElement,
                    new JsonSerializerOptions { WriteIndented = true }), ct).ConfigureAwait(false);

            // Pack into .whxplugin (ZIP)
            if (File.Exists(outputPath)) File.Delete(outputPath);
            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            // Add manifest (distribution version)
            zip.CreateEntryFromFile(tmpManifest, "manifest.json", CompressionLevel.Optimal);

            // Add all files from the input directory (skip build manifest, add all else)
            foreach (var file in Directory.EnumerateFiles(inputDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(inputDir, file);
                if (relative == "manifest.json") continue; // use dist version
                zip.CreateEntryFromFile(file, relative, CompressionLevel.Optimal);
            }
        }
        finally
        {
            if (File.Exists(tmpManifest)) File.Delete(tmpManifest);
        }
    }
}
