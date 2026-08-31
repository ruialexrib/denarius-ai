---
name: denarius-coding
description: Apply DenariusAI C# coding, architecture and documentation standards to every implementation change.
---

# DenariusAI coding standards

Use this skill whenever creating, modifying or reviewing C# code in DenariusAI.

## XML documentation

- Write XML documentation comments in English.
- Every class, interface, record and enum must have a `/// <summary>` describing its responsibility.
- Every method or function must have a `/// <summary>` describing what it does, including private methods when they are explicitly declared.
- Add `<param>` for every parameter when the method has parameters.
- Add `<returns>` when a method returns a value or task result whose meaning is not self-evident.
- Add `<typeparam>` for generic type parameters.
- Add `<exception>` when an exception is intentionally part of the method contract.
- Document public properties when they expose domain, configuration, persistence or API meaning.
- Keep comments useful and concise. Explain intent and contract rather than repeating the identifier name.

## C# conventions

- Enable nullable reference types and implicit usings.
- Treat compiler and analyzer warnings as errors.
- Prefer asynchronous I/O APIs and propagate `CancellationToken` through database, file and AI operations.
- Use dependency injection and constructor injection for services.
- Keep controllers thin; business logic belongs in application/domain services.
- Use `decimal` for monetary calculations.
- Prefer immutable DTOs/records when mutation is not required.
- Validate arguments and external data at system boundaries.
- Do not suppress nullable or analyzer warnings without a justified reason.
- Do not expose secrets, connection strings, API keys or personal financial data in logs.

## Entity Framework Core changes

- Treat changes to persisted entities, `DbContext` configuration, relationships, indexes, constraints and persisted properties as potential database-schema changes.
- For any such change, follow the `denarius-data-lifecycle` skill and keep the EF Core model, model snapshot and committed migrations synchronized in the same change set.
- Do not finish model-related coding work while `dotnet ef migrations has-pending-model-changes` reports differences.
- Do not manually edit the EF Core model snapshot during normal development; migration generation must maintain it.
- Never rewrite a migration that has already been published or may have been applied to a user database. Create a forward migration instead.

## Architecture

- Preserve separation between financial-domain logic, persistence, presentation and AI integration.
- Do not couple deterministic financial calculations to an LLM or model provider.
- AI may explain, interpret or propose; changes to financial records remain under user control.
- Keep configurable operational prompts and model settings outside hard-coded business logic.
- Avoid introducing dependencies without a clear project-level benefit.

## Review

Before considering C# work complete, verify that every new or modified class and method satisfies the XML documentation requirement and that the solution builds without warnings.

If the work touched the EF Core persistence model, also verify that the appropriate migration is committed, the model snapshot is synchronized, `dotnet ef migrations has-pending-model-changes` succeeds and the relevant tests pass.
