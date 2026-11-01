# AGENTS.md

Operating guide for AI agents and humans working in **JDB_Portfolio_API**.

This file is the entry point only. Details live in [`.github/`](.github/) so this
document stays short and does not rot.

**Read order for any task:** this file → `.github/rules/` for the area you touch →
`.github/docs/` for context → `.github/plans/` only if an active plan applies.

---

## 1. What this project is

A **read-only portfolio API** for a personal CV site. One bounded context:
**Portfolio**. It serves experience, projects, tech stacks, recommendations, a
dashboard overview, and Notion page content as JSON.

Current status:

- Read path is complete and deployed (Azure Web App).
- **No write endpoints exist.** `AddNewAsync` was removed; `IRepository<T>` exposes
  only `GetAllAsync`.
- DDD/clean-architecture refactor is complete. Current architecture, domain,
  database, and API behavior is documented under [`.github/docs/`](.github/docs/);
  lasting decisions are recorded in [`.github/docs/adr/`](.github/docs/adr/).
- **Tests cover every invariant.** Four xUnit projects cover the Domain entities and
  value objects, the application services, the DTO mapping, the AutoMapper profile,
  the endpoint contracts and the Notion adapter chain.

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
    → PortfolioDtoMapper       (Domain entity → DTO)
  → Results.Ok(DTO)            ← the domain stops here
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
  JDB_Portfolio_API.Application.Tests/  services with hand-rolled fakes, DTO mapping
  JDB_Portfolio_API.Persistence.Tests/  the AutoMapper profile
  JDB_Portfolio_API.Api.Tests/          endpoint contracts via WebApplicationFactory
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
# EF uses PortfolioContextFactory, not the app host, so it needs the id:
#   export JDB_USER_SECRETS_ID=<id from src/JDB_Portfolio_API/*.csproj>
# Without it every EF command reports a null connection string (docs/database.md §5).

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
| `Github:Token` | GitHub API access for repository metadata |
| `Cors:AllowedOrigins`, `Cors:DevelopmentOrigins` | browser origins; the second is applied in Development only |
| `Swagger:Enabled` | optional; `true` exposes `/swagger` outside Development (default `false`) |

---

## 6. Non-negotiable rules

Each rule has a full explanation in `.github/rules/`.

1. **Dependency direction** — never add a reference that points outward.
   [architecture.md](.github/rules/architecture.md)
2. **Domain purity** — no EF attributes, no `partial`, no `= null!`, no
   `CreatedAt`/`UpdatedAt`, no vendor types in Domain.
3. **Endpoints return DTOs, not domain entities.** No `Domain.Entities` or
   `Domain.ValueObjects` type may appear in
   an endpoint signature or a response.
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
12. **Terminal commands are supervised.** Read-only and local commands are
    routine. Anything that writes to a database, changes git history, deletes
    work, installs outside the project, or starts a long-running process needs
    the owner's go-ahead first. Approval to write a migration is not approval
    to apply it. [terminal-commands.md](.github/rules/terminal-commands.md)

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
| What work is planned? | [`.github/plans/`](.github/plans/) (active plans only) |

---

## 8. Current state and known gaps (short version)

- Domain entities now validate what they hold: `Project`, `Recommendation`,
  `TechStack` and `TechStackCategory` have validating constructors, no `partial`, no
  `= null!`, no audit columns, and `Experience.Projects` is a read-only collection.
  `Project.Id` is get-only. The aggregate boundary is **settled** (3.4, ADR-0003):
  `Project` is the root, `Experience.Projects` is a read-only projection, and
  `Project.TechStacks` holds `TechStackRef` records rather than shared entities. Only
  remaining debt here: `Company`/`Role` do not trim, so `" Acme"` and `"Acme"` are
  different values (KI-044).
- Value objects (`Role`, `Company`, `Period`) are `record`s with value equality and
  validating constructors; `Period.IsOngoing` names what `FinishedAt == null` means.
- All errors used to be `502` with a leaked exception message; they are now typed
  (`ErrorKind`) and mapped to `404/400/401/409/503/502/500` with a machine-readable
  `code`, in one place (`Endpoints/ApiResult.cs`).
- `Period.FinishedAt` is `null` in the mapping only when the column is `null`,
  which correctly means the role is ongoing — **but the existing rows have not
  been backfilled**, so past roles still report a growing duration.
- No structured logging sink and no correlation id beyond `traceId`; failures do reach
  `ILogger` now (2.4), and a global handler turns stray exceptions into
  `ProblemDetails`.
- Auth is a **service-token flow, not user authentication**: one shared key and a
  `sub` of `portfolio-api`. Only `/auth/me` requires a token; portfolio, Notion,
  and GitHub read endpoints are anonymous **on purpose**. GitHub routes return
  public repository data only. Optional authenticated private-repository access
  remains unimplemented; see [plan 002](.github/plans/002-github-visibility-auth.md).

Recently fixed: the `Persistence` → `Application` dependency violation, the
dead code, the duplicated repository fields, the untracked read queries, the
hardcoded user-secrets GUID, the un-gated startup migration, and **the DTO
boundary** — endpoints return DTOs, value objects are flattened, and
`createdAt`/`experienceId`/the `TimeSpan` duration no longer reach the client.
The `finished_at` column and its mapping were added, so a past role can report a
frozen duration — **but the existing rows still need their end dates
backfilled**. Errors are typed and mapped to the right status in one place;
`CancellationToken` runs end-to-end; unhandled exceptions become `ProblemDetails`;
services log their failures. The CORS allow-list is configuration now, with localhost
gated to Development, no credentials flag, and only `GET`/`POST`. Tests now cover
entity invariants, value-object equality, endpoint contracts and the CORS policy.

Current implementation details are in [`.github/docs/`](.github/docs/), with
design rationale in [`.github/docs/adr/`](.github/docs/adr/).

---

## 9. Definition of done

A change is done when **all** of these are true:

- [ ] `dotnet build src/JDB_Portfolio_API.slnx` has **zero warnings**
      (`Directory.Build.props` makes warnings build failures).
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
- **applying a migration** (`dotnet ef database update`) or writing to a
  database with any other means;
- running a terminal command that changes git history, deletes work, installs
  outside the project, or starts a long-running process
  ([terminal-commands.md](.github/rules/terminal-commands.md));
- changing the auth model, CORS allow-list, or any secret handling;
- choosing an aggregate boundary or introducing a new project;
- deleting a domain type that is referenced by a migration or a published API
  response shape;
- reformatting files unrelated to the task.

---

## 11. House style for agents

- **Keep responses short.** Lead with the answer, then at most five bullets. For a
  step, phase, or technical discussion: what it fixes, what it does not, the blocker,
  the recommendation. No preamble, no restating, no essay — expand only when asked.
- Match the surrounding code; do not introduce a second pattern for the same
  problem.
- Prefer the smallest diff that satisfies the task. No drive-by refactors.
- If you find a defect outside the task scope, **record it** in
  `docs/known-issues.md` (or the active plan) instead of fixing it silently.
- When you make a non-obvious decision, add an ADR in `docs/adr/`.
- Cite file paths and line numbers when explaining a change.
