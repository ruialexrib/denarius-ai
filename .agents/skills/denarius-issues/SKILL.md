---
name: denarius-issues
description: Create and refine implementation-ready GitHub issues for DenariusAI using the repository's established issue structure, current codebase evidence, explicit acceptance criteria, and required development workflow.
---

# DenariusAI GitHub issues

Use this skill whenever creating, rewriting, splitting, or materially refining a GitHub issue for DenariusAI.

The goal is to make each issue sufficiently precise that a developer or coding agent can implement it without having to reconstruct the product intent, repository workflow, or expected verification from conversation history.

## Inspect before writing

- Read the root `AGENTS.md` and the repository skills relevant to the requested change before drafting the issue.
- Inspect the current repository state and the specific code, views, services, configuration, scripts, migrations, tests, or workflows affected by the request.
- Inspect related existing implementations when the user identifies a reference area or when a comparable pattern already exists in DenariusAI.
- Check recent/open issues and Pull Requests when necessary to avoid duplicating work or describing behavior that has already changed.
- Treat conversation history as product context, not as proof of the current implementation. Prefer repository evidence whenever it is available.
- Never invent file names, routes, services, settings, labels, issue numbers, PR numbers, commands, or existing behavior.

## Title

Write the issue title in concise English and describe the intended outcome rather than the implementation mechanism.

Prefer forms such as:

```text
Keep movement filters on one responsive row
Show consolidated reminder alerts from the top navigation icon
Separate database initialization from demonstration data seeding
Fix unavailable AI settings link and authorization behavior
```

Avoid vague titles such as `Fix filters`, `Update UI`, `Improve code`, or `Changes to settings`.

## Standard issue structure

Use the following structure when the sections are relevant. Do not add empty sections merely to satisfy a template.

### `## Objective`

State the user-visible, operational, architectural, or maintenance outcome in one or two short paragraphs.

The objective must describe what should become true after implementation. It should not prescribe unnecessary implementation detail.

### `## Current behavior` or `## Current problem`

Document the verified current state that motivates the change.

- Explain what happens today and why it is insufficient.
- Include short code/configuration excerpts only when they materially clarify the cause or constraint.
- Distinguish observed behavior from inferred implementation details.
- If the issue is a new capability with no meaningful current behavior, this section may be omitted.

### `## Reference behavior`

Use this section when an existing DenariusAI page, component, workflow, service, or domain pattern is the intended reference.

State what should be reused conceptually and explicitly warn against blindly copying dimensions or implementation details that do not fit the target context.

### `## Desired behavior`

Describe the resulting behavior precisely enough to remove ambiguity while preserving implementation freedom.

Where useful, separate variants such as administrator/non-administrator, empty/populated state, desktop/mobile, first-run/subsequent-run, success/failure, or configured/unconfigured.

Include relevant constraints, for example:

- preserve existing authorization boundaries;
- preserve double-entry and deterministic financial rules;
- reuse existing visual components and services;
- keep AI provider-neutral where applicable;
- avoid changing unrelated query parameters or business rules;
- degrade gracefully when optional external metadata is unavailable.

### Domain- or implementation-specific sections

Add focused sections only when they make the issue safer or clearer, such as:

- `## Scope`
- `## Interaction requirements`
- `## Compatibility`
- `## Separation of responsibilities`
- `## Historical migration compatibility`
- `## First-installation detection`
- `## Version source`
- `## Commit source`
- `## Pull Request and issue provenance`
- `## Execution points`
- `## Safety and robustness`

These sections should capture constraints discovered from the repository or explicitly requested by the user. They must not be generic filler.

### `## Required process`

For implementation issues, include the repository workflow that the implementer must follow.

Normally require the implementer to:

1. read root `AGENTS.md`;
2. read and follow the relevant `.agents/skills/` skills by name;
3. inspect the current implementation before deciding the minimum safe change;
4. apply `denarius-development-versioning` and assign the next development version before the first repository edit;
5. work on a dedicated branch and create a Pull Request against `main`, unless the user's request or repository workflow explicitly says otherwise;
6. preserve unrelated changes and reuse existing architecture, visual patterns, and shared components.

Mention only the skills relevant to the issue. Examples include `denarius-ui`, `denarius-coding`, `denarius-finance-domain`, `denarius-ai-workflows`, `denarius-data-lifecycle`, `denarius-docker`, `denarius-git`, and `denarius-quality`.

Do not use the issue itself to authorize a release, tag, deployment, destructive migration, or merge unless the user explicitly requested that operation.

### `## Verification`

Specify observable checks tied to the requested behavior.

- Include happy-path and important edge/failure cases.
- For visual changes, require desktop and narrow/responsive checks as appropriate.
- For authorization changes, verify each relevant role/state.
- For persisted data, migrations, backup/restore, accounting, or initialization changes, require the domain/data checks defined by the relevant skills.
- For Docker/runtime changes, require the relevant image/health checks.
- Require tests to be added or updated where the behavior is testable.

For ordinary .NET code changes, include the standard commands when applicable:

```text
dotnet build DenariusAI.slnx --configuration Release
dotnet test DenariusAI.slnx --configuration Release --no-build
```

Do not demand irrelevant commands for documentation-only or metadata-only issues.

### `## Acceptance criteria`

Provide a checklist of externally verifiable outcomes.

Each checkbox must represent a result that can be judged as pass/fail. Prefer behavior and preserved invariants over file-level implementation details.

Good criteria include:

```text
- [ ] The action remains on the same row while reasonable width is available.
- [ ] Non-administrators are not shown a link to an administrator-only page.
- [ ] Missing optional GitHub metadata does not make startup fail.
- [ ] Existing acknowledgement and authorization behavior is preserved.
- [ ] Build, tests and relevant repository quality checks pass.
```

Avoid criteria such as `code updated`, `files changed`, or `looks better`.

### `## Deliverable`

State the expected completion artifact. For implementation work this will normally be a dedicated branch and Pull Request against `main`.

Require the PR description to explain the meaningful before/after behavior, important design or compatibility decisions, and verification performed when those details matter.

## Scope discipline

- Capture the user's requested outcome and the minimum adjacent work required to implement it safely.
- Do not silently expand a small issue into a broad refactor.
- Do not constrain the implementation to a specific code solution unless the repository architecture, safety requirements, or user request makes that constraint necessary.
- Separate unrelated concerns into separate issues rather than creating one omnibus issue.
- Preserve explicitly out-of-scope behavior in the issue when regression risk is significant.

## Precision and language

- Write issue titles and developer-facing issue bodies in English, consistent with the repository guidance.
- Refer to user-facing Portuguese labels exactly when they are relevant to the requested interface behavior, for example `Aplicar` or `Abrir Definições`.
- Use concrete route, class, service, script, CSS selector, or configuration names only after verifying them in the repository.
- Use Markdown headings, short paragraphs, bullets, code blocks, and checklists to keep long issues scannable.
- Explain why a constraint exists when that context affects implementation decisions.

## Safety and repository invariants

Every issue must preserve the relevant DenariusAI invariants from `AGENTS.md` and the domain skills, including where applicable:

- no credentials, API keys, `.env` content, backups, or real personal financial data;
- no weakening of authorization or audit behavior;
- financial calculations remain deterministic in application code;
- AI proposes/interprets/explains and does not autonomously commit financial changes;
- existing .NET 9 / ASP.NET Core MVC / EF Core / SQL Server architecture is preserved unless a structural change is justified;
- persisted-entity changes include migrations and appropriate tests;
- visual changes reuse the established DenariusAI design system before introducing new styles.

## Codex invocation

Creating an issue and assigning implementation work are separate actions.

- Do not append `@codex` merely because an issue is detailed enough for an agent to implement.
- Add an explicit Codex instruction only when the user asks Codex to implement the issue or otherwise explicitly delegates implementation to Codex.
- When included, keep the instruction short and require compliance with `AGENTS.md` and the repository skills.

Example:

```text
@codex please implement this issue and open the Pull Request, following `AGENTS.md` and the repository skills.
```

## Final review before creating the issue

Before submitting the GitHub issue, verify that:

- the title states a specific outcome;
- the current behavior is supported by repository inspection where applicable;
- the desired behavior is unambiguous;
- relevant constraints and preserved behavior are explicit;
- the required process names only relevant skills;
- verification covers the important states and regression risks;
- acceptance criteria are objective and testable;
- the deliverable is clear;
- no secrets or personal financial data are present;
- no implementation, merge, tag, release, or deployment authorization has been inferred beyond the user's request.
