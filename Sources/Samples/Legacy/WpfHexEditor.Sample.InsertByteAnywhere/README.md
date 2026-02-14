# WpfHexEditor.Sample.InsertByteAnywhere

Advanced byte insertion and deletion capabilities demonstration.

## 🎯 Purpose

Showcases dynamic file size modification through insertion and deletion of bytes at any position.

## ✨ Features Demonstrated

- **Insert Bytes**: Add bytes at any file position
- **Delete Bytes**: Remove bytes from selection
- **Dynamic File Size**: File grows/shrinks with operations
- **Insert vs Overwrite**: Toggle between insert and overwrite modes
- **Batch Operations**: Insert/delete multiple bytes at once
- **Undo/Redo**: Full history support for modifications

## 🚀 How to Run

### Visual Studio
1. Open `WpfHexEditorControl.sln`
2. Set as startup project
3. Press F5

### Command Line
```bash
dotnet run
```

## 📦 Project Type

- **Platform**: WPF
- **Language**: C#
- **Target Frameworks**:
  - .NET 7.0-windows
  - .NET 8.0-windows

## 🎓 Use Cases

- **Binary Patching**: Inject code into executables
- **File Modification**: Add headers or trailers
- **Data Injection**: Insert data at specific offsets
- **Format Manipulation**: Modify binary file structures
- **ROM Hacking**: Insert new data into game ROMs

## 📖 Key Operations

### Insert Byte
```csharp
hexEditor.InsertByte(position, value);
```

### Delete Bytes
```csharp
hexEditor.DeleteSelection();
hexEditor.DeleteBytesAtPosition(position, length);
```

### Toggle Insert Mode
```csharp
hexEditor.InsertMode = !hexEditor.InsertMode;
```

## 📚 Related Samples

- **[Main Sample](../WPFHexEditor.Sample.CSharp/)** - Standard editing features
- **[ServiceUsage Sample](../WpfHexEditor.Sample.ServiceUsage/)** - ByteModificationService API
- **[All Samples](../README.md)** - Overview of all samples

---

✨ Advanced byte insertion and deletion demonstration
