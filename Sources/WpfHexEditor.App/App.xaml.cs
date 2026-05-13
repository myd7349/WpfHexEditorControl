//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5, Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Globalization;
using System.IO;
using System.Resources;
using System.Windows;
using WpfHexEditor.Core.Localization.Properties;
using WpfHexEditor.Core.Localization.Services;
using WpfHexEditor.Core.Options;
using LocalizationService = WpfHexEditor.Core.Localization.Services.LocalizationService;

namespace WpfHexEditor.App;

public partial class App : Application
{
    /// <summary>
    /// File or solution path passed via command-line argument (--open "path" or bare path).
    /// Consumed by MainWindow.OnLoaded to open on startup.
    /// </summary>
    public static string? StartupFilePath { get; private set; }

    /// <summary>
    /// When set, MainWindow opens a DiffViewer comparing these two files on startup.
    /// Usage: WpfHexEditor.exe --diff "left.bin" "right.bin"
    /// </summary>
    public static (string Left, string Right)? StartupDiffPaths { get; private set; }

    public App()
    {
        // Restore the saved UI language BEFORE InitializeComponent() processes
        // App.xaml and instantiates all LocalizedResourceDictionary entries.
        // StaticResource bindings resolve at BAML parse time, so the culture
        // must be set here — OnStartup fires too late.
        RestorePreferredLanguage();
        InitializeComponent();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LocalizationService.Instance = new LocalizationService();
        ParseCommandLine(e.Args);
    }

    /// <summary>
    /// Restores the UI language persisted in AppSettings.
    /// Empty/missing = keep the system default (no-op).
    /// </summary>
    private static void RestorePreferredLanguage()
    {
        // Load settings early so PreferredLanguage is available before
        // InitializeComponent() instantiates the localized dictionaries.
        // MainWindow.OnLoaded will call Load() again — that is harmless.
        AppSettingsService.Instance.Load();

        var cultureName = AppSettingsService.Instance.Current.PreferredLanguage;

        CultureInfo culture;
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            // "System default": resolve the culture the IDE will actually display.
            // We probe CommonResources — if no satellite exists for the OS locale,
            // the IDE falls back to en-US (the neutral assembly language).
            // Calling ChangeCulture() with that effective culture ensures plugin
            // dictionaries that DO have a satellite for the OS locale (e.g. es)
            // also fall back consistently instead of displaying a different language.
            culture = ResolveEffectiveCulture(CultureInfo.CurrentUICulture);
        }
        else
        {
            try   { culture = new CultureInfo(cultureName); }
            catch (CultureNotFoundException) { return; }
        }

        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        System.Threading.Thread.CurrentThread.CurrentCulture   = culture;
        LocalizedResourceDictionary.ChangeCulture(culture);
    }

    /// <summary>
    /// Returns the culture the IDE will actually display for <paramref name="osCulture"/>.
    /// Probes CommonResources satellites: if none exists for the OS locale (or its neutral parent),
    /// the IDE falls back to en-US — so we return en-US to keep all plugins in sync.
    /// </summary>
    private static CultureInfo ResolveEffectiveCulture(CultureInfo osCulture)
    {
        var rm = CommonResources.ResourceManager;

        // Try exact locale (e.g. es-MX), then neutral (e.g. es).
        var probes = new[] { osCulture, osCulture.Parent };
        foreach (var probe in probes)
        {
            if (probe.Equals(CultureInfo.InvariantCulture)) break;
            try
            {
                var set = rm.GetResourceSet(probe, createIfNotExists: true, tryParents: false);
                if (set is not null) return probe;
            }
            catch (MissingManifestResourceException) { }
            catch { }
        }

        // No CommonResources satellite → IDE shows en-US (the neutral assembly language).
        return new CultureInfo("en-US");
    }

    private static void ParseCommandLine(string[] args)
    {
        if (args.Length == 0) return;

        // Pattern 1: --diff "left" "right"
        for (int i = 0; i < args.Length - 2; i++)
        {
            if (args[i].Equals("--diff", StringComparison.OrdinalIgnoreCase))
            {
                StartupDiffPaths = (args[i + 1], args[i + 2]);
                return;
            }
        }

        // Pattern 2: --open "path"
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--open", StringComparison.OrdinalIgnoreCase))
            {
                StartupFilePath = args[i + 1];
                return;
            }
        }

        // Pattern 3: bare path as first argument (file association, drag-and-drop)
        var first = args[0];
        if (!first.StartsWith('-') && File.Exists(first))
            StartupFilePath = first;
    }
}
