using System.Xml.Linq;

namespace TestReportGenerator;

public static class TrxResultReader
{
    public static IReadOnlyList<string> ResolveTrxFiles(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            if (!string.Equals(Path.GetExtension(inputPath), ".trx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The input file must use the .trx extension.");

            return [inputPath];
        }

        if (!Directory.Exists(inputPath))
            throw new DirectoryNotFoundException($"Input path '{inputPath}' does not exist.");

        var files = Directory.GetFiles(inputPath, "*.trx", SearchOption.AllDirectories);
        if (files.Length == 0)
            throw new FileNotFoundException($"No TRX files were found under '{inputPath}'.");

        return files;
    }

    public static ReliabilityReport BuildReport(IEnumerable<string> trxFiles)
    {
        var outcomes = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var trxFile in trxFiles)
        {
            var document = XDocument.Load(trxFile, LoadOptions.None);
            var testNames = BuildTestNameLookup(document);

            foreach (var result in document.Descendants().Where(
                         element => element.Name.LocalName == "UnitTestResult"))
            {
                var testId = (string?)result.Attribute("testId");
                var fallbackName = (string?)result.Attribute("testName") ?? "Unknown test";
                var testName = testId is not null && testNames.TryGetValue(testId, out var fullName)
                    ? fullName
                    : fallbackName;
                var outcome = (string?)result.Attribute("outcome") ?? "Unknown";

                if (!outcomes.TryGetValue(testName, out var testOutcomes))
                {
                    testOutcomes = [];
                    outcomes[testName] = testOutcomes;
                }

                testOutcomes.Add(outcome);
            }
        }

        var tests = outcomes
            .Select(pair => CreateReliability(pair.Key, pair.Value))
            .OrderByDescending(test => test.IsPotentiallyFlaky)
            .ThenByDescending(test => test.FlakinessScore)
            .ThenBy(test => test.Test, StringComparer.Ordinal)
            .ToList();

        return new ReliabilityReport(
            DateTime.UtcNow,
            tests.Sum(test => test.Runs),
            tests.Count(test => test.IsPotentiallyFlaky),
            tests);
    }

    private static Dictionary<string, string> BuildTestNameLookup(XDocument document)
    {
        return document.Descendants()
            .Where(element => element.Name.LocalName == "UnitTest")
            .Select(element => new
            {
                Id = (string?)element.Attribute("id"),
                Method = element.Descendants().FirstOrDefault(
                    child => child.Name.LocalName == "TestMethod")
            })
            .Where(item => item.Id is not null && item.Method is not null)
            .ToDictionary(
                item => item.Id!,
                item =>
                {
                    var className = (string?)item.Method!.Attribute("className");
                    var methodName = (string?)item.Method.Attribute("name");
                    return string.IsNullOrWhiteSpace(className)
                        ? methodName ?? "Unknown test"
                        : $"{className}.{methodName}";
                },
                StringComparer.Ordinal);
    }

    private static TestReliability CreateReliability(string testName, IReadOnlyCollection<string> outcomes)
    {
        var passed = outcomes.Count(outcome =>
            string.Equals(outcome, "Passed", StringComparison.OrdinalIgnoreCase));
        var failed = outcomes.Count(outcome =>
            string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase));
        var skipped = outcomes.Count - passed - failed;
        var score = passed > 0 && failed > 0
            ? decimal.Round((decimal)Math.Min(passed, failed) / outcomes.Count, 4)
            : 0m;

        return new TestReliability(testName, outcomes.Count, passed, failed, skipped, score);
    }
}
