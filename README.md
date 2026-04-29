# Public Case Studies

This repository is where I publish technical case studies drawn from real engineering work. Each case study is an independently runnable project that demonstrates a specific architectural pattern, system design decision, or engineering practice at scale.

The goal is to share concrete, working implementations — not slides or theoretical writeups — so that the reasoning, trade-offs, and execution are all visible in the code.

## Case Studies

| Project | Domain | Stack |
|---|---|---|
| [distributed-audit-observability-pipeline](case_study/distributed-audit-observability-pipeline/) | Audit event ingestion, normalization, risk scoring, and redaction for regulated environments | Rust |
| [enterprise-code-intelligence-cli](case_study/enterprise-code-intelligence-cli/) | Static analysis CLI that inventories API endpoints and Azure Functions across large C# codebases | .NET 8, C# |

## Structure

Each case study lives under `case_study/` and includes:

- **Source code** — a working, tested implementation
- **Documentation** — architecture overview, operational model, and a narrative case study
- **Examples** — sample inputs and outputs to illustrate the system in practice

## Running a Case Study

Each project has its own README with build and run instructions. In general:

- **Rust projects**: `cargo build` / `cargo test`
- **.NET projects**: `dotnet build` / `dotnet test`

## Intent

These are published as professional artifacts — they are held to production-grade standards for correctness, security, and documentation. They are not tutorials or starter templates.
