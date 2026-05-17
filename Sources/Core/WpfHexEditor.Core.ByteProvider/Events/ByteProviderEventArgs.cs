// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProviderEventArgs.cs
// Description:
//     Strongly-typed EventArgs for ByteProvider mutation and lifecycle events.
// ==========================================================

using System;

namespace WpfHexEditor.Core
{
    /// <summary>Raised when one or more bytes are modified at a virtual position.</summary>
    public sealed class ByteModifiedEventArgs : EventArgs
    {
        public long VirtualPosition { get; }

        /// <summary>Single old value (single-byte path — no allocation).</summary>
        public byte OldValue { get; }

        /// <summary>Single new value (single-byte path — no allocation).</summary>
        public byte NewValue { get; }

        /// <summary>Multiple old values (multi-byte path). Null for single-byte edits.</summary>
        public byte[]? OldValues { get; }

        /// <summary>Multiple new values (multi-byte path). Null for single-byte edits.</summary>
        public byte[]? NewValues { get; }

        /// <summary>True when this event covers exactly one byte (use <see cref="OldValue"/>/<see cref="NewValue"/>).</summary>
        public bool IsSingleByte { get; }

        internal ByteModifiedEventArgs(long virtualPosition, byte oldValue, byte newValue)
        {
            VirtualPosition = virtualPosition;
            OldValue  = oldValue;
            NewValue  = newValue;
            IsSingleByte = true;
        }

        internal ByteModifiedEventArgs(long virtualPosition, byte[] oldValues, byte[] newValues)
        {
            VirtualPosition = virtualPosition;
            OldValues = oldValues;
            NewValues = newValues;
            OldValue  = oldValues.Length > 0 ? oldValues[0] : (byte)0;
            NewValue  = newValues.Length > 0 ? newValues[0] : (byte)0;
            IsSingleByte = false;
        }
    }

    /// <summary>Raised when bytes are inserted at a virtual position.</summary>
    public sealed class BytesInsertedEventArgs : EventArgs
    {
        public long VirtualPosition { get; }
        public byte[] InsertedBytes { get; }

        public BytesInsertedEventArgs(long virtualPosition, byte[] insertedBytes)
        {
            VirtualPosition = virtualPosition;
            InsertedBytes = insertedBytes;
        }
    }

    /// <summary>Raised when bytes are deleted starting at a virtual position.</summary>
    public sealed class BytesDeletedEventArgs : EventArgs
    {
        public long VirtualPosition { get; }
        public long Count { get; }

        public BytesDeletedEventArgs(long virtualPosition, long count)
        {
            VirtualPosition = virtualPosition;
            Count = count;
        }
    }

    /// <summary>Raised after a successful save operation.</summary>
    public sealed class SaveCompletedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public long BytesWritten { get; }
        public long ElapsedMs { get; }

        public SaveCompletedEventArgs(string filePath, long bytesWritten, long elapsedMs)
        {
            FilePath = filePath;
            BytesWritten = bytesWritten;
            ElapsedMs = elapsedMs;
        }
    }
}
