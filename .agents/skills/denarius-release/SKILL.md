---
name: denarius-release
description: Prepare and publish DenariusAI versions, commits, tags, GitHub releases, bilingual release notes, and the in-app O que ha de novo experience.
---

# DenariusAI releases

Use this skill when preparing a version, release notes, tag, GitHub release, or the content displayed in **O que há de novo**.

Ordinary implementation builds follow the `denarius-development-versioning` convention and display a `MAJOR.MINOR.PATCH-dev.N` version. A release replaces that temporary version with the final stable version.

## Version choice

Use semantic version numbers `MAJOR.MINOR.PATCH` and inspect the changes since the latest release before choosing:

- Increment **MAJOR** (`X.0.0`) when the release contains a considerable expansion of functionality, a major product milestone, or incompatible behavior.
- Increment **MINOR** (`X.Y.0`) for ordinary new functionality or a meaningful set of backward-compatible improvements.
- Increment **PATCH** (`X.Y.Z`) when the release contains only corrections or very small refinements.

If the user supplies an exact version, use it. Otherwise state the recommended version and the evidence for the classification before publishing.

Update `Version`, `AssemblyVersion`, and `FileVersion` consistently in `Directory.Build.props`, remove the project-level development `Version` override, and confirm that the application displays the stable version without a `-dev.N` suffix. Tags and GitHub release titles use the `vX.Y.Z` form.

## Release notes

Every release must describe the delta from the preceding release in both European Portuguese and English. Organize each language into exactly these perspectives:

```markdown
## Português
### Novas funcionalidades
- ...
### Correções
- ...

## English
### New features
- ...
### Fixes
- ...
```

- Mention only observable changes included in the tag.
- If a section has no entries, write `- Sem alterações nesta categoria.` or `- No changes in this category.` rather than removing it.
- Keep Portuguese and English semantically equivalent, concise, and understandable to users rather than only developers.
- Ensure both languages and both perspectives are visible in the application page **O que há de novo**. If its GitHub release parser truncates or flattens the body, adapt and test the presentation before publishing.

## Publishing checks

1. Inspect the working tree and the changes since the previous release. Do not include unrelated user files.
2. Update the application version and any local fallback release information that depends on it.
3. Run the Release build and full test suite. Verify Docker targets when container behavior changed.
4. Commit and push the intended changes.
5. Confirm CI and security checks succeed before tagging, unless the user explicitly accepts a known exception.
6. Create and push the tag, then create the GitHub release with the bilingual notes.
7. Verify the release page, published web and MCP container packages, and in-app version detection.

Do not reuse an existing tag for different code and do not publish a release from an unverified or unintended commit.
