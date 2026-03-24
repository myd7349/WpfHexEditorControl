// ==========================================================
// Project: WpfHexEditor.Plugins.UnitTesting
// File: Services/TrxParser.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-23
// Description:
//     Parses Visual Studio TRX (Test Results XML) files into TestResult records.
// ==========================================================

using System.IO;
using System.Xml.Linq;
using WpfHexEditor.Plugins.UnitTesting.Models;

namespace WpfHexEditor.Plugins.UnitTesting.Services;

/// <summary>
/// Parses a TRX XML file produced by <c>dotnet test --logger trx</c>.
/// </summary>
public static class TrxParser
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    /// <summary>
    /// Parses <paramref name="trxPath"/> and returns all test case results.
    /// Returns an empty list on any parse error.
    /// </summary>
    public static IReadOnlyList<TestResult> Parse(string trxPath)
    {
        if (!File.Exists(trxPath)) return [];

        try
        {
            var doc     = XDocument.Load(trxPath);
            var root    = doc.Root;
            if (root is null) return [];

            // Build lookup: executionId → testName + className from <TestDefinitions>
            var definitions = new Dictionary<string, (string TestName, string ClassName)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var td in root.Descendants(Ns + "UnitTest"))
            {
                var exId = td.Element(Ns + "Execution")?.Attribute("id")?.Value ?? string.Empty;
                var name = td.Attribute("name")?.Value ?? string.Empty;

                var testMethod = td.Element(Ns + "TestMethod");
                var className  = testMethod?.Attribute("className")?.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(exId))
                    definitions[exId] = (name, className);
            }

            var results = new List<TestResult>();

            foreach (var r in root.Descendants(Ns + "UnitTestResult"))
            {
                var exId       = r.Attribute("executionId")?.Value ?? string.Empty;
                var outcomeStr = r.Attribute("outcome")?.Value ?? "NotExecuted";
                var durationStr = r.Attribute("duration")?.Value ?? "0";

                var outcome = outcomeStr switch
                {
                    "Passed"  => TestOutcome.Passed,
                    "Failed"  => TestOutcome.Failed,
                    _         => TestOutcome.Skipped,
                };

                _ = TimeSpan.TryParse(durationStr, out var duration);

                definitions.TryGetValue(exId, out var defInfo);
                var testName  = defInfo.TestName  ?? r.Attribute("testName")?.Value ?? string.Empty;
                var className = defInfo.ClassName ?? string.Empty;
                var assembly  = r.Attribute("testListId")?.Value ?? string.Empty;

                string? errorMsg   = null;
                string? stackTrace = null;

                var output = r.Element(Ns + "Output");
                if (output is not null)
                {
                    var ei = output.Element(Ns + "ErrorInfo");
                    errorMsg   = ei?.Element(Ns + "Message")?.Value?.Trim();
                    stackTrace = ei?.Element(Ns + "StackTrace")?.Value?.Trim();
                }

                results.Add(new TestResult(
                    TestName:     testName,
                    ClassName:    className,
                    AssemblyName: assembly,
                    Outcome:      outcome,
                    Duration:     duration,
                    ErrorMessage: errorMsg,
                    StackTrace:   stackTrace));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }
}
