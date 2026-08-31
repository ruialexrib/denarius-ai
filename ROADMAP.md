# Denarius AI Roadmap

This roadmap captures the current product direction for Denarius AI. Priorities may change as the application evolves and feedback is incorporated.

## Current foundation

Denarius AI already provides the core foundation for personal and family finance management, including double-entry accounting, budgeting, transaction management, reconciliation, financial analytics, reporting, data backup and restore, and optional AI-assisted interpretation.

## Next priorities

### 1. Savings goals

Introduce dedicated savings goals so households can define and track concrete financial objectives such as an emergency fund, holiday, vehicle, home deposit, education, or other planned expenditure.

Planned scope:

- Create, edit, archive, and complete savings goals.
- Define a target amount and optional target date.
- Associate contributions with a goal while preserving the accounting model as the source of truth.
- Show the accumulated amount, remaining amount, and completion percentage.
- Present contribution history and progress over time.
- Estimate the likely completion date from the observed saving rate when sufficient data exists.
- Keep all financial calculations deterministic in the application; AI may explain progress or suggest actions but must not calculate or post financial records autonomously.

### 2. Household cash-flow forecasting

Provide a forward-looking view of expected household liquidity based on known recurring income and expenses, budgets, scheduled commitments, and historical patterns. Forecast calculations should remain deterministic, with AI limited to interpretation and scenario explanation.

### 3. Bank statement import improvements

Improve the import workflow for bank statements, including broader format support, clearer validation, duplicate detection, preview before confirmation, and stronger assistance with transaction classification and reconciliation.

## Later opportunities

- Savings-goal insights and alerts.
- Scenario analysis for changes in income, expenses, and saving rates.
- Improved household financial health indicators.
- More contextual AI explanations across budgets, reconciliation, savings, and analytics.
- Additional integrations where they can be introduced without compromising user control, privacy, or accounting integrity.

## Product principles

- Financial records remain under user control.
- Double-entry accounting remains the authoritative financial model.
- Deterministic calculations are performed by the application, not delegated to an LLM.
- AI proposes, interprets, and explains; it does not silently change financial records.
- Privacy, data portability, and recoverability remain core requirements.
