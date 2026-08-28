---
name: denarius-finance-domain
description: Implement or review DenariusAI accounting, budgets, movements, accounts, categories, reconciliation, balances, savings, and financial calculations.
---

# DenariusAI financial domain

Use this skill whenever a change affects financial meaning, accounting records, balances, budgets, reconciliation, analytics, or reports.

## Accounting invariants

- A `JournalEntry` must contain at least two lines and total debit must equal total credit before it is persisted or cancelled.
- A `JournalEntryLine` contains a positive debit or a positive credit, never both and never neither.
- An expense debits an expense account and credits the asset account used for payment.
- Income debits the receiving asset account and credits an income account.
- A transfer debits the destination asset account and credits the origin asset account. It normally has no category.
- Categories classify the income or expense side of a movement. Do not require a category on an ordinary asset line or transfer.
- Do not expose debit and credit mechanics in simplified workflows when the application can derive them safely from movement type and accounts.
- Cancellation preserves the accounting history. Do not physically delete an established movement unless an explicitly designed destructive workflow requires it.

## Budgets and dates

- A budget represents one calendar month and year. Default budget selections to the current month unless the user deliberately selects another period.
- Associate movements with the selected budget explicitly; do not infer the budget solely from the current screen.
- Treat a movement dated inside the selected budget month as valid.
- A one-month difference may be presented as an overridable warning when the workflow supports it. Larger differences remain blocking unless requirements explicitly change.
- Budget execution must be derived from active movements associated with the budget and classified through its categories.
- Reports for a selected budget must use that budget's whole period, not an unrelated current-month window.

## Financial calculations

- Calculate balances, totals, percentages, projections, budget execution, and variances deterministically in application code or database queries.
- Use `decimal` for monetary calculations. Keep rounding and currency formatting separate from stored values.
- Exclude cancelled entries from active balances and execution unless a report explicitly concerns audit history.
- State the sign convention at data boundaries and test both inflows and outflows.
- Reuse a single calculation service or DTO across dashboard, analytics, PDFs, and AI context where the metric has the same meaning.
- When changing a metric, compare it with representative database records and add tests for totals and period boundaries.

## Reconciliation

- Imported statement values use positive amounts for inflows and negative amounts for outflows at the import boundary.
- Preserve statement date, editable description, and optional reference.
- AI classification is a proposal. The user can change the account, category, description, reference, and permitted budget before processing.
- Do not reconcile or persist rows that fail blocking validation.
- Bulk reconciliation requires explicit confirmation and processes only eligible unreconciled movements.

Explain unfamiliar accounting concepts in user-facing language while keeping the underlying entry balanced and auditable.

