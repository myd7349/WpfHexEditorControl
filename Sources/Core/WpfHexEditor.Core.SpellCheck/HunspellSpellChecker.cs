// ==========================================================
// Project: WpfHexEditor.Core.SpellCheck
// File: HunspellSpellChecker.cs
// Description:
//     ISpellChecker implementation backed by WeCantSpell.Hunspell.
//     WordList is immutable after load — thread-safe for CheckWord/Suggest.
//     User dictionary words are merged into an in-memory HashSet and
//     appended to userdict.txt on AddToUserDictionary.
// ==========================================================

using System.IO;
using WeCantSpell.Hunspell;

namespace WpfHexEditor.Core.SpellCheck;

public sealed class HunspellSpellChecker : ISpellChecker
{
    private readonly SpellCheckerSettings _settings;
    private readonly DictionaryManager    _dictManager;
    private WordList?                     _wordList;
    private readonly HashSet<string>      _userWords = new(StringComparer.OrdinalIgnoreCase);
    private string?                       _activeLanguage;
    private readonly SemaphoreSlim        _loadLock = new(1, 1);

    public bool    IsLoaded       => _wordList is not null;
    public string? ActiveLanguage => _activeLanguage;

    public event EventHandler? DictionaryChanged;

    public HunspellSpellChecker(SpellCheckerSettings settings, DictionaryManager dictManager)
    {
        _settings    = settings;
        _dictManager = dictManager;
        LoadUserWords();
    }

    public async Task LoadAsync(string languageCode, CancellationToken ct = default)
    {
        var info = _dictManager.GetInfo(languageCode);
        if (info is null || !info.IsInstalled) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            _wordList = await WordList.CreateFromFilesAsync(info.DicPath, info.AffPath, ct);
            _activeLanguage = languageCode;
        }
        finally { _loadLock.Release(); }

        DictionaryChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CheckWord(string word)
    {
        if (_wordList is null) return true;
        if (_userWords.Contains(word)) return true;
        return _wordList.Check(word);
    }

    public IReadOnlyList<string> Suggest(string word, int maxSuggestions = 5)
    {
        if (_wordList is null) return [];
        return [.. _wordList.Suggest(word).Take(maxSuggestions)];
    }

    public void AddToUserDictionary(string word)
    {
        if (_userWords.Add(word))
            AppendToUserDictFile(word);
    }

    private string UserDictPath => Path.Combine(_settings.DictionariesPath, "userdict.txt");

    private void LoadUserWords()
    {
        try
        {
            if (!File.Exists(UserDictPath)) return;
            foreach (var line in File.ReadLines(UserDictPath))
            {
                var w = line.Trim();
                if (w.Length > 0) _userWords.Add(w);
            }
        }
        catch { }
    }

    private void AppendToUserDictFile(string word)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(UserDictPath)!);
            File.AppendAllText(UserDictPath, word + Environment.NewLine);
        }
        catch { }
    }
}
