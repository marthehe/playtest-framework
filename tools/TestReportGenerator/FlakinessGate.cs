namespace TestReportGenerator;

/// <summary>
/// Contains the tests that meet or exceed an explicitly configured flakiness threshold.
/// </summary>
public sealed record FlakinessGateResult(
    decimal Threshold,
    IReadOnlyList<TestReliability> Breaches)
{
    public bool IsBreached => Breaches.Count > 0;
}

/// <summary>
/// Evaluates deterministic reliability metrics against an optional quality threshold.
/// </summary>
public static class FlakinessGate
{
    /// <summary>
    /// Finds mixed-outcome tests whose flakiness score meets or exceeds the threshold.
    /// </summary>
    public static FlakinessGateResult Evaluate(
        ReliabilityReport report,
        decimal threshold)
    {
        if (threshold <= 0m || threshold > 0.5m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                "The flakiness threshold must be greater than 0 and no greater than 0.5.");
        }

        var breaches = report.Tests
            .Where(test =>
                test.IsPotentiallyFlaky &&
                test.FlakinessScore >= threshold)
            .OrderByDescending(test => test.FlakinessScore)
            .ThenBy(test => test.Test, StringComparer.Ordinal)
            .ToList();

        return new FlakinessGateResult(threshold, breaches);
    }
}
