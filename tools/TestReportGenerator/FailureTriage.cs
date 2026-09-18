namespace TestReportGenerator;

/// <summary>
/// Identifies a broad, investigation-oriented failure category.
/// </summary>
public enum FailureCategory
{
    Unknown,
    AssertionFailure,
    Timeout,
    ExternalDependency,
    Configuration,
    TestDataOrState,
    ApplicationDefect
}

/// <summary>
/// Contains advisory analysis for one failed test.
/// </summary>
public sealed record FailureTriage(
    string Test,
    FailureCategory Category,
    string Summary,
    IReadOnlyList<string> InvestigationAreas,
    decimal Confidence,
    string Analyzer,
    IReadOnlyList<string> EvidenceSignals);

/// <summary>
/// Measures analyzer performance against explicitly labelled examples.
/// </summary>
public sealed record FailureTriageEvaluation(
    int Samples,
    int Correct,
    decimal Accuracy,
    IReadOnlyDictionary<FailureCategory, decimal> AccuracyByCategory);

/// <summary>
/// Contains advisory triage results and optional labelled evaluation metrics.
/// </summary>
public sealed record FailureTriageReport(
    DateTime GeneratedAtUtc,
    string Analyzer,
    int FailedTests,
    FailureTriageEvaluation? Evaluation,
    IReadOnlyList<FailureTriage> Failures);

/// <summary>
/// Describes a known failure used to evaluate classification accuracy.
/// </summary>
public sealed record LabelledFailure(
    string Name,
    string Message,
    string StackTrace,
    string StandardOutput,
    FailureCategory ExpectedCategory);
