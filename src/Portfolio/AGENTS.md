# Portfolio — module guidance

`src/Portfolio` is the read-only portfolio API: experiences, projects, tech
stacks, recommendations, an overview, Notion page content, and public GitHub
repository data.

It is still its own host today. The single-host merge (phase 3) turns it into a
module composed by `src/JGBackend`, and the layered projects below become
vertical slices.

## Current layout

```
Portfolio.API/              endpoints, DI composition, auth, Swagger
Portfolio.Application/      use cases, Result<T>, DTOs, settings, abstractions
Portfolio.Domain/           entities, value objects, repository ports
Portfolio.Persistence/      EF Core context, persistence entities, repositories, migrations
Portfolio.ExternalService/  Notion + GitHub clients and parsing
```

## Dependency rules

1. Dependencies point inward. `Portfolio.Domain` references nothing.
2. Infrastructure projects never reference `Portfolio.Application`.
3. Nothing references `Portfolio.API`.
4. Domain has no EF Core, ASP.NET, HTTP, JSON, or AutoMapper types.
5. `Portfolio.API` is the only composition root.

## Non-negotiables

- Endpoints return DTOs, never domain entities.
- Typed errors (`Result<T>`), mapped to the right status code in one place.
- Every I/O method takes and forwards a `CancellationToken`.
- New schema changes are a new migration; never edit an applied one.
- Tests ship with the change. See `tests/Portfolio/`.
- Auth here is temporary. Do not add a third token service; the target is
  `src/Core`.

## Commands

```sh
dotnet run --project src/Portfolio/Portfolio.API
dotnet test tests/Portfolio
dotnet ef migrations add <Name> \
  --project src/Portfolio/Portfolio.Persistence \
  --startup-project src/Portfolio/Portfolio.API
```

## Where to look

| Question | Document |
|---|---|
| Layers, request flow, target design | [docs/architecture.md](../../docs/architecture.md) |
| Entities and invariants | [docs/domain-model.md](../../docs/domain-model.md) |
| Endpoint catalogue | [docs/api-reference.md](../../docs/api-reference.md) |
| Schema and migrations | [docs/database.md](../../docs/database.md) |
| Run, configure, deploy | [docs/operations.md](../../docs/operations.md) |
| Known debt | [docs/known-issues.md](../../docs/known-issues.md) |
