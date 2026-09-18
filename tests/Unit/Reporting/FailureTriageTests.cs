using System.Net;
using System.Text;
using FluentAssertions;
using TestReportGenerator;

namespace PlayTest.Unit.Reporting;

public sealed class FailureTriageTests
{
    [Theory]
    [InlineData(
        "Expected value to be 3, but found 4. Xunit.Sdk.EqualException",
        FailureCategory.AssertionFailure)]
    [InlineData(
        "System.TimeoutException: The operation timed out.",
        FailureCategory.Timeout)]
    [InlineData(
        "HttpRequestException: The remote service returned status code 503.",
        FailureCategory.ExternalDependency)]
    [InlineData(
        "Missing environment variable PLAYTEST_ENDPOINT.",
        FailureCategory.Configuration)]
    [InlineData(
        "A player with this username already exists.",
        FailureCategory.TestDataOrState)]
    [InlineData(
        "System.NullReferenceException: Object reference was null.",
        FailureCategory.ApplicationDefect)]
    public async Task AnalyzeAsync_KnownDiagnostic_ClassifiesFailure(
        string message,
        FailureCategory expectedCategory)
    {
        var analyzer = new RuleBasedFailureTriageAnalyzer();

        var result = await analyzer.AnalyzeAsync(CreateFailure(message));

        result.Category.Should().Be(expectedCategory);
        result.Summary.Should().NotBeNullOrWhiteSpace();
        result.InvestigationAreas.Should().NotBeEmpty();
        result.Confidence.Should().BeGreaterThan(0.5m);
    }

    [Fact]
    public void Prepare_SecretsAndPersonalData_RedactsBeforeExternalAnalysis()
    {
        var failure = CreateFailure(
            "password=super-secret Authorization: Bearer abc.def.ghi " +
            "owner@example.com C:\\Users\\marta\\source\\test.cs");

        var redacted = SensitiveDataRedactor.Prepare(failure);

        redacted.Should().NotContain("super-secret");
        redacted.Should().NotContain("abc.def.ghi");
        redacted.Should().NotContain("owner@example.com");
        redacted.Should().NotContain(@"\marta\");
        redacted.Should().Contain("[REDACTED]");
    }

    [Fact]
    public async Task EvaluateAsync_LabelledFixture_MeetsBaselineAccuracy()
    {
        var fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Reporting",
            "Fixtures",
            "labelled-failures.json");
        var labels = LabelledFailureReader.Read(fixture);

        var evaluation = await FailureTriageEvaluator.EvaluateAsync(
            labels,
            new RuleBasedFailureTriageAnalyzer());

        evaluation.Samples.Should().BeGreaterThanOrEqualTo(12);
        evaluation.Accuracy.Should().BeGreaterThanOrEqualTo(0.9m);
    }

    [Fact]
    public async Task AnalyzeAsync_AiProvider_UsesRedactedEvidenceAndStructuredResponse()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"category\":\"Configuration\",\"summary\":\"A required endpoint setting is missing.\",\"investigationAreas\":[\"Verify the endpoint environment variable.\"],\"confidence\":0.91}"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        var analyzer = new OpenAiFailureTriageAnalyzer(
            httpClient,
            new Uri("https://example.test/v1/chat/completions"),
            "test-model",
            "test-key");

        var result = await analyzer.AnalyzeAsync(
            CreateFailure("Missing endpoint. password=do-not-send"));

        result.Category.Should().Be(FailureCategory.Configuration);
        result.Analyzer.Should().Contain("test-model");
        requestBody.Should().NotContain("do-not-send");
        requestBody.Should().Contain("[REDACTED]");
    }

    private static FailureEvidence CreateFailure(string message)
    {
        return new FailureEvidence(
            "PlayTest.ExampleTests.FailingTest",
            "test-results.trx",
            message,
            "at PlayTest.ExampleTests.FailingTest()",
            string.Empty);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }
}
