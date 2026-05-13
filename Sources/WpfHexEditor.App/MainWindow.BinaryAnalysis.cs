//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.App.BinaryAnalysis;
using WpfHexEditor.Docking.Core;

namespace WpfHexEditor.App;

/// <summary>
/// Binary Analysis module wiring: View > Binary Analysis sub-menu handlers.
/// Panel content IDs are served through BuildContentForItem (MainWindow.xaml.cs).
/// </summary>
public partial class MainWindow
{
    private void OnShowStringExtraction()
        => ShowOrCreatePanel("String Extraction", BinaryAnalysisModule.ContentIdStrings, DockDirection.Bottom);

    private void OnShowHashInspector()
        => ShowOrCreatePanel("Hash Inspector", BinaryAnalysisModule.ContentIdHash, DockDirection.Bottom);

    private void OnShowFileCarver()
        => ShowOrCreatePanel("File Carver", BinaryAnalysisModule.ContentIdCarver, DockDirection.Bottom);

    private void OnShowSignatureDb()
        => ShowOrCreatePanel("Signature Database", BinaryAnalysisModule.ContentIdSigDb, DockDirection.Bottom);

    private void OnShowByteFrequency()
        => ShowOrCreatePanel("Byte Frequency", BinaryAnalysisModule.ContentIdFreq, DockDirection.Bottom);
}
