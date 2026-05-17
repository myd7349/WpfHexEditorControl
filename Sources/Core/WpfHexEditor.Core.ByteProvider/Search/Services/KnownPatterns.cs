// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: KnownPatterns.cs
// Description:
//     Pre-built byte patterns for common binary file format magic bytes.
//     All properties return ReadOnlyMemory<byte> to prevent caller mutation.
// ==========================================================

using System;

namespace WpfHexEditor.Core.Search.Services
{
    /// <summary>
    /// Pre-built binary magic-byte patterns for common file formats.
    /// Pass <c>.Span</c> or <c>.ToArray()</c> to any ByteProvider search method.
    /// </summary>
    public static class KnownPatterns
    {
        // ── Executables ───────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> PeHeader      { get; } = new byte[] { 0x4D, 0x5A };
        public static ReadOnlyMemory<byte> ElfMagic      { get; } = new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        public static ReadOnlyMemory<byte> MachO32       { get; } = new byte[] { 0xCE, 0xFA, 0xED, 0xFE };
        public static ReadOnlyMemory<byte> MachO64       { get; } = new byte[] { 0xCF, 0xFA, 0xED, 0xFE };

        // ── Archives ─────────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> ZipLocalHeader { get; } = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        public static ReadOnlyMemory<byte> ZipCentralDir  { get; } = new byte[] { 0x50, 0x4B, 0x01, 0x02 };
        public static ReadOnlyMemory<byte> GzipMagic      { get; } = new byte[] { 0x1F, 0x8B };
        public static ReadOnlyMemory<byte> BzipMagic      { get; } = new byte[] { 0x42, 0x5A, 0x68 };
        public static ReadOnlyMemory<byte> SevenZipMagic  { get; } = new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C };

        // ── Images ────────────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> PngMagic  { get; } = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        public static ReadOnlyMemory<byte> JpegMagic { get; } = new byte[] { 0xFF, 0xD8, 0xFF };
        public static ReadOnlyMemory<byte> GifMagic87 { get; } = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
        public static ReadOnlyMemory<byte> GifMagic89 { get; } = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
        public static ReadOnlyMemory<byte> BmpMagic   { get; } = new byte[] { 0x42, 0x4D };
        public static ReadOnlyMemory<byte> WebpMagic  { get; } = new byte[] { 0x52, 0x49, 0x46, 0x46 };

        // ── Documents ─────────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> PdfMagic { get; } = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        public static ReadOnlyMemory<byte> RtfMagic { get; } = new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 };

        // ── Databases ─────────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> SqliteMagic { get; } = new byte[] {
            0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66,
            0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00
        };

        // ── Text encodings ────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> Utf8Bom    { get; } = new byte[] { 0xEF, 0xBB, 0xBF };
        public static ReadOnlyMemory<byte> Utf16LeBom { get; } = new byte[] { 0xFF, 0xFE };
        public static ReadOnlyMemory<byte> Utf16BeBom { get; } = new byte[] { 0xFE, 0xFF };

        // ── Java ──────────────────────────────────────────────────────────────
        public static ReadOnlyMemory<byte> ClassMagic { get; } = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
    }
}
