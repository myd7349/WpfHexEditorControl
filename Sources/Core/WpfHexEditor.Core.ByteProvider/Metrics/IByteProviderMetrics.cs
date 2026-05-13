// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: IByteProviderMetrics.cs
// Description:
//     Telemetry hook interface for ByteProvider operations.
//     Inject via ByteProviderOptions.WithMetrics() — zero overhead when NullByteProviderMetrics.
// ==========================================================

namespace WpfHexEditor.Core.Metrics
{
    /// <summary>
    /// Receives telemetry callbacks from <see cref="Bytes.ByteProvider"/>.
    /// Implement this interface to wire custom APM, logging, or profiling.
    /// </summary>
    public interface IByteProviderMetrics
    {
        void OnFileOpened(string? filePath, long fileSize);
        void OnFileClosed(string? filePath);
        void OnByteRead(long virtualPosition);
        void OnByteModified(long virtualPosition);
        void OnBytesInserted(long virtualPosition, int count);
        void OnBytesDeleted(long virtualPosition, long count);
        void OnSave(string? filePath, long bytesWritten, long elapsedMs);
        void OnSearchCompleted(int matchCount, long elapsedMs);
    }
}
