---
name: denarius-data-lifecycle
description: Change or verify DenariusAI persistence, EF Core migrations, demonstration seed, audit records, backup, restore, reset, and compatibility of stored data.
---

# DenariusAI data lifecycle

Use this skill for schema changes, migrations, seed data, audit, backup, restore, reset, and database integrity.

## Schema changes

- Update domain entities and EF Core configurations together.
- Create a named migration and update `DenariusDbContextModelSnapshot`; never hand-edit only the snapshot to imitate a migration.
- Review generated migration operations for unintended drops, destructive type changes, missing indexes, or unsafe defaults.
- Preserve existing data where practical. If a migration is intentionally destructive, state the impact and require explicit authorization before applying it to user data.
- Add integration tests covering persistence and any new constraints.

## Demonstration and structural data

- Keep structural seed deterministic and idempotent.
- Demonstration data should form a coherent scenario from the beginning of the current demonstration year, including income, expenses, transfers, budgets, savings certificates, reconciled and unreconciled movements, reminders, and demo users where required.
- Demo entries must remain balanced and use stable relationships between accounts, categories, budgets, and users.
- Never seed real personal information, production credentials, or secrets.
- Keep the documented guest account aligned with the demonstration seed without exposing privileged credentials.

## Audit

- Record insertion, alteration, cancellation/reactivation, and deletion when the application supports those operations.
- Capture record type, record identifier, operation, timestamp, acting user, and meaningful before/after values.
- Resolve user identifiers to display names in the UI while retaining immutable identifiers in storage.
- Administrator audit links open the history for the selected record first; the user then chooses a specific event to inspect.
- Exclude secrets, password hashes, tokens, and unnecessary sensitive payloads from audit details.

## Backup and restore

- Treat the JSON backup as a versioned, integral application-data contract, including newly created configurable records such as categories.
- Validate format, version, required relationships, duplicates, and referential integrity before changing the database.
- Restore in a transaction and roll back fully on failure.
- Before override, require confirmation and attempt a downloadable pre-restore backup. Clearly distinguish failure to create/download that backup from restore success.
- Use the shared waiting indicator during backup and restore, then show an explicit success or error toast.
- Maintain compatibility with earlier exported formats when safe. When incompatible, reject them before mutation with a precise message.

## Reset and containers

- Financial reset and demonstration loading are explicit administrator actions with clear destructive wording.
- Do not delete Docker database volumes as part of an ordinary rebuild, restart, backup, restore, or migration.
- After data operations, verify representative counts, relationships, balanced entries, authentication, and application startup.

