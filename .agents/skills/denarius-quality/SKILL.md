---
name: denarius-quality
description: Validate DenariusAI code and user-visible changes with focused tests, full .NET checks, Docker builds, health checks, and browser-based regression review.
---

# DenariusAI quality checks

Use this skill when verifying implemented changes, investigating repository problems, preparing a commit or release, or reviewing a PR.

## Choose checks by impact

- Start with focused tests for the changed behavior, then run the full suite before a release or broad refactor.
- Domain, persistence, backup, authentication, audit, and financial-calculation changes require integration tests.
- MCP financial-tool changes require `DenariusAI.McpTests` and comparison with the underlying application service results.
- Visual changes require rendered browser inspection in addition to compilation.
- Docker or runtime-configuration changes require building the affected image target and checking service health.

## Standard verification

Use the pinned SDK from `global.json` and run:

```powershell
dotnet restore DenariusAI.slnx
dotnet build DenariusAI.slnx --configuration Release --no-restore
dotnet test DenariusAI.slnx --configuration Release --no-build
```

Treat warnings as failures because the repository enables `TreatWarningsAsErrors`.

For containers, build both `final` and `mcp-final` before a release when shared projects or the Dockerfile changed. For local runtime verification, preserve volumes, wait for Compose health checks, inspect recent logs, and verify `/health`.

## Browser regression review

- Exercise the state changed by the implementation: validation error, populated table, dropdown open, loading state, confirmation, empty state, or exported result as applicable.
- Compare equivalent controls across related pages for typography, dimensions, spacing, focus, hover, disabled, and responsive behavior.
- Check that searchable dropdowns are not clipped and support keyboard interaction.
- Verify user-facing Portuguese, date/number formatting, authorization, and toast behavior.
- Inspect a narrow viewport when the affected layout is responsive.

## Financial and data checks

- Test debit/credit balance, cancelled-record exclusion, period boundaries, current-budget defaults, and decimal rounding when those areas are touched.
- Compare report DTO totals with service or database results rather than accepting generated prose as evidence.
- For migrations, review generated SQL or migration operations and test application startup against the intended schema path.
- For backup/restore, verify both successful round-trip and rollback on invalid input.

## Reporting

- Report the exact commands and observable checks completed.
- Distinguish passed checks, skipped checks, and checks blocked by environment or credentials.
- Do not claim CI, security, Docker health, browser appearance, PDF layout, or deployment success without directly verifying it.
- A failed check is not complete merely because the implementation compiles; diagnose or clearly hand off the remaining failure.

