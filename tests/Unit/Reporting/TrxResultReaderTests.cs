using FluentAssertions;
using TestReportGenerator;

namespace PlayTest.Unit.Reporting;

public sealed class TrxResultReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"playtest-reporting-{Guid.NewGuid():N}");

    [Fact]
    public void BuildReport_MixedOutcomes_CalculatesFlakiness()
    {
        Directory.CreateDirectory(_directory);
        var firstRun = WriteTrx("run-1.trx", "Passed");
        var secondRun = WriteTrx("run-2.trx", "Failed");

        var report = TrxResultReader.BuildReport([firstRun, secondRun]);

        report.TotalRuns.Should().Be(2);
        report.PotentiallyFlakyTests.Should().Be(1);
        report.Tests.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new TestReliability(
                "PlayTest.ExampleTests.CanCreatePlayer",
                2,
                1,
                1,
                0,
                0.5m));
    }

    [Fact]
    public void BuildReport_ConsistentlyFailingTest_IsNotClassifiedAsFlaky()
    {
        Directory.CreateDirectory(_directory);
        var trxFile = WriteTrx("run.trx", "Failed");

        var report = TrxResultReader.BuildReport([trxFile]);

        report.PotentiallyFlakyTests.Should().Be(0);
        report.Tests.Single().FlakinessScore.Should().Be(0);
    }

    [Fact]
    public void ResolveTrxFiles_RejectsUnsupportedFileType()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "results.xml");
        File.WriteAllText(path, "<results />");

        var act = () => TrxResultReader.ResolveTrxFiles(path);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolveTrxFiles_NestedHistory_ReturnsCurrentAndHistoricalResults()
    {
        Directory.CreateDirectory(_directory);
        var current = WriteTrx(
            Path.Combine("tests", "Unit", "TestResults", "current.trx"),
            "Passed");
        var historical = WriteTrx(
            Path.Combine(".test-history", "12345", "tests", "Unit", "historical.trx"),
            "Failed");

        var files = TrxResultReader.ResolveTrxFiles(_directory);

        files.Should().BeEquivalentTo([current, historical]);
    }

    [Fact]
    public void ReadFailures_FailedResult_ExtractsDiagnosticEvidence()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "failed.trx");
        File.WriteAllText(
            path,
            """
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="test-1" testName="CanCreatePlayer" outcome="Failed">
                  <Output>
                    <ErrorInfo>
                      <Message>Expected status code 201 but found 409.</Message>
                      <StackTrace>at PlayTest.ExampleTests.CanCreatePlayer()</StackTrace>
                    </ErrorInfo>
                    <StdOut>Request completed.</StdOut>
                  </Output>
                </UnitTestResult>
              </Results>
              <TestDefinitions>
                <UnitTest id="test-1">
                  <TestMethod className="PlayTest.ExampleTests" name="CanCreatePlayer" />
                </UnitTest>
              </TestDefinitions>
            </TestRun>
            """);

        var failure = TrxFailureReader.ReadFailures([path]).Should().ContainSingle().Subject;

        failure.Test.Should().Be("PlayTest.ExampleTests.CanCreatePlayer");
        failure.Message.Should().Be("Expected status code 201 but found 409.");
        failure.StackTrace.Should().Contain("CanCreatePlayer");
        failure.StandardOutput.Should().Be("Request completed.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private string WriteTrx(string fileName, string outcome)
    {
        var path = Path.Combine(_directory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            $$"""
              <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                <Results>
                  <UnitTestResult testId="test-1" testName="CanCreatePlayer" outcome="{{outcome}}" />
                </Results>
                <TestDefinitions>
                  <UnitTest id="test-1">
                    <TestMethod className="PlayTest.ExampleTests" name="CanCreatePlayer" />
                  </UnitTest>
                </TestDefinitions>
              </TestRun>
              """);
        return path;
    }
}
