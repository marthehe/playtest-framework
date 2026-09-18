namespace TestReportGenerator;

/// <summary>
/// Contains the diagnostic evidence recorded for one failed test execution.
/// </summary>
public sealed record FailureEvidence(
    string Test,
    string Source,
    string Message,
    string StackTrace,
    string StandardOutput);
