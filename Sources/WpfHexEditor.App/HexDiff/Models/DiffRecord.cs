// Project      : WpfHexEditor.App
// File         : HexDiff/Models/DiffRecord.cs
// Description  : One byte-level difference between File A and File B.

namespace WpfHexEditor.App.HexDiff.Models;

public sealed record DiffRecord(
    long     Offset,
    byte     OldByte,  // value in File A (0 for Insertions)
    byte     NewByte,  // value in File B (0 for Deletions)
    DiffKind Kind
);
