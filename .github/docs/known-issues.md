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
| KI-003 | ~~No tests at all~~ **PARTIALLY RESOLVED 2026-10-22** | `tests/JDB_Portfolio_API.Domain.Tests` (19 tests) and `tests/JDB_Portfolio_API.Application.Tests` (5 tests) now exist; `dotnet test` is green. **Still missing:** `Persistence.Tests` (mapping — mandatory per the testing rules), `Api.Tests` (endpoint contracts), and the entity-invariant / VO-equality tests that belong to 3.1 and 3.3 | 3.5 |
| KI-004 | Domain entities are the public API contract | `Results.Ok(result.Value)` in 6 endpoints | 2.1 |
| KI-005 | `Period.FinishedAt` is always null after mapping, so `Duration` grows daily for past roles | `PortfolioMappingProfile.cs:92` `new Period(src.StartedAt, null)`; no `finished_at` column. **The value object itself is correct** — `Period.Duration` stops at `FinishedAt` when it is set, proven by `PeriodTests.Duration_WhenFinishedAtIsSet_StopsAtFinishedAt`. The defect is in the mapping only | 1.3 / D1 |
| KI-006 | `/api/v1/skill` returns `200` with `null` on failure | `PortfolioSkillEndpoints.cs:12-13` — no `IsSuccess` check | 2.5 |
| KI-007 | Every failure returns `502` and leaks the exception message | `Results.Problem(result.Error, statusCode: 502)` in all endpoints | 2.2 |
| KI-008 | `Role`, `Company`, `Period` have reference equality | no `Equals`/`GetHashCode`/`record` in `Domain/ValueObjects` | 3.1 |
| KI-009 | Anemic entities: `Project`, `TechStack`, `TechStackCategory` have no invariants; `Recommendation.Description` unvalidated | public setters, `= null!`, no validation in constructors | 3.3 |
| KI-010 | Authorization is not enforced on any portfolio endpoint | `RequireAuthorization()` commented out in 4 files (5 occurrences); `/skill` has none at all | 2.5 |

## P2 — correctness and robustness risk

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-011 | TechStack reverse mapping maps `string` → `int`; will throw on the first write | `PortfolioMappingProfile.cs:38` | 1.2 / D2 |
| KI-012 | `Project.Period` has no column and no mapping; value is silently discarded | `Project.cs:19`, `ProjectEntity` has no period columns | 1.4 |
| KI-013 | Empty-string `role` / `career_position` throws inside AutoMapper and 500s the whole collection | `new Role(src.CareerPosition)`; `NOT NULL` permits `''` | 1.5 |
| KI-014 | Services catch `HttpRequestException` around database calls; that type never occurs there | **partially fixed 2026-10-22** — the four portfolio services now use the shared `PortfolioServiceBase.ExecuteAsync`, which catches everything except cancellation. `PortfolioService` (overview/Notion) still has `catch (HttpRequestException)` around its three **database** calls; its catch is correct for the Notion call only | 2.4 |
| KI-015 | No `CancellationToken` anywhere; abandoned requests keep querying | all repository/service/endpoint signatures. The new base explicitly does **not** swallow `OperationCanceledException`, so cancellation will propagate once tokens are threaded through | 2.4 |
| KI-016 | No application logging and no global exception handler | no `ILogger` in Domain/Application/Persistence; `Program.cs` has no handler | 2.4 |
| KI-019 | All timestamps are `timestamp without time zone` | `PortfolioContext.cs` column types | 4.x |
| KI-036 | User secrets silently override environment variables | `Program.cs:9` calls `AddUserSecrets<Program>()` **after** `CreateBuilder(args)`, so the secrets file is added last and wins over env vars and command-line args. `ConnectionStrings__JDBConnection=...` is therefore ignored on a developer machine, and a `secrets.json` present on the server would override production configuration | 4.x |
| KI-037 | The only remaining build warning is in `ExternalService` | `NotionClientService.cs:39` — `result.Markdown` dereferences a possibly-null `result` from `ReadFromJsonAsync<T>()`. CS8602. Pre-existing; unrelated to the cleanups | 4.6 |
| KI-038 | Deployment relies on startup migration; CI has no migration step | `.github/workflows/main_jdb-portfolio-api.yml` builds, publishes and deploys only. `Database:AutoMigrate` now exists and is honoured, but it **defaults to `true`** precisely because of this — turning it off would stop migrations being applied in production. The multi-replica race (old KI-018) is therefore controllable, not fixed | 1.8 / 4.x |

## P3 — cleanup and polish

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-025 | `AddControllers()` / `MapControllers()` are registered but no controllers exist | `Program.cs:12,44` | 4.1 |
| KI-026 | `/auth/me` is `async` with no `await` (CS1998) | `AuthEndpoints.cs:57` | 2.5 |
| KI-027 | Notion client rethrows `new HttpRequestException(msg)`, losing the inner exception | `NotionClientService.cs` catch blocks | 4.6 |
| KI-028 | Notion client uses an absolute URL despite `BaseAddress`, sets headers in the constructor, and has no retry/timeout | `NotionClientService.cs:24-33` | 4.6 |
| KI-029 | Notion markdown is parsed as XML wrapped in `<root>`, fragile to malformed input or a literal `</root>` | `NotionPageParserUtility.cs` | 4.7 |
| KI-030 | `/overview` hardcodes display copy and runs three counts sequentially | `PortfolioService.GetOverview` | 4.8 |
| KI-033 | `opencode.json` is committed and points at `127.0.0.1` | repo root | decide |
| KI-034 | No health-check endpoint; nothing to probe in Azure | `Program.cs` | 4.x |
| KI-035 | `Experience.Description`/`Responsibility`/`Type` are not explicitly mapped; population is implicit and untested | `PortfolioMappingProfile.cs:88-117` | 3.3 |

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
