// ==========================================================
// Project: WpfHexEditor.Core.Events
// File: IDEEvents/HexEditorEvents.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-05-12
// Description:
//     IDE-level events published by hex-editor-aware components (Binary Analysis
//     panels, Hex Diff, etc.) to request hex-editor navigation or notify of
//     hex-editor state changes.
//
// Architecture Notes:
//     Pattern: Domain Event (record types, immutable)
//     All records inherit IDEEventBase for IIDEEventBus compatibility.
// ==========================================================

namespace WpfHexEditor.Core.Events.IDEEvents;

/// <summary>
/// Published when a consumer wants the active HexEditor to scroll to and
/// highlight a specific byte offset (e.g. double-click in String Extraction).
/// </summary>
public sealed record NavigateToOffsetEvent : IDEEventBase
{
    /// <summary>Byte offset to navigate to (0-based).</summary>
    public long Offset { get; init; }

    /// <summary>
    /// Optional display name of the panel or feature that triggered the navigation.
    /// Used for diagnostics / output messages only.
    /// </summary>
    public string Source { get; init; } = string.Empty;
}
