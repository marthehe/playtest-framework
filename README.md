# PlayTest

PlayTest is an experimental .NET quality engineering framework for testing distributed APIs and
SDK-style services. It demonstrates layered automated testing, deterministic quality gates,
reliability metrics, secure CI configuration, and reusable test infrastructure.

The repository includes a fictional digital play platform with player profiles, game sessions,
and achievements. This domain exists only to provide realistic behaviours and failure cases for
the testing framework.

> This is a personal portfolio project. It does not contain Microsoft or Xbox source code, data,
> credentials, internal architecture, or other workplace material. It is not affiliated with or
> endorsed by Microsoft.

## Project goals

The project is designed to answer practical quality engineering questions:

- How should unit, integration, contract, and end-to-end tests divide responsibility?
- How can test data remain readable without duplicating setup in every test?
- How can continuous integration enforce measurable and deterministic quality standards?
- How should flaky tests be distinguished from tests that consistently expose a defect?
- How can an API return useful status codes without leaking internal exception details?
- How can dependency and workflow supply-chain risk be reduced in a public repository?

The goal is not to maximise the number of tests. The goal is to demonstrate why each test layer
exists, what risk it addresses, and what trade-offs were made.

## Current status

V1 and V2 are implemented.

| Capability | Status |
| --- | --- |
| Domain model and services | Complete |
| Reusable test data builders | Complete |
| Unit tests | Complete |
| ASP.NET Core API test host | Complete |
| Integration tests | Complete |
| Deterministic contract checks | Complete |
| End-to-end player journeys | Complete |
| 80% line coverage quality gate | Complete |
| TRX reliability reporting | Complete |
| Human-readable test health summary | Complete |
| GitHub Actions pipeline hardening | Complete |
| Performance scenarios | Planned |
| Cross-run CI history collection | Planned |
| AI-assisted failure triage | Complete |

The current suite contains:

- 38 unit test cases
- 3 integration tests
- 2 contract tests
- 2 end-to-end tests
- 92.62% line coverage for the domain assembly
- 81.81% branch coverage for the domain assembly

## Architecture

```text
PlayTest/
|
+-- src/
|   +-- Demo.PlayPlatform/
|   |   +-- Players/
|   |   +-- Sessions/
|   |   +-- Achievements/
|   |   +-- Exceptions/
|   |
|   +-- Demo.PlayPlatform.Api/
|   |   +-- Repositories/
|   |   +-- Program.cs
|   |
|   +-- PlayTest.Core/
|       +-- Assertions/
|       +-- Configuration/
|       +-- TestData/
|
+-- tests/
|   +-- Unit/
|   +-- Integration/
|   +-- Contract/
|   +-- EndToEnd/
|   +-- Performance/
|   +-- PlayTest.TestInfrastructure/
|
+-- tools/
|   +-- TestReportGenerator/
|
+-- .github/
    +-- workflows/
    +-- dependabot.yml
```

### Domain layer

`Demo.PlayPlatform` contains the application-independent domain logic:

- `PlayerService` manages player creation, suspension, and reactivation.
- `SessionService` manages session creation and completion.
- `AchievementService` manages achievement eligibility and duplicate prevention.
- Repository interfaces keep business rules independent from data storage.
- Typed domain exceptions allow API boundaries to map failures consistently.

The domain intentionally includes rules that create meaningful test scenarios:

- Usernames must be unique.
- Only active players can start sessions or unlock achievements.
- A player can have no more than three concurrent sessions.
- Completed sessions cannot be completed again.
- An achievement can only be unlocked once by the same player.
- Banned players cannot be suspended or reactivated.

### API layer

`Demo.PlayPlatform.Api` exposes the domain through minimal ASP.NET Core endpoints.

The API uses thread-safe in-memory repository adapters. This keeps tests isolated and repeatable
without requiring a database, container, cloud account, or external service.

The in-memory implementation is a test adapter, not a production persistence design.

### Test infrastructure

`PlayTest.TestInfrastructure` provides a reusable `WebApplicationFactory<Program>`. Integration,
contract, and end-to-end tests use the same in-process HTTP host rather than starting an external
server.

This provides:

- Real HTTP serialisation and routing
- Real dependency injection configuration
- Isolated process-local state
- Fast execution
- No network dependency
- No test credentials or secrets

## Testing strategy

```text
              / End-to-end \
             /--------------\
            /    Contract    \
           /------------------\
          /    Integration     \
         /----------------------\
        /          Unit          \
       ----------------------------
```

The layers are complementary. A higher layer does not replace the lower layers.

| Layer | Primary responsibility | Example risk detected |
| --- | --- | --- |
| Unit | Business rules in isolation | Invalid state transition |
| Integration | HTTP and application wiring | Incorrect route or persistence behaviour |
| Contract | Published response shape | Renamed or missing JSON property |
| End-to-end | Complete user journey | Failure across several valid operations |
| Performance | Latency and stability under load | Planned for a later version |

### Unit tests

Unit tests use xUnit, NSubstitute, and FluentAssertions. Repository dependencies are substituted so
tests focus on domain decisions rather than infrastructure.

Examples include:

- Rejecting duplicate usernames
- Preventing banned account transitions
- Enforcing the concurrent session limit
- Rejecting duplicate achievement unlocks
- Distinguishing not-found errors from domain rule violations
- Calculating test reliability correctly

### Integration tests

Integration tests send HTTP requests through the in-process ASP.NET Core host.

They verify:

- A created player can be retrieved through a later request.
- Duplicate usernames produce HTTP `409 Conflict`.
- Missing resources produce HTTP `404 Not Found`.
- Error responses do not expose usernames, exception messages, or implementation details.

### Contract tests

Contract checks use `System.Text.Json` from the .NET standard library. No additional contract
testing package is required at this stage.

The tests validate:

- Required JSON property names
- Property value types
- Identifier and timestamp formats
- HTTP status codes
- RFC-style Problem Details response structure
- Absence of sensitive exception details

These are deterministic schema checks. They are not currently provider-consumer Pact tests.
Introducing Pact would only be justified when the project models independently deployed consumers
and providers.

### End-to-end tests

End-to-end tests cover complete user journeys through HTTP.

The main successful journey:

1. Creates a player.
2. Starts a game session.
3. Completes the session.
4. Unlocks the first-session achievement.
5. Verifies identifiers and lifecycle states across responses.

A negative journey verifies that a suspended player cannot start a session.

## Test data builders

PlayTest uses the Builder pattern to provide valid defaults with explicit overrides.

```csharp
var player = new PlayerBuilder()
    .WithUsername("testplayer")
    .Suspended()
    .Build();

var session = new SessionBuilder()
    .WithPlayerId(player.Id)
    .WithGameTitle("PlayTest Adventure")
    .Completed()
    .Build();
```

This approach keeps individual tests focused on the behaviour that matters. It also reduces the
maintenance cost when domain records gain new properties.

Builders should not hide the condition under test. Tests still override every value that affects
the expected outcome.

## Quality gate

The GitHub Actions workflow runs on pull requests targeting `main` and pushes to `main`.

```text
Restore
  |
Build
  |
Unit tests and coverage threshold
  |
Integration tests
  |
Contract tests
  |
End-to-end tests
  |
Reliability report
  |
Advisory failure triage
  |
Test artifact upload
```

The unit test stage fails when total domain line coverage falls below 80%.

Coverage is used as a regression guard, not as proof of correctness. A test suite can execute a
line without verifying the right behaviour, so branch selection, assertions, and boundary cases
remain more important than pursuing 100% coverage.

## Reliability and flaky-test detection

`TestReportGenerator` reads one TRX file or recursively reads a directory of TRX files. Results are
grouped by fully qualified test identity.

A test is considered potentially flaky only when the supplied history contains both passing and
failing outcomes.

```text
flakiness score = min(passed outcomes, failed outcomes) / total runs
```

This definition prevents a consistently failing test from being mislabeled as flaky. A test that
fails every time has a score of zero because it is a reproducible defect or invalid test.

Example output:

```json
{
  "test": "PlayTest.ExampleTests.CanCreatePlayer",
  "runs": 10,
  "passed": 8,
  "failed": 2,
  "skipped": 0,
  "flakinessScore": 0.2,
  "isPotentiallyFlaky": true
}
```

The current CI workflow reports on the TRX files produced by one workflow execution. The tool
supports multiple historical files, but automatic retrieval of artifacts from earlier workflow
runs is not implemented yet. Therefore, the CI report is currently a foundation for cross-run
analysis rather than a complete historical monitoring system.

### Human-readable test health summary

`TestReportGenerator` can combine the reliability and advisory triage results into a Markdown
summary. It reports the overall status, execution counts, potentially flaky tests, failed-test
triage, and labelled-classification accuracy when those inputs are available.

The quality workflow stores this as `TestResults/test-health-summary.md`, uploads it with the other
test artifacts, and publishes the same content to the GitHub Actions job summary. The Markdown
report is informational; deterministic test and coverage commands continue to decide whether the
workflow succeeds.

## AI-assisted failure triage

V3 extends `TestReportGenerator` with advisory analysis of failed TRX results. It extracts the test
identity, error message, stack trace, and captured standard output, then produces:

- A likely failure category
- A concise evidence-based summary
- Up to three investigation areas
- A confidence score and analyzer identity

The default `rule-based-v1` analyzer works offline and classifies broad failure types:

- Assertion failure
- Timeout
- External dependency
- Configuration
- Test data or shared state
- Application defect
- Unknown

This analyzer is intentionally transparent and limited. It provides a deterministic baseline,
not a claim that keyword matching can diagnose every failure.

An optional OpenAI-compatible analyzer can be enabled explicitly. It uses temperature zero,
requests structured JSON, constrains categories to the same fixed taxonomy, and rejects malformed
responses. Common credentials and personal identifiers are redacted before transmission, and the
diagnostic payload is bounded. Test output is still potentially sensitive, so external analysis
must only be enabled when the configured provider and data-handling policy are appropriate.

AI never controls the build. Test outcomes, coverage thresholds, and workflow status remain
deterministic. If explicitly requested AI analysis fails, the command reports an error rather than
silently substituting a success-shaped result.

### Labelled evaluation

The repository contains 12 labelled failure examples covering the six known categories. Unit tests
evaluate the offline analyzer against this dataset and enforce at least 90% classification
accuracy. This small synthetic dataset detects obvious regression but does not establish
production-level generalisation.

## Security approach

Security decisions are intentionally visible in the repository.

### Dependency controls

- Package versions are pinned.
- Floating and pre-release versions are not used.
- Dependencies are limited to established Microsoft and .NET testing packages.
- Contract analysis and report generation use .NET standard libraries where practical.
- Dependabot monitors NuGet and GitHub Actions dependencies.
- `dotnet list package --vulnerable --include-transitive` is used during audits.
- The current dependency audit reports no known vulnerable packages.

### CI controls

- Workflow permissions are explicitly limited to `contents: read`.
- Checkout credentials are not persisted.
- Third-party actions are pinned to immutable commit SHAs.
- Jobs use a timeout to prevent uncontrolled execution.
- Test results are uploaded even when a test stage fails.

### API error handling

The API maps typed domain exceptions to stable HTTP status codes. Public Problem Details responses
intentionally exclude exception messages and stack traces.

| Failure | HTTP status |
| --- | --- |
| Invalid request | `400 Bad Request` |
| Resource not found | `404 Not Found` |
| Duplicate resource | `409 Conflict` |
| Domain rule violation | `422 Unprocessable Entity` |
| Unexpected failure | `500 Internal Server Error` |

The API is a local demonstration host and does not currently implement authentication or
authorisation. It must not be treated as an internet-facing production service.

Security vulnerabilities should be reported privately through the repository security advisory
form. See [SECURITY.md](SECURITY.md).

## Getting started

### Prerequisites

- .NET 8 SDK
- Git

No database, container runtime, cloud subscription, API key, or secret is required for the
framework or default offline triage.

### Clone and restore

```bash
git clone https://github.com/marthehe/playtest-framework.git
cd playtest-framework
dotnet restore
```

### Build

```bash
dotnet build --configuration Release
```

### Run all tests

```bash
dotnet test --configuration Release
```

### Run one test layer

```bash
dotnet test tests/Unit --configuration Release
dotnet test tests/Integration --configuration Release
dotnet test tests/Contract --configuration Release
dotnet test tests/EndToEnd --configuration Release
```

### Enforce the coverage threshold locally

```bash
dotnet test tests/Unit \
  --configuration Release \
  /p:CollectCoverage=true \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total \
  /p:Exclude="[PlayTest.Core]*%2c[TestReportGenerator]*"
```

### Generate a reliability report

```bash
dotnet test \
  --configuration Release \
  --logger trx \
  --results-directory TestResults

dotnet run \
  --project tools/TestReportGenerator \
  -- TestResults \
  --output TestResults/test-reliability-report.json
```

### Generate an advisory failure-triage report

```bash
dotnet run \
  --project tools/TestReportGenerator \
  -- TestResults \
  --output TestResults/test-reliability-report.json \
  --triage-output TestResults/test-failure-triage.json \
  --summary-output TestResults/test-health-summary.md \
  --labels tests/Unit/Reporting/Fixtures/labelled-failures.json
```

### Use optional OpenAI-compatible analysis

Configure a full chat-completions endpoint, model, and key in environment variables. Do not place
the key in shell history or repository files.

```bash
export PLAYTEST_AI_ENDPOINT="https://provider.example/v1/chat/completions"
export PLAYTEST_AI_MODEL="model-name"
export PLAYTEST_AI_API_KEY="secret"

dotnet run \
  --project tools/TestReportGenerator \
  -- TestResults \
  --triage-output TestResults/test-failure-triage.json \
  --ai
```

See [SECURITY.md](SECURITY.md) before sending test diagnostics to an external service.

## API routes

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Check host availability |
| `POST` | `/api/players` | Create a player |
| `GET` | `/api/players/{id}` | Retrieve a player |
| `POST` | `/api/players/{id}/suspension` | Suspend a player |
| `POST` | `/api/players/{id}/sessions` | Start a game session |
| `POST` | `/api/sessions/{id}/completion` | Complete a session |
| `POST` | `/api/players/{id}/achievements` | Unlock an achievement |
| `GET` | `/api/players/{id}/achievements` | List unlocked achievements |

Example player request:

```json
{
  "username": "testplayer",
  "displayName": "Test Player"
}
```

## Engineering decisions and trade-offs

| Decision | Choice | Reason | Limitation |
| --- | --- | --- | --- |
| Test runner | xUnit | Mature .NET ecosystem and strong tooling support | Framework-specific attributes |
| Mocking | NSubstitute | Readable substitute configuration | Excessive mocking can couple tests to implementation |
| Assertions | FluentAssertions | Clear intent and diagnostic output | Additional test dependency |
| Test data | Builder pattern | Valid defaults with focused overrides | Builders can hide important values if misused |
| API hosting | `WebApplicationFactory` | Real HTTP stack without external deployment | Does not reproduce every production hosting concern |
| Persistence | Concurrent in-memory repositories | Fast, isolated and deterministic | Does not model database constraints or transactions |
| Contract checks | `System.Text.Json` | No extra package and explicit assertions | Not a full consumer-driven contract platform |
| Coverage | 80% line threshold | Detects significant coverage regression | Coverage does not prove assertion quality |
| Flakiness | Mixed-outcome calculation | Separates instability from consistent failure | Requires multiple historical runs for strong evidence |
| AI decisions | Advisory only | Keeps pass and fail decisions reproducible | Output still requires human verification |

## Known limitations

- Persistence is process-local and resets when the test host is recreated.
- Concurrent repository operations are thread-safe but do not model database transactions.
- Authentication, authorisation, rate limiting, and production secrets are not implemented.
- Contract tests validate response shape but not compatibility between independently deployed
  services.
- CI reliability reports currently use results from one workflow run.
- Failure categories are broad and cannot identify every root cause.
- The labelled triage dataset is synthetic and intentionally small.
- Optional external analysis depends on provider availability and data-handling suitability.
- The performance project is scaffolded but does not yet contain load scenarios.
- The API is designed for automated test demonstrations, not production deployment.

These limitations are documented intentionally. Future work should address them only when it adds a
clear learning or quality-engineering outcome.

## Roadmap

### V1 - Framework foundation

- [x] Player, session, and achievement domain
- [x] Repository abstractions
- [x] Reusable test data builders
- [x] Custom domain assertions
- [x] Unit test suite
- [x] Initial CI pipeline

### V2 - Quality engineering

- [x] In-process ASP.NET Core API host
- [x] Integration testing
- [x] Deterministic contract validation
- [x] End-to-end player journeys
- [x] 80% coverage quality gate
- [x] Structured reliability report
- [x] Flaky-test metric
- [x] CI and dependency security hardening

### V2.1 - Reliability and performance

- [ ] Retrieve historical TRX artifacts across CI runs
- [ ] Add a configurable flakiness quality threshold
- [ ] Add deterministic latency and throughput scenarios
- [x] Publish a human-readable test health summary
- [ ] Add repository implementations that model persistence boundaries

### V3 - AI-assisted quality

- [x] Classify failed-test output into likely failure categories
- [x] Produce concise failure summaries
- [x] Suggest investigation areas without changing pass or fail results
- [x] Evaluate classification accuracy against labelled failures
- [x] Document privacy, prompt-injection, and hallucination risks

AI will assist diagnosis only. Deterministic tests and explicit thresholds will continue to control
quality-gate outcomes.

## Contributing

This is currently a personal learning project, but issues and pull requests are welcome.

Before submitting a change:

1. Keep changes focused and use conventional commit messages.
2. Add tests for new behaviour.
3. Run formatting, the full test suite, and the dependency vulnerability audit.
4. Do not commit credentials, tokens, production data, or proprietary source material.
5. Explain new dependencies and why the standard library is insufficient.

## License

Licensed under the [MIT License](LICENSE).
