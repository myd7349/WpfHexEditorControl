//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace WpfHexaEditor.V2.ByteProvider
{
    /// <summary>
    /// ByteReader - Intelligent byte reading service that combines FileProvider, EditsManager, and PositionMapper.
    /// Works with VIRTUAL positions (user-visible) and handles Modified/Inserted/Deleted bytes transparently.
    /// Provides ultra-fast reading with multiple caching layers.
    /// </summary>
    public sealed class ByteReader
    {
        private readonly FileProvider _fileProvider;
        private readonly EditsManager _editsManager;
        private readonly PositionMapper _positionMapper;

        // Line cache for rendering performance (most common use case)
        private readonly Dictionary<long, byte[]> _lineCache = new();
        private const int MAX_LINE_CACHE_ENTRIES = 1000; // ~16KB cache for 16-byte lines

        public ByteReader(FileProvider fileProvider, EditsManager editsManager, PositionMapper positionMapper)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _editsManager = editsManager ?? throw new ArgumentNullException(nameof(editsManager));
            _positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
        }

        /// <summary>
        /// Read a single byte at a virtual position.
        /// Returns the byte value considering all edits (Modified/Inserted/Deleted).
        /// </summary>
        /// <param name="virtualPosition">Virtual position (user-visible)</param>
        /// <returns>Byte value and success flag</returns>
        public (byte value, bool success) GetByte(long virtualPosition)
        {
            if (!_fileProvider.IsOpen || virtualPosition < 0)
                return (0, false);

            long physicalFileLength = _fileProvider.Length;
            long virtualLength = _positionMapper.GetVirtualLength(physicalFileLength);

            if (virtualPosition >= virtualLength)
                return (0, false);

            // Convert virtual to physical
            var (physicalPos, isInserted) = _positionMapper.VirtualToPhysical(virtualPosition, physicalFileLength);

            // Handle inserted bytes
            if (isInserted)
            {
                // Find the inserted byte at this virtual position
                var insertions = _editsManager.GetInsertedBytesAt(physicalPos.Value);

                // Calculate which inserted byte this is
                long virtualStart = _positionMapper.PhysicalToVirtual(physicalPos.Value, physicalFileLength);
                long insertionIndex = virtualPosition - virtualStart;

                if (insertionIndex >= 0 && insertionIndex < insertions.Count)
                {
                    return (insertions[(int)insertionIndex].Value, true);
                }

                return (0, false);
            }

            // No physical position = beyond file
            if (!physicalPos.HasValue)
                return (0, false);

            long physical = physicalPos.Value;

            // Check if deleted
            if (_editsManager.IsDeleted(physical))
            {
                // Deleted bytes don't appear in virtual view
                // This shouldn't happen if VirtualToPhysical is correct
                return (0, false);
            }

            // Check if modified
            var (modValue, modExists) = _editsManager.GetModifiedByte(physical);
            if (modExists)
            {
                return (modValue, true);
            }

            // Read from file
            return _fileProvider.ReadByte(physical);
        }

        /// <summary>
        /// Read multiple bytes starting at a virtual position.
        /// Optimized for sequential reads (e.g., rendering a line).
        /// </summary>
        /// <param name="virtualPosition">Starting virtual position</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Byte array (may be shorter than requested)</returns>
        public byte[] GetBytes(long virtualPosition, int count)
        {
            if (!_fileProvider.IsOpen || virtualPosition < 0 || count <= 0)
                return Array.Empty<byte>();

            long physicalFileLength = _fileProvider.Length;
            long virtualLength = _positionMapper.GetVirtualLength(physicalFileLength);

            // Clamp to available bytes
            long available = virtualLength - virtualPosition;
            if (available <= 0)
                return Array.Empty<byte>();

            count = (int)Math.Min(count, available);

            // Try line cache first (common for rendering)
            long lineStart = (virtualPosition / count) * count;
            if (_lineCache.TryGetValue(lineStart, out byte[] cachedLine))
            {
                int offset = (int)(virtualPosition - lineStart);
                int length = Math.Min(count, cachedLine.Length - offset);

                if (length > 0)
                {
                    byte[] result = new byte[length];
                    Array.Copy(cachedLine, offset, result, 0, length);
                    return result;
                }
            }

            // Cache miss - read byte by byte (handles all edit types)
            byte[] bytes = new byte[count];
            int bytesRead = 0;

            for (int i = 0; i < count; i++)
            {
                var (b, success) = GetByte(virtualPosition + i);
                if (!success)
                    break;

                bytes[i] = b;
                bytesRead++;
            }

            if (bytesRead < count)
                Array.Resize(ref bytes, bytesRead);

            // Cache the line if it's a full read
            if (virtualPosition == lineStart && bytesRead == count)
            {
                CacheLine(lineStart, bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Read multiple bytes for a line (optimized for rendering).
        /// Uses aggressive caching.
        /// </summary>
        public byte[] GetLine(long virtualLineStart, int bytesPerLine)
        {
            // Check cache first
            if (_lineCache.TryGetValue(virtualLineStart, out byte[] cachedLine))
            {
                return cachedLine;
            }

            // Read the line
            byte[] line = GetBytes(virtualLineStart, bytesPerLine);

            // Cache it
            CacheLine(virtualLineStart, line);

            return line;
        }

        /// <summary>
        /// Batch read multiple lines (ultra-optimized for viewport rendering).
        /// Pre-warms caches for maximum performance.
        /// </summary>
        public List<byte[]> GetLines(long startVirtualPosition, int lineCount, int bytesPerLine)
        {
            if (!_fileProvider.IsOpen || lineCount <= 0)
                return new List<byte[]>();

            var lines = new List<byte[]>(lineCount);
            long virtualLength = _positionMapper.GetVirtualLength(_fileProvider.Length);

            // Pre-warm position mapper cache for this range
            long endVirtual = Math.Min(startVirtualPosition + (lineCount * bytesPerLine), virtualLength - 1);
            _positionMapper.WarmupCache(startVirtualPosition, endVirtual, _fileProvider.Length);

            // Read each line
            long currentVirtual = startVirtualPosition;
            for (int i = 0; i < lineCount && currentVirtual < virtualLength; i++)
            {
                byte[] line = GetLine(currentVirtual, bytesPerLine);
                if (line.Length == 0)
                    break;

                lines.Add(line);
                currentVirtual += bytesPerLine;
            }

            return lines;
        }

        #region Cache Management

        private void CacheLine(long virtualLineStart, byte[] lineData)
        {
            // Limit cache size
            if (_lineCache.Count >= MAX_LINE_CACHE_ENTRIES)
            {
                // Simple LRU: clear oldest entries (first added)
                var keysToRemove = new List<long>();
                int toRemove = MAX_LINE_CACHE_ENTRIES / 4; // Remove 25%
                int removed = 0;

                foreach (var key in _lineCache.Keys)
                {
                    keysToRemove.Add(key);
                    if (++removed >= toRemove)
                        break;
                }

                foreach (var key in keysToRemove)
                    _lineCache.Remove(key);
            }

            _lineCache[virtualLineStart] = lineData;
        }

        /// <summary>
        /// Clear line cache (call after edits).
        /// </summary>
        public void ClearLineCache()
        {
            _lineCache.Clear();
        }

        /// <summary>
        /// Invalidate line cache in a specific virtual range.
        /// </summary>
        public void InvalidateCacheRange(long startVirtual, long endVirtual, int bytesPerLine)
        {
            long startLine = (startVirtual / bytesPerLine) * bytesPerLine;
            long endLine = (endVirtual / bytesPerLine) * bytesPerLine;

            var keysToRemove = new List<long>();

            foreach (var key in _lineCache.Keys)
            {
                if (key >= startLine && key <= endLine)
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
                _lineCache.Remove(key);
        }

        /// <summary>
        /// Get cache statistics.
        /// </summary>
        public (int lineCacheEntries, long estimatedMemoryKB) GetCacheStats()
        {
            long memory = 0;

            foreach (var line in _lineCache.Values)
            {
                memory += line.Length;
            }

            return (_lineCache.Count, memory / 1024);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if a virtual position is valid.
        /// </summary>
        public bool IsValidPosition(long virtualPosition)
        {
            if (!_fileProvider.IsOpen || virtualPosition < 0)
                return false;

            long virtualLength = _positionMapper.GetVirtualLength(_fileProvider.Length);
            return virtualPosition < virtualLength;
        }

        /// <summary>
        /// Get the total virtual length.
        /// </summary>
        public long GetVirtualLength()
        {
            if (!_fileProvider.IsOpen)
                return 0;

            return _positionMapper.GetVirtualLength(_fileProvider.Length);
        }

        #endregion
    }
}
