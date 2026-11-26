# Portfolio — module guidance

`src/Portfolio` is the portfolio API: anonymous public reads for experiences,
projects, tech stacks, recommendations, an overview, Notion page content and public
GitHub repository data, plus admin-only writes under `/api/v1/admin`. It is a
**class library** composed by `src/JGBackend`; it owns no host pipeline or auth.

## Layout

```
src/Portfolio/
├── Portfolio.csproj            class library (ASP.NET Core framework reference)
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
    ├── Common/                 Result<T>, errors, value objects, shared DTOs
    ├── Endpoints/              the single Result → IResult mapper
    └── Persistence/            EF context, entities, mappers, repositories, migrations
```

Feature code lives in `Features/<Slice>/`; only genuinely shared code lives in
`Shared/`. Existing namespaces still follow the old layered names where types
are shared; do not rename them as part of a file move.

## Rules

1. `src/JGBackend/Program.cs` is the only composition root and calls
   `AddPortfolioFeatures()` / `MapPortfolioFeatures()` for feature wiring. The
   host supplies the passthrough rate-limit policy names.
2. Endpoints return DTOs, never domain or persistence types.
3. Typed errors (`Result<T>`) map to status codes in one place.
4. Every I/O method takes and forwards a `CancellationToken`.
5. New schema changes are a new migration; never edit an applied one.
6. Auth, CORS, Swagger, rate limiting and exception handling belong to the host
   and `Core`. Do not add a second copy here.

## Commands

```sh
dotnet test JGBackend.sln
dotnet ef migrations add <Name> \
  --context PortfolioContext \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/JGBackend/JGBackend.csproj
```

There is no `dotnet run --project src/Portfolio` — run `src/JGBackend` instead.

## Where to look

| Question | Document |
|---|---|
| Slices, request flow, target design | [docs/architecture.md](../../docs/architecture.md) |
| Entities and invariants | [docs/domain-model.md](../../docs/domain-model.md) |
| Endpoint catalogue | [docs/api-reference.md](../../docs/api-reference.md) |
| Schema and migrations | [docs/database.md](../../docs/database.md) |
| Run, configure, deploy | [docs/operations.md](../../docs/operations.md) |
| Known debt | [docs/known-issues.md](../../docs/known-issues.md) |
