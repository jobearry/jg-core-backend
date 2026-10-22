# ADR-0004 — DTO boundary at the API edge

- **Status:** Accepted
- **Date:** 2026-10-21 (proposed) · 2026-10-22 (decided — plan 001 decision D4)
- **Related:** plan 2.1, [`../api-reference.md`](../api-reference.md)

## Context

Every endpoint returns a domain entity:

```csharp
return Results.Ok(result.Value);   // List<Project?> serialized as-is
```

Consequences visible in the current JSON:

- Value objects appear as objects: `"role": { "value": "Developer" }`,
  `"company": { "name": "Acme" }`.
- Persistence-shaped fields leak: `createdAt` on tech stacks, `experienceId` on
  projects.
- `Period.Duration` is serialized as a `c`-format string
  (`"1234.05:06:07"`), which no client asked for.
- Any domain refactor (e.g. converting `Role` to a record, or moving
  `DashboardItem` out of Domain) becomes a breaking API change.

## Decision

Introduce a DTO layer at the application boundary:

- `Application/DTO/*Dto` — `sealed record`s, no domain types, no audit fields.
- Services (or dedicated query handlers) return DTOs, not entities.
- Endpoints return DTOs and declare them with `.Produces<T>()`.
- Mapping domain → DTO is explicit, not AutoMapper-by-convention.

Domain types stop at the application boundary. Persistence types never cross it.

## Consequences

**Positive**

- The API contract can evolve independently of the domain model.
- The wire shape becomes intentional: `{ "role": "Developer" }` instead of
  `{ "role": { "value": "Developer" } }`.
- Sensitive or irrelevant fields cannot leak by accident.
- Contract tests become meaningful.

**Negative / accepted costs**

- More types: roughly one DTO per read model.
- A second mapping layer (domain → DTO) to maintain.
- For a portfolio API with one consumer, this is ceremony — accepted because the
  project exists to practise the boundary.

**Follow-up work**

- Remove `Domain.Entities.DashboardItem` and `Domain.ValueObjects.NotionPage`
  once DTOs exist (plan 3.2). **`DashboardItem` removed 2026-10-22** (now
  `Application/Models`); **`NotionPage` removed 2026-10-23** — the vendor model
  moved to `ExternalService`, the core reads `Application.Models.PageContent`
  through `IPageContentReader`, and `Presentation/Adapters` bridges the two
  (ADR-0005).
- Add `.ProducesProblem(...)` and contract tests per endpoint.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Serialize domain entities with `[JsonIgnore]` / custom converters | Attributes put serialization concerns into the domain; the fix is local and hides the coupling |
| Separate read models via `Select(...)` projections only | Good for queries, but leaves no type for the contract and mixes EF projections into endpoints |
| Do nothing | Acceptable for a private API, rejected because it blocks every Part 3 refactor |
