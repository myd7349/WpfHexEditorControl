//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.CodeEditor
// File: Models/ValidationError.cs
// Description: Validation error model for the CodeEditor assembly.
//              WpfHexEditor.Editor.Core defines its own canonical
//              ValidationError / ValidationSeverity / ValidationLayer.
//              The two sets share identical ordinals so that cross-assembly
//              int casts are safe.
//////////////////////////////////////////////

namespace WpfHexEditor.Editor.CodeEditor.Models;

/// <summary>Severity level of a validation diagnostic.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational — no action required.</summary>
    Info    = 0,
    /// <summary>Non-critical issue.</summary>
    Warning = 1,
    /// <summary>Critical error.</summary>
    Error   = 2,
}

/// <summary>Validation layer that detected the diagnostic.</summary>
public enum ValidationLayer
{
    /// <summary>Layer 1: JSON syntax.</summary>
    JsonSyntax  = 0,
    /// <summary>Layer 2: Schema compliance.</summary>
    Schema      = 1,
    /// <summary>Layer 3: Format-specific rules.</summary>
    FormatRules = 2,
    /// <summary>Layer 4: Semantic errors.</summary>
    Semantic    = 3,
    /// <summary>Layer 5: External Language Server diagnostics.</summary>
    Lsp         = 4,
}

/// <summary>
/// A single validation diagnostic with source location and severity.
/// Produced by <c>FormatSchemaValidator</c> and LSP integration.
/// </summary>
public class ValidationError
{
    /// <summary>Line number where the error occurs (0-based).</summary>
    public int Line { get; set; }
    /// <summary>Column where the error starts (0-based).</summary>
    public int Column { get; set; }
    /// <summary>Length of the error span (for underlining).</summary>
    public int Length { get; set; }
    /// <summary>Human-readable error message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Severity level.</summary>
    public ValidationSeverity Severity { get; set; }
    /// <summary>Optional error code.</summary>
    public string ErrorCode { get; set; } = string.Empty;
    /// <summary>Validation layer that produced this diagnostic.</summary>
    public ValidationLayer Layer { get; set; }
    /// <summary>Sub-system that produced this error (e.g. "lsp", "schema").</summary>
    public string? Source { get; set; }

    /// <summary>Initialises a diagnostic with default Error severity.</summary>
    public ValidationError()
    {
        Severity = ValidationSeverity.Error;
        Length   = 1;
    }

    /// <summary>Initialises a diagnostic with explicit location and message.</summary>
    public ValidationError(int line, int column, string message,
                           ValidationSeverity severity = ValidationSeverity.Error)
    {
        Line     = line;
        Column   = column;
        Message  = message;
        Severity = severity;
        Length   = 1;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"[{Severity}] Line {Line + 1}, Col {Column + 1}: {Message}";
}
