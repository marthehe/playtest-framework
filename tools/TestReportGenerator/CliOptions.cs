using System.Globalization;

namespace TestReportGenerator;

/// <summary>
/// Contains validated command-line options for report generation.
/// </summary>
public sealed record CliOptions(
    string InputPath,
    string ReliabilityOutputPath,
    string? TriageOutputPath,
    string? SummaryOutputPath,
    string? LabelsPath,
    decimal? FlakinessThreshold,
    bool UseAi)
{
    public const string Usage =
        "Usage: dotnet run --project tools/TestReportGenerator -- " +
        "<TRX file or directory> [--output <path>] [--triage-output <path>] " +
        "[--summary-output <path>] [--labels <path>] " +
        "[--flakiness-threshold <0.0-0.5>] [--ai]";

    /// <summary>
    /// Parses paths and feature switches without accepting unknown arguments.
    /// </summary>
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new ArgumentException("A TRX file or directory is required.");

        var inputPath = Path.GetFullPath(args[0]);
        var reliabilityOutputPath = Path.Combine(
            Environment.CurrentDirectory,
            "test-reliability-report.json");
        string? triageOutputPath = null;
        string? summaryOutputPath = null;
        string? labelsPath = null;
        decimal? flakinessThreshold = null;
        var useAi = false;

        for (var index = 1; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--output":
                    reliabilityOutputPath = ReadPathValue(args, ref index, "--output");
                    break;
                case "--triage-output":
                    triageOutputPath = ReadPathValue(args, ref index, "--triage-output");
                    break;
                case "--summary-output":
                    summaryOutputPath = ReadPathValue(args, ref index, "--summary-output");
                    break;
                case "--labels":
                    labelsPath = ReadPathValue(args, ref index, "--labels");
                    break;
                case "--flakiness-threshold":
                    flakinessThreshold = ReadThresholdValue(args, ref index);
                    break;
                case "--ai":
                    useAi = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[index]}'.");
            }
        }

        if (useAi && triageOutputPath is null)
            throw new ArgumentException("--ai requires --triage-output.");
        if (labelsPath is not null && triageOutputPath is null)
            throw new ArgumentException("--labels requires --triage-output.");

        return new CliOptions(
            inputPath,
            reliabilityOutputPath,
            triageOutputPath,
            summaryOutputPath,
            labelsPath,
            flakinessThreshold,
            useAi);
    }

    private static string ReadPathValue(
        IReadOnlyList<string> args,
        ref int index,
        string option)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"{option} requires a path.");

        index++;
        return Path.GetFullPath(args[index]);
    }

    private static decimal ReadThresholdValue(IReadOnlyList<string> args, ref int index)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("--flakiness-threshold requires a decimal value.");

        index++;
        if (!decimal.TryParse(
                args[index],
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var threshold) ||
            threshold <= 0m ||
            threshold > 0.5m)
        {
            throw new ArgumentException(
                "--flakiness-threshold must be greater than 0 and no greater than 0.5.");
        }

        return threshold;
    }
}
