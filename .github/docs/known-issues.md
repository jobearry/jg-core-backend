# Known issues

Live register of defects and debt. **Update this file when you find or fix
something**, even if you do not fix it now. Full reasoning for each item is in
[`../plans/001-clean-architecture-refactor.md`](../plans/001-clean-architecture-refactor.md).

Severity: **P0** blocks running the project · **P1** wrong behaviour or broken
architecture rule · **P2** correctness/robustness risk · **P3** cleanup.

## P0 — blocks running

None known.

## P1 — wrong behaviour or rule violation

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-003 | ~~No tests at all~~ **PARTIALLY RESOLVED 2026-10-22** | four xUnit projects now exist — `Domain.Tests` (19), `Application.Tests` (38), `Persistence.Tests` (18), `Api.Tests` (37) — and `dotnet test` is green (112 tests). **Still missing:** entity-invariant and VO-equality tests, which belong to 3.1 and 3.3 | 3.5 |
| KI-004 | ~~Domain entities are the public API contract~~ **RESOLVED 2026-10-22** | endpoints return `Application/DTO` records; `PortfolioDtoMapper` maps explicitly. No `Domain.Entities`/`Domain.ValueObjects` type appears in an endpoint signature or response. Proven by the wire-shape guards in `PortfolioDtoMapperTests` | 2.1 / D4 |
| KI-005 | `Period.FinishedAt` was always null after mapping, so `Duration` grew daily for past roles | **code fixed 2026-10-22** — `finished_at` column added (migration `20261022064321_AddExperienceFinishedAt`), entity + context + mapping updated, regression tests added. **Still open: the data.** Existing rows have `finished_at IS NULL`, which now correctly means *ongoing*. The past roles need their end dates backfilled by hand | 1.3 / D1 |
| KI-006 | ~~`/api/v1/skill` returns `200` with `null` on failure~~ **RESOLVED 2026-10-22** | it goes through `ApiResult.ToHttpResult()` like its siblings, so a database failure returns `503` + `database_unavailable` instead of a success-shaped empty body. Pinned by `GetSkill_WhenTheDatabaseFails_Returns503NotAnEmpty200` | 2.5 |
| KI-007 | ~~Every failure returns `502` and leaks the exception message~~ **RESOLVED 2026-10-22** | errors are typed (`ErrorKind` + machine-readable `code`) and mapped to a status in one place (`Endpoints/ApiResult.cs`): `404/400/401/409/503/502/500`. Exception text is no longer published. `/skill` now uses the same mapper, and `GlobalExceptionHandler` covers exceptions thrown outside a service | 2.2 |
| KI-008 | `Role`, `Company`, `Period` have reference equality | no `Equals`/`GetHashCode`/`record` in `Domain/ValueObjects` | 3.1 |
| KI-009 | Anemic entities: `Project`, `TechStack`, `TechStackCategory` have no invariants; `Recommendation.Description` unvalidated | public setters, `= null!`, no validation in constructors | 3.3 |
| KI-010 | ~~Authorization is not enforced on any portfolio endpoint~~ **RESOLVED 2026-10-22 (by decision)** | the portfolio read endpoints are **intentionally anonymous** — they feed a public CV site. The misleading commented-out `RequireAuthorization()` lines are deleted, the choice is documented in `Program.cs` and `docs/api-reference.md`, and Swagger now marks only `/auth/me` as requiring a token instead of the whole API. Tokens carry `sub`, and `NameClaimType` reads it, so `User.Identity.Name` is populated. **Deferred (2026-10-22):** an `AuthorizationOptions.FallbackPolicy` secure-by-default flip — which would protect un-annotated endpoints instead of exposing them — was considered and deliberately held until a write endpoint exists; anonymity is still the default | 2.5 |

## P2 — correctness and robustness risk

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-011 | ~~TechStack reverse mapping maps `string` → `int`~~ **RESOLVED 2026-10-22** | the bad `.ForMember(TechStackCategoryId, MapFrom(src => src.Category))` is gone; the id is now explicitly ignored and resolved by a lookup at write time. Proven by `Map_TechStack_To_TechStackEntity_LeavesTheCategoryIdUnset` | 1.2 / D2 |
| KI-012 | ~~`Project.Period` has no column and no mapping~~ **RESOLVED 2026-10-22** | the domain property was deleted. The `"period": null` key no longer appears in `GET /api/v1/project` | 1.4 |
| KI-013 | Empty-string `role` / `career_position` throws inside AutoMapper and 500s the whole collection | `new Role(src.CareerPosition)`; `NOT NULL` permits `''` | 1.5 |
| KI-014 | ~~Services catch `HttpRequestException` around database calls; that type never occurs there~~ **RESOLVED 2026-10-22** | the wrong catch is gone. `PortfolioServiceBase` classifies by exception type (`DbException` → 503, `HttpRequestException` → 502, anything else → 500), and `PortfolioService.GetOverview` no longer catches `HttpRequestException` around its database calls at all — that catch was dead and hid a real unhandled 500 | 2.4 |
| KI-015 | ~~No `CancellationToken` anywhere; abandoned requests keep querying~~ **RESOLVED 2026-10-22** | threaded end-to-end: endpoint → service → repository → `ToListAsync(ct)` → `HttpClient.GetAsync(ct)`. The port signatures take the token as a **required** parameter, so a new call site cannot forget it, and `ExecuteAsync` hands the token to the operation instead of letting it capture one. Pinned by `GetAllExperienceAsync_PassesTheCallersCancellationTokenToTheRepository` and an endpoint-level check | 2.4 |
| KI-016 | ~~No application logging and no global exception handler~~ **RESOLVED 2026-10-22** | the Application services log through `ILogger` (warning for a dependency failure, error for a bug) and the exception detail that plan 2.2 stopped publishing now reaches the log. `GlobalExceptionHandler` (`IExceptionHandler`) turns anything that escapes an endpoint into the same `ProblemDetails` shape, with `499` for a client that disconnected. Note: the default ASP.NET request logging is still the only other logging — there is no structured sink or correlation beyond `traceId` | 2.4 |
| KI-019 | All timestamps are `timestamp without time zone` | `PortfolioContext.cs` column types | 4.x |
| KI-036 | User secrets silently override environment variables | `Program.cs:9` calls `AddUserSecrets<Program>()` **after** `CreateBuilder(args)`, so the secrets file is added last and wins over env vars and command-line args. `ConnectionStrings__JDBConnection=...` is therefore ignored on a developer machine, and a `secrets.json` present on the server would override production configuration. **Confirmed 2026-10-22:** this makes plan 2.2's documented verification recipe (point the API at an unreachable host via env var) silently connect to the real database instead | 4.x |
| KI-037 | ~~The only remaining build warning is in `ExternalService`~~ **RESOLVED 2026-10-22** | `NotionClientService` now checks `result?.Markdown is null` and throws an explicit `InvalidOperationException` (classified as `502 → notion_invalid_response`) instead of dereferencing a possibly-null result. `dotnet build --no-incremental` is now **0 warnings, 0 errors**, which is what the definition of done requires | 2.4 |
| KI-038 | Deployment relies on startup migration; CI has no migration step | `.github/workflows/main_jdb-portfolio-api.yml` builds, publishes and deploys only. `Database:AutoMigrate` now exists and is honoured, but it **defaults to `true`** precisely because of this — turning it off would stop migrations being applied in production. The multi-replica race (old KI-018) is therefore controllable, not fixed | 1.8 / 4.x |
| KI-039 | EF Core version skew: Npgsql 10.0.3 targets EF Core 10.0.4, the solution uses 10.0.10 | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 declares `Microsoft.EntityFrameworkCore.Relational >= 10.0.4`, so NuGet resolves 10.0.4 where EF Design's 10.0.10 does not flow (`PrivateAssets="all"`). Visible as `MSB3277` in `Persistence.Tests` and later in `Api.Tests`. **Mitigated 2026-10-22:** `Microsoft.EntityFrameworkCore.Relational` 10.0.10 is now pinned in `Persistence.csproj`, so the whole graph aligns and `MSB3277` no longer appears anywhere. **The root fix is still upgrading Npgsql to 10.0.10** — a dependency upgrade, so it needs approval | 4.x |
| KI-040 | `Experience.Projects` is mapped by convention before the hand-built `AfterMap` overwrites it | `PortfolioMappingProfile` — `ExperienceEntity → Experience` maps `Projects` (entity) from `Projects` (domain) by name, which would build detached entities, and only then does `AfterMap` replace the collection. Now explicitly ignored. The same pattern remains on `Experience → ExperienceEntity` | 3.3 |
| KI-041 | `Recommendation.PersonName` rejects `null` but not whitespace | `Recommendation.cs:22` — `personName ?? throw`. A `person_name = ''` row passes the constructor and renders as a nameless testimonial. It does **not** crash the mapping (unlike `Role`, which threw), which is why it never showed up as a 500 the way blank roles did. Same defect class as 1.5, different consequence — and note that 1.5 was resolved **without** any database constraint, so nothing stops this row existing | 3.3 |

## P3 — cleanup and polish

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-025 | `AddControllers()` / `MapControllers()` are registered but no controllers exist | `Program.cs:12,44` | 4.1 |
| KI-026 | ~~`/auth/me` is `async` with no `await` (CS1998)~~ **RESOLVED 2026-10-22** | the lambda is synchronous now. JWT issuance moved out of the endpoint entirely into `JwtTokenService` (interface `IAuthTokenService` in Application), so it is unit-tested without HTTP | 2.5 |
| KI-027 | Notion client rethrows `new HttpRequestException(msg)`, losing the inner exception | `NotionClientService.cs` catch blocks | 4.6 |
| KI-028 | Notion client uses an absolute URL despite `BaseAddress`, sets headers in the constructor, and has no retry/timeout | `NotionClientService.cs:24-33` | 4.6 |
| KI-029 | Notion markdown is parsed as XML wrapped in `<root>`, fragile to malformed input or a literal `</root>` | `NotionPageParserUtility.cs` | 4.7 |
| KI-030 | `/overview` hardcodes display copy and runs three counts sequentially | `PortfolioService.GetOverview` | 4.8 |
| KI-033 | `opencode.json` is committed and points at `127.0.0.1` | repo root | decide |
| KI-034 | No health-check endpoint; nothing to probe in Azure | `Program.cs` | 4.x |
| KI-035 | ~~`Experience.Description`/`Responsibility`/`Type` are not explicitly mapped~~ **RESOLVED 2026-10-22** | they are name-mapped and AutoMapper **does** write to the private setters. Proven by `Map_ExperienceEntity_To_Experience_PopulatesDescriptionResponsibilityAndType` in `Persistence.Tests`. The behaviour was never broken — only unverified | 3.3 |

## Resolved

| ID | Issue | How it was resolved |
|---|---|---|
| KI-001 | `docker compose up` failed: `api` depended on an undefined `db` service | the `depends_on` block and the `Host=db` connection string were removed; the compose stack now takes a full connection string from `docker/.env` and expects an external database (2026-10-22) |
| KI-002 | `Persistence` referenced `Application`, breaking the dependency rule | the `ProjectReference` was removed from `Persistence.csproj` and the dead `using JDB_Portfolio_API.Application.Common;` was deleted from `EfPortfolioExperienceRepository.cs:2`; the `using` was the only `Application` usage in the project. The project graph now matches the rule (2026-10-22) |
| KI-017 | Design-time factory hardcoded a developer's user-secrets GUID | `PortfolioContextFactory` now reads the id from `JDB_USER_SECRETS_ID` and the connection string from `ConnectionStrings__JDBConnection`, falling back to `appsettings.Development.json` (now `optional: true`) (2026-10-22) |
| KI-018 | `Database.Migrate()` at startup races with other replicas | **partially** — `Migrate()` is now gated behind `Database:AutoMigrate`. It defaults to `true` because CI has no migration step (see KI-038), so the race is controllable rather than fixed (2026-10-22) |
| KI-020 | `IPortfolioTechStackRepository` registered twice | the duplicate registration in `ConfigurationSettings.cs` was removed (2026-10-22) |
| KI-021 | Read queries track entities and use a 4-level cartesian join | `.AsNoTracking()` on all read paths and `.AsSplitQuery()` on the two multi-level includes — the experience (4-level) and project (3-level) queries (2026-10-22) |
| KI-022 | Dead types: `IMapper<,,>`, `IService<T>`, `ExperienceId`, `NotionEntity`, `PortfolioNotionPage`, `GetCountAsync`, `GetByIdAsync` | all deleted. `GetCountAsync`/`GetByIdAsync` were removed from `IRepository<T>` and `BasePortfolioRepository` (2026-10-22) |
| KI-023 | `BaseService<T>` duplicates each service's repository field and adds no behaviour | `BaseService<T>` and `IService<T>` were replaced by `PortfolioServiceBase<T, TRepository>`, which holds the repository as a `protected` field and provides the shared `ExecuteAsync` catch-and-wrap behaviour. Each service now keeps one repository field; the unreachable `??` fallbacks were removed (2026-10-22) |
| KI-024 | `dotnet-ef` is not installed and there is no tool manifest, yet docs say `dotnet tool restore` | a root-level `dotnet-tools.json` already existed with `dotnet-ef` 10.0.10. `dotnet tool restore` and `dotnet ef --version` both work (2026-10-22) — the register entry was stale, not the repository |
| KI-031 | `docker/.env.example` set `Jwt__Expires`, a key the app does not read | replaced with `Jwt__ExpirationInMinutes` (2026-10-22) |
| KI-032 | `jdb_pgdata` volume declared but unused | the volume declaration was removed with the `db` service (2026-10-22) |

## How to report a new issue

Add a row with:

1. an ID (`KI-NNN`, next free number);
2. the observed behaviour, not the suspected cause;
3. a reproducible evidence snippet (command, file:line, SQL);
4. the plan item that will fix it, or `—` if unplanned.
