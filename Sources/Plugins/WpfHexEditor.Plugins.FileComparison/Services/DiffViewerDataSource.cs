// ==========================================================
// Project: WpfHexEditor.Plugins.FileComparison
// File: DiffViewerDataSource.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude (Anthropic)
// Created: 2026-03-26
// Description:
//     Read-only IBinaryDataSource adapter for the DiffViewer.
//     Wraps a file path with lazy stream opening for format detection.
//
// Architecture Notes:
//     Read-only — DiffViewer does not support writes.
//     One instance per side (left/right) of the diff comparison.
// ==========================================================

using System;
using System.IO;
using WpfHexEditor.Core.Interfaces;

namespace WpfHexEditor.Plugins.FileComparison.Services
{
    /// <summary>
    /// Read-only <see cref="IBinaryDataSource"/> for one side of a diff comparison.
    /// </summary>
    internal sealed class DiffViewerDataSource : IBinaryDataSource, IDisposable
    {
        private FileStream? _stream;

        public DiffViewerDataSource(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public string? FilePath { get; }

        public long Length
        {
            get
            {
                EnsureStream();
                return _stream?.Length ?? 0;
            }
        }

        public bool IsReadOnly => true;

        public byte[] ReadBytes(long offset, int length)
        {
            EnsureStream();
            if (_stream == null || offset < 0 || length <= 0 || offset + length > _stream.Length)
                return Array.Empty<byte>();

            var buffer = new byte[length];
            _stream.Position = offset;
            int bytesRead = _stream.Read(buffer, 0, length);
            if (bytesRead != length)
            {
                var result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            return buffer;
        }

        public void WriteBytes(long offset, byte[] data)
            => throw new InvalidOperationException("DiffViewer data sources are read-only.");

        public event EventHandler? DataChanged;

        private void EnsureStream()
        {
            if (_stream == null && FilePath != null && File.Exists(FilePath))
                _stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
