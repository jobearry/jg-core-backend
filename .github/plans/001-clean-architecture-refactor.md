# Plan 001 — Clean Architecture & DDD Refactor

**Status:** In progress — decisions D1–D4 resolved; items 1.1, 1.2, 1.3, 1.4, 1.6, 1.7, 1.8, 2.3 and three test projects are complete
**Scope:** `src/JDB_Portfolio_API.Domain`, `.Application`, `.Persistence`, `.ExternalService`, `JDB_Portfolio_API` (Presentation), `tests/`
**Goal:** Bring the solution in line with clean architecture dependency rules and turn the Domain project into an actual domain model rather than a set of persistence shapes.

---

## How to use this document

This plan is written for **verification before implementation**. Every item has four parts:

| Field | Meaning |
|---|---|
| **What** | The concrete change, as checkboxes |
| **Why** | The reasoning — why this is recommended, what breaks if you skip it |
| **How to verify** | A command, query, or observation that proves the claim yourself |
| **Risk / Effort** | How likely the change is to break something, and how long it takes |

Severity tags:

- `BUG` — produces wrong behaviour today
- `LATENT` — wrong, but not reachable until a write path or edge case exists
- `VIOLATION` — breaks a stated architecture rule
- `DESIGN` — judgement call; agree before implementing
- `CLEANUP` — no behavioural change

Nothing here is urgent in the "production is down" sense. The value is that Parts 1–2 remove real defects, and Part 3 is the actual DDD learning exercise.

---

## Summary

The project boundaries are already clean-architecture-shaped: Domain has zero project references, EF entities are kept separate from domain entities, repository ports live in Domain, and the composition root is in the API project. The remaining gaps:

1. One real dependency-rule violation (Persistence → Application).
2. A domain model that is largely anemic, with public setters and persistence concerns leaking in.
3. No DTO boundary at the API edge — domain entities are serialized directly to clients.
4. No tests, no logging, no cancellation, and incorrect error mapping.

| Area | Grade | One-liner |
|---|---|---|
| Project layout / dependency direction | B+ | One real violation, otherwise correct |
| Domain model (DDD) | D+ | Anemic, no invariants, no aggregate boundaries |
| Value objects | C | Classes with reference equality |
| Application layer | C | Pass-through services, no DTO boundary |
| Persistence | B− | Right idea (entity ≠ domain), broken mappings |
| API / minimal APIs | B− | Good endpoint splitting, weak contracts |
| Tests | F | None |

---

## Decisions needed before implementation

Four items are not mechanical. Confirm these first; they change the shape of the work.

| # | Decision | Options | Resolution |
|---|---|---|---|
| D1 | Is `Period.FinishedAt` supported? | (a) add `finished_at` column + entity + mapping, or (b) delete `FinishedAt` and rename `Duration` → `TimeSinceStart` | **(a)** — decided 2026-10-22. The model already claims to support it, and `Duration` is currently wrong for past roles |
| D2 | TechStack reverse mapping | (a) resolve category id via lookup, or (b) delete unused reverse mappings | **(b)** — decided 2026-10-22. No write path exists; add it back with a test when one does |
| D3 | Aggregate boundary | (a) Experience owns Project for writes, or (b) Project is an independent aggregate | **(b)** — decided 2026-10-22. A project may exist with **no** experience, and an experience may have **no** projects. The `Experience` → `Project` link is optional, so `ProjectEntity.ExperienceId` stays nullable. `Experience.Projects` becomes a read-only projection; the Project aggregate owns attach/detach |
| D4 | DTOs at the API edge | (a) introduce DTOs now, or (b) accept domain-as-contract | **(a)** — decided 2026-10-22. This is a learning project and it is the most commonly skipped step |

### Consequences of D3 (b)

- `Project` is the aggregate root: `IPortfolioProjectRepository` is the write entry point, `IPortfolioExperienceRepository` is not.
- `Experience.Projects` changes from `List<Project>?` with a public setter to a read-only collection
  (`Project.cs`, `Experience.cs:22`). It is populated by the mapping, never mutated by callers.
- `Project.TechStacks` holds full `TechStack` entities today (`PortfolioMappingProfile.cs:63-70`).
  TechStack is shared reference data with no per-project state, so it must become ids or a small
  `TechStackRef` value object — this holds under D3 (b) and is required by item 3.4 either way.
- A standalone project appears in `GET /api/v1/project` but not under any entry of
  `GET /api/v1/experience`. That is intended, not a defect.

---

# Part 1 — Things that are objectively wrong

Not opinions. Each item is a bug, a dead path, or a stated-rule violation.

## 1.1 — Remove the `Persistence → Application` reference

`VIOLATION` · **Risk:** none · **Effort:** 2 min

**What**

- [x] Delete `using JDB_Portfolio_API.Application.Common;` in `src/JDB_Portfolio_API.Persistence/Repositories/EfPortfolioExperienceRepository.cs:2`
- [x] Delete the `Application` `ProjectReference` in `src/JDB_Portfolio_API.Persistence/JDB_Portfolio_API.Persistence.csproj:28`
- [x] Confirm the build still succeeds

> **Done (2026-10-22).** The `using` was the only mention of `Application` anywhere in the
> `Persistence` project — it was dead. Build: 0 warnings, 0 errors. The project graph now
> matches the rule below exactly (`Domain → nothing`, `Application → Domain`,
> `Persistence → Domain`, `ExternalService → Domain`, `Presentation → all three`).

**Why**

Clean architecture's one hard rule is that dependencies point inward. Persistence is an outer ring; Application is an inner ring. The reference is currently unused — the `using` is dead — but it *permits* Persistence to call `Result<T>`, `AuthDto`, or any application service. Removing a reference that buys nothing costs nothing and makes the boundary enforceable.

**How to verify**

```bash
grep -n "Application" src/JDB_Portfolio_API.Persistence/JDB_Portfolio_API.Persistence.csproj
dotnet build   # after removal
```

**Acceptance:** `JDB_Portfolio_API.Persistence` has no reference to `JDB_Portfolio_API.Application`.

---

## 1.2 — TechStack reverse mapping maps `string` → `int`

`LATENT` · **Risk:** low · **Effort:** 10 min

**What**

- [x] `PortfolioMappingProfile.cs:38` — `.ForMember(dest => dest.TechStackCategoryId, opt => opt.MapFrom(src => src.Category))`
- [x] Either resolve the category id via a lookup, or **delete the reverse mapping** (see D2)

> **Done (2026-10-22), with a deviation from D2's wording.** The bad assignment is gone.
> Instead of deleting the whole `CreateMap<TechStack, TechStackEntity>`, the category id is
> explicitly ignored:
>
> ```csharp
> .ForMember(dest => dest.TechStackCategoryId, opt => opt.Ignore())
> ```
>
> **Why not delete the map:** a deleted map and a broken map both throw
> `AutoMapperMappingException`, so a test cannot tell them apart. With the explicit
> ignore the test is a clean red→green one — the id must be `0`, never garbage — and a
> future write path fails loudly on the missing category rather than writing a wrong id.
>
> `tests/JDB_Portfolio_API.Persistence.Tests/TechStackEntityMappingTests.cs` proves it.
> The test was written first and failed with
> `FormatException: The input string 'Frontend' was not in a correct format`.
>
> The category id is resolved by a lookup when a write path exists.

**Why**

`TechStack.Category` is a `string`; `TechStackEntity.TechStackCategoryId` is an `int`. This compiles because AutoMapper resolves members by name at runtime. The moment a write endpoint maps `TechStack → TechStackEntity`, it either throws or writes garbage. It is harmless today *only because no write path exists* — every domain → entity mapping is currently unused.

**How to verify**

Add a throwaway xUnit test (or a scratch console call):

```csharp
var cfg = new MapperConfiguration(c => c.AddProfile<PortfolioMappingProfile>());
var mapper = cfg.CreateMapper();

var stack = new TechStack { Id = 1, StackName = "Angular", Category = "Frontend" };
mapper.Map<TechStackEntity>(stack);   // observe the throw
```

Note: `AssertConfigurationIsValid()` will **not** catch this. It validates that destination members are *assigned*, not that the values convert. You need the runtime map.

---

## 1.3 — `Period.FinishedAt` is discarded on read, so `Duration` grows forever

`BUG` · **Risk:** medium (schema change) · **Effort:** 1–2 h

**What**

- [x] `PortfolioMappingProfile.cs:92` — `new Period(src.StartedAt, null)` hardcodes `FinishedAt` to `null`
- [x] `ExperienceEntity` has no `FinishedAt` column at all
- [x] Fix per D1: add the column, or remove `FinishedAt` from `Period`

> **Done (2026-10-22).** Decision D1 (a): the column was added.
>
> - `ExperienceEntity.FinishedAt` (`DateTime?`) and the `finished_at` column,
>   `timestamp without time zone` — matching every other timestamp in the schema.
> - New migration `20261022064321_AddExperienceFinishedAt`. One nullable column, no
>   default, no data statement. Applied to the local database.
> - The mapping now reads `new Period(src.StartedAt, src.FinishedAt)`, and the reverse
>   map carries `FinishedAt` too.
> - Regression tests in `tests/JDB_Portfolio_API.Persistence.Tests`:
>   `CarriesFinishedAt` and `WithFinishedAt_MeasuresDurationToTheEndDate`.
>
> **The schema change alone does not fix the reported behaviour.** A `NULL`
> `finished_at` means *the role is ongoing* — that is the correct meaning, and the
> current role must stay `NULL`. The past roles need their **end dates backfilled**,
> which is data work, not code work. Until that happens the API still reports growing
> durations for those roles.
>
> **A second, larger fix is still owed: the columns are `timestamp without time
> zone` (KI-019).** A `DateTime` with no offset is ambiguous — `new Date(...)` in
> JavaScript reads `"2024-06-01T00:00:00"` as *local* time, and the same string with
> a `Z` is *UTC*. The duration arithmetic is only correct as long as both ends are
> interpreted the same way. Migrating to `timestamptz` is a separate item and is
> **not** part of this one.
>
> Verified against the real database: all four roles returned
> `finishedAt: null` with durations of 119, 1269, 1530 and 3115 days — the bug,
> reproduced on demand. After the backfill the same endpoint reports the frozen
> duration for each past role.

**Why**

`Period.Duration` computes:

```csharp
var effectiveEnd = FinishedAt ?? DateTime.UtcNow;
```

Because `FinishedAt` is always `null` after mapping, **every past role is reported as still ongoing**, and its duration increases every time the API is called. This is observable today, not a future concern — any consumer computing tenure gets "today − start date".

**How to verify**

```bash
grep -rn "FinishedAt" src/JDB_Portfolio_API.Persistence/
```

→ appears only in the mapping (as `null`), never in the entity or migrations.

Then call `GET /api/v1/experience` and compare a past role's duration against the real end date. You can also assert it directly:

```csharp
var p = new Period(new DateTime(2020, 1, 1), null);
// Duration == today - 2020-01-01, regardless of when the role actually ended
```

---

## 1.4 — `Project.Period` is a domain property with no storage and no mapping

`CLEANUP` (with a data-loss risk) · **Risk:** low · **Effort:** 15 min

**What**

- [x] `Domain/Entities/Project.cs:19` — `public Period? Period { get; set; }`
- [x] `ProjectEntity` has no period columns; the profile has no rule for it
- [x] Delete it, or add the columns

> **Done (2026-10-22) — deleted.** The domain property is gone from
> `Domain/Entities/Project.cs`, along with the now-unused `ValueObjects` import.
>
> No columns were added, because the field was never displayed: the DTO layer
> (item 2.1) drops it from the wire shape anyway, and it was always `null` in every
> response. A `Project` has no dates of its own — a project's dates belong to the
> role it sits under.
>
> **Wire effect:** the `"period": null` key no longer appears in
> `GET /api/v1/project`. It was always `null`, so no client could have rendered
> anything from it.

**Why**

A property that can be set on the domain object but never persists is worse than an absent one: it looks supported and silently drops data. If a future write path maps `Project → ProjectEntity`, the value vanishes without error.

**How to verify**

```bash
grep -n "Period" src/JDB_Portfolio_API.Persistence/Entities/ProjectEntity.cs      # no match
grep -n "Period" src/JDB_Portfolio_API.Persistence/Profiles/PortfolioMappingProfile.cs
```

→ `Period` appears only for `Experience` (`src.Duration.StartedAt`) and `Experience`'s constructor. Never for `Project`.

---

## 1.5 — Empty-string rows crash the collection mapping

`LATENT` · **Risk:** low · **Effort:** 30 min

> **Correction to the first draft of this plan:** it claimed `CareerPosition` was a nullable column. It is **not**. Both `career_position` and `role` are `NOT NULL` in the migration snapshot (EF Core infers required from the non-nullable `string` properties). The real risk is an **empty string**, which `NOT NULL` permits but `new Role("")` rejects.

**What**

- [ ] `PortfolioMappingProfile.cs:125` — `new Role(src.CareerPosition)` throws on `""` / whitespace
- [ ] Same for `new Role(src.Role)` at line 93 (`Experience`)
- [ ] Add `CHECK (career_position <> '')` / `CHECK (role <> '')` + clean existing data, **or** handle defensively in the mapping

**Why**

`Role`'s constructor rejects `IsNullOrWhiteSpace`. The DB guarantees non-null and ≤ 50 chars, but not non-empty. One row with `career_position = ''` — very plausible from a data import or Notion sync — throws `ArgumentException` inside AutoMapper, which surfaces as a 500 for the **entire collection endpoint**. One bad row takes down the list.

**How to verify**

```sql
UPDATE recommendation SET career_position = '' WHERE id = <some id>;
```

Then `GET /api/v1/recommendation`. Revert the row afterwards. The DB constraint fix is preferable to weakening the value object.

---

## 1.6 — Read queries track entities and use a 4-level join

`CLEANUP` (performance) · **Risk:** none · **Effort:** 15 min

**What**

- [x] Add `.AsNoTracking()` to all read queries in `src/JDB_Portfolio_API.Persistence/Repositories/*.cs`
- [x] Add `.AsSplitQuery()` to `GetAllExperienceWithProjectsAsync` (4-level include)

> **Done (2026-10-22).** `.AsNoTracking()` on all five read paths. `.AsSplitQuery()` on the two
> multi-level includes: `GetAllExperienceWithProjectsAsync` (4 levels) and
> `GetAllProjectsWithTechStacksAsync` (3 levels). The single-level `GetAllTechStacksAsync`
> keeps a plain `Include` — one join, no cartesian fan-out. Verified by running every
> endpoint against a reachable database: all queries execute and return correctly.

**Why**

- Every read goes through EF's change tracker. Nothing is saved in these endpoints, so the snapshotting and identity-map overhead is pure waste.
- The experience query does `Include → ThenInclude → ThenInclude → ThenInclude`. EF generates one JOIN, so 5 tech stacks × 3 categories per project produces a cartesian row explosion for a handful of objects.

**How to verify**

Set `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` in `appsettings.Development.json`, call `/api/v1/experience?includeProjects=true`, and count returned rows versus the number of objects. After `.AsSplitQuery()` you get 4 smaller queries instead of one wide one.

**Caveat:** on a portfolio-sized dataset this is milliseconds. Do it because it is free, not because it is urgent.

---

## 1.7 — Dead code

`CLEANUP` · **Risk:** none · **Effort:** 30 min

**What**

- [x] `Domain/Interfaces/IMapper.cs` — never used (AutoMapper is the real mapper)
- [x] `Domain/Interfaces/IService.cs` + `Application/Services/BaseService.cs` — see 2.3
- [x] `Domain/ValueObjects/ExperienceId.cs` — unused, and a `Guid` record that contradicts `Experience.Id` (`int`)
- [x] `Persistence/Entities/NotionEntity.cs` — empty class
- [x] `Application/DTO/PortfolioNotionPage.cs` — duplicate of `NotionPage`, unused
- [x] `GetCountAsync` / `GetByIdAsync` on `IRepository<T>` — never called
- [x] Duplicate `IPortfolioTechStackRepository` registration (`ConfigurationSettings.cs:39` and `:45`)
- [x] Change `List<T?>` returns to `List<T>` — nullable *elements* are meaningless here

> **Done (2026-10-22).** All eight items removed. Notes:
>
> - `BaseService<T>` was **not** dead — all four services inherited it. It was *inverted*:
>   the base held nothing but a passthrough and a duplicate repository field, while each
>   subclass repeated the same `try/catch → Result` block. It was replaced by
>   `PortfolioServiceBase<T, TRepository>` (see 2.3), not simply deleted.
> - `IMapper<,,>` hid behind a name collision: the repositories use **AutoMapper's**
>   `IMapper` (zero type arguments), so a grep for `IMapper` made the dead interface look
>   alive.
> - The `List<T?>` → `List<T>` change also removed **4 of the 5** build warnings in the
>   solution (CS8602 — dereference of a nullable element). The fifth is in
>   `ExternalService` and is unrelated to this item.
> - `GetByIdAsync` was **not** re-added. If a detail endpoint is built later it belongs on
>   the specific repository interface, and it should return a DTO (decision D4), not a
>   domain entity.

**Why**

Each is either never referenced or contradicted by another implementation. Dead abstractions are not neutral — the next reader interprets them as intent. `ExperienceId` in particular implies you have typed IDs, which you do not. The duplicate registration is silent (last-wins, same implementation) but signals a copy-paste error.

**How to verify**

```bash
grep -rn "IMapper<"        src --include=*.cs | grep -v /obj/
grep -rn "ExperienceId"    src --include=*.cs | grep -v /obj/ | grep -v Migrations
grep -rn "NotionEntity"    src --include=*.cs | grep -v /obj/
grep -rn "PortfolioNotionPage" src --include=*.cs | grep -v /obj/
grep -rn "GetCountAsync\|GetByIdAsync" src --include=*.cs | grep -v /obj/
```

Each returns only the declaration plus the base implementation that nothing calls.

**Caveat:** keep `GetByIdAsync` if you plan detail endpoints — but then it belongs on the specific repository interface, not the generic base.

---

## 1.8 — Persistence misc

`BUG` (startup race) · **Risk:** low · **Effort:** 20 min

**What**

- [x] Stop hardcoding a developer's user-secrets GUID in `Persistence/Contexts/PortfolioContextFactory.cs:14`; read from an env var or rely on `dotnet ef --startup-project`
- [x] Gate `dbContext.Database.Migrate()` in `Program.cs:26` behind a config flag

> **Done (2026-10-22).**
>
> - The design-time factory now reads the user-secrets id from the `JDB_USER_SECRETS_ID`
>   environment variable and the connection string from
>   `ConnectionStrings__JDBConnection`, falling back to `appsettings.Development.json`.
>   The hardcoded GUID is gone. `appsettings.Development.json` is now `optional: true`, so
>   the factory no longer throws when it is run from a directory that has no such file.
> - `Migrate()` is gated behind `Database:AutoMigrate` (`appsettings.json`).
>
> **The flag defaults to `true`, deliberately.** The deploy workflow
> (`.github/workflows/main_jdb-portfolio-api.yml`) builds, publishes and deploys — it has
> **no migration step**. Turning the default off would silently stop applying migrations
> in production. The race condition therefore still exists by default and is now
> *controllable* rather than *fixed*; see KI-038 for the follow-up (give CI a migration
> step, then default the flag to `false`).
>
> Verified: with `Database__AutoMigrate=false` EF issues **no** query against
> `__EFMigrationsHistory`; with `true` it runs the migration check as before.

**Why**

- The GUID is machine-specific. Any other developer running `dotnet ef` gets a context with a null connection string and a confusing failure.
- `Migrate()` at startup means every replica races to apply migrations simultaneously. On a single instance it is fine; the moment you scale or add a deploy slot it is a deadlock/lock-contention bug. It also means a bad migration takes down startup rather than failing a separate step.

**How to verify**

```bash
grep -n "AddUserSecrets" src/JDB_Portfolio_API.Persistence/Contexts/PortfolioContextFactory.cs
# ask a second machine to run: dotnet ef migrations list
```

---

# Part 2 — Boundaries, errors, robustness

Design changes. Recommended, but agree with each before implementing.

## 2.1 — Introduce DTOs at the API edge

`DESIGN` · **Risk:** medium (touches every endpoint) · **Effort:** half a day

**What**

- [ ] Add `Application/DTO/`: `ExperienceDto`, `ProjectDto`, `TechStackDto`, `RecommendationDto`, `DashboardItemDto`, `NotionPageDto`
- [ ] Map domain → DTO inside the application services (or introduce query handlers)
- [ ] Do **not** expose `CreatedAt`, `UpdatedAt`, `ExperienceId`, or EF-shaped fields
- [ ] Update all endpoints to return DTOs
- [ ] Add `.Produces<T>(200)` / `.ProducesProblem(...)` / `.WithName(...)` to each endpoint

**Why**

`Results.Ok(result.Value)` returns domain objects, so **the domain model is the wire contract**. Consequences:

- Every Part 3 refactor becomes a breaking API change.
- Public setters on `Project` exist partly because nothing forces a separate contract.
- `createdAt`, `experienceId`, and nested `techStacks[].createdAt` are published whether or not you want them.
- You already have an unused `Application/DTO/PortfolioNotionPage.cs` — the intent was there.

**How to verify**

```bash
grep -rn "Results.Ok(result.Value)" src/JDB_Portfolio_API/Endpoints/
```

Six occurrences — that is the whole leak. Then look at the JSON from `/api/v1/project` and ask whether you would commit to that shape forever.

**Counter-argument:** for a portfolio with no external consumers, DTOs are ceremony. This is a learning project, and it is the most commonly skipped step. Recommendation: do it (D4).

**Acceptance:** no `Domain.Entities` or `Domain.ValueObjects` type appears in an endpoint signature or response.

---

## 2.2 — Typed errors and correct status codes

`BUG` + `DESIGN` · **Risk:** medium · **Effort:** half a day (with 2.4)

**What**

- [ ] Replace `Result<T>.Failure(string)` with a typed error (`Error.NotFound` / `Validation` / `Infrastructure` / `Conflict`)
- [ ] Add a single `Result → IResult` / `ProblemDetails` mapper in the API layer
- [ ] Remove per-endpoint `statusCode: 502`

**Why**

Every endpoint does:

```csharp
if (!result.IsSuccess) return Results.Problem(result.Error, statusCode: 502);
```

502 means "bad gateway — an upstream server returned an invalid response". A database failure is not a bad gateway; it is a 500 (or 503). A missing record is a 404. Clients cannot distinguish "DB down" from "Notion down" from "bad input". The string-only error also carries no machine-readable code, so the frontend must string-match.

**How to verify**

```bash
# Stop your PostgreSQL instance, or point the API at an unreachable host:
ConnectionStrings__JDBConnection="Host=127.0.0.1;Port=1;Database=jg-portfolio;Username=u;Password=p" \
  dotnet run --project src/JDB_Portfolio_API
curl -i http://localhost:5254/api/v1/experience
```

You will get an **unhandled 500** from the Npgsql exception, not the 502 the code implies — which also proves the `catch (HttpRequestException)` in the services is the wrong catch. Restart the database or the API afterwards.

---

## 2.3 — Delete `BaseService<T>` / `IService<T>`

`DESIGN` + `CLEANUP` · **Risk:** low · **Effort:** 1 h

**What**

- [x] Delete `Application/Services/BaseService.cs` and `Domain/Interfaces/IService.cs`
- [x] Each service keeps one repository field only
- [x] Remove the unreachable fallbacks `await _repo.X() ?? await _repo.GetAllAsync()` in `PortfolioRecommendationService.cs:22` and `PortfolioSkillService.cs:22`

> **Done (2026-10-22), with one deliberate deviation.**
>
> `BaseService<T>` was **not** deleted outright — it was **replaced** by
> `Application/Services/PortfolioServiceBase.cs`. The original base was *inverted*: it held
> the empty part (a passthrough `GetAllAsync`, plus a `public readonly` duplicate of the
> repository) while all four subclasses repeated the same `try/catch → Result` block. The
> replacement holds the behaviour that was actually shared:
>
> ```csharp
> public abstract class PortfolioServiceBase<T, TRepository>
>     where T : class
>     where TRepository : IRepository<T>
> {
>     protected TRepository Repository { get; }
>     protected async Task<Result<TValue>> ExecuteAsync<TValue>(
>         Func<Task<TValue>> operation, string operationDescription) { ... }
> }
> ```
>
> - Two type parameters are needed: the base holds the **concrete** repository port, so the
>   subclass no longer needs a second field for it. One field per service, as required.
> - The repository is now `protected` and read-only — nothing can bypass the service.
> - `IService<T>` was deleted from `Domain`. Domain no longer knows that services exist.
> - The `??` fallbacks were removed. A repository returns an empty list, never `null`, so
>   the fallback was unreachable.
> - The catch is now `catch (Exception ex) when (ex is not OperationCanceledException)`
>   instead of `catch (HttpRequestException)`. The old catch could never fire for a
>   database failure — EF/Npgsql throw `DbException`/`NpgsqlException`/`InvalidOperationException`
>   — so a database outage escaped as an unhandled 500 instead of the intended failure
>   result. Cancellation is explicitly **not** swallowed.
>
> **Still open, and deliberately so:**
>
> - The failure message is still a **string** and the endpoints still return **502**.
>   That is item 2.2, which introduces typed errors.
> - The base does **not** log. That is item 2.4, which introduces `ILogger<T>`. Adding
>   `Microsoft.Extensions.Logging.Abstractions` to the Application project needs its own
>   package approval.
> - `PortfolioService` (the overview/Notion service) was **not** migrated to the new base.
>   It calls Notion as well as three repositories, and its `catch (HttpRequestException)`
>   is *correct* for the Notion call. Its three database calls are still mis-caught; that
>   is item 2.4.
>
> Verified by `tests/JDB_Portfolio_API.Application.Tests` — a repository failure becomes a
> failed `Result` instead of an escaping exception, and cancellation still propagates.
> The test was mutation-checked: disabling the catch makes it fail.

**Why**

- **It adds zero behaviour.** `GetAllAsync()` is a pure passthrough — no validation, mapping, caching, logging, or transaction.
- **Every subclass stores the same instance twice.** `_repo` (from the base) and the concrete field are the same scoped instance.
- **It is not needed for the one real use.** The concrete interfaces inherit `IRepository<T>`, so `_concreteField.GetAllAsync()` works identically.
- **Two of the four uses are dead.** A repository returns an empty list, never `null`.
- **`public readonly IRepository<T> _repo` breaks encapsulation** — anything holding the service can bypass it. Underscore-prefixed but public is actively misleading.
- **It drags an application concept into Domain.** `IService<T>` is implemented only by `BaseService`; Domain should not know that "services" exist.

**How to verify**

```bash
grep -rn "_repo" src/JDB_Portfolio_API.Application/Services/
```

You will see the same instance stored twice per service. Then check each `_repo.X()` call — every one can use the concrete field.

**Caveat:** do this **together with** removing the `??` fallbacks, otherwise you delete the base class and leave dangling references.

**When a base service would be justified:** if it held real shared behaviour, e.g. `protected Task<Result<T>> ExecuteAsync(Func<Task<T>>)` that logs, catches, and wraps in `Result`. Then make it `abstract`, `_repo` `protected`, and it would be doing actual work.

---

## 2.4 — Logging, global exception handler, cancellation

`DESIGN` · **Risk:** medium · **Effort:** half a day

**What**

- [ ] Inject `ILogger<T>` into application services; log every failure with context
- [ ] Add a global exception handler (`IExceptionHandler` / middleware) returning `ProblemDetails`
- [ ] Thread `CancellationToken` end-to-end: endpoint → service → repository → `ToListAsync(ct)`
- [ ] Remove the `catch (HttpRequestException)` around database calls (wrong exception type)

**Why**

- **No logging exists** in Domain/Application/Persistence. A failure either escapes to the default handler (generic 500, no context) or is caught and converted to a string. You cannot diagnose a production failure.
- **No global handler** means unhandled exceptions produce inconsistent output instead of `application/problem+json`.
- **No `CancellationToken`** means a client navigating away still runs the query to completion, holding the connection. Minimal APIs hand you the token for free.
- The services catch `HttpRequestException` around **EF/Npgsql** calls. That exception type never comes from the database — the catch is dead and creates a false sense of safety.

**How to verify**

Trigger the DB-down case from 2.2 and inspect the log output: ASP.NET's request log appears, but no application-level context (which endpoint, which repository, what data). For cancellation: add a temporary `Task.Delay(10_000)`, cancel the request in the browser, and watch it run to completion server-side.

**Priority:** handler + logging matter more than cancellation for a portfolio. Cancellation is a habit worth building.

---

## 2.5 — Endpoint consistency

`BUG` + `DESIGN` · **Risk:** low · **Effort:** 2–3 h

**What**

- [ ] `PortfolioSkillEndpoints.cs:12` never checks `IsSuccess` and returns 200 with `null` on failure
- [ ] Replace hand-spelled `"api/v1/..."` strings in seven files with `app.MapGroup("api/v1")`
- [ ] Add input validation for `AuthDto` (minimal APIs do not run model validation)
- [ ] Move JWT issuance out of `AuthEndpoints.cs` into an `IAuthTokenService` in Application
- [ ] Fix the `async` `/auth/me` lambda with no `await` (CS1998)
- [ ] Decide whether auth is real: all `RequireAuthorization()` are commented out, the token has no `sub`, so `User.Identity.Name` is null

**Why**

- **Missing check:** the skill endpoint returns `200 OK` with `null` on failure while its siblings return errors. A client cannot tell success from failure.
- **Route strings:** `"api/v1/..."` is spelled out in 5 files. Changing a prefix or adding v2 means 5 edits and a guaranteed miss.
- **Validation:** `AuthDto` has no constraints. An empty `Key` is accepted (fixed-time comparison, so it fails safely, but there is no 400 with a useful message).
- **JWT in the endpoint:** token construction is application logic — ~40 lines in a presentation file, untestable without HTTP.
- **`async`/no `await`:** CS1998 plus an unnecessary state machine.
- **Decorative auth:** the security surface looks real but is inert. Either wire it up or rename `/auth/login` → `/auth/token` and document it as a service token.

**How to verify**

```bash
dotnet build                                  # CS1998 warning
grep -rn "api/v1" src/JDB_Portfolio_API/Endpoints/   # 5 files
grep -rn "RequireAuthorization" src/JDB_Portfolio_API/Endpoints/
```

Then `curl` the skill endpoint with the DB down → 200 with an empty body.

---

# Part 3 — The actual DDD work

This is the part that answers "am I doing DDD?". Everything here is a design choice — push back on any of it.

## 3.1 — Value objects must have value equality

`DESIGN` (model correctness) · **Risk:** low · **Effort:** 1 h

**What**

- [ ] `public sealed record Role`
- [ ] `public sealed record Company`
- [ ] `public sealed record Period`
- [ ] `Company` maps `null`/whitespace to `""` — either reject it (`ArgumentException`) or make it optional at the aggregate level and remove the empty-string concept
- [ ] `Period` invariants: `StartedAt` not in the future, `FinishedAt >= StartedAt` (already present), and settle what `FinishedAt == null` means

**Why**

A value object is *defined by its value*. Yours are classes with no `Equals` override, so equality is reference-based:

```csharp
new Role("Developer") == new Role("Developer")   // false today
```

That means you cannot write `if (experience.Role == Role.Developer)`, cannot use them as dictionary keys, and cannot compare a mapped-from-DB role to a constructed one. You have validation wrappers, not value objects.

**How to verify**

No test needed — there is no `Equals`/`GetHashCode`/`operator ==` in any of the three files:

```bash
grep -n "Equals\|GetHashCode\|record" src/JDB_Portfolio_API.Domain/ValueObjects/*.cs
```

**Why `record class`, not `record struct`:** `record class` keeps `null` meaningful for `Company?` and gives `with` expressions. `record struct` removes allocation but changes nullability semantics. For three types on a portfolio API, `record class` is correct. `Period` has `DateTime?` members, so its generated equality is right.

---

## 3.2 — Move non-domain types out of Domain

`DESIGN` · **Risk:** low · **Effort:** 1 h

**What**

- [ ] `Domain/Entities/DashboardItem.cs` → Application (it is a read model)
- [ ] `Domain/ValueObjects/NotionPage.cs` → Application DTO (integration concept)
- [ ] `Domain/Interfaces/INotionService.cs` → `Application/Abstractions/INotionReader`, expressed in application terms
- [ ] Keep in Domain only ports that describe domain needs (the `IPortfolio*Repository` interfaces)

**Why**

- `DashboardItem` is `Title`/`Subtitle`/`Icon`/`Count`/`Description` with **zero behaviour** — a view model for a widget. Nothing in the domain has an invariant about a dashboard tile.
- `NotionPage` is a **vendor** shape. Your domain does not care about Notion. If you swapped Notion for Contentful, the domain should not change — that is the test of a good boundary.
- `INotionService` is named after the vendor and returns the vendor DTO. A domain port should be phrased in domain language.

**How to verify**

Ask "does any domain rule reference this type?"

```bash
grep -rn "DashboardItem" src/JDB_Portfolio_API.Domain/
```

→ only its own declaration. That is the tell.

**Counter-argument:** churn for purity. Worth it because the goal is learning the boundary, not shipping faster.

---

## 3.3 — Give entities invariants and behaviour

`DESIGN` (DDD core) · **Risk:** medium · **Effort:** 1 day

**What**

- [ ] `Project`: private setters, validating constructor/factory, non-empty name
- [ ] `Recommendation`: validate `Description` in the constructor (currently unvalidated while `PersonName` and `CareerPosition` are)
- [ ] `TechStack`: validate name and category; remove `CreatedAt`
- [ ] Remove `partial` and `= null!` from domain entities — those are EF-entity idioms
- [ ] Remove `CreatedAt` / `UpdatedAt` from domain entities (persistence audit columns)
- [ ] `Experience.Projects` has a public setter — make it a read-only collection
- [ ] `Experience.Responsibility` / `.Type` have no mutators — add behaviour or drop them
- [ ] Resolve the identity mismatch: `Experience.Id` is constructor-only, `Project.Id` is a public setter

**Why**

- Today `new Project(1, "")` succeeds, `project.ProjectName = null` succeeds, `recommendation.Description = null` succeeds. An entity that can hold invalid state is a data bag; the "domain" is then a schema with C# syntax.
- `partial` and `= null!` mean the domain was generated from the schema, not modelled.
- `CreatedAt`/`UpdatedAt` are audit concerns; coupling rules to them couples business logic to infrastructure bookkeeping.
- A public `Projects` setter lets any caller replace the collection, bypassing any future rule about which projects may be attached.
- If `Responsibility` and `Type` cannot be set, the aggregate cannot express a third of its own data.

**How to verify**

In a scratch test:

```csharp
new Project(1, "");                                   // succeeds today
new Recommendation(1, "x", null!, new Role("Dev"), DateTime.UtcNow);  // succeeds today
```

After the change both throw.

**Caveat:** private setters plus AutoMapper means **all** construction goes through the factories, including the mapping profile. That is fine — it is also a benefit, because you map entity → domain in AutoMapper rather than letting EF materialize domain types.

---

## 3.4 — Decide the aggregate boundary

`DESIGN` (highest impact) · **Risk:** high · **Effort:** 1–2 days

**What**

- [ ] Choose D3: **Experience owns Project for writes** (recommended), or **Project is an independent aggregate** referenced by a typed `ExperienceId`
- [ ] Enforce it: exactly one repository is the write entry point for the boundary
- [ ] `Project.TechStacks` holds `TechStack` **entities** — replace with ids or a small `TechStackRef` value object
- [ ] Remove `Project.Period` or map it (see 1.4)
- [ ] Add `FinishedAt` to `ExperienceEntity` or drop `Period.FinishedAt` from the read path (see 1.3)

**Why**

An aggregate is a *consistency boundary*: the unit you load and save together, and the only thing a repository should return. You currently have three overlapping boundaries:

- `Experience` contains `List<Project>` (owns them)
- **and** `IPortfolioProjectRepository` returns `Project` as a root
- **and** `Project` contains `List<TechStack>`, where `TechStack` is shared reference data used by many projects

Consequences: no clear transaction scope, no clear home for "a project belongs to at most one experience", and `TechStack` mutation through a project would leak into other projects.

**How to verify**

Answer "how do I create a project?" — there are two ways today. Two answers means no boundary. Also check whether `TechStack` has per-project state: it does not, which proves it does not belong inside `Project`.

**Why this matters most:** it is the difference between DDD-shaped folders and DDD. Everything in 3.3 is downstream of it.

---

## 3.5 — Tests

`DESIGN` (practice) · **Risk:** none · **Effort:** 1 h for the first project

**What**

- [x] `tests/JDB_Portfolio_API.Domain.Tests` (xUnit): VO equality, `Period` validation, entity invariants
- [x] `tests/JDB_Portfolio_API.Application.Tests`: services with fake repositories
- [ ] `tests/JDB_Portfolio_API.Api.Tests`: `WebApplicationFactory` smoke tests per endpoint
- [x] `tests/JDB_Portfolio_API.Persistence.Tests`: the AutoMapper profile

> **Partially done (2026-10-22).** Three test projects exist and `dotnet test` is green
> (32 tests). What they cover today:
>
> - **Domain.Tests** — 19 tests over `Role`, `Company` and `Period`. These are
>   *characterisation* tests: they lock in the value objects' current behaviour so that
>   items 3.1 and 3.3 can change it deliberately.
> - **Application.Tests** — 5 tests over `PortfolioExperienceService`, with a hand-rolled
>   `FakePortfolioExperienceRepository` in `Fakes/`. They cover the shared
>   `ExecuteAsync` behaviour introduced by item 2.3.
> - **Persistence.Tests** — 8 tests over the AutoMapper profile. This is the
>   highest-risk file in the solution, and it now has:
>   - `MappingConfiguration_HasNoUnmappedMembers` — an `AssertConfigurationIsValid()`
>     gate. It found **6 maps with unmapped destination members**, all now explicitly
>     ignored or properly mapped.
>   - Runtime map tests for both directions of the `Experience` and `TechStack` maps —
>     the only way to catch a bad type conversion, which the validity gate cannot.
>   - `PopulatesDescriptionResponsibilityAndType` — this **resolves KI-035**: the
>     private-setter properties *are* populated by AutoMapper. The behaviour was never
>     broken, only unverified.
>
> **Not covered yet, and deliberately so:**
>
> - **Entity invariants** (3.3) are not tested, because the entities do not have
>   invariants yet. Writing those tests red-first is part of item 3.3.
> - **VO equality** (3.1) is not tested, because `Role`/`Company`/`Period` are still
>   classes with reference equality. Same reason.
> - **Endpoint contracts** need `Api.Tests` plus `public partial class Program { }` in
>   the API project.
>
> Package baseline (approved): `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4,
> `Microsoft.NET.Test.Sdk` 17.14.1. The `coverlet.collector` that the template adds by
> default was removed — coverage percentage is not a target.
>
> `Persistence.Tests` also pins `Microsoft.EntityFrameworkCore.Relational` 10.0.10 —
> a version pin to what the solution already uses, not a new dependency. Without it,
> `PrivateAssets="all"` on EF Design stops 10.0.10 flowing to the test project, NuGet
> resolves Npgsql's lower 10.0.4, and MSBuild reports MSB3277.

**Why**

Every change in Parts 1–3 is a refactor, and a refactor without tests is a rewrite you hope works. More importantly for a learning project: tests are how you *prove* invariants exist. `new Project(1, "")` throwing is a test, and writing it forces you to decide what "valid" means.

**How to verify**

```bash
dotnet test    # "no test projects found" — the current state
```

**Pragmatic order:** do not build all three. Start with one Domain test project covering 3.1 and 3.3. That is about an hour and protects the rest.

**Acceptance:** `dotnet test` is green and covers every value object and entity invariant.

---

# Part 4 — Polish (last, or never)

| # | Item | Why | How to verify |
|---|---|---|---|
| 4.1 | Split `ConfigurationSettings.cs` into `AddApplication()` / `AddPersistence()` / `AddExternalServices()` / `AddPresentation()` | Service registrations currently sit inside `AddDatabase`; one method per layer is easier to reason about | `grep -n "AddScoped<Portfolio" ConfigurationSettings.cs` — all inside `AddDatabase` |
| 4.2 | Remove the unused `IConfiguration` parameter from `AddMapper` | A dead parameter implies configuration is used | read the signature, then the body |
| 4.3 | Replace the 150-line `OnModelCreating` with `IEntityTypeConfiguration<T>` classes | One file per entity diffs cleanly and keeps `PortfolioContext` small | `wc -l src/JDB_Portfolio_API.Persistence/Contexts/PortfolioContext.cs` |
| 4.4 | Add `IUnitOfWork` / transaction abstraction before write endpoints | `AddNewAsync` is commented out; you will need it the moment a write exists | `grep -rn "SaveChangesAsync" src` — only in the commented block |
| 4.5 | Add `Directory.Build.props` with `TreatWarningsAsErrors` + analyzers | Catches CS1998-style issues (like the `async` endpoint) at build time | add it, build, see what breaks |
| 4.6 | Fix `NotionClientService.cs`: don't rethrow `new HttpRequestException(msg)` (loses the inner exception); don't use an absolute URL when `BaseAddress` is set; set auth headers in `ConfigureHttpClient`; add retry/timeout | Current code hides the original stack and duplicates configuration | read lines 30–46 |
| 4.7 | Replace the `NotionPageParserUtility` XML trick | `XDocument.Parse("<root>" + markdown + "</root>")` breaks on malformed content or a literal `</root>` | feed it `"<page>unclosed"` |
| 4.8 | Move `PortfolioService.GetOverview` hardcoded copy to config; parallelise the three sequential queries | Display text in the app layer; three independent awaits that could be `Task.WhenAll` | `grep -n "Homelabbing" src` |
| 4.9 | Add `.github/plans/README.md` index if more plans are added | Keeps plans discoverable | `ls .github/plans/` |

---

## Dependency rules to preserve

```
Domain          → (nothing)
Application     → Domain
Persistence     → Domain
ExternalService → Domain
Presentation    → Application, Persistence, ExternalService   (composition root only)
```

No project may reference the Presentation project. Infrastructure projects must never reference Application.

---

## Out of scope

- Rewriting the database schema beyond adding `finished_at` (D1) and the `CHECK` constraints (1.5)
- Adding write endpoints (only the `IUnitOfWork` seam is prepared)
- Replacing AutoMapper or EF Core

---

## Appendix — Suggested verification order

Cheapest first. This is enough to validate the plan without writing any production code.

| Step | Item | Time |
|---|---|---|
| 1 | 1.1 — delete two lines, build | 2 min |
| 2 | 3.1 — grep for `Equals` in value objects | 1 min |
| 3 | 1.3 — call `/api/v1/experience`, compare a past role's duration to reality | 5 min |
| 4 | 1.7 — run the four greps | 2 min |
| 5 | 2.2 — stop Postgres, hit an endpoint, observe the 500 | 5 min |
| 6 | 1.2 — temporary AutoMapper runtime test | 10 min |
| 7 | 3.4 — answer "how many ways can I create a `Project`?" | thinking only |

If all seven confirm, the plan is accurate and Parts 1–3 are safe to schedule.

---

## Changelog

| Date | Change |
|---|---|
| 2026-10-21 | Initial plan (4 phases, checklist only) |
| 2026-10-21 | Restructured into parts with Why / How to verify / Risk per item; added the four open decisions; **corrected** 1.5 — `career_position` is `NOT NULL`, the real risk is an empty string, not null |
