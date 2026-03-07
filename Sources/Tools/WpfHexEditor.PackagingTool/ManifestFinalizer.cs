//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Security.Cryptography;
using System.Text.Json;

namespace WpfHexEditor.PackagingTool;

/// <summary>
/// Finalises a Build Manifest into a Distribution Manifest by:
/// 1. Calculating SHA-256 hash of the plugin assembly.
/// 2. Injecting assembly.sha256 and assembly.size into the manifest.
/// 3. Optionally adding a signature stub (Phase 5 full implementation).
/// </summary>
public sealed class ManifestFinalizer
{
    public async Task<JsonDocument> FinalizeAsync(string manifestPath, string assemblyPath, CancellationToken ct = default)
    {
        if (!File.Exists(manifestPath)) throw new FileNotFoundException("Manifest not found", manifestPath);
        if (!File.Exists(assemblyPath)) throw new FileNotFoundException("Assembly not found", assemblyPath);

        var json = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        var sha256 = await ComputeSha256Async(assemblyPath, ct).ConfigureAwait(false);
        var size   = new FileInfo(assemblyPath).Length;

        // Rebuild JSON with injected assembly metadata
        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "assembly")
            {
                writer.WritePropertyName("assembly");
                writer.WriteStartObject();
                foreach (var asmProp in prop.Value.EnumerateObject())
                    asmProp.WriteTo(writer);
                writer.WriteString("sha256", sha256);
                writer.WriteNumber("size", size);
                writer.WriteEndObject();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }
        writer.WriteEndObject();
        await writer.FlushAsync(ct).ConfigureAwait(false);

        return JsonDocument.Parse(stream.ToArray());
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
