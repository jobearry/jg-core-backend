# Portfolio — module guidance

`src/Portfolio` is the read-only portfolio API: experiences, projects, tech
stacks, recommendations, an overview, Notion page content, and public GitHub
repository data.

It is still its own host today. Phase 2 collapsed the layered projects into one
project organised by vertical slice; phase 3 composes it into `src/JGBackend`.

## Current layout

```
src/Portfolio/
├── Program.cs                  host pipeline and composition root
├── ConfigurationSettings.cs    host-only Swagger, JWT, CORS, ProblemDetails
├── Portfolio.csproj            single Web SDK project
├── Features/
│   ├── DependencyInjection.cs  AddPortfolioFeatures / MapPortfolioFeatures
│   ├── Experience/             endpoint, handler, repository, DTO, mapper tests
│   ├── Project/
│   ├── Skill/
│   ├── Recommendation/
│   ├── Overview/
│   ├── Notion/
│   └── Github/
└── Shared/
    ├── Authentication/         temporary JWT service (target: src/Core)
    ├── Common/                 Result<T>, errors, value objects, shared DTOs
    ├── Endpoints/              auth endpoints and ApiResult mapper
    ├── ErrorHandling/          GlobalExceptionHandler
    └── Persistence/            EF context, entities, mappers, repositories, migrations
```

Feature code lives in `Features/<Slice>/`; only genuinely shared code lives in
`Shared/`. Existing namespaces still follow the old layered names where types
are shared; do not rename them as part of a file move.

## Rules

1. `Program.cs` is the only composition root and calls
   `AddPortfolioFeatures()` / `MapPortfolioFeatures()` for feature wiring.
2. Endpoints return DTOs, never domain or persistence types.
3. Typed errors (`Result<T>`) map to status codes in one place.
4. Every I/O method takes and forwards a `CancellationToken`.
5. New schema changes are a new migration; never edit an applied one.
6. Auth here is temporary. Do not add a third token service; the target is
   `src/Core`.

## Commands

```sh
dotnet run --project src/Portfolio/Portfolio.csproj
dotnet test JGBackend.sln
dotnet ef migrations add <Name> \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/JGBackend/JGBackend.csproj
```

## Where to look

| Question | Document |
|---|---|
| Slices, request flow, target design | [docs/architecture.md](../../docs/architecture.md) |
| Entities and invariants | [docs/domain-model.md](../../docs/domain-model.md) |
| Endpoint catalogue | [docs/api-reference.md](../../docs/api-reference.md) |
| Schema and migrations | [docs/database.md](../../docs/database.md) |
| Run, configure, deploy | [docs/operations.md](../../docs/operations.md) |
| Known debt | [docs/known-issues.md](../../docs/known-issues.md) |
