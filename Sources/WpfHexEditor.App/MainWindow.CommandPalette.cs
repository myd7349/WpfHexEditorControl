//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : MainWindow.CommandPalette.cs
// Description  : Command Palette handler (Ctrl+Shift+P).
//                Builds the full command catalog (built-ins + plugin menu items)
//                and opens the CommandPaletteWindow overlay.
// Architecture : Partial class of MainWindow. No WPF → business logic cross-over.
//////////////////////////////////////////////

using System.Windows.Input;
using WpfHexEditor.App.Dialogs;
using WpfHexEditor.App.Models;
using WpfHexEditor.App.Services;

namespace WpfHexEditor.App;

public partial class MainWindow
{
    // -----------------------------------------------------------------------
    // Handler wired in MainWindow.xaml CommandBindings
    // -----------------------------------------------------------------------

    private void OnShowCommandPalette(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
    {
        var entries = BuildBuiltInEntries();

        // Append plugin-contributed menu items if adapter is ready.
        if (_menuAdapter is not null)
        {
            foreach (var (uiId, descriptor) in _menuAdapter.GetAllMenuItems())
            {
                if (descriptor.Command is null) continue;

                var category = string.IsNullOrWhiteSpace(descriptor.ParentPath)
                    ? "Plugins"
                    : descriptor.ParentPath.TrimStart('_');

                entries.Add(new CommandPaletteEntry(
                    Name:             descriptor.Header?.ToString() ?? uiId,
                    Category:         category,
                    GestureText:      descriptor.GestureText,
                    IconGlyph:        descriptor.IconGlyph,
                    Command:          descriptor.Command,
                    CommandParameter: descriptor.CommandParameter));
            }
        }

        var service = new CommandPaletteService(entries);
        new CommandPaletteWindow(service, this).Show();
    }

    // -----------------------------------------------------------------------
    // Built-in entries — wrap ApplicationCommands / RoutedCommands so that
    // they are executed against the MainWindow (correct CommandTarget).
    // -----------------------------------------------------------------------

    private List<CommandPaletteEntry> BuildBuiltInEntries()
    {
        // Helper: wraps a RoutedCommand so it routes to this MainWindow.
        ICommand Wrap(RoutedCommand cmd, object? param = null) =>
            new RelayCommand(_ => cmd.Execute(param, this), _ => cmd.CanExecute(param, this));

        return
        [
            new("New File",              "File",    "Ctrl+N",         "\uE8A5", Wrap(System.Windows.Input.ApplicationCommands.New)),
            new("Open File…",            "File",    "Ctrl+O",         "\uE8B5", Wrap(System.Windows.Input.ApplicationCommands.Open)),
            new("Save",                  "File",    "Ctrl+S",         "\uE74E", Wrap(System.Windows.Input.ApplicationCommands.Save)),
            new("Close Tab",             "File",    "Ctrl+W",         "\uE711", Wrap(System.Windows.Input.ApplicationCommands.Close)),
            new("Write to Disk",         "File",    "Ctrl+Shift+W",   "\uE74E", Wrap(WriteToDiskCommand)),
            new("Find / Quick Search",   "Edit",    "Ctrl+F",         "\uE721", Wrap(System.Windows.Input.ApplicationCommands.Find)),
            new("Advanced Search",       "Edit",    "Ctrl+Shift+F",   "\uE721", Wrap(AdvancedSearchCommand)),
            new("Find Next",             "Edit",    "F3",             null,     Wrap(FindNextCommand)),
            new("Find Previous",         "Edit",    "Shift+F3",       null,     Wrap(FindPreviousCommand)),
            new("Go to Offset…",         "Edit",    "Ctrl+G",         "\uE8AD", Wrap(GoToOffsetCommand)),
            new("Open Plugin Manager",   "Plugins", null,             "\uE74C", Wrap(OpenPluginManagerCommand)),
            new("Command Palette",       "View",    "Ctrl+Shift+P",   "\uE721", Wrap(ShowCommandPaletteCommand)),
        ];
    }

}
