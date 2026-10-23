# Plan 001 — Clean Architecture & DDD Refactor

**Status:** In progress — decisions D1–D4 resolved; items 1.1–1.8, 2.1–2.5, 2.7, 3.1, 3.1a, 3.2, 3.3, 3.4 and 3.5 complete, plus Batch 2 phase 1–3 (DTO boundary + endpoint contracts). **Part 1 and Part 2 are done** (2.6 is a feature, not debt); **Part 3 is complete** (3.2's Notion half closed 2026-10-23, ADR-0005). **Part 4:** 4.1, 4.2, 4.3, 4.5, 4.6, 4.7 and 4.9 are done, and 4.8's config move landed with parallelisation rejected (2026-10-23); 4.4 is deferred until the first write endpoint. `20261022150752_RequireCompanyName` is applied locally and production Neon reports no pending migrations (CI, 2026-10-23).
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

> **Status 2026-10-23:** the four gaps below are closed and Parts 1–3 are complete.
> This summary is the **original assessment (2026-10-21)**, kept as the rationale for
> the work; read the item-level Done notes for the current state. Today: 188 green
> tests, 0 build warnings, and every dependency edge points inward.

The project boundaries are already clean-architecture-shaped: Domain has zero project references, EF entities are kept separate from domain entities, repository ports live in Domain, and the composition root is in the API project. The remaining gaps:

1. One real dependency-rule violation (Persistence → Application).
2. A domain model that is largely anemic, with public setters and persistence concerns leaking in.
3. No DTO boundary at the API edge — domain entities are serialized directly to clients.
4. No tests, no logging, no cancellation, and incorrect error mapping.

| Area | Grade | One-liner |
|---|---|---|
| Project layout / dependency direction | B+ | One real violation, otherwise correct |
| Domain model (DDD) | B− | Entities validate (3.3); boundary settled (3.4) |
| Value objects | B+ | `record`s with value equality and validating constructors (3.1) |
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

- [x] `PortfolioMappingProfile.cs:125` — `new Role(src.CareerPosition)` throws on `""` / whitespace
- [x] Same for `new Role(src.Role)` at line 93 (`Experience`)
- [x] Add `CHECK (career_position <> '')` / `CHECK (role <> '')` + clean existing data, **or** handle defensively in the mapping

> **Resolved 2026-10-22 — in the mapping, and *not* with a constraint.**
>
> The plan offered two options and recommended the `CHECK` constraint. **That
> recommendation was wrong for this domain, and it was withdrawn mid-implementation
> after the owner described the intent:**
>
> > "I treat it as some experience that I don't want to declare my role, and some
> > recommendations don't want their career position to show."
>
> A blank role is therefore **not bad data — it is a legitimate state.** A
> `CHECK (btrim(role) <> '')` would have made a state the owner actually wants
> impossible to express. The migration
> (`20261022095530_AddNonBlankRoleConstraints`) was generated, then removed with
> `dotnet ef migrations remove` before it was ever applied. No schema change.
>
> **What was built instead.** Blank is *interpreted*, not rejected:
>
> | Layer | Before | After |
> |---|---|---|
> | `experience.role` / `recommendation.career_position` | `NOT NULL`, `''` allowed | unchanged — `''` is the sentinel for "not disclosed" |
> | `Experience.Role` / `Recommendation.CareerPosition` | `Role`, non-nullable, threw on null | `Role?` — absence means undeclared |
> | Mapping, entity → domain | `new Role(src.Role)` → `ArgumentException` | blank/whitespace → `null` (`ToRole`) |
> | Mapping, domain → entity | assigned a `Role` to a `string` and relied on AutoMapper | `null` → `''` (`ToStoredRole`) |
>
> `Role` itself is **unchanged and still strict** — it never holds an empty string.
> "Undeclared" is the *absence* of a `Role`, which is a different and more honest
> thing than a `Role` whose text happens to be empty. That distinction is what the
> constraint would have destroyed.
>
> **A second latent bug was found and fixed on the way:** `Recommendation →
> RecommendationEntity` assigned `src.CareerPosition` (a `Role`) to a `string`
> property and left AutoMapper to convert it — the same class of defect as KI-011.
> It is now an explicit `MapFrom(ToStoredRole(...))`, with a test.
>
> **And a third:** `Role` was convention-mapped by name *after* `ConstructUsing`
> had already set it, so the explicit value was overwritten and a blank row still
> threw. Same pattern as KI-040; fixed with an explicit `Ignore` on both maps.
>
> **Tests** — `UndeclaredRoleMappingTests`, 8 cases written red first. The red run
> was the real bug: `System.ArgumentException : Role value cannot be empty.` at
> `Role.cs:13`. Covered: blank/whitespace/whitespace-with-tab → `null`, for both
> `Experience` and `Recommendation`; `null` → `''` on the way back; and a declared
> role surviving the round trip unchanged.
>
> **No migration is needed and none is pending.** The change is entirely in the
> domain model and the mapping; the EF model — and therefore the snapshot — is
> untouched, which is why `experience.role` can stay `NOT NULL`.
>
> **Wire effect:** a role that is not declared now serializes as `"role": null`
> instead of throwing a 500. Verified against the local database: all five
> portfolio endpoints return 200 and the four existing roles are unchanged.

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
> **The flag defaults to `true` in code.** The deploy workflow
> (`.github/workflows/main_jdb-portfolio-api.yml`) now applies migrations against
> production before deploying the app. **Production configuration completed
> (2026-10-23):** the owner confirmed that the Azure App Service application
> setting `Database__AutoMigrate=false` has been applied and production is
> aligned. The workflow continues to migrate on future deployments. See KI-038
> and `docs/operations.md` §5 for the configuration record and recovery steps.
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

- [x] Add `Application/DTO/`: `ExperienceDto`, `ProjectDto`, `TechStackDto`, `RecommendationDto`, `DashboardItemDto`, `NotionPageDto`
- [x] Map domain → DTO inside the application services (or introduce query handlers)
- [x] Do **not** expose `CreatedAt`, `UpdatedAt`, `ExperienceId`, or EF-shaped fields
- [x] Update all endpoints to return DTOs
- [x] Add `.Produces<T>(200)` / `.ProducesProblem(...)` / `.WithName(...)` to each endpoint

> **Done (2026-10-22) — clean cutover, no compatibility window.**

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

> **Done (2026-10-22) — clean cutover, no compatibility window.**
>
> **The DTOs.** Six `sealed record`s in `Application/DTO/`, plus
> `PortfolioDtoMapper` — explicit extension methods (`project.ToDto()`), no
> AutoMapper. ADR-0004 requires the mapping to be hand-written: the accidental
> contract this replaces was produced by convention, and a second
> convention-mapped layer would reproduce the same class of bug one level out.
>
> **The shapes.** `Company` and `Role` flattened to strings; `startedAt` /
> `finishedAt` lifted out of the old `duration` object; `duration` (the
> `TimeSpan` string) replaced by `durationMonths`. Removed: `experienceId`,
> `period`, `techStacks[].createdAt`, `createdAt` on skills.
>
> **`durationMonths` is calendar months, not `TotalDays / 30`.** A 30-day
> divisor drifts — 12 months becomes 11 or 13 depending on the year, and nobody
> can explain the result. `2020-01-31 → 2020-02-28` is `0` (not a full month
> yet), and an ongoing role counts to now. The mapper takes an optional `now`
> so the ongoing case is deterministic in tests instead of clock-dependent.
>
> **Nullability is preserved, not introduced.** `role` and `careerPosition` stay
> nullable end to end: blank in the database → `null` in the domain (item 1.5) →
> `null` on the wire. Nothing became newly optional.
>
> **Verified against the real database,** which is the only check that matters
> here. `/api/v1/experience` now returns
> `"company": "Applied ICT Inc."`, `"role": "Software Engineer"`,
> `"durationMonths": 3`, and top-level `startedAt`/`finishedAt`. All five
> portfolio endpoints return 200; the four roles report 3, 41, 50 and 102 months.
>
> **Tests — 21, in `Application.Tests`.** Two kinds:
>
> - **Wire-shape guards** on the DTO types themselves (`DoesNotExposeCreatedAt`,
>   `DoesNotExposeExperienceId`, `ReplacesTheDurationTimeSpanWithMonths`,
>   `FlattensCompanyAndRoleToStrings`). These are the tests that stop a field
>   creeping back.
> - **One serialization test** that pins the exact camelCase key list and order
>   for `ExperienceDto`, so the contract the frontend sees is asserted rather
>   than assumed.
>
> **Endpoints** gained `.WithName(...)`, `.Produces<T>(200)` and
> `.ProducesProblem(502)`. `/api/v1/skill` deliberately has **no**
> `ProducesProblem`: it still does not check `IsSuccess`, so it never produces a
> problem response. That inconsistency is item 2.5, not 2.1.
>
> **Two carve-outs, deliberate:**
>
> - `PortfolioService.GetOverview` still builds domain `DashboardItem`s and then
>   maps them, and `GetNotionPageAsync` still receives `NotionPage` from
>   `INotionService`. Both types leave the domain in item 3.2; collapsing them
>   now would mean doing that work twice. **No domain type crosses the DTO
>   boundary** — they are internal to Application.
> - `Api.Tests` was split out of this item because it needs
>   `Microsoft.AspNetCore.Mvc.Testing`, a new package. **It is now done** as Phase 3
>   (11 contract tests, approved).
>
> **Finding, recorded not fixed:** `Experience.Responsibility` and `.Type` have
> private setters and **no mutator at all**, so a test cannot populate them. One
> test uses reflection with a comment pointing at 3.3. That is the first
> concrete evidence for item 3.3 ("add behaviour or drop them").
>
> **Client impact:** breaking. The frontend was updated separately, by design.
> `docs/api-reference.md` carries the full before/after key table; the disruptive
> part is `startedAt`/`finishedAt` moving out of `duration`, which is a structural
> change rather than a rename.

---

## 2.2 — Typed errors and correct status codes

`BUG` + `DESIGN` · **Risk:** medium · **Effort:** half a day (with 2.4)

**What**

- [x] Replace `Result<T>.Failure(string)` with a typed error (`Error.NotFound` / `Validation` / `Infrastructure` / `Conflict`)
- [x] Add a single `Result → IResult` / `ProblemDetails` mapper in the API layer
- [x] Remove per-endpoint `statusCode: 502`

> **Done (2026-10-22).**
>
> **The types.** `Application/Common/Error.cs` — `ErrorKind`
> (`NotFound` / `Validation` / `Conflict` / `Infrastructure` / `Upstream` /
> `Unexpected`) and `Error(Kind, Code, Message)`. `Result<T>.Error` is now
> `Error?` instead of a string. `Code` is the stable machine-readable half so a
> client can branch without matching prose.
>
> **Classification** lives in one place, `ResultErrors`, and maps
> `DbException → Infrastructure`, `HttpRequestException → Upstream`,
> anything else `→ Unexpected`.
>
> **`DbException`, not `NpgsqlException`** — deliberately. `DbException` is in
> `System.Data.Common`, i.e. the BCL, so the Application layer recognises a
> database outage without taking a dependency on Npgsql or EF Core. That
> assumption is load-bearing and invisible, so it is pinned by
> `NpgsqlFailureTypeTests`: it opens a connection to a closed port and asserts the
> exception is a `DbException`. Without that test, a provider change would
> silently turn 503s into 500s.
>
> **Two real defects fixed, neither of which the plan named:**
>
> 1. `GetOverview` caught `HttpRequestException`, which these repositories never
>    throw — so a genuine database failure escaped as an **unhandled 500**. That
>    catch is gone; the method now classifies like everything else.
> 2. `ExecuteAsync` caught every `Exception` and reported it as a dependency
>    failure. A `NullReferenceException` was therefore indistinguishable from a
>    dead database. Bugs are `Unexpected` now.
>
> **`OperationCanceledException` is rethrown**, never converted into a failure — a
> cancelled request is the caller giving up, not an error to report. Covered by
> `GetAllExperienceAsync_WhenOperationIsCancelled_PropagatesCancellation`.
>
> **One mapper, no per-endpoint codes.** `Endpoints/ApiResult.cs` owns the only
> `ErrorKind → status` mapping in the solution. Every endpoint is now
> `return result.ToHttpResult();`. Swagger metadata matches: `503` on the
> portfolio endpoints, `502` on the Notion ones.
>
> **Exception text is no longer published.** `detail` is a stable sentence; the
> provider's message (host names, database names, usernames, SQL) stays out of the
> response. Diagnostics arrive with item 2.4 (logging). This is a **behaviour
> change for clients** and is recorded in `docs/api-reference.md` §5.
>
> **Tests — 12 new.** `ResultErrorsTests` (6, plus a theory: the exception → kind
> mapping, including that a bug is *not* infrastructure and that the exception
> message never reaches the wire), three service-level cases (infrastructure,
> unexpected, upstream), and 7 endpoint tests over HTTP: `503` +
> `database_unavailable` on a database failure, `500` + `unexpected_error` on a
> bug, `503` (not an unhandled 500) from `/overview`, and Notion `502` with
> `notion_unavailable` vs `notion_invalid_response`.
>
> **The plan's own verification recipe does not work.** It says to point the API
> at an unreachable host with `ConnectionStrings__JDBConnection=…`, but
> `Program.cs` calls `AddUserSecrets<Program>()` *after* `CreateBuilder`, so user
> secrets win over environment variables (KI-036). Attempting it silently connects
> to the real database instead. The failure paths are therefore verified through
> `Api.Tests`, which drive the real endpoints over HTTP with a faked port — and
> through `NpgsqlFailureTypeTests` for the provider assumption. KI-036 is the
> reason this recipe cannot be trusted in Development.
>
> **Not done here, by scope:** `/api/v1/skill` still ignores `IsSuccess` (2.5), and
> an exception thrown *outside* a service — model binding, say — still produces a
> bare 500 until 2.4 adds a global handler.

**Why**

Every endpoint does:

```csharp
if (!result.IsSuccess) return Results.Problem(result.Error, statusCode: 502);
```

502 means "bad gateway — an upstream server returned an invalid response". A database failure is not a bad gateway; it is a 500 (or 503). A missing record is a 404. Clients cannot distinguish "DB down" from "Notion down" from "bad input". The string-only error also carries no machine-readable code, so the frontend must string-match.

**How to verify**

```bash
dotnet test tests/JDB_Portfolio_API.Api.Tests   # 503 / 502 / 500 paths over HTTP
dotnet test tests/JDB_Portfolio_API.Persistence.Tests --filter NpgsqlFailureTypeTests
```

The original recipe — point the API at an unreachable host with
`ConnectionStrings__JDBConnection=…` and `curl` it — **does not work**, and that is
itself a finding. `Program.cs` calls `AddUserSecrets<Program>()` after
`CreateBuilder`, so user secrets override environment variables (KI-036) and the
application quietly connects to the real database. Do not trust that recipe until
KI-036 is fixed.

To see it live without touching configuration, the honest option is the endpoint
tests: they boot the real application and exercise the same classification, with
the failure injected at the repository port.

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

- [x] Inject `ILogger<T>` into application services; log every failure with context
- [x] Add a global exception handler (`IExceptionHandler` / middleware) returning `ProblemDetails`
- [x] Thread `CancellationToken` end-to-end: endpoint → service → repository → `ToListAsync(ct)`
- [x] Remove the `catch (HttpRequestException)` around database calls (wrong exception type)

> **Done (2026-10-22).** The wrong catch was already gone as part of 2.2, so this item
> was logging + the handler + cancellation.
>
> **Logging.** `Application` took its first package reference:
> `Microsoft.Extensions.Logging.Abstractions` 10.0.10 — contracts only, no
> implementation, so the composition root still decides where output goes. The level
> is chosen by severity of consequence, not by convenience:
>
> - **Warning** for `Infrastructure` and `Upstream` — an outage is expected, it is
>   already visible to the caller as 503/502, and logging it at Error would drown the
>   failures that need a human.
> - **Error** for `Unexpected` — a bug is the only kind that needs someone to act.
>
> Both carry the exception, which is the point: plan 2.2 stopped publishing the
> provider's message, so the log is now the only place the diagnostic detail exists.
> Verified live — a Notion failure produced
> `Dependency failure while trying to query the Notion page (notion_unavailable).`
>
> **The handler.** `ErrorHandling/GlobalExceptionHandler.cs` implements
> `IExceptionHandler`, registered before everything else in the pipeline. It closes the
> gap 2.2 left open: an exception thrown *outside* a service (model binding, DI) used to
> produce a bodyless 500. It now produces the same `ProblemDetails` with the same
> `code`, so a client never special-cases it.
>
> The client-disconnect case is handled deliberately rather than lumped in:
> `OperationCanceledException` **with** `RequestAborted` signalled answers **499** and
> logs at Debug; a cancellation without that signal (a server-side timeout, a token
> bug) is a real 500 and logs at Error. Treating every cancellation as a disconnect
> would make those invisible.
>
> **Cancellation.** `CancellationToken` is a **required** parameter on every port and
> service method — no default value, so a new call site cannot silently drop it.
> `ExecuteAsync` hands the token to the operation (`Func<CancellationToken, Task<T>>`)
> rather than letting the closure capture one, which is the shape that makes dropping
> it a compile error instead of a habit. The token reaches `ToListAsync(ct)` and
> `HttpClient.GetAsync(ct)`.
>
> **One defect fixed on the way: KI-037.** `NotionClientService` dereferenced a
> possibly-null `ReadFromJsonAsync` result (the last build warning). It now checks and
> throws an explicit `InvalidOperationException`, which the classifier maps to
> `502 → notion_invalid_response`. The old `catch`/rethrow wrappers are also gone — they
> discarded the original stack trace and inner exception to rebuild a message that no
> longer goes anywhere. `dotnet build --no-incremental` is **0 warnings, 0 errors**.
>
> **Tests — 8 new (97 total).** Logging: a capturing logger proves a database failure is
> logged at Warning with the original exception, and a bug at Error. Cancellation: the
> exact token passed to the service is asserted to arrive at the port, plus an
> endpoint-level check that `RequestAborted` is what flows. Handler: 500 + `code` +
> `unexpected_error` with the exception text absent from the body, 499 for a real
> disconnect, and 500 for a cancellation the client did not cause. Plus a registration
> test — forgetting `AddExceptionHandler` is invisible otherwise.

**Why**

- **No logging exists** in Domain/Application/Persistence. A failure either escapes to the default handler (generic 500, no context) or is caught and converted to a string. You cannot diagnose a production failure.
- **No global handler** means unhandled exceptions produce inconsistent output instead of `application/problem+json`.
- **No `CancellationToken`** means a client navigating away still runs the query to completion, holding the connection. Minimal APIs hand you the token for free.
- The services catch `HttpRequestException` around **EF/Npgsql** calls. That exception type never comes from the database — the catch is dead and creates a false sense of safety.

**How to verify**

```bash
dotnet test tests/JDB_Portfolio_API.Api.Tests --filter GlobalExceptionHandlerTests
dotnet test tests/JDB_Portfolio_API.Application.Tests --filter PortfolioExperienceServiceTests
```

The original recipe — trigger the DB-down case and read the log — still works for the
logger, and the live Notion failure above is the cheapest way to see a real warning.
For cancellation, the temporary `Task.Delay(10_000)` trick is unnecessary: the token →
port assertion owns it now, and a test is cheaper than a browser experiment.

**Priority:** handler + logging matter more than cancellation for a portfolio.
Cancellation is a habit worth building.

---

## 2.5 — Endpoint consistency

`BUG` + `DESIGN` · **Risk:** low · **Effort:** 2–3 h

**What**

- [x] `PortfolioSkillEndpoints.cs:12` never checks `IsSuccess` and returns 200 with `null` on failure
- [x] Replace hand-spelled `"api/v1/..."` strings in seven files with `app.MapGroup("api/v1")`
- [x] Add input validation for `AuthDto` (minimal APIs do not run model validation)
- [x] Move JWT issuance out of `AuthEndpoints.cs` into an `IAuthTokenService` in Application
- [x] Fix the `async` `/auth/me` lambda with no `await` (CS1998)
- [x] Decide whether auth is real: all `RequireAuthorization()` are commented out, the token has no `sub`, so `User.Identity.Name` is null

> **Done (2026-10-22).** All six, plus three defects that only showed up while doing it.
>
> **`/skill`.** It now goes through `ApiResult.ToHttpResult()` like its siblings. A
> database failure returns `503 + database_unavailable` instead of a `200` that is
> indistinguishable from "no tech stacks exist".
>
> **One prefix.** `Program.cs` owns `app.MapGroup("api/v1")` and every endpoint declares
> only its own segment. Note the old spelling was inconsistent — five patterns had no
> leading slash and `/skill` had one — which a group makes impossible.
>
> **Validation.** Explicit guard in the handler returning
> `Error.Validation(...)` → `400 + validation_failed`, deliberately *not* .NET 10's
> built-in `AddValidation()`: that would introduce a second error shape
> (`ValidationProblemDetails` with `errors`, no `code`) next to the one 2.2 unified. One
> DTO does not justify a second contract.
>
> **JWT out of the endpoint.** `IAuthTokenService` in Application, `JwtTokenService` in
> Presentation. Application names the capability; the edge owns the crypto, because it
> already references the JWT libraries. The wire shape `{ "token": "…" }` is unchanged
> and now has a named DTO (`AuthTokenDto`) instead of an anonymous object, so Swagger
> documents it and a test guards it.
>
> **Auth posture — a decision, not a fix.** The portfolio read endpoints are **public by
> design**: they feed a public CV site, so requiring a token would break it. The
> commented-out `RequireAuthorization()` lines are deleted (dead code that implied
> security), the intent is documented in `Program.cs` and `docs/api-reference.md`, and
> the token now carries `sub=portfolio-api` with `NameClaimType` reading it, so
> `User.Identity.Name` is populated. It is a **service token**, not a user session — one
> shared key, no user store.
>
> **Three defects found on the way:**
>
> 1. **The signing key was read twice.** `AddAuthentication` read `Jwt:Key` eagerly at
>    registration and `JwtTokenService` read it again through `IOptions` when signing.
>    Two reads of one setting is two sources of truth: any change between them produced
>    tokens signed with one key and validated with another, and the symptom was a silent
>    `401`. It surfaced as `/auth/me` rejecting a token `/auth/login` had just issued.
>    `JwtBearerOptions` is now configured lazily from the same `JwtOptions` binding.
> 2. **Swagger claimed the whole API needed a token.** A global
>    `AddSecurityRequirement` marked every operation as `bearer`-protected, including
>    the public CV endpoints — the opposite of the truth. `AuthorizeOperationFilter` now
>    marks only operations carrying `IAuthorizeData`, so exactly `/auth/me` shows a lock.
> 3. **`/auth/me` was `async` with nothing to await** (KI-026), and `Login` returned a
>    bare `Results.Unauthorized()` instead of the shared ProblemDetails shape.
>
> **Tests — 10 new (107 total).** `JwtTokenServiceTests` (4, no HTTP): the service
> subject/issuer/audience, a wrong key is `Unauthorized`, a blank key is `Validation`,
> and a too-short configured key is `server_misconfigured` rather than a 401.
> `AuthEndpointsTests` (5, over HTTP): login returns a JWT, blank → `400 +
> validation_failed`, wrong → `401 + invalid_credentials`, `/auth/me` without a token →
> `401`, and with one → the `sub` and role claims. Plus `/skill` failure → `503`.

**Why**

- **Missing check:** the skill endpoint returns `200 OK` with `null` on failure while its siblings return errors. A client cannot tell success from failure.
- **Route strings:** `"api/v1/..."` is spelled out in 5 files. Changing a prefix or adding v2 means 5 edits and a guaranteed miss.
- **Validation:** `AuthDto` has no constraints. An empty `Key` is accepted (fixed-time comparison, so it fails safely, but there is no 400 with a useful message).
- **JWT in the endpoint:** token construction is application logic — ~40 lines in a presentation file, untestable without HTTP.
- **`async`/no `await`:** CS1998 plus an unnecessary state machine.
- **Decorative auth:** the security surface looks real but is inert. Either wire it up or rename `/auth/login` → `/auth/token` and document it as a service token.

**How to verify**

```bash
grep -rn "api/v1" src/JDB_Portfolio_API/Endpoints/   # no matches: the prefix lives in Program.cs
grep -rn "RequireAuthorization" src/JDB_Portfolio_API/Endpoints/   # exactly one: /auth/me
dotnet test tests/JDB_Portfolio_API.Api.Tests --filter AuthEndpointsTests
```

Then open `/swagger`: only `GET /api/v1/auth/me` shows a padlock. The original recipe —
`curl` the skill endpoint with the DB down — still cannot be done on a developer machine
because of KI-036; `GetSkill_WhenTheDatabaseFails_Returns503NotAnEmpty200` is the
substitute.

---

## 2.6 — Notion entries on the dashboard overview

`FEATURE` · **Risk:** medium (adds an external call to a currently DB-only endpoint) · **Effort:** 2–4 h

> Raised 2026-10-22 while doing 2.1. Recorded, not implemented.

**What**

The dashboard overview returns three tiles — Projects, Skills, Recommendations — all
counted from the database. A fourth tile for Notion entries is wanted.

- [ ] Decide: a tile **inside** `/overview`, or a **separate** endpoint the frontend
      calls in parallel
- [ ] Decide where the Notion page id comes from: configuration, a query parameter,
      or a fixed endpoint
- [ ] Make the Notion call fail **independently** of the other tiles

**Why it is not a five-line change**

- **It is an outbound call on every page load.** `/overview` is three database
  counts today — milliseconds, and entirely local. Notion is an HTTPS request
  against a rate-limited API (a few requests per second). Folding it in makes the
  dashboard as slow and as unreliable as the slowest external service.
- **Failure would take the whole dashboard down.** `PortfolioService.GetOverview`
  catches `HttpRequestException` and returns a failed `Result`, which the endpoint
  turns into `502` for **all** the tiles. Plan 2.4 notes that this catch is aimed at
  the wrong thing today; adding Notion makes it a live hazard. A Notion outage must
  omit or zero the fourth tile, never blank the other three.
- **The page id has nowhere to come from.** `/overview` has no route parameter, and
  `NotionSettings` lives in `ExternalService`, which `Application` must not
  reference. A config-driven id therefore needs a `NotionOptions` in
  `Application/Settings` (bound in Presentation), or the id becomes a query
  parameter.
- **Caching is probably required.** One Notion call per page load is wasteful and
  invites rate limiting.

**Already available, no backend change**

```bash
GET /api/v1/notion/{pageId}/count     # → 7
```

The frontend can fetch this alongside `/overview` and render the tile itself. That
is the cheapest path and keeps the dashboard independent of Notion. It may be the
whole answer; if so, close this item rather than building it.

**Related**

- Plan 3.2 moves `NotionPage` out of Domain — do that before the DTO for entries is
  fixed, or it will be done twice.

## 2.7 — CORS policy (added 2026-10-22, prompted by a review of 2.5)

`SECURITY` + `OPS` · **Risk:** low · **Effort:** 1 h

**What**

- [x] Move the origin list out of C# into `Cors:AllowedOrigins`
- [x] Apply `http://localhost:4200` only in Development (`Cors:DevelopmentOrigins`)
- [x] Drop `AllowCredentials()`
- [x] Narrow to `GET, POST` and `Content-Type, Authorization`

**Why**

- **Hardcoded origins can't follow the deployment.** Vercel preview URLs
  (`*-git-<branch>-<team>.vercel.app`) are not in a static list, so previewing a branch
  fails with a CORS error — and the usual "fix" is `AllowAnyOrigin()`, which is far worse
  than the thing it solves. As configuration, an origin is an App Service setting.
- **Localhost was allowed in production.** Any page served from port 4200 could read the
  deployed API. Harmless today because the API is anonymous, but it is the exact shape of
  a hole the moment a cookie appears.
- **`AllowCredentials()` had no purpose.** Bearer tokens travel in the `Authorization`
  header, which CORS treats as an ordinary request header; nothing in the API sets a
  cookie. The flag promised all three origins permission to send cookies that don't
  exist, and it permanently rules out wildcards.
- **`AllowAnyMethod()`/`AllowAnyHeader()`** described an API that accepts anything. Six
  GETs and one POST do not.

> **Done (2026-10-22).** `AddCorsOrigins(configuration, environment)` reads both keys,
> trims whitespace and trailing slashes (the browser's `Origin` header never has one),
> de-duplicates, and registers the policy under `ConfigurationSettings.SpaCorsPolicy` so
> `Program.cs` cannot drift from the name. `appsettings.json` ships today's values as
> defaults, so nothing is required to boot.
>
> **Verified live**, which is the only way to check this: preflight from a listed origin
> → `204` with `Allow-Origin`/`Allow-Methods: GET,POST`/`Allow-Headers:
> Content-Type,Authorization` and **no** `Allow-Credentials`; an unlisted origin → no
> `Allow-Origin` at all; `localhost:4200` allowed in Development and silently ignored in
> Production; a preview origin added by `Cors__AllowedOrigins__2=…` accepted without a
> rebuild; and `401`/`400` responses still carry the CORS headers, because `UseCors` sits
> inside `UseExceptionHandler`.
>
> **Tests — 5 new (112 total, `CorsPolicyTests`).** Policy asserted directly rather than
> over HTTP, since only a browser enforces it: production excludes localhost, development
> includes it, no credentials, no wildcards, and a pasted origin with a trailing slash
> still matches.
>
> **Not changed:** `AddControllers()`/`MapControllers()` remain (plan 4.1), and the
> single origin list still cannot express a wildcard subdomain — a preview URL is added
> one at a time. A regex/exists policy would be the next step if previews become routine.

---


# Part 3 — The actual DDD work

This is the part that answers "am I doing DDD?". Everything here is a design choice — push back on any of it.

## 3.1 — Value objects must have value equality

`DESIGN` (model correctness) · **Risk:** low · **Effort:** 1 h

**What**

- [x] `public sealed record Role`
- [x] `public sealed record Company`
- [x] `public sealed record Period`
- [x] `Company` maps `null`/whitespace to `""` — either reject it (`ArgumentException`) or make it optional at the aggregate level and remove the empty-string concept
- [x] `Period` invariants: `StartedAt` not in the future, `FinishedAt >= StartedAt` (already present), and settle what `FinishedAt == null` means

> **Done (2026-10-22).** All five, and two of them were decisions rather than edits.
>
> **Equality.** The three are `sealed record`s now, so `new Role("Developer") ==
> new Role("Developer")` is `true` and they work as dictionary keys. Each property is
> `get`-only, which is what makes the record safe: a generated `with` expression cannot
> change a value and skip the constructor's validation. `ToString()` overrides are kept
> (records would otherwise print `Role { Value = Developer }`), so nothing that relied on
> the old output changes.
>
> **`Company` blank → rejected.** The empty string is gone rather than formalised. The
> old type collapsed `null` and `"   "` into `Name == ""`, so the model carried a value
> that meant "nothing" and every caller would have had to test both `Name == ""` and
> `null`. "Not disclosed" is now the absence of a company, matching how `Role` already
> worked after 1.5. The choice had to move somewhere, so it moved to
> `PortfolioMappingProfile.ToCompany` — the same `IsNullOrWhiteSpace → null` rule as
> `ToRole`, which also keeps a blank column from throwing inside AutoMapper and taking
> down the collection (the 1.5 failure mode). The reverse map writes `null`, not `""`:
> `company_name` is nullable, unlike `role`. **Superseded by 3.1a** — the column became
> required the same day, `ToCompany` was deleted, and the mapping no longer has a
> "blank means not disclosed" branch to make.
>
> **`FinishedAt == null` means ongoing.** Named as `Period.IsOngoing`, true only when
> there is a start and no end. `StartedAt == null && FinishedAt == null` is *unknown*, not
> ongoing — the two are different facts and the mapper treats them differently.
>
> **`StartedAt` not in the future** — compared by `.Date`, so the time of day and the
> caller's timezone can never decide it. Worth knowing the cost: the mapper builds a
> `Period` for every row, so a future-dated row now throws inside AutoMapper and returns
> `500` for the whole collection instead of rendering a nonsense duration. Accepted
> deliberately — a `2062` typo should be loud — but it is read-path enforcement until a
> write endpoint exists to check it earlier. Verified against the live data: no
> future-dated rows, `/experience` still `200`.
>
> **Tests — 20 new (132 total).** Domain: equality, `==`, `GetHashCode` and
> dictionary-key behaviour for all three; `Company` rejects `null`/`""`/whitespace;
> `Period` rejects a future start and accepts today; `IsOngoing` for ongoing, finished and
> unknown. Persistence: a blank or null `company_name` mapped to `null` and did not throw,
> a declared name survived, and the reverse map wrote `null` — **all three superseded by
> 3.1a**, which made that state unrepresentable instead of tolerated.

**Why**

A value object is *defined by its value*. They were classes with no `Equals` override, so equality was reference-based:

```csharp
new Role("Developer") == new Role("Developer")   // false before 3.1, true now
```

That meant you could not write `if (experience.Role == Role.Developer)`, could not use them as dictionary keys, and could not compare a mapped-from-DB role to a constructed one. They were validation wrappers, not value objects.

**How to verify**

```bash
grep -n "record" src/JDB_Portfolio_API.Domain/ValueObjects/*.cs   # three records, one vendor DTO
dotnet test tests/JDB_Portfolio_API.Domain.Tests --filter ValueObjects
```

**Why `record class`, not `record struct`:** `record class` keeps `null` meaningful for `Company?` and gives `with` expressions. `record struct` removes allocation but changes nullability semantics. For three types on a portfolio API, `record class` is correct. `Period` has `DateTime?` members, so its generated equality is right.

---

## 3.1a — A role always names a company

`SCHEMA` (make the rule enforceable) · **Risk:** medium at deploy · **Effort:** 45 min ·
**Written 2026-10-22, migration not applied**

**What**

- [x] `ExperienceEntity.CompanyName` → `string` (`= null!`), `.IsRequired()`
- [x] `PortfolioContext` — `ToTable("experience", t => t.HasCheckConstraint(...))`
- [x] `Experience.Company` and `ExperienceDto.Company` non-nullable
- [x] `ToCompany` helper deleted; the reverse map is `src.Company.Name`
- [x] Migration `20261022150752_RequireCompanyName` — `SET NOT NULL` + `CHECK (btrim(company_name) <> '')`
- [x] `RequiredCompanyMappingTests` replaces `UnnamedCompanyMappingTests`

> **Done (2026-10-22).** 3.1 left the domain forbidding a nameless company and the
> schema permitting one, and after 3.1 the *mapping* was the thing deciding that a blank
> column meant "no company". Three layers, three answers. This item collapses them into
> one and puts the answer where it cannot be bypassed: the database.
>
> **Why `NOT NULL` alone was not enough.** It reproduces exactly the `role` situation — a
> column that insists on a value, filled with `''` when there is not one — so the
> constraint is `btrim(company_name) <> ''` as well. `btrim` also rejects all-whitespace.
> Note that `NULL` *passes* a `CHECK` (the predicate is unknown, not false), which is why
> both constraints are needed; either one alone leaves a hole.
>
> **The asymmetry with `role` is deliberate and documented in `database.md` §3.**
> "Not disclosed" is a plausible answer for a role; a role with no employer is not a role.
> Anything that genuinely cannot be named gets a truthful stand-in — `"Self-employed"`,
> `"Contract — NDA"` — rather than a blank. Do not add a `CHECK` to `role` to "match", and
> do not relax this one to match `role`.
>
> **`defaultValue: ""` was scaffolded and then removed by hand.** EF invented it so the
> `SET NOT NULL` would not fail on a `NULL` row — but it would have written an empty
> company that the new `CHECK` rejects anyway, i.e. data invented and then refused. With
> the default gone, a dirty row makes the migration fail loudly, which is the correct
> outcome. Editing a migration in place is normally forbidden (rule 10); it is allowed
> here because the migration had never been applied to any database at the time.
>
> **Deploy risk, stated plainly.** `Database:AutoMigrate` defaults to `true` (KI-038), so
> this runs on the next deploy. `SET NOT NULL` aborts if a `NULL` row exists. Local data is
> clean — 4 rows, all named — but that does not prove production is, and `dotnet ef` cannot
> reach the production database from here. Run the check in `database.md` §4 against
> production before deploying; a failure is a data fix, not a reason to add a default.
>
> **Tests — 129 total (was 132).** Net −3 because the state under test was removed: two
> persistence tests asserted that a null/blank column became `null` and one mapper test
> asserted that a missing company became a `null` DTO field. Added:
> `Map_Experience_WithACompany_WritesItsName` (the reverse direction, which had no test)
> and `Map_ExperienceEntity_WithABlankCompany_FailsLoudly` — a corrupt row now throws
> instead of being reinterpreted, which documents the boundary rather than the happy path.
>
> **Not done, on purpose:** no `Trim()`. `" Acme"` and `"Acme "` are still distinct values
> under value equality; the `CHECK` rejects all-whitespace but not surrounding whitespace.
> Recorded as KI-044.

**Why**

The cheapest place to enforce "a role names a company" is the place every writer must go
through. There is no write endpoint yet, so the alternative — validating in the API — is a
promise about code that does not exist. The constraint is also stable across the 3.4
decision: if `Company` later becomes an entity, the rule becomes `company_id NOT NULL` and
this column disappears with its constraint.

**How to verify**

```bash
dotnet ef migrations has-pending-model-changes --project src/JDB_Portfolio_API.Persistence \
  --startup-project src/JDB_Portfolio_API        # "No changes … since the last migration"
grep -n "ck_experience_company_name_not_blank" \
  src/JDB_Portfolio_API.Persistence/Contexts/PortfolioContext.cs \
  src/JDB_Portfolio_API.Persistence/Migrations/*RequireCompanyName.cs
dotnet test tests/JDB_Portfolio_API.Persistence.Tests --filter RequiredCompanyMappingTests
```

The constraint itself cannot be exercised by the test suite — Persistence tests use
AutoMapper without a database. It is verified by the migration SQL and by the data check
above.

---

## 3.2 — Move non-domain types out of Domain

`DESIGN` · **Risk:** low · **Effort:** 1 h

**What**

- [x] `Domain/Entities/DashboardItem.cs` → Application (it is a read model)
- [x] `Domain/ValueObjects/NotionPage.cs` → vendor model in `ExternalService`, `PageContent` in `Application` (ADR-0005)
- [x] `Domain/Interfaces/INotionService.cs` → `Application/Abstractions/IPageContentReader`, bridged by `Presentation/Adapters/NotionPageContentReader`
- [x] Keep in Domain only ports that describe domain needs (the `IPortfolio*Repository` interfaces)

> **Partially done (2026-10-22).** `DashboardItem` moved to `Application/Models`;
> it was referenced by exactly two types (`PortfolioService`, `PortfolioDtoMapper`) and
> one test, so the move changed one namespace and three `using` lines. No dependency edge
> moved, nothing at runtime changed.
>
> The other two bullets are **deferred on purpose**, and the reason is not effort — it is
> a rule conflict the plan did not account for. `ExternalService` implements the port and
> constructs the payload (`NotionClientService : INotionService`,
> `NotionPageParserUtility.Parse`), so an `Application`-owned port requires
> `ExternalService → Application`. AGENTS §6 rule 2 and plan line 1400 both forbid that
> edge, and ADR-0001's follow-up went the other way (it removed `Persistence → Application`
> precisely because of the rule). Domain is the only project both sides already see, which
> is why the vendor type landed there.
>
> Both placements work; only the invariant differs. Relaxing the rule to its meaningful
> form — *`Application` must not reference any infrastructure project; infrastructure may
> reference `Application` only to implement its ports* — is the textbook arrangement, but
> doing it while nothing enforces the edges trades a rule that already caught one real
> violation for one trusted to review. Recorded as KI-045, to be revisited once the
> architecture test exists. **No code outside `DashboardItem` changed.**
>
> Two bullets the plan no longer needed: `IService<T>`/`IMapper<,,>` and the `ExperienceId`
> VO were already deleted in the 1.x dead-code pass. `IRepository<T>` stays in Domain — it
> is a genuine port, constrained on by `PortfolioServiceBase`.
>
> **Completed 2026-10-23 — the rule was not relaxed.** `IPageContentReader` and
> `PageContent` live in `Application`; `NotionPage` moved to
> `ExternalService/Notion/Models`; `NotionClientService` implements nothing; and
> `Presentation/Adapters/NotionPageContentReader` bridges the two, because the composition
> root is the only project that references both. AGENTS §6 rule 2 is unchanged and no
> csproj reference moved. ADR-0005 records the decision, the rejected alternatives and why
> the adapter is in Presentation.
>
> The wire is unchanged: same route, same `NotionPageDto` keys, same
> `notion_unavailable` / `notion_invalid_response` codes, verified live. The adapter is the
> one seam the endpoint tests cannot reach (they replace the port), so
> `NotionPageContentReaderTests` runs the real chain — stub `HttpMessageHandler` → parser →
> vendor model → adapter → `PageContent` — and pins that an upstream failure escapes with
> its original type. `Domain` is now vendor-free.

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

- [x] `Project`: private setters, validating constructor/factory, non-empty name
- [x] `Recommendation`: validate `Description` in the constructor (currently unvalidated while `PersonName` and `CareerPosition` are)
- [x] `TechStack`: validate name and category; remove `CreatedAt`
- [x] Remove `partial` and `= null!` from domain entities — those are EF-entity idioms
- [x] Remove `CreatedAt` / `UpdatedAt` from domain entities (persistence audit columns)
- [x] `Experience.Projects` has a public setter — make it a read-only collection
- [x] `Experience.Responsibility` / `.Type` have no mutators — add behaviour or drop them
- [x] Resolve the identity mismatch: `Experience.Id` is constructor-only, `Project.Id` is a public setter

> **Done (2026-10-22).** Every entity now validates at construction and exposes behaviour
> instead of setters. The read path was verified first: no blank `project_name`,
> `person_name`, `description` or `stack_name` in the data, so the new guards cannot turn a
> working endpoint into a `500`. 39 new Domain tests; total went 129 → 168.
>
> **`private set` alone would have been theatre.** The plan's caveat says private setters
> force construction through the factories. AutoMapper writes private setters through
> reflection — `Experience.Description`/`.Responsibility`/`.Type` had exactly that shape
> and were populated in production — so the *Ignore* is what gives the constructor
> authority, not the access modifier. The profile now states that explicitly, and
> `MappingConfiguration_HasNoUnmappedMembers` caught the one place it did not:
> removing `CreatedAt` from the domain left it unmapped on the two reverse maps, where it is
> now ignored on purpose (a write path leaves the audit column to the database).
>
> **`Experience.Projects` and `Project.TechStacks` are `IReadOnlyList` + `Replace…`.** The
> setter allowed a caller to swap the collection while the aggregate believed nothing had
> changed; the method also copies the argument, so a caller cannot keep a handle on it.
> Verified by test (`ReplaceProjects_CopiesTheList`).
>
> **`Project.Id` is get-only**, settling the identity mismatch: identity is assigned once,
> at construction, for every entity.
>
> **`Responsibility` and `Type` got mutators** rather than being dropped — they are real
> columns and real API fields; the defect was that the aggregate had no way to express them,
> while AutoMapper wrote them anyway. `Recommendation` got the validation it half-had:
> `PersonName` now rejects whitespace (KI-041) and `Description` is required.
>
> **`TechStack.Category` stays nullable.** The mapper has a live branch for a missing
> category navigation, so the rule is "blank is rejected, absence is allowed" — requiring it
> would have been a read-path `500` waiting for the first query that does not include the
> navigation.
>
> **Not done, deliberately:** no `CHECK` constraints. 3.1a set that precedent for
> `company_name`; extending it to four more columns is a second migration and another deploy
> risk, and it is not what this item is about. Recorded in `database.md` §3 as the remaining
> asymmetry. KI-044 (no trimming) is also still open — `btrim` rejects an all-whitespace
> name but says nothing about surrounding spaces.

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

- [x] Choose D3: **Experience owns Project for writes** (recommended), or **Project is an independent aggregate** referenced by a typed `ExperienceId`
- [x] Enforce it: exactly one repository is the write entry point for the boundary
- [x] `Project.TechStacks` holds `TechStack` **entities** — replace with ids or a small `TechStackRef` value object
- [x] Remove `Project.Period` or map it (see 1.4)
- [x] Add `FinishedAt` to `ExperienceEntity` or drop `Period.FinishedAt` from the read path (see 1.3)

> **Done (2026-10-23).** Three of the five bullets had already been settled by the time
> this item was reached: D3 was decided on 2026-10-22 (option **b**, Project is an
> independent aggregate — ADR-0003), `Project.Period` was deleted in 2.1, and `finished_at`
> was added in 3.1a. What remained was enforcement and `TechStackRef`.
>
> **"How do I create a project?" — zero ways today.** The item was written when the
> answer was two. Every repository derives from `IRepository<T>`, whose only member is
> `GetAllAsync`; `AddNewAsync` is gone. So "exactly one write entry point" cannot be
> enforced by removing the second one — it is enforced by *shape* until 4.4 adds
> `IUnitOfWork`: `Experience.Projects` is a read-only projection, `Project.ExperienceId`
> changes only through `AttachToExperience`, and `Project.TechStacks` cannot be assigned.
> ADR-0003 now names `IPortfolioProjectRepository` as the future write entry point and
> says explicitly that adding a second one is the thing to refuse.
>
> **`Project.TechStacks` is `IReadOnlyList<TechStackRef>`.** A `record` carrying id,
> name, image and category — the same four fields the nested payload already published,
> so the change is invisible to clients (verified live: the nested key list is unchanged).
> Including the category was the deciding detail: `TechStackRef` with only id+name+image
> would have silently dropped `category` from every project's stacks, which is a contract
> change, not a refactor. Two `TechStackEntity → TechStackRef` conversions in the profile
> replace the entity construction; `TechStack` itself is untouched and still serves
> `/api/v1/skill`.
>
> **`TechStackRef` got its own value-equality tests** (9 of them) — a reference that does
> not compare by value is not a reference, it is a copy with extra steps.
>
> Tests went 168 → 177; no schema change; no migration.

**Why**

> *(Written before 2.1/3.1a/3.3. The three overlapping boundaries described below no
> longer exist — see the Done note above.)*

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
- [x] `tests/JDB_Portfolio_API.Api.Tests`: `WebApplicationFactory` smoke tests per endpoint
- [x] `tests/JDB_Portfolio_API.Persistence.Tests`: the AutoMapper profile

> **Done (2026-10-23).** Four test projects exist and `dotnet test` is green
> (188 tests). What they cover today:
>
> - **Domain.Tests** — 81 tests: the value objects (`Role`, `Company`, `Period`,
>   `TechStackRef`) with equality, `==`, `GetHashCode` and dictionary-key behaviour,
>   plus the entity invariants from 3.3 (`Experience`, `Project`, `Recommendation`,
>   `TechStack`, `TechStackCategory`).
> - **Application.Tests** — 37 tests: `PortfolioExperienceService` with a hand-rolled
>   `FakePortfolioExperienceRepository` in `Fakes/` (the shared `ExecuteAsync`
>   behaviour from 2.3, plus the 2.4 logging levels and token threading),
>   `ResultErrors` (2.2), and `PortfolioDtoMapper` (2.1) — two-thirds of which are
>   wire-shape guards on the DTO types themselves.
> - **Persistence.Tests** — 22 tests: the `Experience` and `TechStack` maps, the
>   "undeclared role" mapping from 1.5, the required-company rule from 3.1a, and
>   `NpgsqlFailureTypeTests` (2.2), which pins the assumption that a provider failure
>   is a `DbException` — the fact the 503 mapping silently depends on.
>   - `MappingConfiguration_HasNoUnmappedMembers` — an `AssertConfigurationIsValid()`
>     gate. It found **6 maps with unmapped destination members**, all now explicitly
>     ignored or properly mapped.
>   - Runtime map tests for both directions of the `Experience` and `TechStack` maps —
>     the only way to catch a bad type conversion, which the validity gate cannot.
>   - `PopulatesDescriptionResponsibilityAndType` — this **resolves KI-035**: the
>     private-setter properties *are* populated by AutoMapper. The behaviour was never
>     broken, only unverified.
> - **Api.Tests** — 39 endpoint contract and policy tests over `WebApplicationFactory<Program>`.
>   The real application boots in-process and the requests go over HTTP, so routing,
>   model binding, status codes and serialization are all production code. Two things
>   make that safe: the repository **ports** are replaced with in-memory fakes, so no
>   database is involved and no real row can be touched, and `Database:AutoMigrate=false`
>   keeps the startup migration out of the picture even though a connection string is
>   configured. The strongest test asserts the **exact JSON key list and order** for
>   `ExperienceDto` at the endpoint — the contract a client sees, not a proxy for it.
>   The failure path is covered too: a throwing repository must produce `502` with
>   `ProblemDetails`, not an unhandled exception. `NotionPageContentReaderTests` runs the
>   real stub-handler → parser → vendor model → adapter chain, the one seam the endpoint
>   tests replace.
>
> **Not covered yet, deliberately:** authentication beyond `/auth/me` (the portfolio
> endpoints are anonymous on purpose, 2.5) and the startup path — `Database.Migrate()`
> is unreachable from the test host, which is why KI-043 has no regression test.
>
> Package baseline (approved): `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4,
> `Microsoft.NET.Test.Sdk` 17.14.1, and `Microsoft.AspNetCore.Mvc.Testing` 10.0.13 for
> `Api.Tests` (pinned to the installed ASP.NET Core shared framework). The
> `coverlet.collector` that the template adds by default was removed — coverage
> percentage is not a target.
>
> `Persistence.Tests` also pins `Microsoft.EntityFrameworkCore.Relational` 10.0.10 —
> a version pin to what the solution already uses, not a new dependency. Without it,
> `PrivateAssets="all"` on EF Design stops 10.0.10 flowing to the test project, NuGet
> resolves Npgsql's lower 10.0.4, and MSBuild reports MSB3277.
>
> **Open:** resolved 2026-10-22 — the same `Relational` 10.0.10 pin was added to
> `Persistence.csproj`, so `Api.Tests` (and every other consumer) inherits a consistent
> graph and `MSB3277` no longer appears. The underlying Npgsql version gap is still
> KI-039.

**Why**

Every change in Parts 1–3 is a refactor, and a refactor without tests is a rewrite you hope works. More importantly for a learning project: tests are how you *prove* invariants exist. `new Project(1, "")` throwing is a test, and writing it forces you to decide what "valid" means.

**How to verify**

```bash
dotnet test    # 188 tests, all green
```

**Pragmatic order, as executed:** Domain first, covering 3.1 and 3.3, then Application,
Persistence and Api as each part landed.

**Acceptance:** `dotnet test` is green and covers every value object and entity invariant.

---

# Part 4 — Polish (last, or never)

| # | Item | Why | How to verify |
|---|---|---|---|
| 4.1 | Split `ConfigurationSettings.cs` into `AddApplication()` / `AddPersistence()` / `AddExternalServices()` / `AddPresentation()`, and delete `AddControllers()` / `MapControllers()` (KI-025) | Service registrations currently sit inside `AddDatabase`; one method per layer is easier to reason about. No controllers exist, so the calls are dead — 2.7 already deferred their removal to this item | **Done (2026-10-23).** `AddPresentation(configuration, environment)` also owns the `IPageContentReader` adapter and the exception handler; `AddPersistence` registers the four repositories and `AddMapper()`; `AddApplication` the five services. `AddControllers`/`MapControllers` are gone |
| 4.2 | Remove the unused `IConfiguration` parameter from `AddMapper` | A dead parameter implies configuration is used | **Done (2026-10-23).** Signature is `AddMapper(this IServiceCollection)`; called from `AddPersistence` |
| 4.3 | Replace the 150-line `OnModelCreating` with `IEntityTypeConfiguration<T>` classes | One file per entity diffs cleanly and keeps `PortfolioContext` small | **Done (2026-10-23).** Six classes in `Persistence/Configurations/`; `OnModelCreating` calls `ApplyConfigurationsFromAssembly`. `has-pending-model-changes` reports no changes, so the snapshot is identical |
| 4.4 | Add `IUnitOfWork` / transaction abstraction before write endpoints | there is no write path at all — `AddNewAsync` is gone and `IRepository<T>` exposes only `GetAllAsync` — but you will need it the moment one exists | **Deferred (2026-10-23).** Reads only, so a unit of work would be dead code; EF Core's `DbContext` already provides the transaction boundary. Decide at the first write endpoint: repository-saves vs Application-owned `IUnitOfWork.SaveChangesAsync()` |
| 4.5 | Add `Directory.Build.props` with `TreatWarningsAsErrors` + analyzers | Nothing enforces the current 0-warning build; this is how the CS1998 that 2.5 fixed would have been caught at build time | **Done (2026-10-23).** Root `Directory.Build.props` sets `TreatWarningsAsErrors`; build stays at 0 warnings. No analyzer package was added — SDK analyzers only |
| 4.6 | Fix `NotionClientService.cs`: don't rethrow `new HttpRequestException(msg)` (loses the inner exception); don't use an absolute URL when `BaseAddress` is set; set auth headers in `ConfigureHttpClient`; add retry/timeout | Current code hides the original stack and duplicates configuration | **Done (2026-10-23).** Relative path; base address, auth headers and a 15s timeout live in `AddExternalServices`; one manual retry on 429/5xx; failures keep their status code. Network errors/timeouts are not retried |
| 4.7 | Replace the `NotionPageParserUtility` XML trick | `XDocument.Parse("<root>" + markdown + "</root>")` breaks on malformed content or a literal `</root>` | **Done (2026-10-23).** Tolerant `<page>…</page>` scan (tempered dot so an unclosed tag cannot swallow the next card); tags stripped, entities decoded, whitespace collapsed. Broken fragments are skipped, not fatal |
| 4.8 | Move `PortfolioService.GetOverview` hardcoded copy to config; parallelise the three sequential queries | Display text in the app layer; three independent awaits that could be `Task.WhenAll` | **Done differently (2026-10-23).** The copy was removed rather than configured: `/overview` now returns `{ key, count }` and the client owns labels, icons and order (Option B). **Parallelisation rejected:** all three repositories share the scoped `PortfolioContext`, and EF Core forbids concurrent operations on one context — `Task.WhenAll` throws. The counts still materialise rows; a `CountAsync` port method is KI-046 |
| 4.9 | Add `.github/plans/README.md` index if more plans are added | Keeps plans discoverable | **Done (2026-10-23).** Index exists and its 001 status row now matches the plan |

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
| 2026-10-22 | Added **2.7 — CORS policy** after a review question about the hardcoded origin list: origins moved to configuration, localhost gated to Development, `AllowCredentials()` dropped, methods/headers narrowed |
| 2026-10-23 | Documentation sync after Part 3 closed: test counts corrected to 179, 3.5 marked done, KI-003/KI-009/KI-013 marked resolved, 4.1 extended to cover the dead controller registrations (KI-025) |
| 2026-10-23 | Part 4: 4.1, 4.2, 4.3, 4.5, 4.6, 4.7 and 4.9 done; 4.8's copy moved to config (parallelisation rejected — one scoped `DbContext`); 4.4 deferred until the first write endpoint. Tests 179 → 187 |
| 2026-10-23 | `Swagger:Enabled` config flag: Swagger stays Development-only by default but can be exposed in a deployment for a smoke test. Tests 187 → 189 |
| 2026-10-23 | `/overview` contract: `{ key, count }` items replace the display-copy tiles (Option B); `OverviewSettings`, its config section and the mapper are removed. Tests 189 → 188 |
