namespace WpfHexaEditor.Core.Platform.Input
{
    /// <summary>
    /// Platform-agnostic keyboard key enumeration.
    /// Maps to both WPF Key and Avalonia Key enums.
    /// Only includes keys used by the HexEditor control.
    /// </summary>
    public enum PlatformKey
    {
        None = 0,

        // Letters
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // Numbers (top row)
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

        // Function keys
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

        // Navigation
        Left, Right, Up, Down,
        Home, End,
        PageUp, PageDown,

        // Editing
        Enter, Return,
        Back, Delete,
        Tab,
        Space,
        Escape,

        // Modifiers
        LeftShift, RightShift,
        LeftCtrl, RightCtrl,
        LeftAlt, RightAlt,

        // Numpad
        NumPad0, NumPad1, NumPad2, NumPad3, NumPad4,
        NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,

        // Special
        Insert,
        Apps,
        Scroll,
        Pause,
        CapsLock,
        NumLock
    }

    /// <summary>
    /// Platform-agnostic keyboard modifier keys.
    /// </summary>
    [System.Flags]
    public enum PlatformModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
}
