# ADR-0001 — Five-project clean-architecture layout

- **Status:** Accepted
- **Date:** 2026-08-08 (solution layout as found)
- **Related:** [`../architecture.md`](../architecture.md), [`../../rules/architecture.md`](../../rules/architecture.md)

## Context

The API was a single ASP.NET Core project with EF Core entities doubling as the
model. The goal is to practise DDD and clean architecture in a codebase small
enough to keep the boundaries visible: one bounded context, five endpoints, one
database.

The constraints are: a single developer, a portfolio-scale dataset, and a
deployment target of a single Azure Web App. Ceremony has to buy something
visible.

## Decision

Split the solution into five projects with dependencies pointing inward:

```
Domain            (no references)
Application       → Domain
Persistence       → Domain
ExternalService   → Domain
Presentation      → Application, Persistence, ExternalService
```

The `.slnx` groups them as `1.0 Presentation`, `2.0 Core`
(Application + Domain), and `3.0 Infrastructure` (Persistence +
ExternalService). `Presentation` is the only composition root.

## Consequences

**Positive**

- The domain can be tested and reasoned about without a database or HTTP.
- Swapping PostgreSQL or Notion does not touch the domain.
- The dependency graph is explicit and reviewable in `.csproj` files.

**Negative / accepted costs**

- Five projects for ~40 types is heavy; navigation cost is real.
- Mapping between persistence entities and domain entities doubles the type
  count and is the main source of defects in this repo.
- Cross-project changes (e.g. adding a field) touch three files.

**Follow-up work**

- Remove the `Persistence → Application` reference, which violates the rule.
- Add a build-time architecture test that asserts the allowed edges.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Single project with folders | Fastest, but the dependency rule becomes unenforceable and the DDD exercise is lost |
| Two projects (core + host) | Fewer projects, but persistence and external services would share a home |
| Vertical slice / feature folders | Excellent for CRUD, but does not demonstrate ports and adapters |
