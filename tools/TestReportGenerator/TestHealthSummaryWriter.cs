using System.Globalization;
using System.Text;

namespace TestReportGenerator;

/// <summary>
/// Writes a concise Markdown view of reliability and advisory triage results.
/// </summary>
public static class TestHealthSummaryWriter
{
    private const int MaximumTableRows = 10;

    /// <summary>
    /// Writes a summary suitable for GitHub Actions and downloadable build artifacts.
    /// </summary>
    public static async Task WriteAsync(
        ReliabilityReport reliabilityReport,
        FailureTriageReport? triageReport,
        FlakinessGateResult? flakinessGate,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var failedRuns = reliabilityReport.Tests.Sum(test => test.Failed);
        var status = reliabilityReport.PotentiallyFlakyTests == 0 && failedRuns == 0
            ? "Healthy"
            : "Attention required";
        var summary = new StringBuilder();

        summary.AppendLine("# PlayTest test health");
        summary.AppendLine();
        summary.AppendLine($"**Status:** {status}");
        summary.AppendLine();
        summary.AppendLine("| Metric | Value |");
        summary.AppendLine("| --- | ---: |");
        summary.AppendLine(
            $"| Recorded test executions | {reliabilityReport.TotalRuns.ToString(CultureInfo.InvariantCulture)} |");
        summary.AppendLine(
            $"| Distinct tests | {reliabilityReport.Tests.Count.ToString(CultureInfo.InvariantCulture)} |");
        summary.AppendLine(
            $"| Failed executions | {failedRuns.ToString(CultureInfo.InvariantCulture)} |");
        summary.AppendLine(
            $"| Potentially flaky tests | {reliabilityReport.PotentiallyFlakyTests.ToString(CultureInfo.InvariantCulture)} |");
        summary.AppendLine(
            $"| Flakiness gate | {FormatGate(flakinessGate)} |");
        summary.AppendLine();

        AppendFlakyTests(summary, reliabilityReport);
        AppendFailureTriage(summary, triageReport);

        summary.AppendLine("## Interpretation");
        summary.AppendLine();
        summary.AppendLine(
            "Reliability metrics and an explicitly configured flakiness threshold are " +
            "deterministic. Failure triage is advisory and cannot change test outcomes or " +
            "quality-gate decisions.");

        await File.WriteAllTextAsync(
            outputPath,
            summary.ToString(),
            Encoding.UTF8,
            cancellationToken);
    }

    private static void AppendFlakyTests(
        StringBuilder summary,
        ReliabilityReport reliabilityReport)
    {
        summary.AppendLine("## Potentially flaky tests");
        summary.AppendLine();
        var flakyTests = reliabilityReport.Tests
            .Where(test => test.IsPotentiallyFlaky)
            .Take(MaximumTableRows)
            .ToList();

        if (flakyTests.Count == 0)
        {
            summary.AppendLine("No mixed pass and fail outcomes were found in the supplied history.");
            summary.AppendLine();
            return;
        }

        summary.AppendLine("| Test | Runs | Passed | Failed | Flakiness score |");
        summary.AppendLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (var test in flakyTests)
        {
            summary.AppendLine(
                $"| {Escape(test.Test)} | {test.Runs} | {test.Passed} | {test.Failed} | " +
                $"{test.FlakinessScore.ToString("0.0000", CultureInfo.InvariantCulture)} |");
        }

        summary.AppendLine();
    }

    private static void AppendFailureTriage(
        StringBuilder summary,
        FailureTriageReport? triageReport)
    {
        summary.AppendLine("## Failed-test triage");
        summary.AppendLine();
        if (triageReport is null)
        {
            summary.AppendLine("Failure triage was not requested for this report.");
            summary.AppendLine();
            return;
        }

        summary.AppendLine($"Analyzer: `{Escape(triageReport.Analyzer)}`");
        summary.AppendLine();
        if (triageReport.Failures.Count == 0)
        {
            summary.AppendLine("No failed tests required advisory triage.");
        }
        else
        {
            summary.AppendLine("| Test | Category | Confidence | Summary |");
            summary.AppendLine("| --- | --- | ---: | --- |");
            foreach (var failure in triageReport.Failures.Take(MaximumTableRows))
            {
                summary.AppendLine(
                    $"| {Escape(failure.Test)} | {failure.Category} | " +
                    $"{FormatPercentage(failure.Confidence, "0")} | " +
                    $"{Escape(failure.Summary)} |");
            }
        }

        summary.AppendLine();
        if (triageReport.Evaluation is not null)
        {
            summary.AppendLine(
                $"Labelled evaluation: {triageReport.Evaluation.Correct}/" +
                $"{triageReport.Evaluation.Samples} correct " +
                $"({FormatPercentage(triageReport.Evaluation.Accuracy, "0.0")}).");
            summary.AppendLine();
        }
    }

    private static string Escape(string value)
    {
        return string.Join(
                " ",
                value.Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string FormatPercentage(decimal value, string format)
    {
        return $"{(value * 100).ToString(format, CultureInfo.InvariantCulture)}%";
    }

    private static string FormatGate(FlakinessGateResult? gate)
    {
        if (gate is null)
            return "Not configured";

        var outcome = gate.IsBreached
            ? $"{gate.Breaches.Count} breach(es)"
            : "Passed";
        return $"{outcome} at {FormatPercentage(gate.Threshold, "0.0")}";
    }
}
