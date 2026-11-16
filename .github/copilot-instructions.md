# Copilot instructions for this repository

GitHub Copilot (and other agentic coding assistants) should treat this file as
the entry point for repository guidance. It only holds what is true everywhere;
per-area rules live next to the paths they govern, under
`.github/instructions/`:

| When editing… | Read |
| --- | --- |
| `Generic/Generic.API/**` | `instructions/generic-api.instructions.md` |
| `Generic/Generic.Features/**` | `instructions/generic-features.instructions.md` |
| `Generic/Generic.Infra/**` | `instructions/generic-infra.instructions.md` |
| `docker/**`, `Dockerfile` | `instructions/docker.instructions.md` |
| `.github/workflows/**` | `instructions/workflows.instructions.md` |

Each scoped file carries YAML frontmatter (`applyTo:`), so Copilot loads it
only when matching files are in scope.

## Tools we use

| Area | Tool |
| --- | --- |
| Runtime / SDK | .NET 10 (`net10.0`), C# 14, nullable reference types, implicit usings |
| Web framework | ASP.NET Core minimal APIs (no controllers) |
| HTTP surface | `IEndpointRouteBuilder` route groups + `MapXxxEndpoints` per slice |
| API docs | Swashbuckle (OpenAPI v1), Bearer JWT security definition, Swagger UI in Development |
| Auth | JWT (HMAC-SHA256) via `System.IdentityModel.Tokens.Jwt` + `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Error handling | RFC 7807 `ProblemDetails` via the built-in `IExceptionHandler` (`GlobalExceptionHandler`) |
| Health | Built-in health checks (`GET /healthz`, anonymous) |
| Rate limiting | Built-in `System.Threading.RateLimiting` fixed window (5 req/min/IP on login) |
| Tests | none yet — CI builds only; add a test project before adding meaningful tests |
| Containers | Docker + Docker Compose, alpine base images, repo root as build context |
| CI/CD | GitHub Actions: `ci.yaml` (restore + Release build of `Generic/Generic.sln`), `deploy.yaml` (OIDC login → Azure App Service `jdbweb`) |
| Secrets | environment variables / `.env` (git-ignored) + `.env.example` as the template; never commit credentials |
| Version control | git, feature branches, PRs against `master`; conventional-commit style messages (`feat:`, `docs:`, `chore:`) |

## Practices we implement

- **Vertical slice architecture** — features are self-contained: endpoint
  mapping, DTOs, handlers/services, and mapping all live together in
  `Generic/Generic.Features/<FeatureName>/`.
- **One-way dependency rule** — `Generic/Generic.API` → `Generic/Generic.Features`
  → `Generic/Generic.Infra`. Slices never reference the host; shared concerns
  go down into `Generic.Infra`, never sideways into another slice.
- **Thin composition root** — `Program.cs` only wires services and the pipeline;
  startup concerns live in `Generic/Generic.API/Configuration/`.
- **Endpoints-only HTTP** — no controllers, no `ApplicationPart`; one
  `app.MapFeatures()` maps everything; slices register via `MapFeatures`.
- **Fail consistently** — unhandled exceptions become RFC 7807 ProblemDetails;
  expected failures return ProblemDetails or the right status code, never a raw 500.
- **Secure by default** — JWT issuer/audience/lifetime validated; fixed-time key
  comparison; `.RequireAuthorization()`; login rate limited; only anonymous
  endpoints are `/healthz` and `/api/v1/auth/login`.
- **No premature abstraction** — add shared infrastructure only when a second
  slice needs it; duplication across slices is acceptable until then.
- **Docs stay honest** — update this file / the scoped instructions when
  structure, commands, or practices change; keep `ci.yaml`/`deploy.yaml`
  aligned with the solution layout.

## Project overview

Vertical slice ASP.NET Core Web API; three projects:

- `Generic/Generic.API` — host: `Program.cs`, startup configuration, Dockerfile entry point.
- `Generic/Generic.Features` — feature slices. Currently `Auth/` (JWT login, `me`) and `Health/` (diagnostic status).
- `Generic/Generic.Infra` — shared infrastructure (`JwtOptions`, `GlobalExceptionHandler`, DI wiring).

The previous portfolio CRUD/Notion implementation was removed; an external
backend owns that data now. EF Core / SQL Server are not used. The full
previous implementation is preserved in git history —
`git show <pre-restructure-commit>:<path>`.

## Build and run

```sh
dotnet restore Generic/Generic.sln
dotnet build Generic/Generic.sln --configuration Release --no-restore
dotnet run --project Generic/Generic.API/Generic.API.csproj

docker compose --project-directory . -f docker/compose.yaml up --build
docker build -f docker/Dockerfile -t myapp .
```

Local `dotnet run` works out of the box in Development (an insecure, loudly
labelled dev-only key baked into `appsettings.Development.json` signs tokens
and must never be used elsewhere). Production/staging must provide `Jwt__Key`,
`Jwt__Issuer`, `Jwt__Audience`, `Jwt__Expires` via environment variables or
user secrets — startup fails fast with a clear error if they're missing.

## Change guidance (applies everywhere)

- Target .NET 10; keep nullable reference types and implicit usings.
- Never commit credentials; `.env.example` is the template.
- Keep HTTP handling in slices, composition in `Generic.API`, shared
  infrastructure in `Generic.Infra`.
- Keep `.github/workflows/ci.yaml` and `deploy.yaml` aligned with the solution.
