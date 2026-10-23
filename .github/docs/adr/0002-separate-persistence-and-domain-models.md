# ADR-0002 — Persistence entities separate from the domain model

- **Status:** Accepted
- **Date:** 2026-08-09 (introduced with `AddProjectWithTechStack`)
- **Related:** [`../domain-model.md`](../domain-model.md), [`../../rules/persistence.md`](../../rules/persistence.md)

## Context

The domain model needs invariants (a role cannot be empty, a period cannot end
before it starts). EF Core works most naturally with mutable classes that have a
parameterless constructor and public setters, and the schema was originally
scaffolded from an existing database.

If EF materialises the domain types directly, the domain constructors and
invariants are bypassed, and EF attributes leak into the core.

## Decision

Keep two models:

- `Domain/Entities` + `Domain/ValueObjects` — behaviour and invariants, no EF
  types.
- `Persistence/Entities/*Entity` — table shapes, public setters, configured
  fluently.

Repositories load persistence entities and map them to domain types with
AutoMapper (`Persistence/Profiles/PortfolioMappingProfile.cs`). Domain types are
never materialised by EF.

## Consequences

**Positive**

- Domain invariants always run, because construction goes through the
  constructor.
- The schema can evolve (rename a column, split a table) without changing the
  domain.
- No EF Core dependency in Domain.

**Negative / accepted costs**

- Double the type definitions and a mapping layer to maintain.
- AutoMapper resolves members by convention at runtime, so mistakes compile. The
  current profile contains a `string → int` mapping and drops `FinishedAt`.
- Mapping must be tested in both directions, or defects stay latent until a
  write path appears.

**Follow-up work**

- Delete unused reverse mappings instead of leaving them broken.
- Add mapping tests (plan 1.2 / 3.5).
- Consider explicit mapping methods if the profile keeps producing defects.

## Alternatives considered

| Option | Why rejected |
|---|---|
| EF entities as the domain model | Simplest, but bypasses invariants and puts `partial`/`= null!` in the core |
| Manual SQL / Dapper | Full control, but loses change tracking, migrations, and the EF learning goal |
| Separate read/write models (CQRS) | Correct long-term, but overkill for five read endpoints |
