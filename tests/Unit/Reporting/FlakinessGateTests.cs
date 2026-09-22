using FluentAssertions;
using TestReportGenerator;

namespace PlayTest.Unit.Reporting;

public sealed class FlakinessGateTests
{
    [Fact]
    public void Parse_ThresholdNotProvided_LeavesGateDisabled()
    {
        var options = CliOptions.Parse(["results"]);

        options.FlakinessThreshold.Should().BeNull();
    }

    [Fact]
    public void Parse_ValidThreshold_UsesInvariantDecimal()
    {
        var options = CliOptions.Parse(
            ["results", "--flakiness-threshold", "0.125"]);

        options.FlakinessThreshold.Should().Be(0.125m);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.51")]
    [InlineData("not-a-number")]
    public void Parse_InvalidThreshold_RejectsConfiguration(string value)
    {
        var act = () => CliOptions.Parse(
            ["results", "--flakiness-threshold", value]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_MixedOutcomesAtThreshold_ReportsDeterministicBreach()
    {
        var report = new ReliabilityReport(
            DateTime.UtcNow,
            20,
            2,
            [
                new TestReliability("Tests.High", 10, 7, 3, 0, 0.3m),
                new TestReliability("Tests.Equal", 10, 9, 1, 0, 0.1m),
                new TestReliability("Tests.ConsistentFailure", 5, 0, 5, 0, 0m)
            ]);

        var result = FlakinessGate.Evaluate(report, 0.1m);

        result.IsBreached.Should().BeTrue();
        result.Breaches.Select(test => test.Test)
            .Should().Equal("Tests.High", "Tests.Equal");
    }

    [Fact]
    public void Evaluate_NoMixedOutcomeMeetsThreshold_Passes()
    {
        var report = new ReliabilityReport(
            DateTime.UtcNow,
            10,
            1,
            [new TestReliability("Tests.Low", 10, 9, 1, 0, 0.1m)]);

        var result = FlakinessGate.Evaluate(report, 0.2m);

        result.IsBreached.Should().BeFalse();
        result.Breaches.Should().BeEmpty();
    }
}
