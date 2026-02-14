# WpfHexEditor.Sample.Winform

Demonstrates integration of the WPF HexEditor control into Windows Forms applications.

## 🎯 Purpose

Shows how to embed a WPF control into a Windows Forms application using ElementHost.

## ✨ Features Demonstrated

- **WPF/WinForms Interoperability**: Using ElementHost
- **Control Integration**: Embedding WPF UserControl in WinForms
- **Event Handling**: Cross-framework event communication
- **Basic Operations**: File open, edit, save

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

- **Platform**: Windows Forms
- **Language**: C#
- **Target Framework**: .NET Framework 4.8
- **WPF Control Integration**: ElementHost

## 🎓 Use Cases

- **Legacy Applications**: Adding hex editing to existing WinForms apps
- **Mixed UI**: Combining WPF and WinForms controls
- **Migration**: Gradual transition from WinForms to WPF

## 📖 Key Integration Code

```csharp
// Create ElementHost
var elementHost = new ElementHost
{
    Dock = DockStyle.Fill,
    Child = new WpfHexaEditor.HexEditor()
};
this.Controls.Add(elementHost);
```

## 📚 Related Samples

- **[WPF Sample (C#)](../WPFHexEditor.Sample.CSharp/)** - Pure WPF implementation
- **[All Samples](../README.md)** - Overview of all samples

---

✨ Windows Forms integration example for WPF HexEditor Control
