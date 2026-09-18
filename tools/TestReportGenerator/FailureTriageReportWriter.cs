using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestReportGenerator;

/// <summary>
/// Serializes advisory triage reports as stable, human-readable JSON artifacts.
/// </summary>
public static class FailureTriageReportWriter
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteAsync(
        FailureTriageReport report,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(
            stream,
            report,
            s_serializerOptions,
            cancellationToken);
    }
}
