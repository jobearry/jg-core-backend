# Technology stack

Versions are taken from the `.csproj` files at the time of writing. Keep this
table in sync when a package changes.

## 1. Platform

| Item | Value |
|---|---|
| Target framework | `net10.0` |
| SDK in use | 10.0.402 |
| Language | C# 14 (implicit), nullable enabled, implicit usings enabled |
| Solution format | `.slnx` (`src/JDB_Portfolio_API.slnx`) |
| OS target | Linux containers |

## 2. Packages

### Presentation — `JDB_Portfolio_API`

| Package | Version | Purpose | Notes |
|---|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | Validate JWTs issued by `/auth/login` | `MapInboundClaims = false` |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.10 | Design-time migrations | `PrivateAssets=all` |
| `Swashbuckle.AspNetCore` | 10.2.3 | OpenAPI + Swagger UI | UI at `/swagger`, Development only |

### Application — `JDB_Portfolio_API.Application`

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 | `ILogger<T>` — the logging **contract** only. No provider, no EF, no ASP.NET, so the composition root still decides where output goes. Added in plan 2.4: without it the Application layer cannot report a failure at all, which is why a 503 used to reach the client with nothing on the server to explain it |

The only other reference is `Domain`.

### Domain — `JDB_Portfolio_API.Domain`

No package references, by design.

### Persistence — `JDB_Portfolio_API.Persistence`

| Package | Version | Purpose |
|---|---|---|
| `AutoMapper` | 16.2.0 | Entity ↔ domain mapping |
| `Microsoft.EntityFrameworkCore` | 10.0.10 | ORM |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.10 | Migrations |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.10 | `dotnet ef` integration |
| `Microsoft.Extensions.Configuration` (+ `.FileExtensions`, `.Json`, `.UserSecrets`) | 10.0.10 | Design-time connection string |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | PostgreSQL provider |

### ExternalService — `JDB_Portfolio_API.ExternalService`

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Extensions.Options` | 10.0.10 | `IOptions<NotionSettings>` |

HTTP itself comes from `IHttpClientFactory` (`AddHttpClient`) registered in the
presentation project.

## 3. Runtime services

| Service | Version | Notes |
|---|---|---|
| PostgreSQL | **not pinned** | The compose file has no `db` service, so no image tag exists. Add `postgres:17-alpine` (or similar) when fixing it. |
| Notion API | `2026-03-11` | Sent as the `Notion-Version` header; configurable via `Notion:Version`. |

## 4. Tooling

| Tool | Status | Notes |
|---|---|---|
| `dotnet-ef` | **available via the root tool manifest** | `dotnet-tools.json` at the repo root pins `dotnet-ef` 10.0.10. Run `dotnet tool restore` once, then `dotnet ef ...` works. |
| Docker / Compose v2 | available | Compose file currently invalid |
| GitHub Actions | active | build + deploy only |
| Azure CLI | via `azure/login@v2` | OIDC federated credentials |

## 5. Expected but absent

These are recommended and currently missing. Adding any of them requires
approval (AGENTS.md §10).

| Concern | Candidate | Why |
|---|---|---|
| Testing | `Testcontainers.PostgreSql` | xUnit and `Microsoft.AspNetCore.Mvc.Testing` are in place (four projects, 179 tests, repository ports faked). What is still missing is a **database-backed** test: nothing exercises the EF model or a migration against real PostgreSQL |
| Assertions | `Shouldly` or `FluentAssertions` | optional; xUnit asserts are enough to start |
| Validation | `FluentValidation.AspNetCore` or a hand-written endpoint filter | minimal APIs do not validate automatically |
| Resilience | `Microsoft.Extensions.Http.Resilience` (Polly v8) | Notion call has no retry/timeout |
| Health checks | `Microsoft.Extensions.Diagnostics.HealthChecks` + Npgsql check | nothing to probe today |
| Structured logging | Serilog / OpenTelemetry | `ILogger` reaches the application services (2.4), but there is no structured sink and no correlation id beyond `traceId` |
| Analyzers | `Directory.Build.props` with `TreatWarningsAsErrors`, `Microsoft.CodeAnalysis.NetAnalyzers` | the build is at 0 warnings, but nothing prevents the next one |
| API versioning | `Asp.Versioning.Http` | path version is hardcoded |
| Migrations at deploy | EF bundle or a job step | `Migrate()` at startup is a race |

## 6. Why these choices

- **Minimal APIs over controllers** — the surface is small and read-only; the
  project already uses endpoint extension classes. `AddControllers()` is a
  leftover and can be removed.
- **AutoMapper** — chosen to keep persistence entities out of the domain. It is
  a legitimate use, but the profile is currently the riskiest file in the
  solution. An alternative is explicit mapping methods, which would be more
  code but compile-time checked.
- **EF Core + Npgsql** — PostgreSQL is the production database; the context was
  originally scaffolded (hence `partial` and `OnModelCreatingPartial`).
- **JWT bearer** — a single shared key is enough for a machine-to-machine
  portfolio API; it is not user authentication.
