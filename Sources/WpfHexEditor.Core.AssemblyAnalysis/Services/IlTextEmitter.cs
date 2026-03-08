// ==========================================================
// Project: WpfHexEditor.Core.AssemblyAnalysis
// File: Services/IlTextEmitter.cs
// Author: Derek Tremblay
// Created: 2026-03-08
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     BCL-only IL disassembler. Reads a method body via PEReader,
//     decodes IL instructions using BlobReader + ILOpCode (BCL enums),
//     and resolves operand tokens to human-readable names.
//     No external NuGet dependency.
//
// Architecture Notes:
//     Pattern: Service (stateless).
//     Requires a live PEReader (not disposed) because MethodBodyBlock
//     reads directly from the memory-mapped PE image.
//     Returns empty string for abstract/extern methods (no body).
// ==========================================================

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace WpfHexEditor.Core.AssemblyAnalysis.Services;

/// <summary>
/// Disassembles .NET method bodies to IL text using only BCL APIs.
/// </summary>
public sealed class IlTextEmitter
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the IL disassembly of the method identified by <paramref name="methodDef"/>,
    /// or an empty string when the method has no body (abstract, extern, interface member).
    /// </summary>
    public string EmitMethod(
        MethodDefinition methodDef,
        MetadataReader   mdReader,
        PEReader         peReader)
    {
        var rva = methodDef.RelativeVirtualAddress;
        if (rva == 0) return string.Empty;

        try
        {
            var body = peReader.GetMethodBody(rva);
            return DisassembleBody(body, mdReader);
        }
        catch (Exception ex)
        {
            return $"// Error reading IL: {ex.Message}";
        }
    }

    // ── Disassembly core ──────────────────────────────────────────────────────

    private static string DisassembleBody(MethodBodyBlock body, MetadataReader mdReader)
    {
        var sb     = new StringBuilder();
        var reader = body.GetILReader();

        sb.AppendLine($"// MaxStack: {body.MaxStack}");
        if (!body.LocalSignature.IsNil)
        {
            sb.AppendLine($"// Locals: {body.LocalSignature}");
        }

        while (reader.RemainingBytes > 0)
        {
            var offset = reader.Offset;
            sb.Append($"  IL_{offset:X4}: ");

            var opCode = ReadOpCode(ref reader);
            sb.Append(opCode.ToString().ToLowerInvariant().Replace('_', '.'));

            AppendOperand(ref reader, opCode, mdReader, sb);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static ILOpCode ReadOpCode(ref BlobReader reader)
    {
        var b = reader.ReadByte();
        if (b == 0xFE)
        {
            // Two-byte opcode: 0xFE XX
            return (ILOpCode)(0xFE00 | reader.ReadByte());
        }
        return (ILOpCode)b;
    }

    private static void AppendOperand(
        ref BlobReader reader,
        ILOpCode       opCode,
        MetadataReader mdReader,
        StringBuilder  sb)
    {
        // Categorise opcode by operand type using ECMA-335 §III.1.7 tables.
        switch (opCode)
        {
            // No operand
            case ILOpCode.Nop:
            case ILOpCode.Break:
            case ILOpCode.Ldarg_0: case ILOpCode.Ldarg_1:
            case ILOpCode.Ldarg_2: case ILOpCode.Ldarg_3:
            case ILOpCode.Ldloc_0: case ILOpCode.Ldloc_1:
            case ILOpCode.Ldloc_2: case ILOpCode.Ldloc_3:
            case ILOpCode.Stloc_0: case ILOpCode.Stloc_1:
            case ILOpCode.Stloc_2: case ILOpCode.Stloc_3:
            case ILOpCode.Ldnull:
            case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0:
            case ILOpCode.Ldc_i4_1:  case ILOpCode.Ldc_i4_2:
            case ILOpCode.Ldc_i4_3:  case ILOpCode.Ldc_i4_4:
            case ILOpCode.Ldc_i4_5:  case ILOpCode.Ldc_i4_6:
            case ILOpCode.Ldc_i4_7:  case ILOpCode.Ldc_i4_8:
            case ILOpCode.Dup:   case ILOpCode.Pop:
            case ILOpCode.Ret:
            case ILOpCode.Add:   case ILOpCode.Sub:   case ILOpCode.Mul:
            case ILOpCode.Div:   case ILOpCode.Rem:
            case ILOpCode.And:   case ILOpCode.Or:    case ILOpCode.Xor:
            case ILOpCode.Shl:   case ILOpCode.Shr:   case ILOpCode.Shr_un:
            case ILOpCode.Neg:   case ILOpCode.Not:
            case ILOpCode.Conv_i1: case ILOpCode.Conv_i2: case ILOpCode.Conv_i4:
            case ILOpCode.Conv_i8: case ILOpCode.Conv_r4: case ILOpCode.Conv_r8:
            case ILOpCode.Conv_u4: case ILOpCode.Conv_u8:
            case ILOpCode.Conv_r_un:
            case ILOpCode.Throw: case ILOpCode.Rethrow:
            case ILOpCode.Ldlen:
            case ILOpCode.Ldelem_i1: case ILOpCode.Ldelem_u1:
            case ILOpCode.Ldelem_i2: case ILOpCode.Ldelem_u2:
            case ILOpCode.Ldelem_i4: case ILOpCode.Ldelem_u4:
            case ILOpCode.Ldelem_i8: case ILOpCode.Ldelem_i:
            case ILOpCode.Ldelem_r4: case ILOpCode.Ldelem_r8:
            case ILOpCode.Ldelem_ref:
            case ILOpCode.Stelem_i:  case ILOpCode.Stelem_i1:
            case ILOpCode.Stelem_i2: case ILOpCode.Stelem_i4:
            case ILOpCode.Stelem_i8: case ILOpCode.Stelem_r4:
            case ILOpCode.Stelem_r8: case ILOpCode.Stelem_ref:
            case ILOpCode.Endfilter: case ILOpCode.Endfinally:
            case ILOpCode.Ceq:   case ILOpCode.Cgt:   case ILOpCode.Cgt_un:
            case ILOpCode.Clt:   case ILOpCode.Clt_un:
            case ILOpCode.Volatile:
            case ILOpCode.Tail:
            case ILOpCode.Readonly:
                return;

            // Inline int8
            case ILOpCode.Ldc_i4_s:
            case ILOpCode.Ldloc_s: case ILOpCode.Stloc_s:
            case ILOpCode.Ldarg_s: case ILOpCode.Starg_s:
            case ILOpCode.Ldarga_s:
            case ILOpCode.Br_s:    case ILOpCode.Brtrue_s:  case ILOpCode.Brfalse_s:
            case ILOpCode.Beq_s:   case ILOpCode.Bge_s:     case ILOpCode.Bgt_s:
            case ILOpCode.Ble_s:   case ILOpCode.Blt_s:
            case ILOpCode.Bne_un_s: case ILOpCode.Bge_un_s: case ILOpCode.Bgt_un_s:
            case ILOpCode.Ble_un_s: case ILOpCode.Blt_un_s:
            case ILOpCode.Leave_s:
            {
                var v = reader.ReadSByte();
                sb.Append($" {v}");
                return;
            }

            // Inline int32
            case ILOpCode.Ldc_i4:
            case ILOpCode.Ldarg:  case ILOpCode.Starg:  case ILOpCode.Ldarga:
            case ILOpCode.Ldloc:  case ILOpCode.Stloc:  case ILOpCode.Ldloca:
            case ILOpCode.Br:     case ILOpCode.Brtrue: case ILOpCode.Brfalse:
            case ILOpCode.Beq:    case ILOpCode.Bge:    case ILOpCode.Bgt:
            case ILOpCode.Ble:    case ILOpCode.Blt:
            case ILOpCode.Bne_un: case ILOpCode.Bge_un: case ILOpCode.Bgt_un:
            case ILOpCode.Ble_un: case ILOpCode.Blt_un:
            case ILOpCode.Leave:
            {
                var v = reader.ReadInt32();
                sb.Append($" {v}");
                return;
            }

            // Inline int64
            case ILOpCode.Ldc_i8:
            {
                var v = reader.ReadInt64();
                sb.Append($" {v}L");
                return;
            }

            // Inline float32 / float64
            case ILOpCode.Ldc_r4:
            {
                var v = reader.ReadSingle();
                sb.Append($" {v:R}f");
                return;
            }
            case ILOpCode.Ldc_r8:
            {
                var v = reader.ReadDouble();
                sb.Append($" {v:R}");
                return;
            }

            // Inline token (method/type/field/string)
            case ILOpCode.Call:    case ILOpCode.Callvirt:  case ILOpCode.Newobj:
            case ILOpCode.Ldftn:   case ILOpCode.Ldvirtftn:
            case ILOpCode.Jmp:     case ILOpCode.Calli:
            {
                var token = reader.ReadInt32();
                sb.Append($" {ResolveMethodOrMember(token, mdReader)}");
                return;
            }

            case ILOpCode.Ldtoken:
            case ILOpCode.Initobj:
            case ILOpCode.Constrained:
            case ILOpCode.Newarr:
            case ILOpCode.Isinst:
            case ILOpCode.Castclass:
            case ILOpCode.Box:     case ILOpCode.Unbox:
            case ILOpCode.Unbox_any:
            case ILOpCode.Ldelema:
            case ILOpCode.Ldobj:   case ILOpCode.Stobj:
            case ILOpCode.Cpobj:
            case ILOpCode.Sizeof:
            case ILOpCode.Mkrefany: case ILOpCode.Refanyval:
            {
                var token = reader.ReadInt32();
                sb.Append($" {ResolveType(token, mdReader)}");
                return;
            }

            case ILOpCode.Ldfld:   case ILOpCode.Ldflda:
            case ILOpCode.Stfld:   case ILOpCode.Ldsfld:
            case ILOpCode.Ldsflda: case ILOpCode.Stsfld:
            {
                var token = reader.ReadInt32();
                sb.Append($" {ResolveMethodOrMember(token, mdReader)}");
                return;
            }

            case ILOpCode.Ldstr:
            {
                var token = reader.ReadInt32();
                sb.Append($" \"{ResolveString(token, mdReader)}\"");
                return;
            }

            // Switch: 4 + N*4 bytes
            case ILOpCode.Switch:
            {
                var n = reader.ReadUInt32();
                sb.Append($" ({n} targets)");
                for (var i = 0; i < n; i++) reader.ReadInt32();
                return;
            }

            default:
                // Unknown — try to read nothing and keep going.
                return;
        }
    }

    // ── Token resolution ──────────────────────────────────────────────────────

    private static string ResolveMethodOrMember(int token, MetadataReader mdReader)
    {
        try
        {
            var handle = MetadataTokens.EntityHandle(token);
            switch (handle.Kind)
            {
                case HandleKind.MethodDefinition:
                    return mdReader.GetString(
                        mdReader.GetMethodDefinition((MethodDefinitionHandle)handle).Name);
                case HandleKind.MemberReference:
                {
                    var mref = mdReader.GetMemberReference((MemberReferenceHandle)handle);
                    return $"{ResolveParentType(mref.Parent, mdReader)}::{mdReader.GetString(mref.Name)}";
                }
                case HandleKind.MethodSpecification:
                {
                    var spec = mdReader.GetMethodSpecification((MethodSpecificationHandle)handle);
                    return ResolveMethodOrMember(MetadataTokens.GetToken(spec.Method), mdReader);
                }
                default:
                    return $"0x{token:X8}";
            }
        }
        catch { return $"0x{token:X8}"; }
    }

    private static string ResolveType(int token, MetadataReader mdReader)
    {
        try
        {
            var handle = MetadataTokens.EntityHandle(token);
            return handle.Kind switch
            {
                HandleKind.TypeDefinition =>
                    mdReader.GetString(mdReader.GetTypeDefinition((TypeDefinitionHandle)handle).Name),
                HandleKind.TypeReference =>
                    mdReader.GetString(mdReader.GetTypeReference((TypeReferenceHandle)handle).Name),
                HandleKind.TypeSpecification => "<TypeSpec>",
                _ => $"0x{token:X8}"
            };
        }
        catch { return $"0x{token:X8}"; }
    }

    private static string ResolveParentType(EntityHandle parent, MetadataReader mdReader)
    {
        try
        {
            return parent.Kind switch
            {
                HandleKind.TypeDefinition =>
                    mdReader.GetString(mdReader.GetTypeDefinition((TypeDefinitionHandle)parent).Name),
                HandleKind.TypeReference =>
                    mdReader.GetString(mdReader.GetTypeReference((TypeReferenceHandle)parent).Name),
                _ => "?"
            };
        }
        catch { return "?"; }
    }

    private static string ResolveString(int token, MetadataReader mdReader)
    {
        try
        {
            // User-string token: upper byte = 0x70
            if ((token >> 24) == 0x70)
            {
                var us = mdReader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00FFFFFF));
                // Truncate long strings for display.
                return us.Length > 60 ? us[..57] + "..." : us;
            }
            return $"0x{token:X8}";
        }
        catch { return "?"; }
    }
}
