---
name: denarius-ui
description: Implement or review DenariusAI user-interface changes, including Razor views, forms, tables, cards, navigation, alerts, responsive layout, and shared visual consistency.
---

# DenariusAI UI

Use this skill for visual or interaction changes in the DenariusAI web application.

## Design direction

- Extend the existing clean financial-dashboard language: dark navy structure, restrained emerald accents, soft semantic colors, generous but efficient spacing, subtle borders, and low-noise shadows.
- Prefer shared CSS and components over page-specific duplication. Check `wwwroot/css/site.css`, `buttons.css`, `data-forms.css`, `page-headers.css`, and the closest feature stylesheet first.
- Keep typography, height, padding, border radius, hover, focus, disabled, and loading states consistent for equivalent controls.
- Use green for positive/income states, soft red for expense/destructive states, and amber for warnings or transfers without relying on color alone.
- Keep dense financial information readable. Avoid shrinking a control or content area merely to fit more on screen.

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

