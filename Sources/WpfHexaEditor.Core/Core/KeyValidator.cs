//////////////////////////////////////////////
// Apache 2.0  - 2016-2020
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using WpfHexaEditor.Core.Platform.Input;

namespace WpfHexaEditor.Core
{
    /// <summary>
    /// Static class for valid keyboard key.
    /// Platform-agnostic version using PlatformKey.
    /// </summary>
    public static class KeyValidator
    {
        /// <summary>
        /// Check if is a numeric key as pressed
        /// </summary>
        public static bool IsNumericKey(PlatformKey key)
        {
            return key == PlatformKey.D0 || key == PlatformKey.D1 || key == PlatformKey.D2 || key == PlatformKey.D3 || key == PlatformKey.D4 || key == PlatformKey.D5 ||
                   key == PlatformKey.D6 || key == PlatformKey.D7 || key == PlatformKey.D8 || key == PlatformKey.D9 ||
                   key == PlatformKey.NumPad0 || key == PlatformKey.NumPad1 || key == PlatformKey.NumPad2 || key == PlatformKey.NumPad3 ||
                   key == PlatformKey.NumPad4 || key == PlatformKey.NumPad5 || key == PlatformKey.NumPad6 || key == PlatformKey.NumPad7 ||
                   key == PlatformKey.NumPad8 || key == PlatformKey.NumPad9;
        }

        /// <summary>
        /// Get if key is a Hexakey (alpha)
        /// </summary>
        public static bool IsHexKey(PlatformKey key)
        {
            return key == PlatformKey.A || key == PlatformKey.B || key == PlatformKey.C || key == PlatformKey.D || key == PlatformKey.E || key == PlatformKey.F ||
                   IsNumericKey(key);
        }

        /// <summary>
        /// Get the digit from key
        /// </summary>
        public static int GetDigitFromKey(PlatformKey key)
        {
            switch (key)
            {
                case PlatformKey.D0:
                case PlatformKey.NumPad0: return 0;
                case PlatformKey.D1:
                case PlatformKey.NumPad1: return 1;
                case PlatformKey.D2:
                case PlatformKey.NumPad2: return 2;
                case PlatformKey.D3:
                case PlatformKey.NumPad3: return 3;
                case PlatformKey.D4:
                case PlatformKey.NumPad4: return 4;
                case PlatformKey.D5:
                case PlatformKey.NumPad5: return 5;
                case PlatformKey.D6:
                case PlatformKey.NumPad6: return 6;
                case PlatformKey.D7:
                case PlatformKey.NumPad7: return 7;
                case PlatformKey.D8:
                case PlatformKey.NumPad8: return 8;
                case PlatformKey.D9:
                case PlatformKey.NumPad9: return 9;
                default: throw new ArgumentOutOfRangeException("Invalid key: " + key);
            }
        }

        public static bool IsIgnoredKey(PlatformKey key)
        {
            return key == PlatformKey.Tab ||
                   key == PlatformKey.Enter ||
                   key == PlatformKey.Return ||
                   key == PlatformKey.CapsLock ||
                   key == PlatformKey.LeftAlt ||
                   key == PlatformKey.RightAlt ||
                   key == PlatformKey.LeftCtrl ||
                   key == PlatformKey.F1 || key == PlatformKey.F2 || key == PlatformKey.F3 || key == PlatformKey.F4 || key == PlatformKey.F5 || key == PlatformKey.F6 ||
                   key == PlatformKey.F7 || key == PlatformKey.F8 || key == PlatformKey.F9 || key == PlatformKey.F10 || key == PlatformKey.F11 ||
                   key == PlatformKey.F12 ||
                   key == PlatformKey.Home ||
                   key == PlatformKey.Insert ||
                   key == PlatformKey.End;
        }

        public static bool IsArrowKey(PlatformKey key) =>
            key == PlatformKey.Up || key == PlatformKey.Down || key == PlatformKey.Left || key == PlatformKey.Right;

        public static bool IsBackspaceKey(PlatformKey key) => key == PlatformKey.Back;

        public static bool IsDeleteKey(PlatformKey key) => key == PlatformKey.Delete;

        public static bool IsCapsLock(PlatformKey key) => key == PlatformKey.CapsLock;

        public static bool IsEscapeKey(PlatformKey key) => key == PlatformKey.Escape;

        public static bool IsUpKey(PlatformKey key) => key == PlatformKey.Up;

        public static bool IsDownKey(PlatformKey key) => key == PlatformKey.Down;

        public static bool IsRightKey(PlatformKey key) => key == PlatformKey.Right;

        public static bool IsLeftKey(PlatformKey key) => key == PlatformKey.Left;

        public static bool IsPageDownKey(PlatformKey key) => key == PlatformKey.PageDown;

        public static bool IsPageUpKey(PlatformKey key) => key == PlatformKey.PageUp;

        public static bool IsEnterKey(PlatformKey key) => key == PlatformKey.Enter;

        public static bool IsTabKey(PlatformKey key) => key == PlatformKey.Tab;

        // Note: Modifier-based key checks (IsCtrlCKey, IsCtrlZKey, etc.) removed
        // These should be handled at the platform-specific UI layer where modifier state is available
    }
}
