// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: NullByteProviderMetrics.cs
// Description:
//     No-op implementation of IByteProviderMetrics (null object pattern).
//     Used as default when no metrics sink is configured — all methods inline to nothing.
// ==========================================================

namespace WpfHexEditor.Core.Metrics
{
    /// <summary>
    /// No-op <see cref="IByteProviderMetrics"/> — zero overhead, all methods are inlinable stubs.
    /// This is the default when no metrics sink is configured via <see cref="ByteProviderOptions.WithMetrics"/>.
    /// </summary>
    public sealed class NullByteProviderMetrics : IByteProviderMetrics
    {
        /// <summary>Shared singleton — allocate once.</summary>
        public static readonly NullByteProviderMetrics Instance = new();

        private NullByteProviderMetrics() { }

        public void OnFileOpened(string? filePath, long fileSize) { }
        public void OnFileClosed(string? filePath) { }
        public void OnByteRead(long virtualPosition) { }
        public void OnByteModified(long virtualPosition) { }
        public void OnBytesInserted(long virtualPosition, int count) { }
        public void OnBytesDeleted(long virtualPosition, long count) { }
        public void OnSave(string? filePath, long bytesWritten, long elapsedMs) { }
        public void OnSearchCompleted(int matchCount, long elapsedMs) { }
    }
}
