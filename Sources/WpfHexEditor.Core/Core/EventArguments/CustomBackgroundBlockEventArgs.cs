// ==========================================================
// Project: WpfHexEditor.Core
// File: CustomBackgroundBlockEventArgs.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude (Anthropic)
// Created: 2026-03-06
// Description:
//     Custom EventArgs for events related to custom background block changes
//     in the hex editor, carrying the affected block and change type
//     information to subscribers.
//
// Architecture Notes:
//     Located in EventArguments (legacy namespace); mirrored in Events/.
//     Pure event data container — no WPF dependencies.
//
// ==========================================================

using System;

namespace WpfHexEditor.Core.EventArguments
{
    /// <summary>
    /// Custom event arguments used for pass somes informations to delegate
    /// </summary>
    public class CustomBackgroundBlockEventArgs : EventArgs
    {
        public CustomBackgroundBlockEventArgs() { }

        public CustomBackgroundBlockEventArgs(CustomBackgroundBlock customBlock) => CustomBlock = customBlock;

        /// <summary>
        /// CustomBackgroundBlock to pass in arguments
        /// /// </summary>
        public CustomBackgroundBlock CustomBlock;
    }
}
