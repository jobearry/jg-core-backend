# Architecture rules

Normative. These are not suggestions.

## 1. Single-host shape

Portfolio is a **class library** (`src/Portfolio/Portfolio.csproj`) organised by
vertical slice. `src/JGBackend` is the only host and composes it with `Core`.

```
src/JGBackend/
└── Program.cs                  the only composition root and middleware pipeline
src/Portfolio/
├── Portfolio.csproj            class library (ASP.NET Core framework reference)
├── Features/<Slice>/           endpoint, handler, repository, DTO, slice tests
├── Features/DependencyInjection.cs
└── Shared/
    ├── Common/                 Result<T>, errors, value objects, shared DTOs
    ├── Endpoints/              the single ApiResult mapper
    └── Persistence/            EF context, entities, mappers, repositories, migrations
```

Existing namespaces still follow the old layered names where types are shared.
Keep them until a deliberate rename; do not recreate the old projects.

### Allowed dependencies

- A feature may depend on `Shared/Common` and `Shared/Persistence`.
- A feature must not depend on another feature's internals. Share through a
  repository port, a DTO in the owning slice, or `Shared/Common`.
- `Shared/Persistence` must not depend on a feature or on HTTP types.
- `Shared/Common` must not depend on EF Core, ASP.NET, or vendor clients.
- Only `src/JGBackend/Program.cs` and `Features/DependencyInjection.cs` compose object graphs.
- Modules never reference the host. `Portfolio` takes rate-limit policy names as
  optional arguments instead of depending on `Core`.

### Enforcement

- Review the `.csproj` diff of every PR. Adding a project reference is an
  architecture change, not a detail.
- Do not add a new `src/Portfolio/Features/<Slice>` dependency on another slice.
- A structural change needs an ADR (supersede the old one; never edit an ADR).

## 2. Where code belongs

| If it is… | It goes in | Not in |
|---|---|---|
| A business rule or invariant | the owning slice, or `Shared/Common/Entities` | endpoint, EF repository |
| A concept defined by its value (`Role`, `Period`) | `Shared/Common` as a `record` | entities |
| A need the data layer has from outside | a repository port in the owning slice | endpoint |
| A use case / orchestration step | `Features/<Slice>/...Handler.cs` | endpoint |
| A wire contract | `Features/<Slice>/*Dto.cs` | EF layer |
| A vendor shape (Notion payload, GitHub JSON) | the owning slice | `Shared/Common` |
| A table shape | `Shared/Persistence/Entities` | domain types |
| SQL / EF query | `Features/<Slice>/Ef*Repository.cs` | handler |
| HTTP routing, status codes | `Features/<Slice>/*Endpoints.cs` | handler |
| Object graph construction | `src/JGBackend/Program.cs` / `Features/DependencyInjection.cs` | slice files |

**Test:** "would this type still exist if we replaced PostgreSQL with a file and
Notion with a CSV?" If yes, keep it out of EF and vendor folders.

## 3. Aggregate rules

An aggregate is a **consistency boundary**: the unit loaded and saved together,
and the only thing a repository may return.

1. One repository interface per aggregate root.
2. Exactly **one write entry point** per aggregate. Today there are none, so
   this is forward-looking.
3. Entities inside an aggregate are reached through the root, never through
   their own repository for writes.
4. Reference other aggregates **by identity**, not by object reference.
5. Shared reference data (e.g. `TechStack`) is not owned by an aggregate that
   merely uses it — reference it by id or a small immutable `TechStackRef`.

Current boundary: `Project` is an aggregate root; `Experience.Projects` is a
read-only projection, and `Project.TechStacks` contains immutable `TechStackRef`
values. See [ADR-0003](../docs/adr/0003-aggregate-boundary.md) and
[`domain-model.md`](../docs/domain-model.md).

## 4. Presentation rules

- `src/JGBackend/Program.cs` is the **only** composition root.
- Endpoints do three things: bind input, call one handler, translate the result
  to an HTTP response. No business logic, no EF, no mapping beyond DTO → response.
- Endpoints must not return `Domain.*` or `Persistence.*` types. See `rules/api.md`.
- `MapXxxEndpoints()` lives in the owning slice; registration happens through
  `MapPortfolioFeatures()` in `Features/DependencyInjection.cs`.

## 5. Request flow

```
Endpoint
  → Feature handler
    → Repository port (or vendor client port)
      → EF Core / HTTP client
        → hand-written mapper / vendor parser
      ← Domain entity or vendor DTO
    ← Application DTO wrapped in Result<T>
  ← ApiResult.ToHttpResult()
```

Domain types stop at the handler boundary. Persistence and vendor types never
cross it upward.

## 6. Change checklist

Before opening a PR that touches architecture:

- [ ] Did the change stay inside one slice, or is the shared dependency justified?
- [ ] Did I avoid a new project reference?
- [ ] Did I keep EF, HTTP, and vendor types out of domain-oriented code?
- [ ] Does any endpoint signature mention a `Domain.*` type?
- [ ] Is there still exactly one repository per aggregate root?
- [ ] Did I update `docs/architecture.md` and, if a decision changed, add an ADR?
