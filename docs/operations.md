# Operations

How to run, configure, and deploy the API.

## 1. Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.x | 10.0.114 in use |
| PostgreSQL | any recent | local install or container |
| `dotnet-ef` | matching EF 10 | available via the root tool manifest — run `dotnet tool restore` |
| Docker + Compose v2 | 20.10+ | optional; the API container uses an external PostgreSQL instance (see §4) |

## 2. Local development

```bash
# 1. restore + build
dotnet restore JGBackend.sln
dotnet build   JGBackend.sln

# 2. configure secrets (user-secrets, host project)
dotnet user-secrets init --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "ConnectionStrings:JDBConnection" "Host=localhost;Port=5432;Database=jdb_portfolio;Username=postgres;Password=…" --project src/JGBackend/JGBackend.csproj
# Local writes use the same connection/user as reads. Production uses a separate
# DML-only writer credential in the JDBConnectionWrite App Service setting.
dotnet user-secrets set "ConnectionStrings:JDBConnectionWrite" "Host=localhost;Port=5432;Database=jdb_portfolio;Username=postgres;Password=…" --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "Jwt:Key"       "<at least 32 bytes>" --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "Jwt:Issuer"    "jdb_api"            --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "Jwt:Audience"  "PortfolioApiUsers"  --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "Notion:Token"  "secret_…"           --project src/JGBackend/JGBackend.csproj
dotnet user-secrets set "Github:Token"  "<GitHub personal access token>" --project src/JGBackend/JGBackend.csproj

# 3. apply migrations
dotnet ef database update \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/JGBackend/JGBackend.csproj

# 4. run
dotnet run --project src/JGBackend/JGBackend.csproj
```

| Profile | URL |
|---|---|
| `http` | http://localhost:5031 |
| `https` | https://localhost:7054 / http://localhost:5031 |
| Swagger | http://localhost:5031/swagger |

Migrations are applied with `dotnet ef database update` using the host as the
startup project. `JGBackend` also applies them at startup when
`Database:AutoMigrate` is enabled (default `true`); production sets it to `false`
because the deploy workflow owns migrations there (see §5).

## 3. Configuration matrix

| Key | Local | Docker | Azure |
|---|---|---|---|
| `ConnectionStrings:JDBConnection` | user-secrets | `ConnectionStrings__JDBConnection` in `.env` | App Service setting (`portfolio_app`, `SELECT` only) |
| `ConnectionStrings:JDBConnectionWrite` | user-secrets, same value as `JDBConnection` | `ConnectionStrings__JDBConnectionWrite` in `.env`, same value as the read connection | App Service setting (`portfolio_writer`, DML only) |
| `Jwt:Key` | user-secrets | `Jwt__Key` | App Service setting |
| `Jwt:Issuer` | user-secrets | `Jwt__Issuer` | App Service setting |
| `Jwt:Audience` | user-secrets | `Jwt__Audience` | App Service setting |
| `Jwt:ExpirationInMinutes` | default 60 | `Jwt__ExpirationInMinutes` | optional |
| `Notion:Token` | user-secrets | `Notion__Token` | App Service setting |
| `Notion:Version` | no in-code default; `2026-03-11` in `.env.example` | `Notion__Version` | set it — sent as the `Notion-Version` request header |
| `Github:Token` | user-secrets | `Github__Token` | App Service setting |
| `Github:BaseUrl` | `https://api.github.com` | `Github__BaseUrl` | optional |
| `Cors:AllowedOrigins` | `appsettings.json` | `Cors__AllowedOrigins__0`, `__1`, … | App Service setting |
| `Cors:DevelopmentOrigins` | `appsettings.json` | applied only when `ASPNETCORE_ENVIRONMENT=Development` | ignored |
| `Swagger:Enabled` | `appsettings.json` (`false`) | `Swagger__Enabled` | optional; `true` exposes `/swagger` |

`JDBConnection` feeds `PortfolioContext` (reads and migrations); `JDBConnectionWrite`
feeds `PortfolioWriteContext` (admin writes). Locally and in Docker both settings may
hold the same connection string, provided that user can write. In production they must
differ: `portfolio_app` is `SELECT`-only and `portfolio_writer` has `INSERT`, `UPDATE`
and `DELETE` on the portfolio tables but no DDL. The GitHub Actions migration secret
`PRODUCTION_CONNECTION_STRING` is unchanged and remains the only owner connection.
See [ADR-0007](adr/0007-admin-authorization-and-writer-role.md).

Admin write routes live under `/api/v1/admin` and require the `role=Admin` token
from `/api/v1/auth/login`. They are capped at 30 requests/minute per IP by the
`admin-write` policy and use a CORS policy that permits `PUT` and `DELETE` from the
configured origins. CRUD is available for experience, project, recommendation,
skill and skill-category, plus project tech-stack replacement; the request/response
contract and status codes are in [`api-reference.md`](api-reference.md) §3.
Public reads do not present a token and never reach `PortfolioWriteContext`.

Origins are configuration so that a new domain or a Vercel preview deployment
(`https://demo-git-my-branch-team.vercel.app`) is an added setting rather than a code
change and a redeploy. Adding one is still a security review item — see
`docs/api-reference.md` §6.

The GitHub token stays server-side and is used only for upstream GitHub requests.
Current GitHub endpoints are anonymous but return public repository data only;
the API checks visibility before returning repository details or languages.
Grant the GitHub token only the repository metadata access required for the
intended repositories. Optional API-JWT access to private repository data is
not implemented; see the [deferred proposal](../plan.md).
Its presence is not authorization to implement it.

Note: `.env.example` sets `Jwt__ExpirationInMinutes`, matching the key
the app reads.

## 4. Docker

One image (`docker/JGBackend.Dockerfile`) serves both modules from the single
host. `docker/compose.yaml` runs the **API only**; PostgreSQL is external.

```bash
cp .env.example .env   # then set both JDBConnection and JDBConnectionWrite
docker compose --project-directory . -f docker/compose.yaml up --build
```

The connection string is supplied whole, with no `Host=db` shortcut, because
there is no `db` service:

```ini
ConnectionStrings__JDBConnection=Host=host.docker.internal;Port=5432;Database=jg-portfolio;Username=postgres;Password=postgres;Ssl Mode=Disable
```

`host.docker.internal` reaches a PostgreSQL running on the host; the compose
file maps that name on Linux as well as Docker Desktop. For a hosted database
use its host and the SSL settings it requires. When `Database:AutoMigrate` is
enabled (default `true`), the API applies pending migrations at startup and
fails to boot if the database is unreachable. Set `Database__AutoMigrate=false`
to skip that step.

Image build (works independently of compose):

```bash
docker build -f docker/JGBackend.Dockerfile -t jg-backend .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__JDBConnection=Host=<host>;Port=5432;Database=jdb_portfolio;Username=…;Password=…" \
  -e "ConnectionStrings__JDBConnectionWrite=Host=<host>;Port=5432;Database=jdb_portfolio;Username=…;Password=…" \
  -e "Jwt__Key=<32+ bytes>" -e "Jwt__Issuer=jdb_api" -e "Jwt__Audience=PortfolioApiUsers" \
  -e "Notion__Token=secret_…" \
  jg-backend
```

The container listens on `8080` (`ASPNETCORE_URLS=http://+:8080`), runs as
`$APP_UID` (non-root), and the build context must be the repository root.

## 5. Deployment

One deployable, one workflow:

| Workflow | App | Trigger |
|---|---|---|
| `.github/workflows/deploy.yaml` | `jdbweb` | push to `master` touching `src/**`, `tests/Portfolio/**`, the solution, or the tool manifest |

It is path-filtered so the app ships only when its code changes;
`.github/workflows/ci.yaml` builds and tests the whole solution on every push
and pull request. Runs are serialized by a `jdbweb-production-deploy`
concurrency group so two deploys cannot race the database.

`deploy.yaml`:

1. Trigger: push to `master` under `src/**` or `tests/Portfolio/**`, or manual
   dispatch.
2. `dotnet restore` → `dotnet tool restore` → `has-pending-model-changes` gate (fails
   if the model changed without a migration) → `dotnet build -c Release` →
   `dotnet publish -o ./publish`.
3. `dotnet ef database update` against the production connection string, before the
   app is replaced. A failure stops the deploy; the running app stays up (KI-038).
4. `azure/login@v2` with OIDC (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
   `AZURE_SUBSCRIPTION_ID`).
5. `azure/webapps-deploy@v2` to app `jdbweb`.

The migration step needs the repository secret `PRODUCTION_CONNECTION_STRING`. Use
the **direct** Neon endpoint (no `-pooler`), in Npgsql key-value form — not the
`postgresql://` URI — because pooling can break DDL and session state.

Production is configured to let GitHub Actions own migrations. The
**`Database__AutoMigrate=false`** App Service setting has been applied and
verified by the owner (2026-10-23). If recreating the App Service or its
settings, restore this configuration only after the **Apply database
migrations** workflow step has succeeded against production:

1. Open the Azure Portal and select the `jdbweb` App Service.
2. Go to **Settings → Environment variables** (called **Configuration** in some
   portal views).
3. Add or update the application setting `Database__AutoMigrate` to `false`.
   The double underscore maps to the .NET configuration key
   `Database:AutoMigrate`.
4. Save the setting. Azure restarts the app; after it is healthy, confirm the
   setting is present and the app starts successfully.

Saving the setting restarts the app. Confirm it starts successfully afterwards.
This is an Azure configuration, not a code or workflow change. The GitHub
Actions migration step continues to apply pending migrations before each
deployment; this setting only prevents the app from also applying migrations
during startup. It is separate from the GitHub `PRODUCTION_CONNECTION_STRING`
secret used by that workflow.

Tests live in `tests/` (four xUnit projects under `tests/Portfolio/`, covering
both modules and the composed host). CI runs them with `dotnet test
JGBackend.sln`; the deploy workflow does not, because CI already gates the
branch.

Swagger is enabled when `ASPNETCORE_ENVIRONMENT=Development` or when the
`Swagger:Enabled` setting is `true`. Production leaves it unset (off); turn it on
for a smoke test and off again.

## 6. Runbook

| Symptom | Likely cause | Action |
|---|---|---|
| App fails at startup with an Npgsql connection error | `Migrate()` ran with a bad/missing connection string | check `ConnectionStrings__JDBConnection` |
| Deploy fails at "Apply database migrations" | migration could not run: dirty data, unreachable DB, or a missing/wrong `PRODUCTION_CONNECTION_STRING` | read the step log; the old app is still live. Fix the data or secret and re-run |
| `docker compose up` fails with "required variable ConnectionStrings__JDBConnection is missing" | `.env` is missing, or Compose is using `docker/` as the project directory | from the repo root, run `cp .env.example .env`, fill it in, then `docker compose --project-directory . -f docker/compose.yaml up --build`; `-f docker/compose.yaml` alone still makes `docker/` the project directory |
| `dotnet ef` says "command not found" | the tool manifest was not restored | `dotnet tool restore` (from the repo root) |
| `dotnet ef` sees a null connection string | no `JDBConnection` value was found in the host project's User Secrets, `appsettings.Development.json`, or environment | confirm the secret key is `ConnectionStrings:JDBConnection`, or set `ConnectionStrings__JDBConnection` |
| `/auth/login` returns 401 with the right key | whitespace, or the key was rotated | re-read `Jwt:Key` |
| `/auth/login` returns 500 `server_misconfigured` | `Jwt:Key` is shorter than 32 bytes | HS256 needs a 256-bit key |
| `/auth/me` returns 401 | missing or expired token, or the signing key changed | re-issue from `/auth/login` |
| Collection endpoint returns 500 after a data import | an empty-string `role`/`career_position` | see `known-issues.md` |
| Collection endpoint returns 503 `database_unavailable` | the database is unreachable | check the connection string and the DB; the real exception is in the log |
| A failure returns 502 `notion_*` | Notion is down, or returned something unreadable | distinguish by `code`; retry only `notion_unavailable` |
| Browser reports a CORS error, `curl` works | the origin is not in `Cors:AllowedOrigins`, or it is a preview URL | add it as a setting; `Cors:DevelopmentOrigins` (localhost) applies only in Development |

## 7. Operational features

| Feature | Status | Notes / remaining gap |
|---|---|---|
| Liveness | present | `/healthz` via `MapHealthChecks` (`src/JGBackend/Program.cs:58`); no checks are registered, so it does not probe PostgreSQL. Exempt from rate limiting |
| Diagnostics | present | `/api/v1/health` returns `status`, `version`, `environment`, `timestamp`, `uptimeSeconds` (`src/Core/Features/Health/HealthEndpoints.cs:18-40`). Exempt from rate limiting |
| Rate limiting | present | fixed window per client IP: a 120/min `default` policy is applied to every mapped endpoint except the two health probes; stricter endpoint policies override it — 5/min on `/api/v1/auth/login`, 30/min on each Notion and GitHub passthrough (`src/JGBackend/Configuration/RateLimitConfiguration.cs:23-26`, `src/Core/Infrastructure/RateLimitPolicies.cs:8-9`). No distributed store — limits are per instance |
| Structured logs | default only | add Serilog or OpenTelemetry |
| Metrics / tracing | none | OpenTelemetry + Azure Monitor |
| Backup / restore | not documented | define an RPO/RTO for the Neon PostgreSQL instance |
| Config validation | JWT key only | startup fails when `Jwt:Key` is missing or shorter than 32 bytes (`src/JGBackend/Configuration/AuthenticationConfiguration.cs:17-31`); other settings are not validated |

## 8. Post-restructure handoff

The repository restructure moved code, docs, and workflows but did not touch
anything outside git. The single-host merge is complete in git. The owner
completed the items marked **Done** on 2026-11-22; the rest remain open.

| Item | Status | Why it matters |
|---|---|---|
| Rename the GitHub repository to `jg-core-backend` | Done 2026-11-22 | the layout is named for the monorepo target; the rename invalidates anything keyed off the repository name, most importantly the OIDC federated credential |
| Review the Azure federated credential subject | Done 2026-11-22 | updated to match the renamed repository; `azure/login@v2` must be `repo:jobearry/jg-core-backend:ref:refs/heads/master` or `deploy.yaml` fails. Confirm on the first deploy |
| Confirm repository secrets | Done 2026-11-22 | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `PRODUCTION_CONNECTION_STRING` are present; no values are stored in the repo |
| Review the `jdbweb` App Service | Done 2026-11-22 | app settings and deployment secrets updated for the single host; confirm the CORS allow-list (`Cors:AllowedOrigins`) and custom domain/SSL binding still match the frontend after the first deploy |
| Retire the `jdb-portfolio-api` Web App | Open | orphaned now that one host serves both modules; decommission it in Azure so it cannot be deployed or billed by mistake |
| Update the local clone | Done 2026-11-22 (remote) | `origin` points at `jg-core-backend`; the local working folder keeps the old name, which has no effect on git |

### Which `jdbweb` settings to keep

The composed host reads only the keys in §3. On `jdbweb`, use this list;
"remove" means delete the App Service setting if it exists.

| Setting | Action | Why |
|---|---|---|
| `ConnectionStrings__JDBConnection` | keep | runtime read connection (`portfolio_app`, `SELECT` only); the workflow's migration step uses the separate `PRODUCTION_CONNECTION_STRING` secret (§5) |
| `ConnectionStrings__JDBConnectionWrite` | add/verify | required runtime write connection (`portfolio_writer`, DML only); GitHub Actions does not set it, and the same value as the read connection is acceptable only outside production |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` | keep | issuer and audience are validated on every request (`AuthenticationConfiguration.cs:47-52`); changing them invalidates existing tokens |
| `Jwt__ExpirationInMinutes` | keep or unset | defaults to 60 |
| `Jwt__Expires` | remove | old Generic-host key; the app reads `Jwt:ExpirationInMinutes` (KI-031) |
| `Notion__Token` | keep | upstream bearer token |
| `Notion__Version` | keep | no in-code default; sent as the `Notion-Version` header (`src/Portfolio/Features/DependencyInjection.cs:56`) |
| `Github__Token` | keep | upstream token, kept server-side |
| `Github__BaseUrl` | keep or unset | defaults to `https://api.github.com` in `appsettings.json` |
| `Cors__AllowedOrigins__0`, `__1`, … | keep | production allow-list for the frontend |
| `Cors__DevelopmentOrigins__0`, … | remove | applied only in Development (`src/JGBackend/Configuration/CorsConfiguration.cs:17-20`) |
| `Database__AutoMigrate` | keep as `false` | the deploy workflow owns migrations (§5, KI-018/KI-038) |
| `Swagger__Enabled` | remove or `false` | `true` exposes `/swagger` in production |
| `AllowedHosts`, `Logging__*` | optional | framework defaults are in `appsettings.json` |

Do not copy the retired app's settings wholesale: import only the keys above.
Any App Service setting not listed here is no longer read by the composed host.

The dead `docker/Dockerfile.dockerignore` was deleted with the second Dockerfile
(KI-047); no external handoff is needed for it.
