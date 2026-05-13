// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProviderDecorator.cs
// Description:
//     Abstract base class for IByteProvider decorators (AOP pattern).
//     Forwards all calls to the wrapped Inner provider by default.
//     Subclass and override only the methods you need to intercept.
// ==========================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WpfHexEditor.Core.Changesets;
using WpfHexEditor.Core.Search.Models;

namespace WpfHexEditor.Core.Decorators
{
    /// <summary>
    /// Base class for <see cref="Interfaces.IByteProvider"/> decorators.
    /// All members forward to <see cref="Inner"/> by default — override only what you need.
    /// <example>
    /// <code>
    /// public sealed class LoggingByteProvider : ByteProviderDecorator
    /// {
    ///     public LoggingByteProvider(IByteProvider inner) : base(inner) { }
    ///
    ///     public override void ModifyByte(long pos, byte value)
    ///     {
    ///         Console.WriteLine($"ModifyByte {pos:X} = {value:X2}");
    ///         base.ModifyByte(pos, value);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public abstract class ByteProviderDecorator : Interfaces.IByteProvider
    {
        /// <summary>The wrapped provider.</summary>
        protected Interfaces.IByteProvider Inner { get; }

        // Stored so they can be removed from Inner's event lists in Dispose().
        // Anonymous lambdas cannot be unsubscribed, so we keep named references.
        private readonly EventHandler _onChangesCleared;
        private readonly EventHandler<ByteModifiedEventArgs> _onByteModified;
        private readonly EventHandler<BytesInsertedEventArgs> _onBytesInserted;
        private readonly EventHandler<BytesDeletedEventArgs> _onBytesDeleted;
        private readonly EventHandler<SaveCompletedEventArgs> _onSaveCompleted;

        protected ByteProviderDecorator(Interfaces.IByteProvider inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));

            _onChangesCleared  = (s, e) => ChangesCleared?.Invoke(this, e);
            _onByteModified    = (s, e) => ByteModified?.Invoke(this, e);
            _onBytesInserted   = (s, e) => BytesInserted?.Invoke(this, e);
            _onBytesDeleted    = (s, e) => BytesDeleted?.Invoke(this, e);
            _onSaveCompleted   = (s, e) => SaveCompleted?.Invoke(this, e);

            inner.ChangesCleared += _onChangesCleared;
            inner.ByteModified   += _onByteModified;
            inner.BytesInserted  += _onBytesInserted;
            inner.BytesDeleted   += _onBytesDeleted;
            inner.SaveCompleted  += _onSaveCompleted;
        }

        // ── Identity ──────────────────────────────────────────────────────────

        public virtual string? FilePath             => Inner.FilePath;
        public virtual bool IsOpen                  => Inner.IsOpen;
        public virtual bool IsReadOnly              => Inner.IsReadOnly;
        public virtual long PhysicalLength          => Inner.PhysicalLength;
        public virtual long VirtualLength           => Inner.VirtualLength;
        public virtual bool HasChanges              => Inner.HasChanges;
        public virtual (int modified, int inserted, int deleted) ModificationStats => Inner.ModificationStats;

        // ── Events ────────────────────────────────────────────────────────────

        public event EventHandler? ChangesCleared;
        public event EventHandler<ByteModifiedEventArgs>? ByteModified;
        public event EventHandler<BytesInsertedEventArgs>? BytesInserted;
        public event EventHandler<BytesDeletedEventArgs>? BytesDeleted;
        public event EventHandler<SaveCompletedEventArgs>? SaveCompleted;

        // ── File operations (sync) ────────────────────────────────────────────

        public virtual void OpenFile(string filePath, bool readOnly = false)            => Inner.OpenFile(filePath, readOnly);
        public virtual void OpenStream(Stream stream, bool readOnly = false)            => Inner.OpenStream(stream, readOnly);
        public virtual void OpenMemory(byte[] data, bool readOnly = false)              => Inner.OpenMemory(data, readOnly);
        public virtual void Close()                                                     => Inner.Close();
        public virtual void Reload()                                                    => Inner.Reload();

        // ── File operations (async) ───────────────────────────────────────────

        public virtual Task OpenFileAsync(string filePath, bool readOnly = false, CancellationToken ct = default)        => Inner.OpenFileAsync(filePath, readOnly, ct);
        public virtual Task OpenStreamAsync(Stream stream, bool readOnly = false, CancellationToken ct = default)        => Inner.OpenStreamAsync(stream, readOnly, ct);
        public virtual Task OpenMemoryAsync(byte[] data, bool readOnly = false, CancellationToken ct = default)          => Inner.OpenMemoryAsync(data, readOnly, ct);
        public virtual Task SaveAsync(CancellationToken ct = default)                                                    => Inner.SaveAsync(ct);
        public virtual Task SaveAsAsync(string newFilePath, bool overwrite = false, CancellationToken ct = default)      => Inner.SaveAsAsync(newFilePath, overwrite, ct);

        // ── Read (sync) ───────────────────────────────────────────────────────

        public virtual (byte value, bool success) GetByte(long virtualPosition)                            => Inner.GetByte(virtualPosition);
        public virtual byte[] GetBytes(long virtualPosition, int count)                                    => Inner.GetBytes(virtualPosition, count);
        public virtual byte[] GetLine(long virtualLineStart, int bytesPerLine)                             => Inner.GetLine(virtualLineStart, bytesPerLine);
        public virtual List<byte[]> GetLines(long startVirtualPosition, int lineCount, int bytesPerLine)   => Inner.GetLines(startVirtualPosition, lineCount, bytesPerLine);

        // ── Read (async / streaming) ──────────────────────────────────────────

        public virtual ValueTask<(byte value, bool success)> GetByteAsync(long virtualPosition, CancellationToken ct = default)     => Inner.GetByteAsync(virtualPosition, ct);
        public virtual ValueTask<byte[]> GetBytesAsync(long virtualPosition, int count, CancellationToken ct = default)             => Inner.GetBytesAsync(virtualPosition, count, ct);
        public virtual IAsyncEnumerable<byte[]> ReadLinesAsync(long startVirtualPosition, int bytesPerLine, CancellationToken ct = default) => Inner.ReadLinesAsync(startVirtualPosition, bytesPerLine, ct);
        public virtual Task CopyToAsync(Stream destination, long start, long count, CancellationToken ct = default)                 => Inner.CopyToAsync(destination, start, count, ct);
        public virtual Task CopyToAsync(Stream destination, CancellationToken ct = default)                                         => Inner.CopyToAsync(destination, ct);

        // ── Write ─────────────────────────────────────────────────────────────

        public virtual void ModifyByte(long virtualPosition, byte value)                        => Inner.ModifyByte(virtualPosition, value);
        public virtual void ModifyBytes(long startVirtualPosition, byte[] values)               => Inner.ModifyBytes(startVirtualPosition, values);
        public virtual void InsertBytes(long virtualPosition, byte[] bytes)                     => Inner.InsertBytes(virtualPosition, bytes);
        public virtual void InsertByte(long virtualPosition, byte value)                        => Inner.InsertByte(virtualPosition, value);
        public virtual void DeleteByte(long virtualPosition)                                    => Inner.DeleteByte(virtualPosition);
        public virtual void DeleteBytes(long startVirtualPosition, long count)                  => Inner.DeleteBytes(startVirtualPosition, count);

        // ── Save (sync) ───────────────────────────────────────────────────────

        public virtual void Save()                                          => Inner.Save();
        public virtual void SaveAs(string newFilePath, bool overwrite = false) => Inner.SaveAs(newFilePath, overwrite);

        // ── Edit management ───────────────────────────────────────────────────

        public virtual void ClearAllEdits()                                                                         => Inner.ClearAllEdits();
        public virtual void ClearModifications()                                                                    => Inner.ClearModifications();
        public virtual void ClearInsertions()                                                                       => Inner.ClearInsertions();
        public virtual void ClearDeletions()                                                                        => Inner.ClearDeletions();
        public virtual bool RestoreOriginalByte(long virtualPosition)                                               => Inner.RestoreOriginalByte(virtualPosition);
        public virtual int RestoreOriginalBytesInRange(long startVirtualPosition, long stopVirtualPosition)         => Inner.RestoreOriginalBytesInRange(startVirtualPosition, stopVirtualPosition);
        public virtual int RestoreAllModifications()                                                                => Inner.RestoreAllModifications();

        // ── Undo/Redo ─────────────────────────────────────────────────────────

        public virtual bool CanUndo                                                         => Inner.CanUndo;
        public virtual bool CanRedo                                                         => Inner.CanRedo;
        public virtual void Undo()                                                          => Inner.Undo();
        public virtual void Redo()                                                          => Inner.Redo();
        public virtual void ClearUndoRedoHistory()                                          => Inner.ClearUndoRedoHistory();
        public virtual void BeginUndoTransaction(string description)                        => Inner.BeginUndoTransaction(description);
        public virtual void CommitUndoTransaction()                                         => Inner.CommitUndoTransaction();
        public virtual void RollbackUndoTransaction()                                       => Inner.RollbackUndoTransaction();
        public virtual string? PeekUndoDescription()                                        => Inner.PeekUndoDescription();
        public virtual string? PeekRedoDescription()                                        => Inner.PeekRedoDescription();
        public virtual IReadOnlyList<string> GetUndoDescriptions(int maxCount = 20)         => Inner.GetUndoDescriptions(maxCount);
        public virtual IReadOnlyList<string> GetRedoDescriptions(int maxCount = 20)         => Inner.GetRedoDescriptions(maxCount);

        // ── Batch ─────────────────────────────────────────────────────────────

        public virtual void BeginBatch() => Inner.BeginBatch();
        public virtual void EndBatch()   => Inner.EndBatch();

        // ── Changeset ─────────────────────────────────────────────────────────

        public virtual ChangesetSnapshot GetChangesetSnapshot()                 => Inner.GetChangesetSnapshot();
        public virtual void ImportChangeset(ChangesetSnapshot snapshot)         => Inner.ImportChangeset(snapshot);
        public virtual byte[] ExportChangesetJson()                             => Inner.ExportChangesetJson();
        public virtual void ImportChangesetJson(byte[] jsonUtf8)                => Inner.ImportChangesetJson(jsonUtf8);
        public virtual void CreateCheckpoint(string name)                       => Inner.CreateCheckpoint(name);
        public virtual void RestoreCheckpoint(string name)                      => Inner.RestoreCheckpoint(name);
        public virtual bool DeleteCheckpoint(string name)                       => Inner.DeleteCheckpoint(name);
        public virtual IReadOnlyList<string> GetCheckpoints()                   => Inner.GetCheckpoints();

        // ── Search (sync) ─────────────────────────────────────────────────────

        public virtual long FindFirst(byte[] pattern, long startPosition = 0)                                           => Inner.FindFirst(pattern, startPosition);
        public virtual long FindNext(byte[] pattern, long currentPosition)                                              => Inner.FindNext(pattern, currentPosition);
        public virtual long FindLast(byte[] pattern, long startPosition = 0)                                            => Inner.FindLast(pattern, startPosition);
        public virtual IEnumerable<long> FindAll(byte[] pattern, long startPosition = 0)                                => Inner.FindAll(pattern, startPosition);
        public virtual int CountOccurrences(byte[] pattern, long startPosition = 0)                                     => Inner.CountOccurrences(pattern, startPosition);
        public virtual SearchResult Search(SearchOptions options, CancellationToken cancellationToken = default)         => Inner.Search(options, cancellationToken);

        // ── Search (async / streaming) ────────────────────────────────────────

        public virtual IAsyncEnumerable<long> FindAllAsync(byte[] pattern, long startPosition = 0, CancellationToken ct = default)      => Inner.FindAllAsync(pattern, startPosition, ct);
        public virtual IAsyncEnumerable<SearchMatch> SearchStreamAsync(SearchOptions options, CancellationToken ct = default)            => Inner.SearchStreamAsync(options, ct);

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// Unsubscribes all forwarding delegates from Inner's events.
        /// Does NOT dispose Inner — the decorator does not own the wrapped provider.
        /// Override to release additional resources; call base.Dispose() first.
        /// </summary>
        public virtual void Dispose()
        {
            Inner.ChangesCleared -= _onChangesCleared;
            Inner.ByteModified   -= _onByteModified;
            Inner.BytesInserted  -= _onBytesInserted;
            Inner.BytesDeleted   -= _onBytesDeleted;
            Inner.SaveCompleted  -= _onSaveCompleted;
        }
    }
}
