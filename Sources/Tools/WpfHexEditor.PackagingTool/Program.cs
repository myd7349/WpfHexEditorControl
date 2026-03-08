//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

// whxpack — WpfHexEditor plugin packaging tool
// Usage: whxpack --input <dir> --output <file.whxplugin> [--sign <certPath>]

namespace WpfHexEditor.PackagingTool;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        string? inputDir    = null;
        string? outputFile  = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input"  when i + 1 < args.Length: inputDir   = args[++i]; break;
                case "--output" when i + 1 < args.Length: outputFile = args[++i]; break;
                case "--sign"   when i + 1 < args.Length: i++;                    break; // Phase 5
                case "--help": PrintHelp(); return 0;
            }
        }

        if (inputDir is null || outputFile is null) { PrintHelp(); return 1; }

        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Error: input directory not found: {inputDir}");
            return 2;
        }

        try
        {
            Console.WriteLine($"Packaging '{inputDir}' → '{outputFile}'...");
            var builder = new PackageBuilder();
            await builder.BuildAsync(inputDir, outputFile).ConfigureAwait(false);
            Console.WriteLine($"Done. Package written to: {outputFile}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            whxpack — WpfHexEditor Plugin Packager

            Usage:
              whxpack --input <pluginDir> --output <output.whxplugin> [--sign <certPath>]

            Options:
              --input   Directory containing manifest.json and the plugin assembly
              --output  Output .whxplugin package path
              --sign    (Phase 5) Path to signing certificate (.pfx)
            """);
    }
}
