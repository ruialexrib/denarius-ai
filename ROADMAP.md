# DenariusAI Roadmap

This roadmap captures the current product direction for DenariusAI. Priorities may change as the application evolves and feedback is incorporated.

## Current foundation

Denarius AI already provides the core foundation for personal and family finance management, including double-entry accounting, budgeting, transaction management, reconciliation, financial analytics, reporting, savings management, stock portfolio tracking, market watchlists, data backup and restore, and optional AI-assisted interpretation.

## Next priorities

### 1. Insurance portfolio

Introduce a dedicated insurance portfolio so households can register and monitor their insurance policies, premiums, payments, and supporting documents alongside the rest of their financial information.

#### Policy management

- Create, edit, archive, and cancel insurance policies.
- Support common insurance categories such as home, motor, health, life, personal accident, and other policies.
- Record the insurer, policy number, insured object or person, coverage period, payment frequency, renewal date, useful references, and notes.
- Track policy status and upcoming renewals or expirations without storing unnecessary sensitive information.

#### Premium management

- Treat each insurance premium as a separate record belonging to a policy, preserving the history of premiums over time.
- Record the premium amount, period covered, due date, payment date, payment status, and optional reference.
- Support recurring premiums according to the policy payment frequency without assuming that a scheduled premium has been paid.
- Provide consolidated periodic and annual insurance costs calculated deterministically by the application.

#### Financial movement association

- Allow a premium to be associated with the existing financial movement that represents its payment.
- Preserve the accounting movement as the financial source of truth and avoid creating duplicate accounting records when a matching payment already exists.
- Allow the user to select an existing eligible movement when associating a premium payment.
- When no movement exists, provide an explicit workflow to register the premium payment through the normal Denarius AI movement/accounting mechanism.
- Allow imported or reconciled bank movements to be proposed as possible premium matches using amount, date, reference, and other available context.
- Require user confirmation before creating or changing the association between a premium and a financial movement.
- Keep AI or automated matching limited to suggestions; it must never silently post, reconcile, or alter financial records.

#### Premium attachments

- Allow one or more attachments to be stored against each premium, such as payment notices, receipts, invoices, or other insurer documents.
- Keep attachments associated with the premium rather than duplicating the same document on the financial movement.
- Make premium attachments accessible from the associated movement so the supporting documentation can be consulted from the financial workflow.
- Reuse the application's existing attachment storage, validation, upload controls, and security rules wherever possible.

#### Portfolio experience

- Present active policies, upcoming renewals, premiums awaiting payment, and recent payments in a consolidated insurance portfolio.
- Provide policy detail pages with premium history, associated movements, and attachments.
- Surface renewal and expiry reminders while keeping policy changes under user control.
- Allow AI to explain the portfolio, highlight relevant information, or suggest possible movement matches, while totals, dates, statuses, and accounting calculations remain deterministic.

#### Implementation requirements

- Introduce the required persisted entities and relationships with EF Core migrations and corresponding tests.
- Preserve the existing double-entry accounting model rather than introducing a parallel payment ledger for insurance.
- Reuse existing movement, attachment, form, table, upload, alert, and visual components before introducing new ones.
- Ensure backup and restore include insurance policies, premiums, their movement associations, and attachment metadata/files according to the existing data-lifecycle rules.

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

Provide a forward-looking view of expected household liquidity based on known recurring income and expenses, budgets, scheduled commitments, insurance premiums, and historical patterns. Forecast calculations should remain deterministic, with AI limited to interpretation and scenario explanation.

### 4. Bank statement import improvements

Improve the import workflow for bank statements, including broader format support, clearer validation, duplicate detection, preview before confirmation, and stronger assistance with transaction classification and reconciliation. Where relevant, reconciliation may also suggest associations between imported movements and outstanding insurance premiums.

## Later opportunities

- Insurance renewal insights and alerts.
- Premium-payment matching and anomaly insights.
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
