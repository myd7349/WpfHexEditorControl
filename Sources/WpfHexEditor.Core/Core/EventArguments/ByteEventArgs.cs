// ==========================================================
// Project: WpfHexEditor.Core
// File: ByteEventArgs.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude (Anthropic), ehsan69h
// Created: 2026-03-06
// Description:
//     Custom EventArgs passing byte value and position information to event
//     subscribers for byte-level editing events raised by byte control elements
//     in the hex editor view.
//
// Architecture Notes:
//     Originally authored by ehsan69h. Located in EventArguments (legacy namespace).
//     Pure event data container — no WPF dependencies.
//
// ==========================================================

using System;

namespace WpfHexEditor.Core.EventArguments
{
    /// <summary>
    /// Custom event arguments used for pass somes informations to delegate
    /// </summary>
    public class ByteEventArgs : EventArgs
    {
        #region Constructors
        public ByteEventArgs() { }

        public ByteEventArgs(long position) => BytePositionInStream = position;

        public ByteEventArgs(long position, int index)
        {
            BytePositionInStream = position;
            Index = index;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Pass the position of byte 
        /// </summary>
        public long BytePositionInStream { get; set; }

        /// <summary>
        /// Pass index if byte using with BytePositionInStream in somes situations 
        /// </summary>
        public int Index { get; set; }
        #endregion
    }
}
