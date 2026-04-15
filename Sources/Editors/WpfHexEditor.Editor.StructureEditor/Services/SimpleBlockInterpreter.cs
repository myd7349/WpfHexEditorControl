//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Services/SimpleBlockInterpreter.cs
// Description: Lightweight format-against-file interpreter for the Test Panel.
//              Fully handles: field, signature, conditional, loop, metadata.
//              Reports skips for union, nested, repeating (partial), pointer.
//////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using WpfHexEditor.Core.FormatDetection;

namespace WpfHexEditor.Editor.StructureEditor.Services;

/// <summary>Outcome of interpreting a single block against a file.</summary>
internal sealed class BlockTestResult
{
    public string  BlockName    { get; init; } = "";
    public string  BlockType    { get; init; } = "";
    public long    Offset       { get; init; } = -1;   // -1 = not applicable (skipped/summary)
    public int     Length       { get; init; }
    public string  RawHex       { get; init; } = "";
    public string  ParsedValue  { get; init; } = "";
    public string  Status       { get; init; } = "OK";   // OK | Warning | Error | Skipped
    public string  Note         { get; init; } = "";
    public bool    IsSummary    { get; init; }           // True for conditional/loop summary rows
}

/// <summary>
/// Executes a <see cref="FormatDefinition"/> against raw file bytes and returns
/// per-block test results.
/// <list type="bullet">
///   <item>field, signature     — fully parsed</item>
///   <item>conditional          — condition evaluated; Then/Else branch executed</item>
///   <item>loop                 — iterated up to MaxTestIterations; body blocks executed</item>
///   <item>metadata             — variable value shown in ParsedValue</item>
///   <item>computeFromVariables — skipped (expression evaluation not supported)</item>
///   <item>action               — skipped (runtime state only)</item>
///   <item>repeating, union, nested, pointer — skipped</item>
/// </list>
/// </summary>
internal sealed class SimpleBlockInterpreter
{
    private readonly Dictionary<string, object> _vars = new(StringComparer.Ordinal);
    private readonly byte[] _bytes;

    /// <summary>Maximum loop iterations executed in test mode (prevents flood).</summary>
    private const int MaxTestIterations = 32;

    public SimpleBlockInterpreter(byte[] fileBytes)
    {
        _bytes = fileBytes;
    }

    public List<BlockTestResult> Run(FormatDefinition def)
    {
        _vars.Clear();

        // Seed declared variables with their default values
        if (def.Variables is not null)
        {
            foreach (var kv in def.Variables)
                _vars[kv.Key] = kv.Value ?? (object)0L;
        }

        var results = new List<BlockTestResult>();
        foreach (var block in def.Blocks ?? [])
            InterpretBlock(block, results);

        return results;
    }

    // ── Block dispatch ────────────────────────────────────────────────────────

    private void InterpretBlock(BlockDefinition block, List<BlockTestResult> results)
    {
        switch (block.Type?.ToLowerInvariant())
        {
            case "field":
            case "signature":
                results.Add(InterpretField(block));
                break;

            case "conditional":
                InterpretConditional(block, results);
                break;

            case "loop":
                InterpretLoop(block, results);
                break;

            case "metadata":
                results.Add(InterpretMetadata(block));
                break;

            case "computefromvariables":
                results.Add(new BlockTestResult
                {
                    BlockName = block.Name ?? "",
                    BlockType = block.Type ?? "",
                    Status    = "Skipped",
                    Note      = "Expression evaluation not supported in test mode.",
                });
                break;

            case "action":
                results.Add(new BlockTestResult
                {
                    BlockName = block.Name ?? "",
                    BlockType = block.Type ?? "",
                    Status    = "Skipped",
                    Note      = "Action blocks modify runtime state only.",
                });
                break;

            default:
                results.Add(new BlockTestResult
                {
                    BlockName = block.Name ?? "",
                    BlockType = block.Type ?? "",
                    Status    = "Skipped",
                    Note      = $"Block type '{block.Type}' — not supported in test mode.",
                });
                break;
        }
    }

    // ── Conditional ───────────────────────────────────────────────────────────

    private void InterpretConditional(BlockDefinition block, List<BlockTestResult> results)
    {
        var cond = block.Condition;
        if (cond is null)
        {
            results.Add(new BlockTestResult
            {
                BlockName = block.Name ?? "",
                BlockType = "conditional",
                Status    = "Skipped",
                Note      = "No condition defined.",
            });
            return;
        }

        bool? evaluated = TryEvaluateCondition(cond);
        if (evaluated is null)
        {
            results.Add(new BlockTestResult
            {
                BlockName  = block.Name ?? "",
                BlockType  = "conditional",
                Status     = "Skipped",
                IsSummary  = true,
                Note       = $"Condition [{cond}] — could not be evaluated (unsupported operator or expression).",
            });
            return;
        }

        bool taken         = evaluated.Value;
        var  branch        = taken ? (block.Then ?? []) : (block.Else ?? []);
        var  branchLabel   = taken ? (block.TrueLabel ?? "Then") : (block.FalseLabel ?? "Else");
        int  beforeCount   = results.Count;

        // Summary row for the conditional itself
        results.Add(new BlockTestResult
        {
            BlockName  = block.Name ?? "",
            BlockType  = "conditional",
            Status     = "OK",
            IsSummary  = true,
            ParsedValue = taken ? "TRUE" : "FALSE",
            Note       = $"[{cond}] → {(taken ? "TRUE" : "FALSE")} — executing '{branchLabel}' branch ({branch.Count} block(s))",
        });

        foreach (var b in branch)
            InterpretBlock(b, results);
    }

    private bool? TryEvaluateCondition(ConditionDefinition cond)
    {
        if (string.IsNullOrEmpty(cond.Field) || string.IsNullOrEmpty(cond.Operator))
            return null;

        long actual;
        if (cond.Field.StartsWith("var:", StringComparison.Ordinal))
        {
            var varName = cond.Field[4..];
            if (!_vars.TryGetValue(varName, out var vv)) return null;
            actual = ToLong(vv);
        }
        else if (cond.Field.StartsWith("offset:", StringComparison.Ordinal)
            && long.TryParse(cond.Field[7..], out var off)
            && off >= 0 && off + cond.Length <= _bytes.Length)
        {
            actual = ReadIntFromBytes(off, cond.Length);
        }
        else
        {
            return null;
        }

        long expected = ParseConditionValue(cond.Value);

        return cond.Operator.ToLowerInvariant() switch
        {
            "equals"       or "==" or "eq" => actual == expected,
            "notequals"    or "!=" or "ne" => actual != expected,
            "greaterthan"  or ">"  or "gt" => actual > expected,
            "lessthan"     or "<"  or "lt" => actual < expected,
            "greaterorequal" or ">="       => actual >= expected,
            "lessorequal"    or "<="       => actual <= expected,
            _                              => (bool?)null,
        };
    }

    private long ReadIntFromBytes(long offset, int length)
    {
        long v = 0;
        for (int i = 0; i < Math.Min(length, 8); i++)
            v |= (long)_bytes[offset + i] << (i * 8);
        return v;
    }

    private static long ParseConditionValue(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0;
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(raw[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
            return hex;
        if (long.TryParse(raw, out var dec)) return dec;
        return 0;
    }

    // ── Loop ─────────────────────────────────────────────────────────────────

    private void InterpretLoop(BlockDefinition block, List<BlockTestResult> results)
    {
        long rawCount = ResolveNumber(block.Count, -1);
        if (rawCount < 0)
        {
            results.Add(new BlockTestResult
            {
                BlockName = block.Name ?? "",
                BlockType = "loop",
                Status    = "Skipped",
                Note      = "Cannot resolve loop count — variable not declared.",
            });
            return;
        }

        int count    = (int)Math.Min(rawCount, MaxTestIterations);
        int bodySize = (block.Body ?? []).Count;
        bool capped  = rawCount > MaxTestIterations;

        // Summary row
        results.Add(new BlockTestResult
        {
            BlockName  = block.Name ?? "",
            BlockType  = "loop",
            Status     = "OK",
            IsSummary  = true,
            ParsedValue = $"{count} / {rawCount}",
            Note       = capped
                ? $"Loop: {rawCount} iteration(s) declared — capped at {MaxTestIterations} in test mode. {bodySize} block(s) per iteration."
                : $"Loop: {count} iteration(s) × {bodySize} block(s) per iteration.",
        });

        for (int i = 0; i < count; i++)
        {
            // Write index variable if declared
            if (!string.IsNullOrEmpty(block.IndexVar))
                _vars[block.IndexVar] = (long)i;

            foreach (var b in block.Body ?? [])
            {
                var iterResults = new List<BlockTestResult>();
                InterpretBlock(b, iterResults);

                foreach (var r in iterResults)
                {
                    // Prefix block name with [i=N] to distinguish iterations
                    results.Add(new BlockTestResult
                    {
                        BlockName   = $"[i={i}] {r.BlockName}",
                        BlockType   = r.BlockType,
                        Offset      = r.Offset,
                        Length      = r.Length,
                        RawHex      = r.RawHex,
                        ParsedValue = r.ParsedValue,
                        Status      = r.Status,
                        Note        = r.Note,
                        IsSummary   = r.IsSummary,
                    });
                }
            }
        }
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    private BlockTestResult InterpretMetadata(BlockDefinition block)
    {
        var name = block.Name ?? "";

        // Look up the variable by name or by StoreAs
        object? val = null;
        if (!string.IsNullOrEmpty(name) && _vars.TryGetValue(name, out var v1)) val = v1;
        else if (!string.IsNullOrEmpty(block.StoreAs) && _vars.TryGetValue(block.StoreAs, out var v2)) val = v2;

        string parsed = val is not null ? val.ToString() ?? "—" : "—";

        return new BlockTestResult
        {
            BlockName   = name,
            BlockType   = "metadata",
            ParsedValue = parsed,
            Status      = "OK",
            Note        = val is not null
                ? "Variable value at this point in execution."
                : "Variable not yet set at this point.",
        };
    }

    // ── Field / Signature ─────────────────────────────────────────────────────

    private BlockTestResult InterpretField(BlockDefinition block)
    {
        // Resolve offset
        long offset;
        if (!string.IsNullOrEmpty(block.OffsetFrom))
        {
            long baseOff = _vars.TryGetValue(block.OffsetFrom, out var bv) ? ToLong(bv) : 0;
            long addOff  = ResolveNumber(block.OffsetAdd, 0);
            offset = baseOff + addOff;
        }
        else
        {
            var raw = ResolveNumber(block.Offset, -1);
            if (raw < 0)
            {
                return new BlockTestResult
                {
                    BlockName = block.Name ?? "",
                    BlockType = block.Type ?? "",
                    Status    = "Error",
                    Note      = "Cannot resolve offset — variable not declared or expression not supported.",
                };
            }
            offset = raw;
        }

        // Resolve length
        int length = (int)ResolveNumber(block.Length, 0);
        if (length < 0) length = 0;

        // Bounds check
        if (offset < 0 || offset > _bytes.Length)
        {
            return new BlockTestResult
            {
                BlockName   = block.Name ?? "",
                BlockType   = block.Type ?? "",
                Offset      = offset,
                Length      = length,
                Status      = "Error",
                Note        = $"Offset 0x{offset:X} is beyond file end (0x{_bytes.Length:X}).",
            };
        }

        int safeLen  = (int)Math.Min(length, _bytes.Length - offset);
        var rawBytes = _bytes[(int)offset..(int)(offset + safeLen)];
        var rawHex   = Convert.ToHexString(rawBytes);

        // Parse typed value
        bool   bigEndian = string.Equals(block.Endianness, "big", StringComparison.OrdinalIgnoreCase);
        string parsed    = ParseValue(rawBytes, block.ValueType, bigEndian);
        string status    = "OK";
        string note      = "";

        // Apply ValueMap if defined
        if (block.ValueMap is { Count: > 0 } && rawBytes.Length > 0)
        {
            long numericVal = TryParseNumeric(rawBytes, block.ValueType, bigEndian) ?? 0;
            var  key        = numericVal.ToString();
            if (block.ValueMap.TryGetValue(key, out var mapped))
                parsed = $"{parsed} ({mapped})";
        }

        // Store variable
        if (!string.IsNullOrEmpty(block.StoreAs) && length > 0)
        {
            var numVal = TryParseNumeric(rawBytes, block.ValueType, bigEndian);
            if (numVal.HasValue)
                _vars[block.StoreAs] = numVal.Value;
        }

        // Signature validation
        if (string.Equals(block.Type, "signature", StringComparison.OrdinalIgnoreCase))
        {
            note   = "Signature — bytes shown; expected pattern not validated in test mode.";
            status = "Warning";
        }

        // Truncation warning
        if (safeLen < length)
        {
            status = "Warning";
            note   = $"File too short: read {safeLen} of {length} bytes.";
        }

        return new BlockTestResult
        {
            BlockName   = block.Name ?? "",
            BlockType   = block.Type ?? "",
            Offset      = offset,
            Length      = safeLen,
            RawHex      = rawHex,
            ParsedValue = parsed,
            Status      = status,
            Note        = note,
        };
    }

    // ── Value parsing ─────────────────────────────────────────────────────────

    private static string ParseValue(byte[] data, string? valueType, bool bigEndian)
    {
        if (data.Length == 0) return "(empty)";

        try
        {
            return valueType?.ToLowerInvariant() switch
            {
                "uint8"  => data[0].ToString(),
                "int8"   => ((sbyte)data[0]).ToString(),
                "uint16" => (bigEndian
                    ? (ushort)((data[0] << 8) | data[1])
                    : BitConverter.ToUInt16(Pad(data, 2))).ToString(),
                "int16"  => (bigEndian
                    ? (short)((data[0] << 8) | data[1])
                    : BitConverter.ToInt16(Pad(data, 2))).ToString(),
                "uint32" => (bigEndian
                    ? (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3])
                    : BitConverter.ToUInt32(Pad(data, 4))).ToString(),
                "int32"  => (bigEndian
                    ? (int)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3])
                    : BitConverter.ToInt32(Pad(data, 4))).ToString(),
                "uint64" => (bigEndian
                    ? BEUInt64(data)
                    : BitConverter.ToUInt64(Pad(data, 8))).ToString(),
                "int64"  => (bigEndian
                    ? (long)BEUInt64(data)
                    : BitConverter.ToInt64(Pad(data, 8))).ToString(),
                "float"  => BitConverter.ToSingle(Pad(data, 4)).ToString("G"),
                "double" => BitConverter.ToDouble(Pad(data, 8)).ToString("G"),
                "ascii" or "string" => Encoding.ASCII.GetString(data).TrimEnd('\0'),
                "utf8"  => Encoding.UTF8.GetString(data).TrimEnd('\0'),
                "utf16" => Encoding.Unicode.GetString(data).TrimEnd('\0'),
                "bool"  => (data[0] != 0).ToString(),
                "char"  => ((char)data[0]).ToString(),
                "bytes" => Convert.ToHexString(data),
                "padding" => $"({data.Length} padding byte{(data.Length == 1 ? "" : "s")})",
                _        => Convert.ToHexString(data),
            };
        }
        catch
        {
            return Convert.ToHexString(data);
        }
    }

    private static long? TryParseNumeric(byte[] data, string? valueType, bool bigEndian)
    {
        try
        {
            return valueType?.ToLowerInvariant() switch
            {
                "uint8"  => data[0],
                "int8"   => (sbyte)data[0],
                "uint16" => bigEndian ? (data[0] << 8) | data[1] : BitConverter.ToUInt16(Pad(data, 2)),
                "int16"  => bigEndian ? (short)((data[0] << 8) | data[1]) : BitConverter.ToInt16(Pad(data, 2)),
                "uint32" => bigEndian ? (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]) : BitConverter.ToUInt32(Pad(data, 4)),
                "int32"  => bigEndian ? (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3] : BitConverter.ToInt32(Pad(data, 4)),
                "uint64" => (long)(bigEndian ? BEUInt64(data) : BitConverter.ToUInt64(Pad(data, 8))),
                "int64"  => bigEndian ? (long)BEUInt64(data) : BitConverter.ToInt64(Pad(data, 8)),
                "bool"   => data[0] != 0 ? 1 : 0,
                _        => (long?)null,
            };
        }
        catch { return null; }
    }

    // ── Numeric resolution ────────────────────────────────────────────────────

    private long ResolveNumber(object? raw, long fallback)
    {
        if (raw is null) return fallback;

        if (raw is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Number => je.TryGetInt64(out var n) ? n : (long)je.GetDouble(),
                JsonValueKind.String => ResolveString(je.GetString(), fallback),
                _ => fallback,
            };
        }

        return raw switch
        {
            int    i => i,
            long   l => l,
            double d => (long)d,
            string s => ResolveString(s, fallback),
            _        => fallback,
        };
    }

    private long ResolveString(string? s, long fallback)
    {
        if (string.IsNullOrEmpty(s)) return fallback;
        if (s.StartsWith("var:", StringComparison.Ordinal))
        {
            var name = s[4..];
            return _vars.TryGetValue(name, out var v) ? ToLong(v) : fallback;
        }
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
            return hex;
        if (long.TryParse(s, out var parsed)) return parsed;
        return fallback;
    }

    private static long ToLong(object? v) => v switch
    {
        int    i => i,
        long   l => l,
        double d => (long)d,
        JsonElement je => je.TryGetInt64(out var n) ? n : (long)je.GetDouble(),
        _        => 0,
    };

    // ── Byte helpers ──────────────────────────────────────────────────────────

    private static byte[] Pad(byte[] src, int minLen)
    {
        if (src.Length >= minLen) return src;
        var result = new byte[minLen];
        Array.Copy(src, result, src.Length);
        return result;
    }

    private static ulong BEUInt64(byte[] data)
    {
        ulong v = 0;
        for (int i = 0; i < Math.Min(8, data.Length); i++)
            v = (v << 8) | data[i];
        return v;
    }
}
