# Core/Bytes

Core classes for byte-level data manipulation and file I/O.

## 📁 Contents

### 🗄️ Data Provider

- **[ByteProvider.cs](ByteProvider.cs)** - Main data access layer
  - File and stream manipulation
  - Byte modification tracking with dictionary
  - Undo/redo support
  - Insert/delete/modify operations
  - Copy/paste functionality
  - TBL file support for custom character tables
  - Read-only mode support
  - Returns tuple `(byte? value, bool success)` for safe access

### 🔧 Byte Modification Classes

- **[ByteModified.cs](ByteModified.cs)** - Tracks individual byte changes
  - Original byte value preservation
  - Modification type (modified, deleted, inserted)
  - Position tracking
  - Used by undo/redo system

- **[ByteDifference.cs](ByteDifference.cs)** - Byte comparison results
  - Stores position of differences between files
  - Used by file comparison features
  - Color information for visual highlighting

### 🔄 Byte Conversion

- **[ByteConverters.cs](ByteConverters.cs)** - Static utility class
  - Byte array to hex string conversion
  - Hex string to byte array parsing
  - ASCII/UTF8/Unicode encoding support
  - Binary string representation
  - Handles various character encodings

### 📦 Byte Display Classes

- **[Byte_8bit.cs](Byte_8bit.cs)** - 8-bit byte UI control
- **[Byte_16bit.cs](Byte_16bit.cs)** - 16-bit word UI control
- **[Byte_32bit.cs](Byte_32bit.cs)** - 32-bit dword UI control

These are WPF UserControls for displaying and editing bytes in the hex view with various data type interpretations.

## 🎯 Purpose

This folder contains the core data model and business logic for:
- File and stream access with modification tracking
- Byte-level operations (read, write, insert, delete)
- Conversion between different data representations
- Change history management for undo/redo

## 🔗 Architecture

```
ByteProvider (Main API)
    ├── Uses: ByteModified (change tracking)
    ├── Uses: ByteConverters (conversions)
    ├── Uses: TBLStream (custom character tables)
    └── Manages: Stream (file I/O)

ByteModified
    └── Implements: IByteModified

ByteDifference
    └── Used by: File comparison features
```

## 🎓 Usage Example

```csharp
// Create a ByteProvider
using var provider = new ByteProvider("file.bin");

// Read a byte (returns tuple)
var (byteValue, success) = provider.GetByte(100);
if (success)
{
    Console.WriteLine($"Byte at 100: 0x{byteValue:X2}");
}

// Modify bytes
provider.AddByteModified(100, 0xFF, 0x00); // position, newByte, oldByte

// Insert bytes
provider.AddByteToStream(1000, new byte[] { 0x01, 0x02, 0x03 });

// Delete bytes
provider.RemoveByte(500, 10); // position, count

// Undo/Redo
provider.Undo();
provider.Redo();

// Save changes
provider.SubmitChanges();
```

## ✨ Key Features

- **Change Tracking**: All modifications tracked for undo/redo
- **Memory Efficient**: Only modified bytes stored in memory
- **Stream Support**: Works with any .NET Stream (file, memory, network)
- **Insert Anywhere**: Optional mode to insert bytes without overwriting
- **Read-Only Mode**: Prevents accidental modifications
- **Progress Events**: Long-running operations report progress
- **Safe Access**: Tuple return types avoid exceptions

## 📚 Related Components

- **[ByteModificationService](../../Services/ByteModificationService.cs)** - Higher-level service layer
- **[UndoRedoService](../../Services/UndoRedoService.cs)** - Undo/redo management
- **[TBLStream](../CharacterTable/TBLStream.cs)** - Custom character encoding support
- **[HexEditor.xaml.cs](../../HexEditor.xaml.cs)** - Main UI control using ByteProvider

---

✨ Core byte manipulation and file I/O infrastructure
