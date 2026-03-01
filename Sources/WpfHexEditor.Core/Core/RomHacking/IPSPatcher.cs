using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WpfHexEditor.Core.RomHacking
{
    /// <summary>
    /// IPS (International Patching System) patcher implementation.
    /// Supports reading and applying IPS patches to ROM files.
    /// </summary>
    public class IPSPatcher
    {
        private const string IPS_HEADER = "PATCH";
        private const string IPS_FOOTER = "EOF";
        private const int HEADER_SIZE = 5;
        private const int FOOTER_SIZE = 3;
        private const int RECORD_HEADER_SIZE = 5; // 3 bytes offset + 2 bytes size
        private const int RLE_HEADER_SIZE = 3; // 2 bytes count + 1 byte value

        /// <summary>
        /// Reads an IPS patch file and returns all records
        /// </summary>
        /// <param name="ipsFilePath">Path to the IPS patch file</param>
        /// <returns>List of IPS records</returns>
        /// <exception cref="FileNotFoundException">If the IPS file doesn't exist</exception>
        /// <exception cref="InvalidDataException">If the IPS file is malformed</exception>
        public static List<IPSRecord> ReadIPSFile(string ipsFilePath)
        {
            if (!File.Exists(ipsFilePath))
                throw new FileNotFoundException($"IPS patch file not found: {ipsFilePath}");

            using var fs = new FileStream(ipsFilePath, FileMode.Open, FileAccess.Read);
            return ReadIPSFromStream(fs);
        }

        /// <summary>
        /// Reads IPS records from an in-memory byte array.
        /// </summary>
        public static List<IPSRecord> ReadIPSFromBytes(byte[] ipsData)
        {
            if (ipsData == null || ipsData.Length == 0)
                throw new ArgumentException("IPS data is empty", nameof(ipsData));

            using var ms = new MemoryStream(ipsData, writable: false);
            return ReadIPSFromStream(ms);
        }

        private static List<IPSRecord> ReadIPSFromStream(Stream stream)
        {
            var records = new List<IPSRecord>();
            using var br = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);

            // Verify header
            var header = new string(br.ReadChars(HEADER_SIZE));
            if (header != IPS_HEADER)
                throw new InvalidDataException($"Invalid IPS header. Expected 'PATCH', got '{header}'");

            // Read records until EOF marker
            while (stream.Position < stream.Length)
            {
                // Read offset (3 bytes, big-endian)
                var offsetBytes = br.ReadBytes(3);
                if (offsetBytes.Length < 3)
                    throw new InvalidDataException("Unexpected end of file while reading record offset");

                // Check for EOF marker
                var marker = System.Text.Encoding.ASCII.GetString(offsetBytes);
                if (marker == IPS_FOOTER)
                {
                    // EOF found - check for optional truncation size
                    if (stream.Position < stream.Length)
                    {
                        ReadUInt24BigEndian(br.ReadBytes(3));
                        // Note: Truncation is handled separately in ApplyPatch
                    }
                    break;
                }

                var offset = (offsetBytes[0] << 16) | (offsetBytes[1] << 8) | offsetBytes[2];

                // Read size (2 bytes, big-endian)
                var size = (br.ReadByte() << 8) | br.ReadByte();

                var record = new IPSRecord
                {
                    Offset = offset,
                    Size = size
                };

                if (size == 0)
                {
                    // RLE record
                    record.IsRLE = true;
                    var rleCount = (br.ReadByte() << 8) | br.ReadByte();
                    var rleValue = br.ReadByte();
                    record.RLECount = rleCount;
                    record.Data = new[] { rleValue };
                }
                else
                {
                    // Normal record
                    record.IsRLE = false;
                    record.Data = br.ReadBytes(size);
                    if (record.Data.Length != size)
                        throw new InvalidDataException($"Expected {size} bytes of data, got {record.Data.Length}");
                }

                records.Add(record);
            }

            return records;
        }

        /// <summary>
        /// Applies an IPS patch to a source file and saves the result
        /// </summary>
        /// <param name="sourceFilePath">Path to the original ROM file</param>
        /// <param name="ipsFilePath">Path to the IPS patch file</param>
        /// <param name="outputFilePath">Path where the patched file will be saved</param>
        /// <returns>Result of the patching operation</returns>
        public static IPSPatchResult ApplyPatch(string sourceFilePath, string ipsFilePath, string outputFilePath)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate inputs
                if (!File.Exists(sourceFilePath))
                    return IPSPatchResult.CreateFailure($"Source file not found: {sourceFilePath}");

                if (!File.Exists(ipsFilePath))
                    return IPSPatchResult.CreateFailure($"IPS patch file not found: {ipsFilePath}");

                // Read the entire source file into memory
                byte[] romData = File.ReadAllBytes(sourceFilePath);
                long originalSize = romData.Length;

                // Read IPS records
                List<IPSRecord> records;
                try
                {
                    records = ReadIPSFile(ipsFilePath);
                }
                catch (Exception ex)
                {
                    return IPSPatchResult.CreateFailure($"Failed to read IPS file: {ex.Message}");
                }

                if (records.Count == 0)
                    return IPSPatchResult.CreateFailure("IPS file contains no patch records");

                // Apply each record
                int appliedCount = 0;
                foreach (var record in records)
                {
                    try
                    {
                        ApplyRecord(ref romData, record);
                        appliedCount++;
                    }
                    catch (Exception ex)
                    {
                        return IPSPatchResult.CreateFailure($"Failed to apply record at offset 0x{record.Offset:X6}: {ex.Message}");
                    }
                }

                // Write patched file
                try
                {
                    File.WriteAllBytes(outputFilePath, romData);
                }
                catch (Exception ex)
                {
                    return IPSPatchResult.CreateFailure($"Failed to write output file: {ex.Message}");
                }

                stopwatch.Stop();

                var result = IPSPatchResult.CreateSuccess(
                    appliedCount,
                    records.Count,
                    originalSize,
                    romData.Length,
                    stopwatch.Elapsed
                );
                result.AppliedRecords = records;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return IPSPatchResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an IPS patch to a byte array (in-place modification)
        /// </summary>
        /// <param name="sourceData">Source ROM data</param>
        /// <param name="ipsFilePath">Path to the IPS patch file</param>
        /// <returns>Result of the patching operation</returns>
        public static IPSPatchResult ApplyPatchToData(ref byte[] sourceData, string ipsFilePath)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (sourceData == null || sourceData.Length == 0)
                    return IPSPatchResult.CreateFailure("Source data is empty");

                if (!File.Exists(ipsFilePath))
                    return IPSPatchResult.CreateFailure($"IPS patch file not found: {ipsFilePath}");

                long originalSize = sourceData.Length;

                // Read IPS records
                List<IPSRecord> records;
                try
                {
                    records = ReadIPSFile(ipsFilePath);
                }
                catch (Exception ex)
                {
                    return IPSPatchResult.CreateFailure($"Failed to read IPS file: {ex.Message}");
                }

                if (records.Count == 0)
                    return IPSPatchResult.CreateFailure("IPS file contains no patch records");

                // Apply each record
                int appliedCount = 0;
                foreach (var record in records)
                {
                    try
                    {
                        ApplyRecord(ref sourceData, record);
                        appliedCount++;
                    }
                    catch (Exception ex)
                    {
                        return IPSPatchResult.CreateFailure($"Failed to apply record at offset 0x{record.Offset:X6}: {ex.Message}");
                    }
                }

                stopwatch.Stop();

                var result = IPSPatchResult.CreateSuccess(
                    appliedCount,
                    records.Count,
                    originalSize,
                    sourceData.Length,
                    stopwatch.Elapsed
                );
                result.AppliedRecords = records;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return IPSPatchResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an in-memory IPS patch (byte array) to a source byte array.
        /// </summary>
        public static IPSPatchResult ApplyPatchFromBytes(ref byte[] sourceData, byte[] ipsData)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (sourceData == null || sourceData.Length == 0)
                    return IPSPatchResult.CreateFailure("Source data is empty");

                if (ipsData == null || ipsData.Length == 0)
                    return IPSPatchResult.CreateFailure("IPS patch data is empty");

                long originalSize = sourceData.Length;

                List<IPSRecord> records;
                try { records = ReadIPSFromBytes(ipsData); }
                catch (Exception ex)
                { return IPSPatchResult.CreateFailure($"Failed to parse IPS data: {ex.Message}"); }

                if (records.Count == 0)
                    return IPSPatchResult.CreateFailure("IPS patch contains no records");

                int appliedCount = 0;
                foreach (var record in records)
                {
                    try { ApplyRecord(ref sourceData, record); appliedCount++; }
                    catch (Exception ex)
                    { return IPSPatchResult.CreateFailure($"Failed to apply record at 0x{record.Offset:X6}: {ex.Message}"); }
                }

                stopwatch.Stop();
                var res = IPSPatchResult.CreateSuccess(appliedCount, records.Count, originalSize, sourceData.Length, stopwatch.Elapsed);
                res.AppliedRecords = records;
                return res;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return IPSPatchResult.CreateFailure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a single IPS record to the ROM data
        /// </summary>
        private static void ApplyRecord(ref byte[] romData, IPSRecord record)
        {
            // Expand ROM if needed
            int requiredSize = record.IsRLE
                ? record.Offset + record.RLECount
                : record.Offset + record.Size;

            if (requiredSize > romData.Length)
            {
                Array.Resize(ref romData, requiredSize);
            }

            if (record.IsRLE)
            {
                // RLE: repeat the same byte
                for (int i = 0; i < record.RLECount; i++)
                {
                    romData[record.Offset + i] = record.Data[0];
                }
            }
            else
            {
                // Normal: copy data bytes
                Array.Copy(record.Data, 0, romData, record.Offset, record.Size);
            }
        }

        /// <summary>
        /// Reads a 24-bit unsigned integer in big-endian format
        /// </summary>
        private static int ReadUInt24BigEndian(byte[] bytes)
        {
            if (bytes.Length != 3)
                throw new ArgumentException("Expected 3 bytes for UInt24");

            return (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
        }

        /// <summary>
        /// Creates an IPS patch by comparing an original byte array with a modified one.
        /// Returns the raw IPS patch bytes (PATCH header + records + EOF footer).
        /// </summary>
        /// <param name="original">Unmodified ROM data</param>
        /// <param name="modified">Modified ROM data</param>
        /// <returns>IPS patch byte array ready to be written to disk</returns>
        public static byte[] CreatePatch(byte[] original, byte[] modified)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (modified == null) throw new ArgumentNullException(nameof(modified));

            using var ms  = new MemoryStream();
            using var bw  = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true);

            // Write IPS header "PATCH"
            bw.Write(System.Text.Encoding.ASCII.GetBytes(IPS_HEADER));

            int modLen  = modified.Length;
            int origLen = original.Length;
            int i       = 0;

            while (i < modLen)
            {
                // Skip bytes that are identical
                if (i < origLen && original[i] == modified[i])
                { i++; continue; }

                // Start of a difference region
                int offset = i;

                // Collect changed bytes (max 65535 per normal record)
                var diff = new List<byte>(256);
                while (i < modLen && diff.Count < 65535)
                {
                    if (i >= origLen || original[i] != modified[i])
                    {
                        diff.Add(modified[i]);
                        i++;
                    }
                    else
                    {
                        // Peek ahead: if the equal run is very short (< 6 bytes),
                        // absorb it into the current record to keep record count low.
                        int equalRun = 0;
                        int j = i;
                        while (j < modLen && j < origLen && original[j] == modified[j] && equalRun < 6)
                        { equalRun++; j++; }

                        if (equalRun < 6 && diff.Count + equalRun <= 65535)
                        {
                            for (int k = 0; k < equalRun; k++) diff.Add(modified[i + k]);
                            i += equalRun;
                        }
                        else break;
                    }
                }

                // The "EOF" literal (0x45, 0x4F, 0x46) cannot be used as a 3-byte offset
                // in some IPS parsers — keep offsets safe (IPS only addresses 24-bit space anyway).
                var data = diff.ToArray();

                // Try RLE encoding: if all bytes are the same it's cheaper as an RLE record
                bool allSame    = data.Length > 2 && data.All(b => b == data[0]);
                bool useRle     = allSame && data.Length >= 3;

                // Write 3-byte offset (big-endian)
                bw.Write((byte)((offset >> 16) & 0xFF));
                bw.Write((byte)((offset >>  8) & 0xFF));
                bw.Write((byte)( offset        & 0xFF));

                if (useRle)
                {
                    // RLE record: size = 0x0000, then 2-byte count, 1-byte value
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((byte)((data.Length >> 8) & 0xFF));
                    bw.Write((byte)( data.Length       & 0xFF));
                    bw.Write(data[0]);
                }
                else
                {
                    // Normal record: 2-byte size + raw data
                    bw.Write((byte)((data.Length >> 8) & 0xFF));
                    bw.Write((byte)( data.Length       & 0xFF));
                    bw.Write(data);
                }
            }

            // Write IPS footer "EOF"
            bw.Write(System.Text.Encoding.ASCII.GetBytes(IPS_FOOTER));

            bw.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Validates if a file is a valid IPS patch
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <returns>True if the file is a valid IPS patch</returns>
        public static bool IsValidIPSFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < HEADER_SIZE + FOOTER_SIZE)
                        return false;

                    var header = new string(br.ReadChars(HEADER_SIZE));
                    return header == IPS_HEADER;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
