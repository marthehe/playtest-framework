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
    var flakinessGate = options.FlakinessThreshold is null
        ? null
        : FlakinessGate.Evaluate(report, options.FlakinessThreshold.Value);
    await ReliabilityReportWriter.WriteAsync(report, options.ReliabilityOutputPath);

    Console.WriteLine(
        $"Analysed {report.TotalRuns} test runs across {report.Tests.Count} tests. " +
        $"Potentially flaky tests: {report.PotentiallyFlakyTests}.");
    Console.WriteLine($"Reliability report: {options.ReliabilityOutputPath}");
    if (flakinessGate is not null)
    {
        Console.WriteLine(
            $"Flakiness threshold: {flakinessGate.Threshold:P1}. " +
            $"Breaches: {flakinessGate.Breaches.Count}.");
    }

    FailureTriageReport? triageReport = null;
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

        triageReport = new FailureTriageReport(
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

    if (options.SummaryOutputPath is not null)
    {
        await TestHealthSummaryWriter.WriteAsync(
            report,
            triageReport,
            flakinessGate,
            options.SummaryOutputPath);
        Console.WriteLine($"Test health summary: {options.SummaryOutputPath}");
    }

    if (flakinessGate?.IsBreached == true)
    {
        Console.Error.WriteLine(
            $"{flakinessGate.Breaches.Count} test(s) met or exceeded the configured " +
            $"flakiness threshold of {flakinessGate.Threshold:P1}.");
        return 1;
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
