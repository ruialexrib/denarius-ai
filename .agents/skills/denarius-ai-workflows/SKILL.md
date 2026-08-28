---
name: denarius-ai-workflows
description: Build or review DenariusAI Mistral prompts, AI suggestions, structured responses, financial context tools, confidence, reports, and human-confirmation workflows.
---

# DenariusAI AI workflows

Use this skill for Mistral integration, prompts, assistant behavior, classifications, welcome messages, and intelligent financial reports.

## Configuration

- Every operational prompt used by the web application must have an administrator-editable setting with a safe default in `ApplicationSettingsDefaults`.
- The runtime service must read the effective setting. Do not display one prompt in Settings while executing a different hard-coded prompt.
- Keep prompts in European Portuguese where the model communicates with the application or user in Portuguese.
- Preserve `mistral-small-2603` as the installation default unless the user explicitly requests a model change.
- Never place API keys, credentials, personal data, or hidden environment values in prompts or logs.

## Model boundary

- Supply the model with a bounded, explicit context and only IDs it is permitted to return.
- Calculate accounting totals, balances, budget execution, projections, variances, and percentages in deterministic application services before model invocation.
- Use the model to summarize, explain, classify, or propose; do not rely on it as the source of financial arithmetic.
- The web application's built-in AI may call internal read-only services equivalent to MCP tools. It does not require the optional MCP container merely to use Mistral.
- Add or extend read-only financial data tools when a report lacks required facts. Keep queries scoped to the authenticated user's authorized data.

## Structured workflows

- Require strict JSON for extraction and suggestion workflows and validate it before use.
- Use only catalog IDs supplied in context; reject unknown, empty, or unauthorized IDs.
- Return `needs_clarification` and ask one short, specific question when required information is absent or ambiguous.
- Do not invent a category, account, budget, date, value, or reference to force completion.
- Treat confidence as model-provided classification metadata, not a mathematical probability unless the application implements and documents a calibrated score.
- Show high confidence in green, low confidence in amber, and missing classification in red, with accessible text or labels in addition to color.

## Human control

- AI output remains editable and is not persisted until the user confirms it.
- Clearly tell the user when a step only interprets or previews data and writes nothing.
- Record ordinary application audit events when confirmed AI suggestions cause data changes; do not audit uncommitted previews as financial records.
- If model output is malformed, incomplete, truncated, or inconsistent, fail safely with actionable feedback and preserve the user's input.

## Verification

- Test the exact effective prompt path, structured parsing, invalid IDs, missing fields, malformed JSON, and cancellation.
- For financial reports, compare supplied DTO values with the database/service results and test long output handling and export formats.
- For classification, test both supported pasted formats and mixed three-column/four-column input.

