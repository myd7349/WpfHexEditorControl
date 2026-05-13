//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.BinaryAnalysis.ViewModels;

public sealed class SignatureDbViewModel : ViewModelBase
{
    private readonly UserSignatureDbStore _store;
    private IIDEHostContext? _context;
    private UserSignature? _selected;

    public ObservableCollection<UserSignature> Signatures { get; } = [];

    public UserSignature? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); }
    }

    public SignatureDbViewModel(UserSignatureDbStore store)
    {
        _store = store;
        Reload();
    }

    public void SetContext(IIDEHostContext context) => _context = context;

    public void Reload()
    {
        Signatures.Clear();
        foreach (var s in _store.GetAll()) Signatures.Add(s);
    }

    public void Add()
    {
        var sig = new UserSignature { Name = "New", HexPattern = "00", Offset = 0 };
        Signatures.Add(sig);
        Selected = sig;
        Persist();
    }

    public void Remove()
    {
        if (Selected is null) return;
        Signatures.Remove(Selected);
        Selected = null;
        Persist();
    }

    public void Persist() => _store.ReplaceAll(Signatures);

    public async Task TestOnCurrentFileAsync()
    {
        if (_context is null || !_context.HexEditor.IsActive) return;

        var sigs = _store.GetAll();
        using var stream = new HexEditorStream(_context.HexEditor);
        var header = new byte[(int)Math.Min(512, stream.Length)];
        stream.Position = 0;
        await stream.ReadExactlyAsync(header);

        int hits = 0;
        foreach (var sig in sigs)
        {
            var pattern = sig.PatternBytes();
            if (pattern is null) continue;
            if (sig.Offset + pattern.Length > header.Length) continue;
            if (header.AsSpan(sig.Offset, pattern.Length).SequenceEqual(pattern))
            {
                _context.Output?.Write("Signatures", $"HIT: {sig.Name} @ offset {sig.Offset}");
                hits++;
            }
        }
        _context.Output?.Write("Signatures", $"Test complete — {hits} hit(s) out of {sigs.Count} signature(s).");
    }
}
