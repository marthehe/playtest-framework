namespace TestReportGenerator;

/// <summary>
/// Produces advisory failure analysis without changing test outcomes.
/// </summary>
public interface IFailureTriageAnalyzer
{
    string Name { get; }

    Task<FailureTriage> AnalyzeAsync(
        FailureEvidence failure,
        CancellationToken cancellationToken = default);
}
