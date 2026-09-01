# DenariusAI Roadmap

This roadmap captures the current product direction for DenariusAI. Priorities may change as the application evolves and feedback is incorporated.

## Current foundation

Denarius AI already provides the core foundation for personal and family finance management, including double-entry accounting, budgeting, transaction management, reconciliation, financial analytics, reporting, savings management, stock portfolio tracking, market watchlists, data backup and restore, and optional AI-assisted interpretation.

## Next priorities

### 1. Insurance portfolio

Introduce a dedicated insurance portfolio so households can register and monitor their active insurance policies alongside the rest of their financial information.

Planned scope:

- Create, edit, archive, and cancel insurance policies.
- Support common insurance categories such as home, motor, health, life, personal accident, and other policies.
- Record the insurer, policy number, insured object or person, coverage period, premium, payment frequency, and renewal date.
- Track policy status and upcoming renewals or expirations.
- Keep useful policy references and notes without storing unnecessary sensitive information.
- Provide a consolidated view of annual and periodic insurance costs.
- Allow insurance expenses to be associated with the existing financial model where appropriate, without duplicating accounting records.
- Surface renewal and expiry reminders while keeping policy changes under user control.
- Allow AI to explain the portfolio or highlight relevant information, while deterministic totals and dates remain calculated by the application.

### 2. Savings goals

Introduce dedicated savings goals so households can define and track concrete financial objectives such as an emergency fund, holiday, vehicle, home deposit, education, or other planned expenditure.

Planned scope:

- Create, edit, archive, and complete savings goals.
- Define a target amount and optional target date.
- Associate contributions with a goal while preserving the accounting model as the source of truth.
- Show the accumulated amount, remaining amount, and completion percentage.
- Present contribution history and progress over time.
- Estimate the likely completion date from the observed saving rate when sufficient data exists.
- Keep all financial calculations deterministic in the application; AI may explain progress or suggest actions but must not calculate or post financial records autonomously.

### 3. Household cash-flow forecasting

Provide a forward-looking view of expected household liquidity based on known recurring income and expenses, budgets, scheduled commitments, and historical patterns. Forecast calculations should remain deterministic, with AI limited to interpretation and scenario explanation.

### 4. Bank statement import improvements

Improve the import workflow for bank statements, including broader format support, clearer validation, duplicate detection, preview before confirmation, and stronger assistance with transaction classification and reconciliation.

## Later opportunities

- Insurance renewal insights and alerts.
- Savings-goal insights and alerts.
- Scenario analysis for changes in income, expenses, insurance costs, and saving rates.
- Improved household financial health indicators.
- More contextual AI explanations across budgets, reconciliation, savings, investments, insurance, and analytics.
- Additional integrations where they can be introduced without compromising user control, privacy, or accounting integrity.

## Product principles

- Financial records remain under user control.
- Double-entry accounting remains the authoritative financial model.
- Deterministic calculations are performed by the application, not delegated to an LLM.
- AI proposes, interprets, and explains; it does not silently change financial records.
- Privacy, data portability, and recoverability remain core requirements.
