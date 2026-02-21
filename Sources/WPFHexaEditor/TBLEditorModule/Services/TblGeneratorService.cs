//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.SearchModule.Models;
using WpfHexaEditor.SearchModule.Services;
using WpfHexaEditor.TBLEditorModule.Models;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Services
{
    /// <summary>
    /// Service for generating TBL entries from sample text using RelativeSearchEngine
    /// </summary>
    public class TblGeneratorService
    {
        private readonly ByteProvider _byteProvider;
        private readonly TblStream _targetTbl;

        public TblGeneratorService(ByteProvider byteProvider, TblStream targetTbl)
        {
            _byteProvider = byteProvider ?? throw new ArgumentNullException(nameof(byteProvider));
            _targetTbl = targetTbl ?? throw new ArgumentNullException(nameof(targetTbl));
        }

        /// <summary>
        /// Generate TBL entries from sample text asynchronously
        /// </summary>
        public async Task<TblGenerationResult> GenerateFromTextAsync(
            TblGenerationOptions options,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() => GenerateFromText(options, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Generate TBL entries from sample text (synchronous)
        /// </summary>
        public TblGenerationResult GenerateFromText(
            TblGenerationOptions options,
            CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var result = new TblGenerationResult();

            try
            {
                // Step 1: Create RelativeSearchEngine
                var searchEngine = new RelativeSearchEngine(_byteProvider, _targetTbl);

                // Step 2: Convert TblGenerationOptions to RelativeSearchOptions
                var searchOptions = new RelativeSearchOptions
                {
                    SearchText = options.SampleText,
                    CaseSensitive = options.CaseSensitive,
                    StartPosition = options.StartPosition,
                    EndPosition = options.EndPosition,
                    MinMatchesRequired = options.MinMatches,
                    MaxProposals = 10,
                    UseParallelSearch = true
                };

                // Step 3: Perform relative search
                var searchResult = searchEngine.Search(searchOptions, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    result.Success = false;
                    result.ErrorMessage = "Operation cancelled by user";
                    return result;
                }

                if (!searchResult.Success || searchResult.Proposals.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = searchResult.ErrorMessage ?? "No encoding proposals found";
                    return result;
                }

                // Step 4: Convert proposals to TblEntryViewModel list
                result.Proposals = ConvertProposalsToEntries(searchResult.Proposals);
                result.Success = true;
                result.SearchDurationMs = searchResult.DurationMs;
                result.TotalProposals = searchResult.Proposals.Count;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error generating TBL: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Convert EncodingProposal list to TblProposal list
        /// </summary>
        private List<TblProposal> ConvertProposalsToEntries(List<EncodingProposal> encodingProposals)
        {
            var tblProposals = new List<TblProposal>();

            foreach (var proposal in encodingProposals)
            {
                var tblProposal = new TblProposal
                {
                    Offset = proposal.Offset,
                    Score = proposal.Score,
                    MatchCount = proposal.MatchCount,
                    SampleText = proposal.SampleText,
                    PreviewText = proposal.PreviewText,
                    Entries = new List<TblEntryViewModel>()
                };

                // Convert character mapping to TBL entries
                foreach (var mapping in proposal.CharacterMapping.OrderBy(m => m.Value.actualByte))
                {
                    string hexKey = ByteConverters.ByteToHex(mapping.Value.actualByte);
                    char character = mapping.Value.character;

                    var dte = new Dte(hexKey, character.ToString());
                    var entry = new TblEntryViewModel(dte);

                    tblProposal.Entries.Add(entry);
                }

                tblProposals.Add(tblProposal);
            }

            return tblProposals;
        }

        /// <summary>
        /// Merge a selected proposal into the target TBL
        /// </summary>
        public TblMergeResult MergeProposal(
            TblProposal proposal,
            MergeStrategy strategy,
            IEnumerable<TblEntryViewModel> existingEntries = null)
        {
            var result = new TblMergeResult();
            var conflicts = new List<TblMergeConflict>();

            foreach (var newEntry in proposal.Entries)
            {
                string hexKey = newEntry.Entry.ToUpperInvariant();

                // Check if entry already exists
                var existing = _targetTbl.GetEntry(hexKey);

                if (existing != null)
                {
                    // Conflict detected
                    if (strategy == MergeStrategy.Skip)
                    {
                        result.Skipped.Add(hexKey);
                        continue;
                    }
                    else if (strategy == MergeStrategy.Ask)
                    {
                        conflicts.Add(new TblMergeConflict
                        {
                            HexKey = hexKey,
                            ExistingValue = existing.Value,
                            NewValue = newEntry.Value
                        });
                        continue;
                    }
                    else if (strategy == MergeStrategy.Overwrite)
                    {
                        // Remove existing entry
                        _targetTbl.Remove(existing);
                        result.Overwritten.Add(hexKey);
                    }
                }

                // Add new entry
                try
                {
                    var dte = new Dte(hexKey, newEntry.Value);

                    if (dte.IsValid)
                    {
                        _targetTbl.Add(dte);
                        result.Added.Add(hexKey);
                    }
                }
                catch (Exception)
                {
                    result.Skipped.Add(hexKey);
                }
            }

            result.Conflicts = conflicts;
            return result;
        }

        /// <summary>
        /// Preview what would happen if proposal is merged (without actually merging)
        /// </summary>
        public TblMergeResult PreviewMerge(TblProposal proposal)
        {
            var result = new TblMergeResult();

            foreach (var newEntry in proposal.Entries)
            {
                string hexKey = newEntry.Entry.ToUpperInvariant();
                var existing = _targetTbl.GetEntry(hexKey);

                if (existing != null)
                {
                    result.Conflicts.Add(new TblMergeConflict
                    {
                        HexKey = hexKey,
                        ExistingValue = existing.Value,
                        NewValue = newEntry.Value
                    });
                }
                else
                {
                    result.Added.Add(hexKey);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolve conflicts by applying user decisions
        /// </summary>
        public void ResolveConflicts(
            TblProposal proposal,
            Dictionary<string, ConflictResolution> resolutions)
        {
            foreach (var newEntry in proposal.Entries)
            {
                string hexKey = newEntry.Entry.ToUpperInvariant();

                if (resolutions.TryGetValue(hexKey, out var resolution))
                {
                    var existing = _targetTbl.GetEntry(hexKey);

                    if (resolution == ConflictResolution.UseNew)
                    {
                        if (existing != null)
                            _targetTbl.Remove(existing);

                        var dte = new Dte(hexKey, newEntry.Value);
                        if (dte.IsValid)
                            _targetTbl.Add(dte);
                    }
                    else if (resolution == ConflictResolution.KeepExisting)
                    {
                        // Do nothing, keep existing
                    }
                }
            }
        }
    }

    /// <summary>
    /// Result of TBL generation from text
    /// </summary>
    public class TblGenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<TblProposal> Proposals { get; set; } = new List<TblProposal>();
        public long SearchDurationMs { get; set; }
        public int TotalProposals { get; set; }
    }

    /// <summary>
    /// A single TBL proposal with score and entries
    /// </summary>
    public class TblProposal
    {
        public byte Offset { get; set; }
        public double Score { get; set; }
        public int MatchCount { get; set; }
        public string SampleText { get; set; }
        public string PreviewText { get; set; }
        public List<TblEntryViewModel> Entries { get; set; } = new List<TblEntryViewModel>();

        public string ScoreDisplay => $"{Score:F1}";
        public string OffsetDisplay => $"+{Offset:D3}";
        public int EntryCount => Entries.Count;
    }

    /// <summary>
    /// Conflict resolution decision
    /// </summary>
    public enum ConflictResolution
    {
        UseNew,
        KeepExisting,
        Skip
    }
}
