namespace TestReportGenerator;

/// <summary>
/// Summarizes the outcomes and calculated instability of one test.
/// </summary>
public sealed record TestReliability(
    string Test,
    int Runs,
    int Passed,
    int Failed,
    int Skipped,
    decimal FlakinessScore)
{
    /// <summary>
    /// Indicates that the supplied history contains both passing and failing outcomes.
    /// </summary>
    public bool IsPotentiallyFlaky => Passed > 0 && Failed > 0;
}

/// <summary>
/// Contains reliability metrics aggregated across all supplied test results.
/// </summary>
public sealed record ReliabilityReport(
    DateTime GeneratedAtUtc,
    int TotalRuns,
    int PotentiallyFlakyTests,
    IReadOnlyList<TestReliability> Tests);
