using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestReportGenerator;

/// <summary>
/// Reads labelled diagnostic examples from JSON.
/// </summary>
public static class LabelledFailureReader
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<LabelledFailure> Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Labelled failure file '{path}' does not exist.");

        var labels = JsonSerializer.Deserialize<List<LabelledFailure>>(
            File.ReadAllText(path),
            s_serializerOptions);
        if (labels is null || labels.Count == 0)
            throw new InvalidDataException("The labelled failure file contains no examples.");

        return labels;
    }
}

/// <summary>
/// Compares analyzer classifications with explicit expected categories.
/// </summary>
public static class FailureTriageEvaluator
{
    public static async Task<FailureTriageEvaluation> EvaluateAsync(
        IReadOnlyCollection<LabelledFailure> labels,
        IFailureTriageAnalyzer analyzer,
        CancellationToken cancellationToken = default)
    {
        if (labels.Count == 0)
            throw new ArgumentException("At least one labelled failure is required.", nameof(labels));

        var results = new List<(FailureCategory Expected, FailureCategory Actual)>();
        foreach (var label in labels)
        {
            var analysis = await analyzer.AnalyzeAsync(
                new FailureEvidence(
                    label.Name,
                    "labelled-evaluation",
                    label.Message,
                    label.StackTrace,
                    label.StandardOutput),
                cancellationToken);
            results.Add((label.ExpectedCategory, analysis.Category));
        }

        var correct = results.Count(result => result.Expected == result.Actual);
        var accuracyByCategory = results
            .GroupBy(result => result.Expected)
            .ToDictionary(
                group => group.Key,
                group => decimal.Round(
                    (decimal)group.Count(result => result.Expected == result.Actual) /
                    group.Count(),
                    4));

        return new FailureTriageEvaluation(
            results.Count,
            correct,
            decimal.Round((decimal)correct / results.Count, 4),
            accuracyByCategory);
    }
}
