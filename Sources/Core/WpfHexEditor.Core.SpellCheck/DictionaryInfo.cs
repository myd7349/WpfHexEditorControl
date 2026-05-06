// ==========================================================
// Project: WpfHexEditor.Core.SpellCheck
// File: DictionaryInfo.cs
// Description: Metadata for a spell-check dictionary entry.
// ==========================================================

namespace WpfHexEditor.Core.SpellCheck;

public sealed record DictionaryInfo(
    string LanguageCode,
    string DisplayName,
    bool   IsInstalled,
    string DicPath,
    string AffPath);
