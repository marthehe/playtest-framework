using System.Text.Json;

namespace TestReportGenerator;

/// <summary>
/// Serializes reliability reports as stable, human-readable JSON artifacts.
/// </summary>
public static class ReliabilityReportWriter
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Writes a reliability report and creates the target directory when necessary.
    /// </summary>
    public static async Task WriteAsync(
        ReliabilityReport report,
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
