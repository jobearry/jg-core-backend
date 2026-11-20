# Architecture rules

Normative. These are not suggestions.

## 1. Layer model

Five projects, two rings.

| Project | Ring | Responsibility |
|---|---|---|
| `Portfolio.Domain` | Core | Entities, value objects, repository ports, domain invariants |
| `Portfolio.Application` | Core | Use cases, `Result<T>`, DTOs, application settings, abstractions |
| `Portfolio.Persistence` | Infrastructure | EF Core context, persistence entities, repository implementations, mapping profile |
| `Portfolio.ExternalService` | Infrastructure | Third-party clients (Notion), parsing, vendor DTOs |
| `Portfolio` | Presentation | Minimal API endpoints, DI composition, auth, OpenAPI, CORS |

### Allowed references

```
Domain          → (none)
Application     → Domain
Persistence     → Domain
ExternalService → Domain
Presentation    → Application, Persistence, ExternalService
```

Violations to fix, not to copy:

- `Persistence.csproj` referencing `Application` — **fixed 2026-10-22**.
- `Persistence/Repositories/EfPortfolioExperienceRepository.cs` importing
  `Portfolio.Application.Common` — **fixed 2026-10-22**.

### Enforcement

- Review the `.csproj` diff of every PR. A new `ProjectReference` is an
  architecture change, not a detail.
- Once `Directory.Build.props` exists, add an architecture test that asserts the
  reference graph (e.g. read the `.csproj` files and assert allowed edges).

## 2. Where code belongs

| If it is… | It goes in | Not in |
|---|---|---|
| A business rule or invariant | `Domain/Entities` or `Domain/ValueObjects` | Application, Persistence |
| A concept defined by its value (`Role`, `Period`) | `Domain/ValueObjects` as a `record` | Entities |
| A need the domain has from the outside world | `Domain/Interfaces` (port) | Application |
| A use case / orchestration step | `Application/Services` | Endpoints |
| A wire contract | `Application/DTO` | Domain |
| A read model for a UI widget | `Application/DTO` | Domain |
| A vendor shape (Notion payload) | `ExternalService/.../DTO` | Domain |
| A table shape | `Persistence/Entities` | Domain |
| SQL / EF query | `Persistence/Repositories` | Application |
| HTTP routing, status codes | `Portfolio/Endpoints` | Application |
| Object graph construction | `Program.cs` / `ConfigurationSettings.cs` | Any other project |

**Test:** "would this type still exist if we replaced PostgreSQL with a file and
Notion with a CSV?" If yes → Domain. If no → Infrastructure.

## 3. Aggregate rules

An aggregate is a **consistency boundary**: the unit loaded and saved together,
and the only thing a repository may return.

1. One repository interface per aggregate root.
2. Exactly **one write entry point** per aggregate. Today there are none, so
   this is a forward-looking rule.
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

- `Program.cs` is the **only** composition root.
- Endpoints do three things: bind input, call one application method, translate
  the result to an HTTP response. No business logic, no EF, no mapping beyond
  DTO → response.
- Endpoints must not return `Domain.*` types. See `rules/api.md`.
- Extension methods (`MapXxxEndpoints`) may live in `Endpoints/`; registration
  happens in `Program.cs`.

## 5. Target request flow

```
Endpoint
  → Application service / handler
    → Domain port (repository)
      → EF Core
        → Persistence entity
      ← AutoMapper / manual mapping
    ← Domain entity (or directly a DTO projection)
  ← DTO
← Results.Ok(dto) / TypedResults
```

Domain types stop at the application boundary. Persistence types never cross it
upward.

## 6. Change checklist

Before opening a PR that touches architecture:

- [ ] Did I add a project reference? Is it inward?
- [ ] Did I put a vendor or EF type in Domain?
- [ ] Does any endpoint signature mention a `Domain` type?
- [ ] Is there still exactly one repository per aggregate root?
- [ ] Did I update `docs/architecture.md` and, if a decision changed,
      add an ADR?
