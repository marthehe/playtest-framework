namespace TestReportGenerator;

/// <summary>
/// Provides an offline, auditable classification baseline for failed-test diagnostics.
/// </summary>
public sealed class RuleBasedFailureTriageAnalyzer : IFailureTriageAnalyzer
{
    private static readonly IReadOnlyList<ClassificationRule> s_rules =
    [
        new(
            FailureCategory.Configuration,
            0.92m,
            ["configuration", "environment variable", "missing setting", "uriformatexception",
                "credential", "connection string"],
            ["Verify test configuration and required environment variables.",
                "Check that endpoints and settings are valid for the test environment."]),
        new(
            FailureCategory.Timeout,
            0.94m,
            ["timeout", "timed out", "taskcanceledexception", "operation canceled"],
            ["Identify the operation that exceeded its deadline.",
                "Check contention, retries, blocking calls, and environment capacity."]),
        new(
            FailureCategory.ExternalDependency,
            0.9m,
            ["httprequestexception", "socketexception", "connection refused", "service unavailable",
                "status code 429", "status code 500", "status code 502", "status code 503"],
            ["Check dependency availability and the recorded response status.",
                "Verify retry, isolation, and test-double boundaries."]),
        new(
            FailureCategory.AssertionFailure,
            0.88m,
            ["assert", "expected", "fluentassertions", "xunit.sdk", "should()."],
            ["Compare the expected behaviour with the actual value.",
                "Confirm whether the product behaviour or the test expectation changed."]),
        new(
            FailureCategory.TestDataOrState,
            0.86m,
            ["duplicate", "already exists", "not found", "invalid state", "constraint",
                "test data", "seed"],
            ["Inspect test-data setup and cleanup.",
                "Check whether state leaked from another test or run."]),
        new(
            FailureCategory.ApplicationDefect,
            0.82m,
            ["nullreferenceexception", "invalidoperationexception", "argumentexception",
                "indexoutofrangeexception", "keynotfoundexception"],
            ["Start at the first application frame in the stack trace.",
                "Reproduce with the smallest input that reaches the failing branch."])
    ];

    public string Name => "rule-based-v1";

    public Task<FailureTriage> AnalyzeAsync(
        FailureEvidence failure,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evidence = string.Join(
            "\n",
            failure.Message,
            failure.StackTrace,
            failure.StandardOutput).ToLowerInvariant();
        var rule = s_rules.FirstOrDefault(candidate =>
            candidate.Signals.Any(signal => evidence.Contains(signal, StringComparison.Ordinal)));

        var result = rule is null
            ? new FailureTriage(
                failure.Test,
                FailureCategory.Unknown,
                BuildSummary(failure),
                ["Review the first meaningful error and application stack frame.",
                    "Reproduce the failure locally with diagnostic logging enabled."],
                0.35m,
                Name,
                [])
            : new FailureTriage(
                failure.Test,
                rule.Category,
                BuildSummary(failure),
                rule.InvestigationAreas,
                rule.Confidence,
                Name,
                rule.Signals
                    .Where(signal => evidence.Contains(signal, StringComparison.Ordinal))
                    .Take(3)
                    .ToList());

        return Task.FromResult(result);
    }

    private static string BuildSummary(FailureEvidence failure)
    {
        var source = new[] { failure.Message, failure.StandardOutput, failure.StackTrace }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (source is null)
            return "The test failed without recording diagnostic output.";

        var summary = string.Join(
            " ",
            source.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return summary.Length <= 240 ? summary : $"{summary[..237]}...";
    }

    private sealed record ClassificationRule(
        FailureCategory Category,
        decimal Confidence,
        IReadOnlyList<string> Signals,
        IReadOnlyList<string> InvestigationAreas);
}
