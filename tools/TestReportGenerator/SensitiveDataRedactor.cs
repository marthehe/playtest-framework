using System.Text.RegularExpressions;

namespace TestReportGenerator;

/// <summary>
/// Removes common credentials and personal identifiers before external analysis.
/// </summary>
public static partial class SensitiveDataRedactor
{
    private const int MaximumEvidenceLength = 6_000;

    /// <summary>
    /// Creates a bounded, redacted diagnostic payload suitable for an external analyzer.
    /// </summary>
    public static string Prepare(FailureEvidence failure)
    {
        var evidence = $"""
            Test: {failure.Test}
            Message:
            {failure.Message}
            Stack trace:
            {failure.StackTrace}
            Standard output:
            {failure.StandardOutput}
            """;

        evidence = SecretAssignmentRegex().Replace(evidence, "$1[REDACTED]");
        evidence = BearerTokenRegex().Replace(evidence, "$1[REDACTED]");
        evidence = JsonWebTokenRegex().Replace(evidence, "[REDACTED_TOKEN]");
        evidence = EmailRegex().Replace(evidence, "[REDACTED_EMAIL]");
        evidence = WindowsUserPathRegex().Replace(evidence, @"C:\Users\[REDACTED]");

        return evidence.Length <= MaximumEvidenceLength
            ? evidence
            : $"{evidence[..MaximumEvidenceLength]}\n[TRUNCATED]";
    }

    [GeneratedRegex(
        @"(?im)\b(password|pwd|secret|api[-_ ]?key|connection[-_ ]?string|token)\s*[:=]\s*[^\s;,""]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(
        @"(?im)(authorization\s*:\s*bearer\s+)[A-Za-z0-9._~+/=-]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(
        @"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonWebTokenRegex();

    [GeneratedRegex(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(
        @"C:\\Users\\[^\\\s]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WindowsUserPathRegex();
}
