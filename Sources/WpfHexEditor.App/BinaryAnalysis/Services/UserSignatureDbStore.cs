//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Text.Json;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

/// <summary>A user-defined binary signature for file carving / detection.</summary>
public sealed class UserSignature
{
    public string Name        { get; set; } = string.Empty;
    public string HexPattern  { get; set; } = string.Empty; // e.g. "504B0304"
    public int    Offset      { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Parsed byte array; null if <see cref="HexPattern"/> is invalid.</summary>
    public byte[]? PatternBytes()
    {
        try   { return Convert.FromHexString(HexPattern.Replace(" ", "")); }
        catch { return null; }
    }
}

/// <summary>
/// JSON-backed store for user-defined signatures persisted to
/// <c>%AppData%\WpfHexEditor\signatures.json</c>.
/// </summary>
public sealed class UserSignatureDbStore
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexEditor", "signatures.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;
    private List<UserSignature>? _cache;
    private readonly object _lock = new();

    public UserSignatureDbStore() : this(DefaultPath) { }
    public UserSignatureDbStore(string path) => _path = path;

    public IReadOnlyList<UserSignature> GetAll()
    {
        lock (_lock) return EnsureLoaded().ToList();
    }

    public void ReplaceAll(IEnumerable<UserSignature> signatures)
    {
        var list = signatures.Select(s => new UserSignature
        {
            Name        = s.Name,
            HexPattern  = s.HexPattern,
            Offset      = s.Offset,
            Description = s.Description,
        }).ToList();

        lock (_lock)
        {
            _cache = list;
            Save(list);
        }
    }

    private List<UserSignature> EnsureLoaded() => _cache ??= Load();

    private List<UserSignature> Load()
    {
        try
        {
            if (!File.Exists(_path)) return [];
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<UserSignature>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSignatureDbStore] load failed: {ex.Message}");
            return [];
        }
    }

    private void Save(List<UserSignature> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, JsonSerializer.Serialize(list, JsonOptions));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSignatureDbStore] save failed: {ex.Message}");
        }
    }
}
