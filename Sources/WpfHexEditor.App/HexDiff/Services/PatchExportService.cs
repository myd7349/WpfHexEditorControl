// Project      : WpfHexEditor.App
// File         : HexDiff/Services/PatchExportService.cs
// Description  : Serialises a list of DiffRecord to JSON or plain text.

using System.IO;
using System.Text;
using System.Text.Json;
using WpfHexEditor.App.HexDiff.Models;

namespace WpfHexEditor.App.HexDiff.Services;

public static class PatchExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void ExportJson(IReadOnlyList<DiffRecord> diffs, string path)
        => File.WriteAllText(path, JsonSerializer.Serialize(diffs, JsonOptions));

    public static void ExportText(IReadOnlyList<DiffRecord> diffs, string path)
    {
        var sb = new StringBuilder("Offset,Kind,OldByte,NewByte\r\n", diffs.Count * 32);
        foreach (var d in diffs)
            sb.Append($"0x{d.Offset:X8},{d.Kind},{d.OldByte:X2},{d.NewByte:X2}\r\n");
        File.WriteAllText(path, sb.ToString());
    }
}
