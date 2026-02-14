//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfHexaEditor.V2.ByteProvider
{
    /// <summary>
    /// PositionMapper - Converts between Virtual positions (what user sees) and Physical positions (file offsets).
    /// Virtual positions include inserted bytes, Physical positions are actual file offsets.
    /// Uses aggressive caching for performance.
    /// </summary>
    public sealed class PositionMapper
    {
        private readonly EditsManager _editsManager;

        // Caches for bidirectional mapping
        private readonly Dictionary<long, long> _virtualToPhysicalCache = new();
        private readonly Dictionary<long, long> _physicalToVirtualCache = new();

        // Cache metadata
        private bool _cacheValid = false;
        private long _cachedVirtualLength = -1;

        public PositionMapper(EditsManager editsManager)
        {
            _editsManager = editsManager ?? throw new ArgumentNullException(nameof(editsManager));
        }

        /// <summary>
        /// Convert virtual position to physical position.
        /// Handles inserted bytes by mapping multiple virtual positions to the same physical position.
        /// </summary>
        /// <param name="virtualPosition">Virtual position (user-visible)</param>
        /// <param name="physicalFileLength">Physical file length</param>
        /// <returns>Physical position (file offset) or null if virtual position is an inserted byte</returns>
        public (long? physicalPosition, bool isInsertedByte) VirtualToPhysical(long virtualPosition, long physicalFileLength)
        {
            if (virtualPosition < 0)
                return (null, false);

            // Try cache first
            if (_cacheValid && _virtualToPhysicalCache.TryGetValue(virtualPosition, out long cachedPhysical))
            {
                return (cachedPhysical, false);
            }

            // Walk through physical positions, counting virtual positions
            long currentVirtual = 0;
            long physicalPos = 0;

            while (physicalPos < physicalFileLength)
            {
                // Check for deletions
                if (_editsManager.IsDeleted(physicalPos))
                {
                    // Deleted bytes don't appear in virtual view
                    physicalPos++;
                    continue;
                }

                // Check for insertions BEFORE this physical position
                var insertions = _editsManager.GetInsertedBytesAt(physicalPos);
                if (insertions.Count > 0)
                {
                    // Check if virtual position falls within inserted bytes
                    if (virtualPosition >= currentVirtual && virtualPosition < currentVirtual + insertions.Count)
                    {
                        // This virtual position is an inserted byte
                        return (physicalPos, true);
                    }

                    currentVirtual += insertions.Count;
                }

                // Check if we've reached the target virtual position
                if (currentVirtual == virtualPosition)
                {
                    // Cache result
                    if (_cacheValid)
                    {
                        _virtualToPhysicalCache[virtualPosition] = physicalPos;
                        _physicalToVirtualCache[physicalPos] = virtualPosition;
                    }

                    return (physicalPos, false);
                }

                currentVirtual++;
                physicalPos++;
            }

            // Virtual position beyond end of file
            return (null, false);
        }

        /// <summary>
        /// Convert physical position to virtual position.
        /// </summary>
        /// <param name="physicalPosition">Physical file offset</param>
        /// <param name="physicalFileLength">Physical file length</param>
        /// <returns>Virtual position (user-visible)</returns>
        public long PhysicalToVirtual(long physicalPosition, long physicalFileLength)
        {
            if (physicalPosition < 0)
                return -1;

            // Try cache first
            if (_cacheValid && _physicalToVirtualCache.TryGetValue(physicalPosition, out long cachedVirtual))
            {
                return cachedVirtual;
            }

            // Walk through physical positions, counting virtual positions
            long virtualPos = 0;

            for (long physPos = 0; physPos < physicalPosition && physPos < physicalFileLength; physPos++)
            {
                // Check for deletions
                if (_editsManager.IsDeleted(physPos))
                {
                    // Deleted bytes don't count
                    continue;
                }

                // Add inserted bytes BEFORE this physical position
                int insertionCount = _editsManager.GetInsertionCountAt(physPos);
                virtualPos += insertionCount;

                // Count this physical byte
                virtualPos++;
            }

            // Add insertions at target position
            int insertionsAtTarget = _editsManager.GetInsertionCountAt(physicalPosition);
            virtualPos += insertionsAtTarget;

            // Cache result
            if (_cacheValid)
            {
                _physicalToVirtualCache[physicalPosition] = virtualPos;
                _virtualToPhysicalCache[virtualPos] = physicalPosition;
            }

            return virtualPos;
        }

        /// <summary>
        /// Calculate total virtual length (physical length + insertions - deletions).
        /// </summary>
        public long GetVirtualLength(long physicalFileLength)
        {
            // Return cached value if valid
            if (_cacheValid && _cachedVirtualLength >= 0)
                return _cachedVirtualLength;

            long virtualLength = physicalFileLength;

            // Add inserted bytes
            virtualLength += _editsManager.TotalInsertedBytesCount;

            // Subtract deleted bytes
            virtualLength -= _editsManager.DeletedCount;

            // Cache result
            if (_cacheValid)
                _cachedVirtualLength = virtualLength;

            return virtualLength;
        }

        /// <summary>
        /// Get the physical position range that corresponds to a virtual position range.
        /// Useful for determining which file regions need to be read.
        /// </summary>
        public (long startPhysical, long endPhysical) VirtualRangeToPhysicalRange(
            long startVirtual, long endVirtual, long physicalFileLength)
        {
            var (startPhys, _) = VirtualToPhysical(startVirtual, physicalFileLength);
            var (endPhys, _) = VirtualToPhysical(endVirtual, physicalFileLength);

            long start = startPhys ?? 0;
            long end = endPhys ?? physicalFileLength - 1;

            return (start, end);
        }

        #region Cache Management

        /// <summary>
        /// Enable caching (improves performance for repeated queries).
        /// Should be disabled during bulk edit operations.
        /// </summary>
        public void EnableCache()
        {
            _cacheValid = true;
        }

        /// <summary>
        /// Disable caching (for bulk edit operations).
        /// </summary>
        public void DisableCache()
        {
            _cacheValid = false;
            InvalidateCache();
        }

        /// <summary>
        /// Invalidate cache (call after edits change).
        /// </summary>
        public void InvalidateCache()
        {
            _virtualToPhysicalCache.Clear();
            _physicalToVirtualCache.Clear();
            _cachedVirtualLength = -1;
        }

        /// <summary>
        /// Pre-warm cache for a specific range.
        /// Useful before rendering a viewport.
        /// </summary>
        public void WarmupCache(long startVirtual, long endVirtual, long physicalFileLength)
        {
            if (!_cacheValid)
                return;

            // Pre-calculate mappings for the range
            for (long vPos = startVirtual; vPos <= endVirtual && vPos < GetVirtualLength(physicalFileLength); vPos++)
            {
                VirtualToPhysical(vPos, physicalFileLength);
            }
        }

        /// <summary>
        /// Get cache statistics.
        /// </summary>
        public (int virtualToPhysicalEntries, int physicalToVirtualEntries, bool valid) GetCacheStats()
        {
            return (_virtualToPhysicalCache.Count, _physicalToVirtualCache.Count, _cacheValid);
        }

        #endregion
    }
}
