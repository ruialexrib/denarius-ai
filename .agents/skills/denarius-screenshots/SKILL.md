---
name: denarius-screenshots
description: Capture and refresh DenariusAI README screenshots and the animated application tour with consistent high-resolution browser framing.
---

# DenariusAI screenshots and README tour

Use this skill when screenshots, the README gallery, or `docs/assets/denarius-ai-tour.gif` must be refreshed.

## Capture

- Use the running local application with demonstration data and a clean, representative state.
- Capture at a consistent high-resolution desktop viewport. The first screenshot must be the login page when producing the complete application tour.
- Remove or mask personal email addresses, credentials, tokens, real financial information, browser chrome, and transient notifications that are not part of the requested feature.
- Prefer ten representative screens for a complete tour: login, dashboard, movements, account statement, budget, savings certificates, analytics, assistant, help, and **O que há de novo**.
- Keep stable filenames under `docs/assets/screenshots/` so README links do not churn.

## Animated tour

- Build the GIF from the final verified screenshots in numerical order.
- Preserve readable resolution and use the transition duration requested by the user; otherwise use five seconds per screen.
- Optimize file size without making text unreadable.

## Verification

- Inspect every source image and the rendered GIF for cropping, stale versions, private data, inconsistent viewport size, and unreadable text.
- Update the README only after the media has been verified.
- Do not capture screenshots from a production or public demo unless the user explicitly asks for that source.

