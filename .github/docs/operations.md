# Operations

How to run, configure, and deploy the API.

## 1. Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.x | 10.0.402 in use |
| PostgreSQL | any recent | local install or container |
| `dotnet-ef` | matching EF 10 | available via the root tool manifest — run `dotnet tool restore` |
| Docker + Compose v2 | 20.10+ | optional; the API container uses an external PostgreSQL instance (see §4) |

## 2. Local development

```bash
# 1. restore + build
dotnet restore src/JDB_Portfolio_API.slnx
dotnet build   src/JDB_Portfolio_API.slnx

# 2. configure secrets (user-secrets, API project)
dotnet user-secrets init --project src/JDB_Portfolio_API
dotnet user-secrets set "ConnectionStrings:JDBConnection" "Host=localhost;Port=5432;Database=jdb_portfolio;Username=postgres;Password=…" --project src/JDB_Portfolio_API
dotnet user-secrets set "Jwt:Key"       "<at least 32 bytes>" --project src/JDB_Portfolio_API
dotnet user-secrets set "Jwt:Issuer"    "jdb_api"            --project src/JDB_Portfolio_API
dotnet user-secrets set "Jwt:Audience"  "PortfolioApiUsers"  --project src/JDB_Portfolio_API
dotnet user-secrets set "Notion:Token"  "secret_…"           --project src/JDB_Portfolio_API
dotnet user-secrets set "Github:Token"  "<GitHub personal access token>" --project src/JDB_Portfolio_API

# 3. apply migrations
dotnet ef database update \
  --project src/JDB_Portfolio_API.Persistence \
  --startup-project src/JDB_Portfolio_API

# 4. run
dotnet run --project src/JDB_Portfolio_API
```

| Profile | URL |
|---|---|
| `http` | http://localhost:5254 |
| `https` | https://localhost:7248 / http://localhost:5254 |
| Swagger | http://localhost:5254/swagger |

`Program.cs` calls `Database.Migrate()` at startup when
`Database:AutoMigrate` is enabled (default `true`), so a valid connection string
is required to boot unless `Database__AutoMigrate=false`.

## 3. Configuration matrix

| Key | Local | Docker | Azure |
|---|---|---|---|
| `ConnectionStrings:JDBConnection` | user-secrets | `ConnectionStrings__JDBConnection` in `docker/.env` | App Service setting |
| `Jwt:Key` | user-secrets | `Jwt__Key` | App Service setting |
| `Jwt:Issuer` | user-secrets | `Jwt__Issuer` | App Service setting |
| `Jwt:Audience` | user-secrets | `Jwt__Audience` | App Service setting |
| `Jwt:ExpirationInMinutes` | default 60 | `Jwt__ExpirationInMinutes` | optional |
| `Notion:Token` | user-secrets | `Notion__Token` | App Service setting |
| `Notion:Version` | default `2026-03-11` | `Notion__Version` | optional |
| `Github:Token` | user-secrets | `Github__Token` | App Service setting |
| `Github:BaseUrl` | `https://api.github.com` | `Github__BaseUrl` | optional |
| `Cors:AllowedOrigins` | `appsettings.json` | `Cors__AllowedOrigins__0`, `__1`, … | App Service setting |
| `Cors:DevelopmentOrigins` | `appsettings.json` | applied only when `ASPNETCORE_ENVIRONMENT=Development` | ignored |
| `Swagger:Enabled` | `appsettings.json` (`false`) | `Swagger__Enabled` | optional; `true` exposes `/swagger` |

Origins are configuration so that a new domain or a Vercel preview deployment
(`https://demo-git-my-branch-team.vercel.app`) is an added setting rather than a code
change and a redeploy. Adding one is still a security review item — see
`docs/api-reference.md` §6.

The GitHub token stays server-side and is used only for upstream GitHub requests.
Current GitHub endpoints are anonymous but return public repository data only;
the API checks visibility before returning repository details or languages.
Grant the GitHub token only the repository metadata access required for the
intended repositories. Optional API-JWT access to private repository data is
not implemented; see [plan 002](../plans/002-github-visibility-auth.md).

Note: `docker/.env.example` sets `Jwt__ExpirationInMinutes`, matching the key
the app reads.

## 4. Docker

The compose file runs the **API only**. PostgreSQL is external.

```bash
cp docker/.env.example docker/.env   # then set ConnectionStrings__JDBConnection
docker compose -f docker/docker-compose.yml up --build
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
docker build -f docker/Dockerfile -t jdb-portfolio-api .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__JDBConnection=Host=<host>;Port=5432;Database=jdb_portfolio;Username=…;Password=…" \
  -e "Jwt__Key=<32+ bytes>" -e "Jwt__Issuer=jdb_api" -e "Jwt__Audience=PortfolioApiUsers" \
  -e "Notion__Token=secret_…" \
  jdb-portfolio-api
```

The container listens on `8080` (`ASPNETCORE_URLS=http://+:8080`), runs as
`$APP_UID` (non-root), and the build context must be the repository root.

## 5. Deployment

`.github/workflows/main_jdb-portfolio-api.yml`:

1. Trigger: push to `main`, or manual dispatch. Runs are serialized by a
   `production-deploy` concurrency group.
2. `dotnet restore` → `dotnet tool restore` → `has-pending-model-changes` gate (fails
   if the model changed without a migration) → `dotnet build -c Release` →
   `dotnet publish -o ./publish`.
3. `dotnet ef database update` against the production connection string, before the
   app is replaced. A failure stops the deploy; the running app stays up (KI-038).
4. `azure/login@v2` with OIDC (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
   `AZURE_SUBSCRIPTION_ID`).
5. `azure/webapps-deploy@v2` to app `jdb-portfolio-api`, `clean: true`.

The migration step needs the repository secret `PRODUCTION_CONNECTION_STRING`. Use
the **direct** Neon endpoint (no `-pooler`), in Npgsql key-value form — not the
`postgresql://` URI — because pooling can break DDL and session state.

Production is configured to let GitHub Actions own migrations. The
**`Database__AutoMigrate=false`** App Service setting has been applied and
verified by the owner (2026-10-23). If recreating the App Service or its
settings, restore this configuration only after the **Apply database
migrations** workflow step has succeeded against production:

1. Open the Azure Portal and select the `jdb-portfolio-api` App Service.
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

Tests live in `tests/` (four xUnit projects). The workflow currently
does not run them; add a `dotnet test src/JDB_Portfolio_API.slnx` step when
updating CI.

Swagger is enabled when `ASPNETCORE_ENVIRONMENT=Development` or when the
`Swagger:Enabled` setting is `true`. Production leaves it unset (off); turn it on
for a smoke test and off again.

## 6. Runbook

| Symptom | Likely cause | Action |
|---|---|---|
| App fails at startup with an Npgsql connection error | `Migrate()` ran with a bad/missing connection string | check `ConnectionStrings__JDBConnection` |
| Deploy fails at "Apply database migrations" | migration could not run: dirty data, unreachable DB, or a missing/wrong `PRODUCTION_CONNECTION_STRING` | read the step log; the old app is still live. Fix the data or secret and re-run |
| `docker compose up` fails with "required variable ConnectionStrings__JDBConnection is missing" | `docker/.env` not created | `cp docker/.env.example docker/.env` and fill it in |
| `dotnet ef` says "command not found" | the tool manifest was not restored | `dotnet tool restore` (from the repo root) |
| `dotnet ef` sees a null connection string | no user-secrets id or connection string was supplied | set `JDB_USER_SECRETS_ID` and `ConnectionStrings__JDBConnection`, or use the startup project |
| `/auth/login` returns 401 with the right key | whitespace, or the key was rotated | re-read `Jwt:Key` |
| `/auth/login` returns 500 `server_misconfigured` | `Jwt:Key` is shorter than 32 bytes | HS256 needs a 256-bit key |
| `/auth/me` returns 401 | missing or expired token, or the signing key changed | re-issue from `/auth/login` |
| Collection endpoint returns 500 after a data import | an empty-string `role`/`career_position` | see `known-issues.md` |
| Collection endpoint returns 503 `database_unavailable` | the database is unreachable | check the connection string and the DB; the real exception is in the log |
| A failure returns 502 `notion_*` | Notion is down, or returned something unreadable | distinguish by `code`; retry only `notion_unavailable` |
| Browser reports a CORS error, `curl` works | the origin is not in `Cors:AllowedOrigins`, or it is a preview URL | add it as a setting; `Cors:DevelopmentOrigins` (localhost) applies only in Development |

## 7. Missing operational features

| Feature | Status | Recommendation |
|---|---|---|
| Health endpoint | none | add `/health` with an Npgsql probe before scaling |
| Structured logs | default only | add Serilog or OpenTelemetry |
| Metrics / tracing | none | OpenTelemetry + Azure Monitor |
| Rate limiting | none | `AddRateLimiter` on `/auth/login` at minimum |
| Backup / restore | not documented | define an RPO/RTO for the Neon PostgreSQL instance |
| Config validation | none | fail fast at startup if `Jwt:Key` < 32 bytes |
