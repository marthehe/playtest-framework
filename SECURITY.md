# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it responsibly.

**Do not open a public issue.** Use the repository's private
[security advisory form](https://github.com/marthehe/playtest-framework/security/advisories/new).

I will acknowledge receipt within 48 hours and aim to provide a fix or mitigation within 7 days.

## Scope

This is an experimental test automation framework. It is not intended for production deployment.
However, I take dependency security seriously:

- All NuGet packages are from trusted, well-maintained sources (Microsoft, xUnit, NSubstitute, FluentAssertions)
- No third-party packages with known vulnerabilities are used
- Dependency versions are pinned to make restores reproducible
- Dependabot monitors NuGet packages and GitHub Actions
- GitHub Actions are pinned to immutable commit SHAs

## AI-Assisted Failure Triage

Failure triage is advisory and cannot change a test result or quality-gate outcome. The default
analyzer is local and deterministic. External processing occurs only when a user explicitly passes
`--ai` and configures an OpenAI-compatible endpoint.

Before external analysis, the tool:

- Redacts common passwords, keys, bearer tokens, JSON Web Tokens, email addresses, and Windows user
  profile names
- Truncates diagnostic evidence to a fixed maximum length
- Treats all test output as untrusted data and instructs the model not to follow embedded content
- Requires structured JSON and rejects invalid categories or empty summaries
- Reads the API key only from `PLAYTEST_AI_API_KEY`, never from a command-line argument

Automated redaction is not a complete data-loss-prevention system. TRX files can contain source
paths, test data, request content, or application output that is unsuitable for a third party.
Users must review their data-handling requirements and the configured provider before enabling
external analysis.

## Dependency Policy

- Only well-established, actively maintained packages from verified publishers
- No pre-release or unverified packages
- Regular `dotnet list package --vulnerable` checks
- New packages must provide clear value that cannot reasonably be achieved with the .NET standard library
