namespace TestReportGenerator;

public sealed record TestReliability(
    string Test,
    int Runs,
    int Passed,
    int Failed,
    int Skipped,
    decimal FlakinessScore)
{
    public bool IsPotentiallyFlaky => Passed > 0 && Failed > 0;
}

public sealed record ReliabilityReport(
    DateTime GeneratedAtUtc,
    int TotalRuns,
    int PotentiallyFlakyTests,
    IReadOnlyList<TestReliability> Tests);
