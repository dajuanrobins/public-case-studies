# Enterprise Code Intelligence CLI

> A compact .NET case study for extracting architectural metadata from enterprise application code and generating structured documentation.

This repository demonstrates how a developer-tooling system can convert scattered application code into an inspectable inventory of APIs, Azure Functions, service classes, routes, parameters, return types, and documentation artifacts.

The implementation is intentionally small enough to review quickly, but the design is written as a production-grade pattern: separate scanning, modeling, exporting, and command orchestration.

## Executive summary

Large enterprise codebases often fail not because the code is impossible to change, but because the system has become difficult to understand. APIs, background jobs, functions, data-access pathways, and service responsibilities spread across projects over years.

This CLI treats source code as a discoverable system of record. It scans a solution, extracts stable metadata, and produces documentation that can support onboarding, audits, refactoring, modernization planning, and operational readiness.

## What this demonstrates

| Area | Demonstrated capability |
|---|---|
| Architecture | Separation between CLI orchestration, analysis core, and exporters |
| Enterprise engineering | Practical tooling for large .NET systems |
| Code analysis | Lightweight source scanning with extensible metadata models |
| Documentation | JSON, Markdown, CSV, and HTML outputs |
| Maintainability | Testable services and deterministic output examples |

## Repository structure

```text
src/
  EnterpriseCodeIntel.Cli/        # Command-line entrypoint
  EnterpriseCodeIntel.Core/       # Domain model and source scanner
  EnterpriseCodeIntel.Exporters/  # JSON, Markdown, CSV, HTML exporters
samples/
  SampleEnterpriseApp/            # Small example app used by the scanner
output-examples/                  # Generated sample artifacts
docs/
  architecture.md
  case-study.html
  research-brief.md
tests/
  EnterpriseCodeIntel.Tests/
```

## Run

```bash
dotnet restore
dotnet test
dotnet run --project src/EnterpriseCodeIntel.Cli -- scan ./samples/SampleEnterpriseApp --out ./output
```

## Design principles

1. **Prefer inspectability over magic.** The tool emits simple artifacts humans can review.
2. **Make metadata portable.** JSON output becomes the canonical format; Markdown, HTML, and CSV are projections.
3. **Separate analysis from presentation.** Exporters should not know how scanning works.
4. **Optimize for enterprise reality.** Partial scans, legacy code, and inconsistent patterns should degrade gracefully.

## Public-safety note

This is a sanitized technical showcase. It contains no proprietary product code, customer data, private employer code, or confidential architecture.
