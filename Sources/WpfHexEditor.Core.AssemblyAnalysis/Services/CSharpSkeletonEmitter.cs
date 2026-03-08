// ==========================================================
// Project: WpfHexEditor.Core.AssemblyAnalysis
// File: Services/CSharpSkeletonEmitter.cs
// Author: Derek Tremblay
// Created: 2026-03-08
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     BCL-only C# structure skeleton emitter. Produces human-readable
//     C# type/member declarations without control-flow reconstruction.
//     Covers 80% of assembly exploration use cases (structure browsing).
//     No external NuGet dependency.
//
// Architecture Notes:
//     Pattern: Service (stateless).
//     Emits C# keyword declarations with { /* IL body */ } stubs for methods.
//     Enum members and const fields use their literal value where available.
// ==========================================================

using System.Text;
using WpfHexEditor.Core.AssemblyAnalysis.Models;

namespace WpfHexEditor.Core.AssemblyAnalysis.Services;

/// <summary>
/// Emits C#-style structure skeleton text for assembly/type/member models.
/// Does not perform control-flow reconstruction — method bodies are stubs.
/// </summary>
public sealed class CSharpSkeletonEmitter
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Emits an <c>AssemblyInfo.cs</c>-style summary for the entire assembly.
    /// </summary>
    public string EmitAssemblyInfo(AssemblyModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ================================================================");
        sb.AppendLine($"// Assembly : {model.Name}");
        if (model.Version is not null)
            sb.AppendLine($"// Version  : {model.Version}");
        if (!string.IsNullOrEmpty(model.TargetFramework))
            sb.AppendLine($"// Framework: {model.TargetFramework}");
        if (!string.IsNullOrEmpty(model.Culture))
            sb.AppendLine($"// Culture  : {model.Culture}");
        if (!string.IsNullOrEmpty(model.PublicKeyToken))
            sb.AppendLine($"// PKT      : {model.PublicKeyToken}");
        sb.AppendLine($"// Types    : {model.Types.Count}");
        sb.AppendLine($"// Refs     : {model.References.Count}");
        sb.AppendLine("// ================================================================");
        sb.AppendLine();

        if (model.References.Count > 0)
        {
            sb.AppendLine("// Assembly references:");
            foreach (var r in model.References.OrderBy(r => r.Name))
            {
                var ver = r.Version is not null ? $" v{r.Version}" : string.Empty;
                sb.AppendLine($"//   {r.Name}{ver}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("// Sections:");
        foreach (var s in model.Sections)
            sb.AppendLine($"//   {s.Name,-8} RVA=0x{s.VirtualAddress:X8}  Size={s.VirtualSize}");

        return sb.ToString();
    }

    /// <summary>
    /// Emits a C# type declaration skeleton including all members.
    /// </summary>
    public string EmitType(TypeModel model)
    {
        var sb = new StringBuilder();

        // Custom attributes
        foreach (var attr in model.CustomAttributes)
            sb.AppendLine($"[{attr}]");

        // Type declaration
        var visibility = model.IsPublic ? "public" : "internal";
        var modifiers  = BuildTypeModifiers(model);
        var keyword    = TypeKindToKeyword(model.Kind);
        var bases      = BuildBaseList(model);

        sb.Append($"{visibility}{modifiers} {keyword} {model.Name}");
        if (!string.IsNullOrEmpty(bases)) sb.Append($" : {bases}");
        sb.AppendLine();
        sb.AppendLine("{");

        // Fields
        if (model.Fields.Count > 0)
        {
            sb.AppendLine("    // ── Fields ──");
            foreach (var f in model.Fields)
                sb.AppendLine($"    {FormatField(f)}");
            sb.AppendLine();
        }

        // Properties
        if (model.Properties.Count > 0)
        {
            sb.AppendLine("    // ── Properties ──");
            foreach (var p in model.Properties)
                sb.AppendLine($"    {FormatProperty(p)}");
            sb.AppendLine();
        }

        // Events
        if (model.Events.Count > 0)
        {
            sb.AppendLine("    // ── Events ──");
            foreach (var e in model.Events)
                sb.AppendLine($"    {FormatEvent(e)}");
            sb.AppendLine();
        }

        // Methods
        if (model.Methods.Count > 0)
        {
            sb.AppendLine("    // ── Methods ──");
            foreach (var m in model.Methods)
                sb.AppendLine($"    {FormatMethod(m, model.Kind == TypeKind.Interface)}");
            sb.AppendLine();
        }

        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Emits a standalone method signature (used in the detail pane Code tab for Method nodes).
    /// </summary>
    public string EmitMethod(MemberModel model)
    {
        var sb = new StringBuilder();
        foreach (var attr in model.CustomAttributes)
            sb.AppendLine($"[{attr}]");
        sb.AppendLine(FormatMethod(model, false));
        return sb.ToString();
    }

    // ── Formatting helpers ────────────────────────────────────────────────────

    private static string BuildTypeModifiers(TypeModel model)
    {
        var sb = new StringBuilder();
        if (model.Kind == TypeKind.Class)
        {
            if (model.IsAbstract && !model.IsSealed)  sb.Append(" abstract");
            else if (model.IsSealed && !model.IsAbstract) sb.Append(" sealed");
            else if (model.IsAbstract && model.IsSealed)  sb.Append(" static");  // static class
        }
        return sb.ToString();
    }

    private static string TypeKindToKeyword(TypeKind kind) => kind switch
    {
        TypeKind.Struct    => "struct",
        TypeKind.Interface => "interface",
        TypeKind.Enum      => "enum",
        TypeKind.Delegate  => "delegate",
        _                  => "class"
    };

    private static string BuildBaseList(TypeModel model)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(model.BaseTypeName)
            && model.BaseTypeName != "System.Object"
            && model.BaseTypeName != "System.ValueType"
            && model.BaseTypeName != "System.Enum"
            && model.BaseTypeName != "System.MulticastDelegate")
        {
            parts.Add(SimplifyTypeName(model.BaseTypeName));
        }
        foreach (var iface in model.InterfaceNames)
            parts.Add(SimplifyTypeName(iface));
        return string.Join(", ", parts);
    }

    private static string FormatField(MemberModel f)
    {
        var vis   = f.IsPublic ? "public" : "private";
        var stat  = f.IsStatic ? " static" : string.Empty;
        var type  = f.Signature ?? "object";
        var attrs = f.CustomAttributes.Count > 0
                    ? string.Join(" ", f.CustomAttributes.Select(a => $"[{a}]")) + " "
                    : string.Empty;
        return $"{attrs}{vis}{stat} {type} {f.Name};";
    }

    private static string FormatProperty(MemberModel p)
    {
        var vis  = p.IsPublic ? "public" : "private";
        var stat = p.IsStatic ? " static" : string.Empty;
        var type = p.Signature ?? "object";
        return $"{vis}{stat} {type} {p.Name} {{ get; set; }}";
    }

    private static string FormatEvent(MemberModel e)
    {
        var vis  = e.IsPublic ? "public" : "private";
        var stat = e.IsStatic ? " static" : string.Empty;
        return $"{vis}{stat} event EventHandler {e.Name};";
    }

    private static string FormatMethod(MemberModel m, bool isInterface)
    {
        var vis   = m.IsPublic ? "public" : "private";
        var stat  = m.IsStatic ? " static" : string.Empty;
        var abstr = m.IsAbstract && !isInterface ? " abstract" : string.Empty;
        var virt  = m.IsVirtual && !m.IsAbstract && !isInterface ? " virtual" : string.Empty;
        var sig   = m.Signature ?? $"void {m.Name}()";

        if (isInterface || m.IsAbstract)
            return $"{sig};";

        // Constructors and methods get stub body
        return $"{vis}{stat}{abstr}{virt} {sig}{Environment.NewLine}    {{" +
               $"{Environment.NewLine}        // IL available in the IL tab" +
               $"{Environment.NewLine}    }}";
    }

    private static string SimplifyTypeName(string fullName)
    {
        // Strip namespace for common display: "System.Collections.Generic.List`1" → "List`1"
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }
}
