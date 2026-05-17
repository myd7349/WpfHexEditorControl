// ==========================================================
// Project: WpfHexEditor.Core
// File: ByteProvider.Changeset.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude (Anthropic)
// Created: 2026-03-06
// Description:
//     Partial class of ByteProvider — changeset snapshot, import,
//     serialization helpers, and named checkpoints (Phase 6).
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfHexEditor.Core.Changesets;

namespace WpfHexEditor.Core.Bytes
{
    public sealed partial class ByteProvider
    {
        // ── Checkpoints ───────────────────────────────────────────────────────

        private readonly Dictionary<string, ChangesetSnapshot> _checkpoints = new();

        /// <summary>
        /// Save the current edit state under <paramref name="name"/>.
        /// Overwrites any existing checkpoint with the same name.
        /// </summary>
        public void CreateCheckpoint(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Checkpoint name cannot be empty.", nameof(name));
            _checkpoints[name] = GetChangesetSnapshot();
        }

        /// <summary>
        /// Restore the edit state previously saved under <paramref name="name"/>.
        /// Clears all current edits first, then replays the snapshot.
        /// </summary>
        public void RestoreCheckpoint(string name)
        {
            if (!_checkpoints.TryGetValue(name, out var snapshot))
                throw new KeyNotFoundException($"Checkpoint '{name}' does not exist.");
            ImportChangeset(snapshot);
        }

        /// <summary>Delete a named checkpoint.</summary>
        public bool DeleteCheckpoint(string name) => _checkpoints.Remove(name);

        /// <summary>Names of all stored checkpoints (alphabetical).</summary>
        public IReadOnlyList<string> GetCheckpoints() =>
            _checkpoints.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        // ── Export ────────────────────────────────────────────────────────────

        /// <summary>
        /// Captures an immutable snapshot of all pending edits (modify / insert / delete).
        /// O(e) — only iterates the edit dictionaries; never reads from the physical file.
        /// Returns <see cref="ChangesetSnapshot.Empty"/> when the buffer is clean.
        /// </summary>
        public ChangesetSnapshot GetChangesetSnapshot()
        {
            if (!_editsManager.HasChanges)
                return ChangesetSnapshot.Empty;

            var modifiedRanges = BuildModifiedRanges();
            var insertedBlocks = BuildInsertedBlocks();
            var deletedRanges  = BuildDeletedRanges();

            return new ChangesetSnapshot(modifiedRanges, insertedBlocks, deletedRanges);
        }

        /// <summary>Serialize the current changeset to a UTF-8 JSON byte array.</summary>
        public byte[] ExportChangesetJson()
        {
            var snapshot = GetChangesetSnapshot();
            return JsonSerializer.SerializeToUtf8Bytes(snapshot, ChangesetJsonContext.Default.ChangesetSnapshot);
        }

        // ── Import ────────────────────────────────────────────────────────────

        /// <summary>
        /// Clear all current edits and apply <paramref name="snapshot"/>.
        /// After this call the provider reflects exactly the edits in the snapshot.
        /// </summary>
        public void ImportChangeset(ChangesetSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (IsReadOnly) throw new InvalidOperationException("Cannot import changeset: provider is read-only.");

            _editsManager.ClearAll();
            _undoRedoManager.ClearAll();

            // Apply modifications (physical positions — no virtual-to-physical mapping needed).
            foreach (var range in snapshot.Modified)
                for (int i = 0; i < range.Values.Length; i++)
                    _editsManager.ModifyByte(range.Offset + i, range.Values[i]);

            // Apply insertions.
            foreach (var block in snapshot.Inserted)
                _editsManager.InsertBytes(block.Offset, block.Bytes);

            // Physical offsets only — VirtualToPhysical mapping intentionally bypassed here.
            foreach (var range in snapshot.Deleted)
                _editsManager.DeleteBytes(range.Start, range.Count);

            InvalidateCaches();
        }

        /// <summary>Deserialize a JSON changeset (from <see cref="ExportChangesetJson"/>) and import it.</summary>
        public void ImportChangesetJson(byte[] jsonUtf8)
        {
            if (jsonUtf8 == null || jsonUtf8.Length == 0)
                throw new ArgumentException("JSON data cannot be null or empty.", nameof(jsonUtf8));

            var snapshot = JsonSerializer.Deserialize(jsonUtf8, ChangesetJsonContext.Default.ChangesetSnapshot)
                ?? throw new InvalidOperationException("Failed to deserialize changeset JSON.");
            ImportChangeset(snapshot);
        }

        // ── Private builders ──────────────────────────────────────────────────

        private List<ModifiedRange> BuildModifiedRanges()
        {
            var result = new List<ModifiedRange>();
            long runStart = -1;
            long prevPos  = -2;
            var  runValues = new List<byte>();

            foreach (var kvp in _editsManager.GetAllModifiedBytes())
            {
                if (kvp.Key == prevPos + 1)
                {
                    runValues.Add(kvp.Value);
                }
                else
                {
                    if (runValues.Count > 0)
                        result.Add(new ModifiedRange(runStart, runValues.ToArray()));
                    runStart = kvp.Key;
                    runValues.Clear();
                    runValues.Add(kvp.Value);
                }
                prevPos = kvp.Key;
            }
            if (runValues.Count > 0)
                result.Add(new ModifiedRange(runStart, runValues.ToArray()));
            return result;
        }

        private List<InsertedBlock> BuildInsertedBlocks()
        {
            var result = new List<InsertedBlock>();
            foreach (var kvp in _editsManager.GetInsertionPositionsWithCounts())
            {
                var ordered = _editsManager.GetInsertedBytesAt(kvp.Key)
                    .OrderBy(ib => ib.VirtualOffset)
                    .Select(ib => ib.Value)
                    .ToArray();
                if (ordered.Length > 0)
                    result.Add(new InsertedBlock(kvp.Key, ordered));
            }
            return result;
        }

        private List<DeletedRange> BuildDeletedRanges()
        {
            var result = new List<DeletedRange>();
            long rangeStart = -1;
            long prevDel    = -2;
            long rangeCount = 0;

            foreach (var pos in _editsManager.GetAllDeletedPositions())
            {
                if (pos == prevDel + 1)
                {
                    rangeCount++;
                }
                else
                {
                    if (rangeCount > 0)
                        result.Add(new DeletedRange(rangeStart, rangeCount));
                    rangeStart = pos;
                    rangeCount = 1;
                }
                prevDel = pos;
            }
            if (rangeCount > 0)
                result.Add(new DeletedRange(rangeStart, rangeCount));
            return result;
        }
    }

    // ── Source-generated JSON context (AOT-safe, no reflection) ──────────────

    [JsonSerializable(typeof(ChangesetSnapshot))]
    [JsonSerializable(typeof(ModifiedRange))]
    [JsonSerializable(typeof(InsertedBlock))]
    [JsonSerializable(typeof(DeletedRange))]
    internal sealed partial class ChangesetJsonContext : JsonSerializerContext { }
}
