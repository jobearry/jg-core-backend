# Technology stack

Versions are taken from the `.csproj` files at the time of writing. Keep this
table in sync when a package changes.

## 1. Platform

| Item | Value |
|---|---|
| Target framework | `net10.0` |
| SDK in use | 10.0.114 |
| Language | C# 14 (implicit), nullable enabled, implicit usings enabled |
| Solution format | `.sln` (`JGBackend.sln`) |
| OS target | Linux containers |

## 2. Packages

### `Portfolio` (single Web SDK project)

| Package | Version | Purpose | Notes |
|---|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 | Validate JWTs issued by `/auth/login` | `MapInboundClaims = false` |
| `Microsoft.EntityFrameworkCore` | 10.0.10 | ORM | |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.10 | Version pin | avoids MSB3277 with Npgsql |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.10 | Design-time migrations | `PrivateAssets=all` |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.10 | `dotnet ef` integration | `PrivateAssets=all` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | PostgreSQL provider | |
| `Swashbuckle.AspNetCore` | 10.2.3 | OpenAPI + Swagger UI | UI at `/swagger`, Development only unless `Swagger:Enabled` |

`src/Core` and `src/JGBackend` remain separate until phase 3. Portfolio has no
`ProjectReference` to another module today. Entity ↔ domain mapping is
hand-written in `Shared/Persistence/Mappers/`; AutoMapper was removed in phase 2.
HTTP clients come from `IHttpClientFactory`, registered in
`Features/DependencyInjection.cs`.

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
| Testing | `Testcontainers.PostgreSql` | Portfolio's four xUnit projects cover domain, application, persistence mapping, and API behavior, with repository ports faked. What is still missing is a **database-backed** test: nothing exercises the EF model or a migration against real PostgreSQL |
| Assertions | `Shouldly` or `FluentAssertions` | optional; xUnit asserts are enough to start |
| Validation | `FluentValidation.AspNetCore` or a hand-written endpoint filter | minimal APIs do not validate automatically |
| Resilience | `Microsoft.Extensions.Http.Resilience` (Polly v8) | the Notion client now has a 15s timeout and one manual retry on 429/5xx; Polly would add circuit breaking |
| Health checks | `Microsoft.Extensions.Diagnostics.HealthChecks` + Npgsql check | nothing to probe today |
| Structured logging | Serilog / OpenTelemetry | `ILogger` reaches the application services, but there is no structured sink and no correlation id beyond `traceId` |
| Analyzers (third-party) | `Microsoft.CodeAnalysis.NetAnalyzers` | SDK analyzers run by default, and a root `Directory.Build.props` turns warnings into errors (2026-10-23); the extra package is not needed yet |
| API versioning | `Asp.Versioning.Http` | path version is hardcoded |
| Migrations at deploy | EF bundle or a job step | `Migrate()` at startup is a race |

## 6. Why these choices

- **Minimal APIs over controllers** — the surface is small and read-only; the
  project uses endpoint extension classes and has no controller registrations.
- **Hand-written mappers** — chosen over AutoMapper after the profile repeatedly
  hid bad conversions. More code, but explicit and testable at runtime; the
  mappers now live in `Shared/Persistence/Mappers/`.
- **EF Core + Npgsql** — PostgreSQL is the production database; the context was
  originally scaffolded (hence `partial` and `OnModelCreatingPartial`).
- **JWT bearer** — a single shared key is enough for a machine-to-machine
  portfolio API; it is not user authentication.
