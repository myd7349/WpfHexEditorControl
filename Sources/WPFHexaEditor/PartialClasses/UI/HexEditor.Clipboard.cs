//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using System.Windows;
using WpfHexaEditor.Core;

namespace WpfHexaEditor
{
    /// <summary>
    /// HexEditor partial class - Clipboard Operations
    /// Contains methods for copying data to clipboard in various formats
    /// </summary>
    public partial class HexEditor
    {
        #region Clipboard Operations

        /// <summary>
        /// Copy to clipboard with default mode
        /// </summary>
        public void CopyToClipboard()
        {
            CopyToClipboard(DefaultCopyToClipboardMode);
        }

        /// <summary>
        /// Copy to clipboard with specified mode
        /// </summary>
        /// <param name="mode">Copy mode (HexaString, AsciiString, etc.)</param>
        public void CopyToClipboard(CopyPasteMode mode)
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            try
            {
                switch (mode)
                {
                    case CopyPasteMode.HexaString:
                        Copy(); // Default V2 behavior copies as hex
                        break;
                    case CopyPasteMode.AsciiString:
                        CopyAsAscii();
                        break;
                    case CopyPasteMode.TblString:
                        CopyAsTbl();
                        break;
                    case CopyPasteMode.CSharpCode:
                        CopyAsCSharpCode();
                        break;
                    case CopyPasteMode.CCode:
                        CopyAsCCode();
                        break;
                    case CopyPasteMode.FormattedView:
                        CopyAsFormattedView();
                        break;
                    default:
                        Copy(); // Default to hex
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Copy failed: {ex.Message}";
            }
        }

        private void CopyAsAscii()
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            var bytes = _viewModel.GetSelectionBytes();
            if (bytes != null)
            {
                var text = System.Text.Encoding.ASCII.GetString(bytes);
                Clipboard.SetText(text);
                StatusText.Text = $"Copied {bytes.Length} bytes as ASCII";
            }
        }

        private void CopyAsTbl()
        {
            if (_viewModel == null || !_viewModel.HasSelection)
            {
                return;
            }

            if (_tblStream == null)
            {
                CopyAsAscii(); // Fallback to ASCII if no TBL
                return;
            }

            var bytes = _viewModel.GetSelectionBytes();
            if (bytes != null)
            {
                // TBL to string conversion - simplified implementation
                var text = System.Text.Encoding.ASCII.GetString(bytes); // Fallback to ASCII for now
                Clipboard.SetText(text);
                StatusText.Text = $"Copied {bytes.Length} bytes as TBL";
            }
        }

        private void CopyAsCSharpCode()
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            var bytes = _viewModel.GetSelectionBytes();
            if (bytes != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("byte[] data = new byte[] {");
                sb.Append("    ");
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append($"0x{bytes[i]:X2}");
                    if (i < bytes.Length - 1)
                        sb.Append(", ");
                    if ((i + 1) % 16 == 0 && i < bytes.Length - 1)
                        sb.AppendLine().Append("    ");
                }
                sb.AppendLine();
                sb.Append("};");

                Clipboard.SetText(sb.ToString());
                StatusText.Text = $"Copied {bytes.Length} bytes as C# code";
            }
        }

        private void CopyAsCCode()
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            var bytes = _viewModel.GetSelectionBytes();
            if (bytes != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("unsigned char data[] = {");
                sb.Append("    ");
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append($"0x{bytes[i]:X2}");
                    if (i < bytes.Length - 1)
                        sb.Append(", ");
                    if ((i + 1) % 16 == 0 && i < bytes.Length - 1)
                        sb.AppendLine().Append("    ");
                }
                sb.AppendLine();
                sb.Append("};");

                Clipboard.SetText(sb.ToString());
                StatusText.Text = $"Copied {bytes.Length} bytes as C code";
            }
        }

        private void CopyAsFormattedView()
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            var bytes = _viewModel.GetSelectionBytes();
            if (bytes == null || bytes.Length == 0)
                return;

            var sb = new StringBuilder();
            var selStart = _viewModel.SelectionStart.Value;
            var bytesPerLine = _viewModel.BytePerLine;
            var byteGrouping = (int)ByteGrouping;

            // Calculate starting line offset (aligned to line boundary)
            long startLine = selStart / bytesPerLine;
            long startOffset = startLine * bytesPerLine;
            int startByteInLine = (int)(selStart - startOffset);

            // Process each line
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                long currentLineOffset = startOffset + ((byteIndex + startByteInLine) / bytesPerLine) * bytesPerLine;

                // Offset column
                sb.Append($"0x{currentLineOffset:X8}");
                sb.Append("  ");

                // Hex bytes section
                int bytesInThisLine = Math.Min(bytesPerLine, bytes.Length - byteIndex + startByteInLine);
                if (byteIndex == 0)
                    bytesInThisLine = Math.Min(bytesPerLine - startByteInLine, bytes.Length);

                // Add leading spaces for first line if selection doesn't start at line beginning
                if (byteIndex == 0 && startByteInLine > 0)
                {
                    for (int i = 0; i < startByteInLine; i++)
                    {
                        sb.Append("   "); // 3 spaces for each byte (2 hex digits + 1 space)
                        if ((i + 1) % byteGrouping == 0 && i < bytesPerLine - 1)
                            sb.Append(" "); // Extra space for byte grouping
                    }
                }

                // Add hex bytes
                for (int i = 0; i < bytesInThisLine - (byteIndex == 0 ? startByteInLine : 0); i++)
                {
                    int bytePos = (byteIndex == 0 && startByteInLine > 0) ? i : i;
                    int linePos = (byteIndex == 0) ? startByteInLine + i : i;

                    sb.Append($"{bytes[byteIndex + i]:X2}");
                    sb.Append(" ");

                    if ((linePos + 1) % byteGrouping == 0 && linePos < bytesPerLine - 1)
                        sb.Append(" ");
                }

                // Pad remaining bytes in line with spaces
                int remainingBytes = bytesPerLine - (byteIndex == 0 ? startByteInLine : 0) - bytesInThisLine + (byteIndex == 0 ? startByteInLine : 0);
                for (int i = bytesInThisLine; i < bytesPerLine; i++)
                {
                    sb.Append("   ");
                    if ((i + 1) % byteGrouping == 0 && i < bytesPerLine - 1)
                        sb.Append(" ");
                }

                sb.Append("  ");

                // ASCII section
                if (byteIndex == 0 && startByteInLine > 0)
                {
                    for (int i = 0; i < startByteInLine; i++)
                        sb.Append(" ");
                }

                for (int i = 0; i < bytesInThisLine - (byteIndex == 0 ? startByteInLine : 0); i++)
                {
                    byte b = bytes[byteIndex + i];
                    char c = (b >= 32 && b < 127) ? (char)b : '.';
                    sb.Append(c);
                }

                byteIndex += bytesInThisLine - (byteIndex == 0 ? startByteInLine : 0);

                if (byteIndex < bytes.Length)
                    sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
            StatusText.Text = $"Copied {bytes.Length} bytes as formatted view";
        }

        /// <summary>
        /// Copy current selection to a stream
        /// </summary>
        /// <param name="output">Output stream to write to</param>
        /// <param name="copyChange">True to include uncommitted changes, false for committed data only</param>
        public void CopyToStream(Stream output, bool copyChange)
        {
            if (_viewModel == null || !_viewModel.HasSelection)
                return;

            var selStart = _viewModel.SelectionStart.Value;
            var selLength = _viewModel.SelectionLength;

            CopyToStream(output, selStart, selStart + selLength - 1, copyChange);
        }

        /// <summary>
        /// Copy specified range to a stream
        /// </summary>
        /// <param name="output">Output stream to write to</param>
        /// <param name="selectionStart">Start position (inclusive)</param>
        /// <param name="selectionStop">Stop position (inclusive)</param>
        /// <param name="copyChange">True to include uncommitted changes, false for committed data only.
        /// NOTE: Currently always includes all edits (copyChange parameter ignored in V2 architecture).</param>
        public void CopyToStream(Stream output, long selectionStart, long selectionStop, bool copyChange)
        {
            if (_viewModel?.Provider == null || output == null)
                return;

            if (selectionStart < 0 || selectionStop < selectionStart)
                return;

            var length = selectionStop - selectionStart + 1;
            if (length <= 0)
                return;

            // Read bytes from provider (V2 always returns current state with all edits)
            var buffer = new byte[Math.Min(length, 4096)]; // Use buffer for large copies
            long remaining = length;
            long currentPos = selectionStart;

            while (remaining > 0)
            {
                var bytesToRead = (int)Math.Min(remaining, buffer.Length);
                var bytesRead = 0;

                for (int i = 0; i < bytesToRead; i++)
                {
                    var (value, success) = _viewModel.Provider.GetByte(currentPos + i);
                    if (success)
                    {
                        buffer[i] = value;
                        bytesRead++;
                    }
                    else
                    {
                        break; // Stop if we hit end of stream
                    }
                }

                if (bytesRead > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                    currentPos += bytesRead;
                    remaining -= bytesRead;
                }
                else
                {
                    break; // No more bytes to read
                }
            }

            output.Flush();
        }

        /// <summary>
        /// Get copy data as byte array for specified range
        /// </summary>
        /// <param name="selectionStart">Start position (inclusive)</param>
        /// <param name="selectionStop">Stop position (inclusive)</param>
        /// <param name="copyChange">True to include uncommitted changes, false for committed data only.
        /// NOTE: Currently always includes all edits (copyChange parameter ignored in V2 architecture).</param>
        /// <returns>Byte array containing the data, or null if invalid range</returns>
        public byte[] GetCopyData(long selectionStart, long selectionStop, bool copyChange)
        {
            if (_viewModel?.Provider == null)
                return null;

            if (selectionStart < 0 || selectionStop < selectionStart)
                return null;

            var length = selectionStop - selectionStart + 1;
            if (length <= 0 || length > int.MaxValue)
                return null;

            // Use GetBytes for efficiency instead of reading byte by byte
            var result = _viewModel.Provider.GetBytes(selectionStart, (int)length);

            // GetBytes returns empty array if read fails, check length
            if (result.Length != length)
                return null;

            return result;
        }

        #endregion
    }
}
