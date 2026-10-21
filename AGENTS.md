# AGENTS.md

Operating guide for AI agents and humans working in **JDB_Portfolio_API**.

This file is the entry point only. Details live in [`.github/`](.github/) so this
document stays short and does not rot.

**Read order for any task:** this file → `.github/rules/` for the area you touch →
`.github/docs/` for context → `.github/plans/` for known debt.

---

## 1. What this project is

A **read-only portfolio API** for a personal CV site. One bounded context:
**Portfolio**. It serves experience, projects, tech stacks, recommendations, a
dashboard overview, and Notion page content as JSON.

Current status:

- Read path is complete and deployed (Azure Web App).
- **No write endpoints exist.** `AddNewAsync` is commented out.
- DDD/clean-architecture refactor is **in progress**, not complete. Do not treat
  the current code as the target design — see
  [`.github/plans/001-clean-architecture-refactor.md`](.github/plans/001-clean-architecture-refactor.md).
- **Tests exist but are thin.** Two xUnit projects cover the Domain value objects and
  the application services (24 tests). There is no mapping test project and no
  endpoint test project yet.

---

## 2. Architecture in one screen

Five projects, one solution (`src/JDB_Portfolio_API.slnx`):

| Project | Layer | Role | May reference |
|---|---|---|---|
| `JDB_Portfolio_API.Domain` | Core | Entities, value objects, repository ports | **nothing** |
| `JDB_Portfolio_API.Application` | Core | Use cases, `Result<T>`, DTOs, settings | Domain |
| `JDB_Portfolio_API.Persistence` | Infrastructure | EF Core context, entities, repositories, AutoMapper profile | Domain |
| `JDB_Portfolio_API.ExternalService` | Infrastructure | Notion HTTP client + parser | Domain |
| `JDB_Portfolio_API` | Presentation | Minimal API endpoints, DI composition root, auth, Swagger | Application, Persistence, ExternalService |

```
Domain  ←  Application
   ↑           ↑
   │           │
Persistence  ExternalService
   ↑           ↑
   └── Presentation ──┘   (composition root)
```

**Hard rules**

1. Dependencies point inward. Domain references nothing.
2. Infrastructure projects must never reference `Application`.
3. No project references the Presentation project.
4. Domain contains no EF Core, ASP.NET, HTTP, JSON, or AutoMapper types.
5. Presentation is the only place that constructs the object graph (`Program.cs`
   + `ConfigurationSettings.cs`).

**Request flow today**

```
Endpoint (minimal API)
  → Application service        (try/catch → Result<T>)
    → Domain repository port   (IPortfolio*Repository)
      → EF Core query          (PortfolioContext)
        → AutoMapper           (Persistence entity → Domain entity)
  → Results.Ok(domain entity)  ← domain leaks to the wire; DTOs are planned
```

---

## 3. Technology

| Concern | Choice | Version |
|---|---|---|
| Runtime | .NET | `net10.0` (SDK 10.0.402) |
| Web | ASP.NET Core **Minimal APIs** | 10.0.x |
| API docs | Swashbuckle (Swagger UI) | 10.2.3 |
| ORM | EF Core + Npgsql | 10.0.10 / 10.0.3 |
| Database | PostgreSQL | version not pinned (compose service missing) |
| Mapping | AutoMapper | 16.2.0 |
| Auth | JWT Bearer (shared-key login) | 10.0.10 |
| External API | Notion (markdown endpoint) | `2026-03-11` |
| Container | Docker multi-stage | SDK 10 / ASP.NET 10 |
| CI/CD | GitHub Actions → Azure Web App (OIDC) | — |
| Tests | xUnit | 2.9.3 (runner 3.1.4, Test.Sdk 17.14.1) |

Full detail, including why each package is present:
[`.github/docs/technology-stack.md`](.github/docs/technology-stack.md).

---

## 4. Repository map

```
AGENTS.md                        ← you are here
.github/
  rules/                         coding + architecture standards (read before editing)
  docs/                          current-state documentation
  docs/adr/                      architecture decision records
  plans/                         numbered refactor plans / debt
  skills/                        task playbooks (add endpoint, migration, TDD, review)
  workflows/                     GitHub Actions
src/
  JDB_Portfolio_API.Domain/      Entities/ ValueObjects/ Interfaces/
  JDB_Portfolio_API.Application/ Common/ Services/ DTO/ Settings/
  JDB_Portfolio_API.Persistence/ Contexts/ Entities/ Repositories/ Profiles/ Migrations/
  JDB_Portfolio_API.ExternalService/ Notion/{Services,Utils,Settings,DTO}
  JDB_Portfolio_API/             Endpoints/ Program.cs ConfigurationSettings.cs
tests/
  JDB_Portfolio_API.Domain.Tests/       value objects, entity invariants
  JDB_Portfolio_API.Application.Tests/  services with hand-rolled fakes
docker/                          Dockerfile, docker-compose.yml, .env.example
```

---

## 5. Commands

```bash
# Build / run
dotnet build src/JDB_Portfolio_API.slnx
dotnet run --project src/JDB_Portfolio_API            # http://localhost:5254
                                                      # https://localhost:7248

# Tests
dotnet test src/JDB_Portfolio_API.slnx

# EF Core — the tool manifest is at the repo root
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/JDB_Portfolio_API.Persistence \
  --startup-project src/JDB_Portfolio_API
dotnet ef database update \
  --project src/JDB_Portfolio_API.Persistence \
  --startup-project src/JDB_Portfolio_API

# Docker (API only; set ConnectionStrings__JDBConnection in docker/.env)
docker compose -f docker/docker-compose.yml up --build
```

Swagger UI: `http://localhost:5254/swagger` (Development only).

Required configuration (user-secrets in dev, env vars in prod):

| Key | Purpose |
|---|---|
| `ConnectionStrings:JDBConnection` | PostgreSQL connection |
| `Database:AutoMigrate` | apply pending migrations at startup (default `true`) |
| `Jwt:Key` | ≥ 32 bytes; also the `/auth/login` shared secret |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationInMinutes` | token settings |
| `Notion:Token`, `Notion:Version` | Notion integration |

---

## 6. Non-negotiable rules

Each rule has a full explanation in `.github/rules/`.

1. **Dependency direction** — never add a reference that points outward.
   [architecture.md](.github/rules/architecture.md)
2. **Domain purity** — no EF attributes, no `partial`, no `= null!`, no
   `CreatedAt`/`UpdatedAt`, no vendor types in Domain.
3. **Endpoints return DTOs, not domain entities.** This is the target; the
   current code violates it and is being fixed (plan 2.1).
4. **One repository per aggregate, one write entry point per aggregate.**
5. **Value objects are `record`s with value equality and validating
   constructors.** No public setters on entities.
6. **Typed errors, not strings.** `Result<T>` must carry an error kind; a
   database failure is a 500/503, never a 502.
7. **Every I/O method takes `CancellationToken`** and passes it down.
8. **Never log or commit secrets.** `docker/.env` is git-ignored; `Jwt:Key` and
   `Notion:Token` never appear in source or logs.
9. **Tests come with the change, not after it.** Test-first for domain
   invariants, mappings, and endpoint contracts. [testing.md](.github/rules/testing.md)
10. **Never edit an applied migration.** Add a new one.
11. **No new NuGet package without asking.** Every dependency is a long-term
    liability.

---

## 7. Where to look

| Question | Document |
|---|---|
| How are the layers wired, and what is the target design? | [docs/architecture.md](.github/docs/architecture.md) |
| What packages are used and why? | [docs/technology-stack.md](.github/docs/technology-stack.md) |
| What are the entities, value objects, and invariants? | [docs/domain-model.md](.github/docs/domain-model.md) |
| What endpoints exist, with params and responses? | [docs/api-reference.md](.github/docs/api-reference.md) |
| What is in the database? How do I migrate it? | [docs/database.md](.github/docs/database.md) |
| How do I run, configure, and deploy this? | [docs/operations.md](.github/docs/operations.md) |
| What is broken or debt-laden right now? | [docs/known-issues.md](.github/docs/known-issues.md) |
| What does "aggregate", "VO", "bounded context" mean here? | [docs/glossary.md](.github/docs/glossary.md) |
| Why was a decision made? | [docs/adr/](.github/docs/adr/) |
| What refactor is planned? | [plans/001-clean-architecture-refactor.md](.github/plans/001-clean-architecture-refactor.md) |

---

## 8. Current state and known gaps (short version)

- Domain entities are anemic: public setters, `partial`, `CreatedAt` on
  `TechStack`, no invariants on `Project`/`Recommendation.Description`.
- Value objects (`Role`, `Company`, `Period`) are classes → reference equality.
- Domain entities are serialized directly to clients (no DTO boundary).
- All errors are `502` with a string.
- `Period.FinishedAt` is always `null` after mapping → past roles report a
  duration that grows daily. (The value object itself is correct; the mapping
  is not.)
- No logging, no global exception handler, zero cancellation.
- Auth is effectively decorative: `RequireAuthorization()` is commented out on
  every portfolio and Notion endpoint, and absent entirely from `/skill`.

Recently fixed: the `Persistence` → `Application` dependency violation, the
dead code, the duplicated repository fields, the untracked read queries, the
hardcoded user-secrets GUID, and the un-gated startup migration. Tests now
exist but are thin — there is no mapping test project and no endpoint test
project.

Full reasoning and verification steps:
[`.github/plans/001-clean-architecture-refactor.md`](.github/plans/001-clean-architecture-refactor.md).

---

## 9. Definition of done

A change is done when **all** of these are true:

- [ ] `dotnet build src/JDB_Portfolio_API.slnx` has **zero warnings**
      (treat warnings as errors once `Directory.Build.props` exists).
- [ ] `dotnet test` passes, and new behaviour has a test that failed first.
- [ ] Dependency direction is unchanged or improved.
- [ ] No secret, connection string, or token added to source.
- [ ] Endpoint changes update `docs/api-reference.md` and Swagger metadata.
- [ ] Schema changes are a **new** migration, reviewed, and reflected in
      `docs/database.md`.
- [ ] Failure paths return `ProblemDetails` with a correct status code.
- [ ] `docs/known-issues.md` updated if the change removes or adds debt.

---

## 10. Ask before you

Stop and ask the user before:

- adding or upgrading a NuGet package;
- adding a **write endpoint** (there is no `IUnitOfWork` yet);
- changing the database schema, or editing a migration;
- changing the auth model, CORS allow-list, or any secret handling;
- choosing an aggregate boundary or introducing a new project;
- deleting a domain type that is referenced by a migration or a published API
  response shape;
- reformatting files unrelated to the task.

---

## 11. House style for agents

- Match the surrounding code; do not introduce a second pattern for the same
  problem.
- Prefer the smallest diff that satisfies the task. No drive-by refactors.
- If you find a defect outside the task scope, **record it** in
  `docs/known-issues.md` (or the active plan) instead of fixing it silently.
- When you make a non-obvious decision, add an ADR in `docs/adr/`.
- Cite file paths and line numbers when explaining a change.
