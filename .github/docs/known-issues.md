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
| KI-002 | `Persistence` references `Application`, breaking the dependency rule | `Persistence.csproj` `ProjectReference`; unused `using` in `EfPortfolioExperienceRepository.cs:2` | 1.1 |
| KI-003 | No tests at all | `dotnet test` finds no test projects | 3.5 |
| KI-004 | Domain entities are the public API contract | `Results.Ok(result.Value)` in 6 endpoints | 2.1 |
| KI-005 | `Period.FinishedAt` is always null after mapping, so `Duration` grows daily for past roles | `PortfolioMappingProfile.cs:92` `new Period(src.StartedAt, null)`; no `finished_at` column | 1.3 / D1 |
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
| KI-014 | Services catch `HttpRequestException` around database calls; that type never occurs there | `Portfolio*Service.cs` try/catch blocks | 2.4 |
| KI-015 | No `CancellationToken` anywhere; abandoned requests keep querying | all repository/service/endpoint signatures | 2.4 |
| KI-016 | No application logging and no global exception handler | no `ILogger` in Domain/Application/Persistence; `Program.cs` has no handler | 2.4 |
| KI-017 | Design-time factory hardcodes a developer's user-secrets GUID | `PortfolioContextFactory.cs:14` | 1.8 |
| KI-018 | `Database.Migrate()` at startup races with other replicas | `Program.cs:26` | 1.8 |
| KI-019 | All timestamps are `timestamp without time zone` | `PortfolioContext.cs` column types | 4.x |
| KI-020 | `IPortfolioTechStackRepository` registered twice | `ConfigurationSettings.cs:39` and `:45` | 1.7 |
| KI-021 | Read queries track entities and use a 4-level cartesian join | no `AsNoTracking`/`AsSplitQuery` in repositories | 1.6 |

## P3 — cleanup and polish

| ID | Issue | Evidence | Plan |
|---|---|---|---|
| KI-022 | Dead types: `IMapper<,,>`, `IService<T>`, `ExperienceId`, `NotionEntity`, `PortfolioNotionPage`, `GetCountAsync`, `GetByIdAsync` | solution-wide grep returns only declarations | 1.7 |
| KI-023 | `BaseService<T>` duplicates each service's repository field and adds no behaviour | `Application/Services/BaseService.cs` + subclasses | 2.3 |
| KI-024 | `dotnet-ef` is not installed and there is no tool manifest, yet docs say `dotnet tool restore` | no `.config/dotnet-tools.json`; `dotnet ef` → "not found" | 1.8 |
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
| KI-031 | `docker/.env.example` set `Jwt__Expires`, a key the app does not read | replaced with `Jwt__ExpirationInMinutes` (2026-10-22) |
| KI-032 | `jdb_pgdata` volume declared but unused | the volume declaration was removed with the `db` service (2026-10-22) |

## How to report a new issue

Add a row with:

1. an ID (`KI-NNN`, next free number);
2. the observed behaviour, not the suspected cause;
3. a reproducible evidence snippet (command, file:line, SQL);
4. the plan item that will fix it, or `—` if unplanned.
