# DenariusAI project guidance

These instructions apply to every task in this repository.

## Product and language

- Treat DenariusAI as a personal and family finance application based on double-entry accounting, budgets, reconciliation, savings, analytics, and optional AI assistance.
- Write all user-facing application text in European Portuguese unless the existing surface is explicitly English.
- Preserve the principle that AI proposes or explains and the user confirms before financial records are changed.
- Never commit `.env`, credentials, API keys, database backups, or real personal financial data.

## Implementation

- Preserve the existing .NET 9, ASP.NET Core MVC, Entity Framework Core, SQL Server, and Razor architecture.
- Follow the established visual system before adding new CSS. Reuse shared controls, layouts, tokens, partials, and `wwwroot/css/buttons.css` where applicable.
- Keep create and edit forms, tables, filters, buttons, alerts, and feedback consistent across the application.
- Changes to persisted entities must include the appropriate EF Core migration and tests.
- Put configurable AI prompts in application settings so an administrator can change model behavior. Do not hide operational prompts only in source code.
- AI-generated financial reports must receive deterministic, pre-calculated financial data; do not ask the model to perform accounting arithmetic that the application can calculate.

## Verification

- Inspect existing user changes before editing and do not overwrite unrelated work.
- Build and test the solution after code changes when practical: `dotnet build DenariusAI.slnx --configuration Release` and `dotnet test DenariusAI.slnx --configuration Release --no-build`.
- For container-impacting changes, build the relevant Docker targets and verify service health.
- For visual changes, inspect the rendered page at desktop resolution and check a narrow viewport when the layout is responsive.

## Repository workflows

- Use the project skills under `.agents/skills/` for financial-domain changes, AI workflows, data lifecycle, quality checks, Git commits, UI work, Docker operations, screenshots/README work, and releases.
- A request to commit or push does not implicitly authorize creating a release; create a release only when explicitly requested.
- Before committing, summarize the intended files and exclude unrelated user changes where possible.
