// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteSlice.cs
// Description:
//     Span-like zero-copy view over a virtual range of an IByteProvider.
//     Struct — no heap allocation for the slice descriptor itself.
// ==========================================================

using System;
using WpfHexEditor.Core.Interfaces;

namespace WpfHexEditor.Core.Slices
{
    /// <summary>
    /// A zero-allocation view over a contiguous virtual range of an <see cref="IByteProvider"/>.
    /// The struct itself is allocation-free; <see cref="ToArray"/> and <see cref="CopyTo"/> allocate
    /// only when the caller explicitly materializes the data.
    /// </summary>
    public readonly struct ByteSlice : IEquatable<ByteSlice>
    {
        /// <summary>The provider this slice refers to.</summary>
        public IByteProvider Source { get; }

        /// <summary>Virtual start position (inclusive).</summary>
        public long Start { get; }

        /// <summary>Number of bytes in this slice.</summary>
        public long Length { get; }

        /// <summary>Virtual end position (exclusive).</summary>
        public long End => Start + Length;

        /// <summary>True when the slice covers zero bytes.</summary>
        public bool IsEmpty => Length == 0;

        public ByteSlice(IByteProvider source, long start, long length)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            Source = source;
            Start  = start;
            Length = length;
        }

        // ── Indexer ───────────────────────────────────────────────────────────

        /// <summary>Read the byte at <paramref name="index"/> relative to <see cref="Start"/>.</summary>
        public byte this[long index]
        {
            get
            {
                if ((ulong)index >= (ulong)Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var (value, success) = Source.GetByte(Start + index);
                if (!success)
                    throw new InvalidOperationException($"Could not read byte at virtual position {Start + index}.");
                return value;
            }
        }

        // ── Sub-slicing ───────────────────────────────────────────────────────

        /// <summary>Return a sub-slice starting at <paramref name="offset"/> with <paramref name="length"/> bytes.</summary>
        public ByteSlice Slice(long offset, long length)
        {
            if (offset < 0 || offset > Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0 || offset + length > Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            return new ByteSlice(Source, Start + offset, length);
        }

        /// <summary>Return a sub-slice from <paramref name="offset"/> to the end.</summary>
        public ByteSlice Slice(long offset) => Slice(offset, Length - offset);

        // ── Materialization ───────────────────────────────────────────────────

        /// <summary>
        /// Copy all bytes into a new array. Allocates.
        /// Throws <see cref="NotSupportedException"/> for slices longer than 2 GB —
        /// use <see cref="CopyTo"/> with a pre-allocated buffer instead.
        /// </summary>
        public byte[] ToArray()
        {
            if (Length == 0) return Array.Empty<byte>();
            if (Length > int.MaxValue)
                throw new NotSupportedException($"ByteSlice is too large to materialize ({Length} bytes). Use CopyTo with a pre-allocated buffer.");
            return Source.GetBytes(Start, (int)Length);
        }

        /// <summary>Copy bytes into <paramref name="destination"/>. Single allocation via GetBytes.</summary>
        public void CopyTo(Span<byte> destination)
        {
            if (destination.Length < Length)
                throw new ArgumentException("Destination span is too small.", nameof(destination));
            if (Length > int.MaxValue)
                throw new NotSupportedException($"ByteSlice is too large to copy in one call ({Length} bytes).");
            Source.GetBytes(Start, (int)Length).AsSpan().CopyTo(destination);
        }

        // ── Comparison ────────────────────────────────────────────────────────

        /// <summary>Returns true when both slices cover the same virtual range on the same provider.</summary>
        public bool Equals(ByteSlice other) =>
            ReferenceEquals(Source, other.Source) && Start == other.Start && Length == other.Length;

        /// <summary>
        /// Compare byte-by-byte content. O(n), zero allocation — uses the indexer directly.
        /// </summary>
        public bool SequenceEqual(ByteSlice other)
        {
            if (Length != other.Length) return false;
            for (long i = 0; i < Length; i++)
                if (this[i] != other[i]) return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is ByteSlice other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Source, Start, Length);

        public static bool operator ==(ByteSlice left, ByteSlice right) => left.Equals(right);
        public static bool operator !=(ByteSlice left, ByteSlice right) => !left.Equals(right);

        public override string ToString() => $"ByteSlice[{Start}..{End}) len={Length}";
    }
}
