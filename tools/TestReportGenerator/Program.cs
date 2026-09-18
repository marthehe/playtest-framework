using TestReportGenerator;

CliOptions options;

try
{
    options = CliOptions.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(CliOptions.Usage);
    return 2;
}

try
{
    var trxFiles = TrxResultReader.ResolveTrxFiles(options.InputPath);
    var report = TrxResultReader.BuildReport(trxFiles);
    await ReliabilityReportWriter.WriteAsync(report, options.ReliabilityOutputPath);

    Console.WriteLine(
        $"Analysed {report.TotalRuns} test runs across {report.Tests.Count} tests. " +
        $"Potentially flaky tests: {report.PotentiallyFlakyTests}.");
    Console.WriteLine($"Reliability report: {options.ReliabilityOutputPath}");

    if (options.TriageOutputPath is not null)
    {
        using var httpClient = new HttpClient();
        IFailureTriageAnalyzer analyzer = options.UseAi
            ? OpenAiFailureTriageAnalyzer.FromEnvironment(httpClient)
            : new RuleBasedFailureTriageAnalyzer();

        var failures = TrxFailureReader.ReadFailures(trxFiles);
        var triageResults = new List<FailureTriage>();

        foreach (var failure in failures)
            triageResults.Add(await analyzer.AnalyzeAsync(failure));

        FailureTriageEvaluation? evaluation = null;
        if (options.LabelsPath is not null)
        {
            var labels = LabelledFailureReader.Read(options.LabelsPath);
            evaluation = await FailureTriageEvaluator.EvaluateAsync(labels, analyzer);
        }

        var triageReport = new FailureTriageReport(
            DateTime.UtcNow,
            analyzer.Name,
            triageResults.Count,
            evaluation,
            triageResults);

        await FailureTriageReportWriter.WriteAsync(triageReport, options.TriageOutputPath);

        Console.WriteLine(
            $"Generated advisory triage for {triageReport.FailedTests} failed tests " +
            $"using '{triageReport.Analyzer}'.");
        if (evaluation is not null)
            Console.WriteLine(
                $"Labelled evaluation accuracy: {evaluation.Correct}/{evaluation.Samples} " +
                $"({evaluation.Accuracy:P1}).");
        Console.WriteLine($"Failure triage report: {options.TriageOutputPath}");
        Console.WriteLine("Triage is advisory and does not alter test or quality-gate outcomes.");
    }

    return 0;
}
catch (Exception exception) when (
    exception is ArgumentException
        or DirectoryNotFoundException
        or FileNotFoundException
        or InvalidDataException
        or HttpRequestException)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
