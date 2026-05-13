//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.App.Scripting;
using WpfHexEditor.Docking.Core;

namespace WpfHexEditor.App;

/// <summary>
/// Scripting Console module wiring: View > Scripting Console menu handler.
/// Panel content ID served through BuildContentForItem (MainWindow.xaml.cs).
/// </summary>
public partial class MainWindow
{
    private void OnShowScriptingConsole()
        => ShowOrCreatePanel("Scripting Console", ScriptingModule.ContentId, DockDirection.Bottom);
}
