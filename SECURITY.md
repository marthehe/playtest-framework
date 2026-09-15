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

## Dependency Policy

- Only well-established, actively maintained packages from verified publishers
- No pre-release or unverified packages
- Regular `dotnet list package --vulnerable` checks
- New packages must provide clear value that cannot reasonably be achieved with the .NET standard library
