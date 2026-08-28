---
name: denarius-git
description: Inspect DenariusAI changes, propose consistent Git commit messages, separate unrelated work, and create commits when the user explicitly requests them.
---

# DenariusAI Git commits

Use this skill when the user asks for a commit message, asks to commit changes, or when a release workflow needs one or more commits.

## Establish the commit scope

- Inspect `git status`, the staged diff, the unstaged diff, and recent commit history before proposing a message.
- Base the message only on changes that will actually be committed. Never infer content from the conversation alone.
- Preserve unrelated user changes. If the working tree contains independent changes, propose separate commits and stage only the intended files or hunks.
- Do not include `.env`, credentials, API keys, database backups, generated personal data, or other secrets.
- Generating a message does not authorize staging, committing, pushing, tagging, or publishing. Perform each mutation only when requested by the user or when it is an explicitly authorized step of a requested release.

## Message format

Write commit messages in English using Conventional Commits:

```text
type(optional-scope): concise imperative summary
```

Choose the narrowest accurate type:

- `feat`: user-visible functionality or capability.
- `fix`: defect correction.
- `style`: visual presentation with no material behavior change.
- `refactor`: internal restructuring without a feature or fix.
- `test`: tests only.
- `docs`: documentation only.
- `build`: build system, dependencies, or container image definition.
- `ci`: GitHub Actions or other continuous-integration configuration.
- `chore`: repository maintenance not covered above.

Use a scope only when it adds useful precision, such as `analytics`, `budget`, `reconciliation`, `docker`, or `release`.

## Writing rules

- Use the imperative mood, lowercase after the colon, no final period, and normally no more than 72 characters in the subject.
- Describe the outcome, not implementation trivia such as file names.
- Do not use vague subjects such as `update files`, `changes`, or `fix stuff`.
- Do not mention Codex, AI authorship, or generated-by attribution.
- Add a body for a non-trivial change when it helps explain motivation, behavior, migration impact, or verification. Wrap it for terminal readability.
- Add issue references only when a real issue or PR number is available; never invent one.

Examples:

```text
feat(reconciliation): add editable AI classification suggestions
fix(analytics): use deterministic totals in financial reports
style(forms): align save and cancel actions
docs: refresh README application tour
ci: publish versioned web and MCP images
```

## Before creating a commit

1. Show or state the proposed subject and, when applicable, its body.
2. Confirm the staged diff contains the intended changes and no sensitive files.
3. Run verification appropriate to the change, or clearly report what was not run.
4. Create the commit without rewriting existing history unless the user explicitly requests it.
5. Report the commit hash and subject. Do not push unless the user also requested a push.

