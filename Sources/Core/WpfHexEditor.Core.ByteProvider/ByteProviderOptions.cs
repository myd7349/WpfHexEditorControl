// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProviderOptions.cs
// Description:
//     Fluent configuration object for ByteProvider. Controls memory-mapping threshold,
//     undo depth, cache sizes, auto-reload behavior, and telemetry sink.
// ==========================================================

using WpfHexEditor.Core.Metrics;

namespace WpfHexEditor.Core
{
    /// <summary>
    /// Configuration for <see cref="Bytes.ByteProvider"/>. Use the fluent With* methods
    /// to customise behavior before passing to the constructor.
    /// </summary>
    public sealed class ByteProviderOptions
    {
        /// <summary>Default options (same behavior as the parameterless constructor).</summary>
        public static readonly ByteProviderOptions Default = new();

        /// <summary>
        /// Files larger than this threshold open via <see cref="IO.MemoryMappedFileProvider"/>
        /// instead of <see cref="Bytes.FileProvider"/>.
        /// Set to <c>long.MaxValue</c> to disable memory-mapping entirely.
        /// Default: 512 MB.
        /// </summary>
        public long MemoryMapThresholdBytes { get; private set; } = 512L * 1024 * 1024;

        /// <summary>Maximum undo stack depth. Default: 1 000.</summary>
        public int MaxUndoDepth { get; private set; } = 1_000;

        /// <summary>FileProvider read-ahead cache size in bytes. Default: 64 KB.</summary>
        public int FileCacheSizeBytes { get; private set; } = 64 * 1024;

        /// <summary>
        /// When true, ByteProvider watches the backing file for external changes
        /// and raises <c>ExternalChangeDetected</c>. Default: false.
        /// </summary>
        public bool WatchExternalChanges { get; private set; } = false;

        /// <summary>
        /// Telemetry sink that receives operation callbacks.
        /// Defaults to <see cref="NullByteProviderMetrics.Instance"/> — zero overhead.
        /// </summary>
        public IByteProviderMetrics Metrics { get; private set; } = NullByteProviderMetrics.Instance;

        // ── Fluent setters ────────────────────────────────────────────────────

        public ByteProviderOptions WithMemoryMapThreshold(long bytes) =>
            Clone(o => o.MemoryMapThresholdBytes = bytes);

        public ByteProviderOptions WithNoMemoryMapping() =>
            Clone(o => o.MemoryMapThresholdBytes = long.MaxValue);

        public ByteProviderOptions WithMaxUndoDepth(int depth) =>
            Clone(o => o.MaxUndoDepth = depth);

        public ByteProviderOptions WithFileCacheSize(int bytes) =>
            Clone(o => o.FileCacheSizeBytes = bytes);

        public ByteProviderOptions WithExternalChangeWatcher(bool enabled = true) =>
            Clone(o => o.WatchExternalChanges = enabled);

        /// <summary>Attach a telemetry sink. Pass <see cref="NullByteProviderMetrics.Instance"/> to disable.</summary>
        public ByteProviderOptions WithMetrics(IByteProviderMetrics metrics) =>
            Clone(o => o.Metrics = metrics ?? NullByteProviderMetrics.Instance);

        // ── Private helpers ───────────────────────────────────────────────────

        private ByteProviderOptions Clone(System.Action<ByteProviderOptions> mutate)
        {
            var copy = (ByteProviderOptions)MemberwiseClone();
            mutate(copy);
            return copy;
        }
    }
}
