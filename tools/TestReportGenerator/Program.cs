using TestReportGenerator;

if (args.Length == 0)
{
    Console.Error.WriteLine(
        "Usage: dotnet run --project tools/TestReportGenerator -- <TRX file or directory> [--output <path>]");
    return 2;
}

var inputPath = Path.GetFullPath(args[0]);
var outputIndex = Array.IndexOf(args, "--output");
var outputPath = outputIndex >= 0 && outputIndex + 1 < args.Length
    ? Path.GetFullPath(args[outputIndex + 1])
    : Path.Combine(Environment.CurrentDirectory, "test-reliability-report.json");

try
{
    var trxFiles = TrxResultReader.ResolveTrxFiles(inputPath);
    var report = TrxResultReader.BuildReport(trxFiles);
    await ReliabilityReportWriter.WriteAsync(report, outputPath);

    Console.WriteLine(
        $"Analysed {report.TotalRuns} test runs across {report.Tests.Count} tests. " +
        $"Potentially flaky tests: {report.PotentiallyFlakyTests}.");
    Console.WriteLine($"Report: {outputPath}");
    return 0;
}
catch (Exception exception) when (
    exception is ArgumentException or DirectoryNotFoundException or FileNotFoundException)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
