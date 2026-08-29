---
name: denarius-development-versioning
description: Start every DenariusAI repository change by assigning a new visible development version, including features, bug fixes, refactors, configuration, documentation, and other edits.
---

# DenariusAI development versioning

Use this skill whenever beginning work that will modify the DenariusAI repository. Read-only investigation and explanation do not require a version change.

## Mandatory development version

- Before the first implementation edit, inspect the stable version in `Directory.Build.props` and any development override in `src/DenariusAI.Web/DenariusAI.Web.csproj`.
- Give every new change set a version distinct from both production and the preceding development work. Use `MAJOR.MINOR.PATCH-dev.N` and increment `N` whenever a new feature, correction, refactor, configuration change, documentation change, or other repository modification begins.
- If a development version already exists for the planned release, preserve its `MAJOR.MINOR.PATCH` part and increment only `N`. Never reuse or decrement a development version.
- If development starts from a stable version, select the next release candidate using semantic versioning: normally increment `MINOR` for functionality and `PATCH` for corrections, then start at `dev.1`. When work already targets a higher compatible release, keep that target rather than lowering it.
- Keep `Directory.Build.props` as the last production/released version during ordinary development. Store the temporary development version in the web project so the application, exported backups, and diagnostic surfaces visibly distinguish the build from production.
- Verify after building that the UI reports the complete prerelease value, including `-dev.N`; do not rely only on the numeric assembly version.

The version increment identifies the start of a new change set. It does not authorize a commit, tag, release, container update, or deployment.

## Release transition

When publishing, use the `denarius-release` workflow to choose and set the final stable version consistently, remove the temporary project-level development override, and verify the displayed version.
