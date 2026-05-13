// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Services/PngSequenceExportService.cs
// Description: Exports each frame as a numbered PNG file (0000.png … NNNN.png).
//              OutputScale is applied before encoding.
// ==========================================================

using System.IO;
using WpfHexEditor.Plugins.ScreenRecorder.Models;

namespace WpfHexEditor.Plugins.ScreenRecorder.Services;

public static class PngSequenceExportService
{
    public static async Task ExportAsync(
        IReadOnlyList<CaptureFrame> frames,
        ExportOptions               options,
        IProgress<int>?             progress = null,
        CancellationToken           ct       = default)
    {
        if (frames.Count == 0) throw new ArgumentException("No frames to export.", nameof(frames));

        Directory.CreateDirectory(options.OutputPath);

        for (var i = 0; i < frames.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var frame  = frames[i];
            var scaled = options.OutputScale != 1.0
                ? FrameCaptureEngine.ScaleBitmap(frame.Bitmap, options.OutputScale)
                : frame.Bitmap;

            var filePath = Path.Combine(options.OutputPath, $"{frame.Index:D4}.png");
            var bytes    = await FrameCaptureEngine.EncodePngOnUiThreadAsync(scaled);
            await File.WriteAllBytesAsync(filePath, bytes, ct);

            progress?.Report((i + 1) * 100 / frames.Count);
        }
    }
}
