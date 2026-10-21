# Operations

How to run, configure, and deploy the API.

## 1. Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.x | 10.0.402 in use |
| PostgreSQL | any recent | local install or container |
| `dotnet-ef` | matching EF 10 | available via the root tool manifest — run `dotnet tool restore` |
| Docker + Compose v2 | 20.10+ | optional, currently broken (see §4) |

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

`Program.cs` also calls `Database.Migrate()` at startup, so a valid connection
string is required just to boot. Set `Database__AutoMigrate=false` to skip it.

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
use its host and the SSL settings it requires. `Program.cs` calls
`Database.Migrate()` at startup, so the API applies pending migrations itself
and fails to boot if the database is unreachable. Set `Database__AutoMigrate=false`
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

1. Trigger: push to `main`, or manual dispatch.
2. `dotnet restore` → `dotnet build -c Release` → `dotnet publish -o ./publish`.
3. `azure/login@v2` with OIDC (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
   `AZURE_SUBSCRIPTION_ID`).
4. `azure/webapps-deploy@v2` to app `jdb-portfolio-api`, `clean: true`.

No test step exists. Tests now live in `tests/` (two xUnit projects, 24 tests) —
add a `dotnet test src/JDB_Portfolio_API.slnx` step to the workflow.

Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development`. Production
should not set it.

## 6. Runbook

| Symptom | Likely cause | Action |
|---|---|---|
| App fails at startup with an Npgsql connection error | `Migrate()` ran with a bad/missing connection string | check `ConnectionStrings__JDBConnection` |
| `docker compose up` fails with "required variable ConnectionStrings__JDBConnection is missing" | `docker/.env` not created | `cp docker/.env.example docker/.env` and fill it in |
| `dotnet ef` says "command not found" | the tool manifest was not restored | `dotnet tool restore` (from the repo root) |
| `dotnet ef` sees a null connection string | no user-secrets id or connection string was supplied | set `JDB_USER_SECRETS_ID` and `ConnectionStrings__JDBConnection`, or use the startup project |
| `/auth/login` returns 401 with the right key | whitespace, or the key was rotated | re-read `Jwt:Key` |
| `/auth/me` returns `{}` | token has no `sub`, only a role claim | expected |
| Collection endpoint returns 500 after a data import | an empty-string `role`/`career_position` | see `known-issues.md` |
| `/skill` returns 200 with an empty body | missing `IsSuccess` check | known issue |
| Every failure returns 502 | hardcoded `statusCode: 502` | known issue |

## 7. Missing operational features

| Feature | Status | Recommendation |
|---|---|---|
| Health endpoint | none | add `/health` with an Npgsql probe before scaling |
| Structured logs | default only | add Serilog or OpenTelemetry |
| Metrics / tracing | none | OpenTelemetry + Azure Monitor |
| Rate limiting | none | `AddRateLimiter` on `/auth/login` at minimum |
| Backup / restore | not documented | define an RPO/RTO for the Azure PostgreSQL instance |
| Config validation | none | fail fast at startup if `Jwt:Key` < 32 bytes |
