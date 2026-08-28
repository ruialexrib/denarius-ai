# Contributing to DenariusAI

Thank you for your interest in contributing to DenariusAI.

This document defines the basic rules for proposing changes to the project. The goal is to keep contributions focused, reviewable, secure, and consistent with the existing architecture.

## Before You Start

For significant features, architectural changes, database changes, or changes that affect deployment or security, please open an issue or start a discussion before investing substantial development effort.

Small bug fixes, documentation improvements, tests, and clearly scoped maintenance changes can normally be submitted directly as a pull request.

## Development Workflow

1. Fork the repository or create a dedicated branch if you have write access.
2. Create your branch from the latest `main` branch.
3. Keep the branch focused on a single change or closely related set of changes.
4. Implement and test the change locally.
5. Push the branch and open a pull request against `main`.

Use descriptive branch names, for example:

```text
feature/budget-forecast
fix/transaction-validation
docs/update-deployment-guide
refactor/account-service
```

Do not commit directly to `main` for normal development work.

## Pull Request Rules

Pull requests should be small enough to review effectively and should have a clear purpose.

A pull request should:

- explain what was changed and why;
- describe how the change was tested;
- reference a related issue when applicable;
- avoid unrelated formatting, refactoring, or dependency changes;
- update documentation when behaviour, configuration, deployment, or public interfaces change;
- include tests when the change introduces behaviour that can reasonably be tested;
- keep existing tests passing;
- compile successfully before being submitted for review.

Large changes may be requested to be split into smaller pull requests.

## Code and Architecture

Follow the existing project structure, naming conventions, and architectural patterns.

Avoid introducing new frameworks, major dependencies, architectural layers, or infrastructure components unless they provide a clear benefit and have been discussed beforehand.

Do not combine a large refactoring with a new feature unless the refactoring is necessary to implement that feature.

## Database Changes

Database changes must be explicit and reviewable.

When a contribution changes the database schema or persistence behaviour:

- include the required migration or schema change;
- explain the impact in the pull request;
- avoid destructive migrations unless clearly justified;
- preserve existing data whenever reasonably possible;
- document any manual deployment or migration step that may be required.

## Configuration

Never commit secrets or environment-specific credentials.

This includes, but is not limited to:

- passwords;
- API keys;
- OAuth client secrets;
- access tokens;
- private keys;
- production connection strings;
- personal financial information.

If a new environment variable or configuration value is required, update `.env.example` and the relevant documentation using safe placeholder values.

## Privacy and Financial Data

Denarius AI deals with personal-finance functionality. Contributions must not contain real personal financial data, bank statements, account identifiers, authentication data, or other private information.

Use synthetic or anonymised data in tests, examples, screenshots, fixtures, and documentation.

## AI and LLM Integrations

Changes involving AI providers, prompts, tool calling, MCP, or LLM integrations should:

- keep provider-specific configuration externalised where practical;
- avoid hard-coding API credentials;
- document new configuration requirements;
- handle model output as untrusted input where appropriate;
- avoid sending financial or personal data to external services without an explicit and documented reason.

## Dependencies

New dependencies should be necessary, actively maintained, and appropriate for the project.

Avoid adding a dependency when the same result can reasonably be achieved using the existing stack or platform libraries.

Dependency upgrades should preferably be submitted separately from unrelated feature changes.

## Commit Messages

Use concise commit messages that describe the change clearly.

Examples:

```text
feat: add monthly budget comparison
fix: validate duplicate transactions
docs: update Docker deployment guide
test: add transaction service tests
refactor: simplify account balance calculation
```

## Review and Merge

Submitting a pull request does not guarantee that it will be merged.

A pull request may require changes before approval. Review may consider correctness, security, maintainability, architecture, test coverage, documentation, and consistency with the direction of the project.

Pull requests should only be merged after review and after required automated checks, when configured, have passed.

## License

By submitting a contribution, you agree that your contribution will be licensed under the same license as the project.
