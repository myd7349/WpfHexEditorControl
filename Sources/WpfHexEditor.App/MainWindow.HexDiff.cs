//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.App.HexDiff;
using WpfHexEditor.Docking.Core;

namespace WpfHexEditor.App;

/// <summary>
/// Hex Diff module wiring: View > Hex Diff menu handler.
/// Panel content ID served through BuildContentForItem (MainWindow.xaml.cs).
/// </summary>
public partial class MainWindow
{
    private void OnShowHexDiff()
        => ShowOrCreatePanel("Hex Diff", HexDiffModule.ContentId, DockDirection.Bottom);
}
