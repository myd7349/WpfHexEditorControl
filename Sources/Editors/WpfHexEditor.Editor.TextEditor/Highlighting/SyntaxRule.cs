//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Text.RegularExpressions;

namespace WpfHexEditor.Editor.TextEditor.Highlighting;

/// <summary>
/// A single syntax-highlighting rule: a compiled regex + a theme color key.
/// </summary>
public sealed class SyntaxRule
{
    /// <summary>
    /// Logical name of the rule (e.g. "Keyword", "Comment").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Regex pattern as defined in the .whlang file.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Theme resource key (e.g. "TE_Keyword") used to look up the <see cref="System.Windows.Media.Brush"/>.
    /// </summary>
    public string ColorKey { get; init; } = string.Empty;

    /// <summary>
    /// Pre-compiled regex (lazy, thread-safe).
    /// Returns <see langword="null"/> when <see cref="Pattern"/> is not a valid regular expression.
    /// </summary>
    public Regex? CompiledRegex
    {
        get
        {
            if (_regexResolved) return _regex;
            _regexResolved = true;
            try
            {
                _regex = new Regex(Pattern,
                    RegexOptions.Compiled | RegexOptions.Multiline,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (ArgumentException)
            {
                // Invalid pattern from .whlang file — silently skip this rule.
                _regex = null;
            }
            return _regex;
        }
    }

    private Regex? _regex;
    private bool   _regexResolved;
}
