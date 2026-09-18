using System.Xml.Linq;

namespace TestReportGenerator;

/// <summary>
/// Extracts failed-test messages, stack traces, and captured output from TRX files.
/// </summary>
public static class TrxFailureReader
{
    /// <summary>
    /// Reads failed executions in stable source and test-name order.
    /// </summary>
    public static IReadOnlyList<FailureEvidence> ReadFailures(IEnumerable<string> trxFiles)
    {
        var failures = new List<FailureEvidence>();

        foreach (var trxFile in trxFiles.Order(StringComparer.Ordinal))
        {
            var document = XDocument.Load(trxFile, LoadOptions.None);
            var testNames = TrxResultReader.BuildTestNameLookup(document);

            foreach (var result in document.Descendants().Where(
                         element =>
                             element.Name.LocalName == "UnitTestResult" &&
                             string.Equals(
                                 (string?)element.Attribute("outcome"),
                                 "Failed",
                                 StringComparison.OrdinalIgnoreCase)))
            {
                var testId = (string?)result.Attribute("testId");
                var fallbackName = (string?)result.Attribute("testName") ?? "Unknown test";
                var testName = testId is not null && testNames.TryGetValue(testId, out var fullName)
                    ? fullName
                    : fallbackName;

                failures.Add(new FailureEvidence(
                    testName,
                    Path.GetFileName(trxFile),
                    ReadValue(result, "ErrorInfo", "Message"),
                    ReadValue(result, "ErrorInfo", "StackTrace"),
                    ReadValue(result, "Output", "StdOut")));
            }
        }

        return failures
            .OrderBy(failure => failure.Source, StringComparer.Ordinal)
            .ThenBy(failure => failure.Test, StringComparer.Ordinal)
            .ToList();
    }

    private static string ReadValue(XElement result, string parentName, string valueName)
    {
        var parent = result.Descendants().FirstOrDefault(
            element => element.Name.LocalName == parentName);
        return parent?.Descendants().FirstOrDefault(
            element => element.Name.LocalName == valueName)?.Value.Trim() ?? string.Empty;
    }
}
