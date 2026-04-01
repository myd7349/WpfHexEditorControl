// Project: WpfHexEditor.SDK
// File: Contracts/ITitleBarContributor.cs
// Description: Interface for plugins to contribute buttons/icons to the IDE title bar.
// Architecture: Registered via IUIRegistry.RegisterTitleBarItem; displayed in TitleBarPluginZone.

using System.Windows;

namespace WpfHexEditor.SDK.Contracts;

/// <summary>
/// Allows a plugin to contribute a UI element to the IDE title bar
/// (displayed between the main menu and the notification bell).
/// </summary>
public interface ITitleBarContributor
{
    /// <summary>Unique identifier for this contributor.</summary>
    string ContributorId { get; }

    /// <summary>
    /// Creates the WPF element to display in the title bar.
    /// Typically a Button or Border with an icon and optional badge.
    /// </summary>
    UIElement CreateButton();

    /// <summary>
    /// Display order. Lower values appear closer to the notification bell (right side).
    /// Default: 100. Claude AI uses 10.
    /// </summary>
    int Order { get; }
}
