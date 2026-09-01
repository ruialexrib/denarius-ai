---
name: denarius-ui
description: Implement or review DenariusAI user-interface changes, including Razor views, forms, tables, cards, navigation, alerts, responsive layout, and shared visual consistency.
---

# DenariusAI UI

Use this skill for visual or interaction changes in the DenariusAI web application.

## Design direction

- Extend the existing clean financial-dashboard language: dark navy structure, restrained emerald accents, soft semantic colors, generous but efficient spacing, subtle borders, and low-noise shadows.
- Prefer shared CSS and components over page-specific duplication. Check `wwwroot/css/site.css`, `buttons.css`, `data-forms.css`, `page-headers.css`, `summary-cards.css`, and the closest feature stylesheet first.
- Keep typography, height, padding, border radius, hover, focus, disabled, and loading states consistent for equivalent controls.
- Use green for positive/income states, soft red for expense/destructive states, and amber for warnings or transfers without relying on color alone.
- Keep dense financial information readable. Avoid shrinking a control or content area merely to fit more on screen.

## Summary cards

- Use the summary-card language established by Budget, Savings Certificates and Insurance as the canonical pattern for area-level KPI summaries.
- New or revised area summaries should use the shared `area-summary` grid and semantic card modifiers from `wwwroot/css/summary-cards.css` instead of introducing a feature-specific card system.
- The first card should normally carry the principal KPI and use `summary-primary` with the dark navy gradient. Use `summary-positive`, `summary-negative`, `summary-warning`, and `summary-info` only when the metric semantics justify them.
- Each card should have a short uppercase label, one prominent deterministic value, and a concise contextual line. Preserve the meaning of the underlying area rather than forcing identical metrics across unrelated features.
- Prefer three or four cards. Use `area-summary three` when three metrics provide the clearest summary. Keep card heights, spacing, radii, shadows and responsive collapse consistent across areas.
- Do not use decorative colour to imply a financial state that the metric does not represent. Neutral counts should remain neutral unless they are the principal card.
- Budget, Savings Certificates and Insurance are reference implementations. When changing their cards, preserve this shared visual grammar and migrate reusable improvements into `summary-cards.css` where appropriate.

## Interaction rules

- Searchable selectors must retain the visual dimensions of ordinary selectors, display all relevant matches, support keyboard use, and escape containers without being clipped.
- Confirmation and error feedback should use the shared toast and progress patterns.
- Destructive actions require clear wording and confirmation proportional to their impact.
- Preserve authorization boundaries: administrator-only actions and audit links must remain administrator-only.
- User-facing AI suggestions must expose uncertainty when relevant and remain editable before confirmation.

## Verification

- Render the affected page and inspect the changed state, not only the initial state.
- Check alignment against comparable screens and verify focus, hover, empty, validation, and narrow-screen states when applicable.
- Run the relevant tests and a Release build after changes.
