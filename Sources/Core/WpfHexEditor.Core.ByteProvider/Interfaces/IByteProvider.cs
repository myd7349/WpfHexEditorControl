// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: IByteProvider.cs
// Description:
//     Public abstraction for the byte provider — enables mocking, substitution,
//     and decorator patterns without depending on the sealed ByteProvider class.
// ==========================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WpfHexEditor.Core.Changesets;
using WpfHexEditor.Core.Search.Models;

namespace WpfHexEditor.Core.Interfaces
{
    /// <summary>
    /// Public contract for a virtual-position byte provider with edit tracking,
    /// undo/redo, search, changeset snapshots, async I/O, and streaming reads.
    /// </summary>
    public interface IByteProvider : IDisposable
    {
        // ── Identity ──────────────────────────────────────────────────────────

        string? FilePath { get; }
        bool IsOpen { get; }
        bool IsReadOnly { get; }
        long PhysicalLength { get; }
        long VirtualLength { get; }
        bool HasChanges { get; }
        (int modified, int inserted, int deleted) ModificationStats { get; }

        // ── Events ────────────────────────────────────────────────────────────

        event EventHandler? ChangesCleared;
        event EventHandler<ByteModifiedEventArgs>? ByteModified;
        event EventHandler<BytesInsertedEventArgs>? BytesInserted;
        event EventHandler<BytesDeletedEventArgs>? BytesDeleted;
        event EventHandler<SaveCompletedEventArgs>? SaveCompleted;

        // ── File operations (sync) ────────────────────────────────────────────

        void OpenFile(string filePath, bool readOnly = false);
        void OpenStream(Stream stream, bool readOnly = false);
        void OpenMemory(byte[] data, bool readOnly = false);
        void Close();
        void Reload();

        // ── File operations (async) ───────────────────────────────────────────

        Task OpenFileAsync(string filePath, bool readOnly = false, CancellationToken ct = default);
        Task OpenStreamAsync(Stream stream, bool readOnly = false, CancellationToken ct = default);
        Task OpenMemoryAsync(byte[] data, bool readOnly = false, CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);
        Task SaveAsAsync(string newFilePath, bool overwrite = false, CancellationToken ct = default);

        // ── Read (sync) ───────────────────────────────────────────────────────

        (byte value, bool success) GetByte(long virtualPosition);
        byte[] GetBytes(long virtualPosition, int count);
        byte[] GetLine(long virtualLineStart, int bytesPerLine);
        List<byte[]> GetLines(long startVirtualPosition, int lineCount, int bytesPerLine);

        // ── Read (async / streaming) ──────────────────────────────────────────

        ValueTask<(byte value, bool success)> GetByteAsync(long virtualPosition, CancellationToken ct = default);
        ValueTask<byte[]> GetBytesAsync(long virtualPosition, int count, CancellationToken ct = default);
        IAsyncEnumerable<byte[]> ReadLinesAsync(long startVirtualPosition, int bytesPerLine, CancellationToken ct = default);
        Task CopyToAsync(Stream destination, long start, long count, CancellationToken ct = default);
        Task CopyToAsync(Stream destination, CancellationToken ct = default);

        // ── Write ─────────────────────────────────────────────────────────────

        void ModifyByte(long virtualPosition, byte value);
        void ModifyBytes(long startVirtualPosition, byte[] values);
        void InsertBytes(long virtualPosition, byte[] bytes);
        void InsertByte(long virtualPosition, byte value);
        void DeleteByte(long virtualPosition);
        void DeleteBytes(long startVirtualPosition, long count);

        // ── Save (sync) ───────────────────────────────────────────────────────

        void Save();
        void SaveAs(string newFilePath, bool overwrite = false);

        // ── Edit management ───────────────────────────────────────────────────

        void ClearAllEdits();
        void ClearModifications();
        void ClearInsertions();
        void ClearDeletions();
        bool RestoreOriginalByte(long virtualPosition);
        int RestoreOriginalBytesInRange(long startVirtualPosition, long stopVirtualPosition);
        int RestoreAllModifications();

        // ── Undo/Redo ─────────────────────────────────────────────────────────

        bool CanUndo { get; }
        bool CanRedo { get; }
        void Undo();
        void Redo();
        void ClearUndoRedoHistory();
        void BeginUndoTransaction(string description);
        void CommitUndoTransaction();
        void RollbackUndoTransaction();
        string? PeekUndoDescription();
        string? PeekRedoDescription();
        IReadOnlyList<string> GetUndoDescriptions(int maxCount = 20);
        IReadOnlyList<string> GetRedoDescriptions(int maxCount = 20);

        // ── Batch ─────────────────────────────────────────────────────────────

        void BeginBatch();
        void EndBatch();

        // ── Changeset ─────────────────────────────────────────────────────────

        ChangesetSnapshot GetChangesetSnapshot();
        void ImportChangeset(ChangesetSnapshot snapshot);
        byte[] ExportChangesetJson();
        void ImportChangesetJson(byte[] jsonUtf8);
        void CreateCheckpoint(string name);
        void RestoreCheckpoint(string name);
        bool DeleteCheckpoint(string name);
        IReadOnlyList<string> GetCheckpoints();

        // ── Search (sync) ─────────────────────────────────────────────────────

        long FindFirst(byte[] pattern, long startPosition = 0);
        long FindNext(byte[] pattern, long currentPosition);
        long FindLast(byte[] pattern, long startPosition = 0);
        IEnumerable<long> FindAll(byte[] pattern, long startPosition = 0);
        int CountOccurrences(byte[] pattern, long startPosition = 0);
        SearchResult Search(SearchOptions options, CancellationToken cancellationToken = default);

        // ── Search (async / streaming) ────────────────────────────────────────

        IAsyncEnumerable<long> FindAllAsync(byte[] pattern, long startPosition = 0, CancellationToken ct = default);
        IAsyncEnumerable<SearchMatch> SearchStreamAsync(SearchOptions options, CancellationToken ct = default);
    }
}
