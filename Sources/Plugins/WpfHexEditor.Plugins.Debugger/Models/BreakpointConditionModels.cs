// ==========================================================
// Project: WpfHexEditor.Plugins.Debugger
// File: Models/BreakpointConditionModels.cs
// Description:
//     Enums and record defining the full breakpoint condition/action/options
//     model used by BreakpointConditionDialog and the debugger service.
// Architecture:
//     Plugin-layer types only. IDebuggerService.UpdateBreakpointSettingsAsync
//     accepts BreakpointSettings and maps it to BreakpointLocation extensions.
// ==========================================================

namespace WpfHexEditor.Plugins.Debugger.Models;

/// <summary>The kind of condition applied to a breakpoint.</summary>
public enum BreakpointConditionKind
{
    /// <summary>No condition — always breaks.</summary>
    None,

    /// <summary>Breaks when the boolean expression is true or changes.</summary>
    ConditionalExpression,

    /// <summary>Breaks when the hit count matches the operator/target.</summary>
    HitCount,

    /// <summary>Breaks when machine/process/thread filter matches.</summary>
    Filter,
}

/// <summary>Evaluation mode for a conditional-expression breakpoint.</summary>
public enum BreakpointConditionMode
{
    /// <summary>Break when the expression evaluates to true.</summary>
    IsTrue,

    /// <summary>Break when the expression value changes between hits.</summary>
    WhenChanged,
}

/// <summary>Comparison operator for a hit-count breakpoint.</summary>
public enum BreakpointHitCountOp
{
    /// <summary>Break when hit count equals target.</summary>
    Equal,

    /// <summary>Break when hit count is greater than or equal to target.</summary>
    GreaterOrEqual,

    /// <summary>Break when hit count is a multiple of target.</summary>
    MultipleOf,
}

/// <summary>
/// Full settings snapshot produced by <see cref="WpfHexEditor.Plugins.Debugger.Dialogs.BreakpointConditionDialog"/>.
/// Passed to <c>IDebuggerService.UpdateBreakpointSettingsAsync</c> after the dialog closes.
/// </summary>
public sealed record BreakpointSettings(
    // ── Condition ──────────────────────────────────────────────────────────
    BreakpointConditionKind ConditionKind     = BreakpointConditionKind.None,
    string?                 ConditionExpr     = null,
    BreakpointConditionMode ConditionMode     = BreakpointConditionMode.IsTrue,
    BreakpointHitCountOp    HitCountOp        = BreakpointHitCountOp.Equal,
    int                     HitCountTarget    = 1,
    string?                 FilterExpr        = null,

    // ── Action ─────────────────────────────────────────────────────────────
    bool                    HasAction         = false,
    string?                 LogMessage        = null,
    bool                    ContinueExecution = true,

    // ── Options ────────────────────────────────────────────────────────────
    bool                    DisableOnceHit    = false,
    string?                 DependsOnBpKey    = null
);
