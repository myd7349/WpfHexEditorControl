// Project      : WpfHexEditor.App
// File         : HexDiff/Models/DiffKind.cs
// Description  : Classifies a byte-level difference between two binary files.

namespace WpfHexEditor.App.HexDiff.Models;

public enum DiffKind
{
    Substitution, // same offset, different byte values
    Insertion,    // byte exists in B but not A (tail of longer file B)
    Deletion,     // byte exists in A but not B (tail of longer file A)
}
