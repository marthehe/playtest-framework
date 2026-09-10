# PlayTest: A .NET Quality Engineering Framework for Distributed APIs

PlayTest is an experimental test automation framework for validating distributed APIs and SDK-style services. It explores layered testing, deterministic CI quality gates, test reliability metrics and AI-assisted failure triage.

## Architecture

```
PlayTest/
|
+-- src/
|   +-- PlayTest.Core/          # Reusable test infrastructure
|   |   +-- Configuration/      # Test environment settings
|   |   +-- Fixtures/           # Shared test fixtures
|   |   +-- Assertions/         # Custom domain assertions
|   |   +-- TestData/           # Builder pattern test data
|   |   +-- Clients/            # HTTP/API client abstractions
|   |   +-- Reporting/          # Test result reporting
|   |
|   +-- Demo.PlayPlatform/      # Fictional digital play platform
|       +-- Players/            # Player profiles and state management
|       +-- Sessions/           # Game session lifecycle
|       +-- Achievements/       # Achievement tracking and unlocking
|
+-- tests/
|   +-- Unit/                   # Isolated business logic tests
|   +-- Integration/            # API and database integration tests
|   +-- Contract/               # API schema/contract validation
|   +-- EndToEnd/               # Full player journey tests
|   +-- Performance/            # Load and reliability testing
|
+-- tools/
|   +-- TestReportGenerator/    # Structured test reporting and flaky-test detection
|
+-- .github/workflows/
    +-- quality.yml             # CI quality gate pipeline
```

## Test Pyramid

```
          /  E2E  \              Few, high-value journey tests
         /----------\
        / Contract   \           API schema validation
       /--------------\
      /  Integration   \         Service boundary tests
     /------------------\
    /      Unit          \       Fast, isolated logic tests
    ----------------------
```

Each layer catches different failure categories:
- **Unit** - logic errors, state transitions, input validation
- **Integration** - API contract drift, serialisation issues, dependency failures
- **Contract** - breaking schema changes between services
- **E2E** - user journey regressions across the full stack
- **Performance** - latency regressions, throughput degradation

## Test Data Builders

The framework uses the Builder pattern for readable, maintainable test data:

```csharp
var player = new PlayerBuilder()
    .WithUsername("testplayer")
    .Suspended()
    .Build();

var session = new SessionBuilder()
    .WithPlayerId(player.Id)
    .WithGameTitle("Halo Infinite")
    .Completed()
    .Build();
```

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/Unit

# With coverage
dotnet test tests/Unit --collect:"XPlat Code Coverage"
```

## CI Quality Gate

Every PR runs the full pipeline via GitHub Actions:
1. Build (Release configuration)
2. Unit Tests
3. Integration Tests
4. Contract Tests
5. Test result upload

## Roadmap

### V1 (Current) - Framework Foundation
- [x] Domain model (Players, Sessions, Achievements)
- [x] Test data builders
- [x] Custom assertions
- [x] Unit tests with xUnit + NSubstitute + FluentAssertions
- [x] GitHub Actions CI pipeline
- [ ] Integration test infrastructure
- [ ] Configuration management

### V2 - Quality Engineering
- [ ] Contract testing
- [ ] E2E player journey scenarios
- [ ] Coverage gates (target: 80%)
- [ ] Test reporting
- [ ] Flaky-test detection and reliability metrics

### V3 - AI-Assisted Quality
- [ ] LLM-based failure classification
- [ ] Test run summaries
- [ ] Root cause suggestions

## Engineering Trade-offs

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Test framework | xUnit | Industry standard, good parallelisation, extensible |
| Mocking | NSubstitute | Clean syntax, less ceremony than Moq |
| Assertions | FluentAssertions | Readable failure messages, fluent chaining |
| Test data | Builder pattern | Readable defaults, explicit overrides, immutable objects |
| AI in quality gates | Deterministic only | LLMs assist triage, not pass/fail decisions |

## License

MIT
