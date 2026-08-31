# Docker Containerization

This folder containerizes the **JDB Portfolio API** (a .NET 10 ASP.NET Core Web
API) together with a **PostgreSQL** database, using Docker Compose.

## What's in here

| File                  | Purpose                                                            |
|-----------------------|--------------------------------------------------------------------|
| `Dockerfile`          | Multi-stage build (build → publish) for the API image              |
| `docker-compose.yml`  | Orchestrates the `api` and `db` (PostgreSQL) services              |
| `.env.example`        | Template for environment variables (credentials/ports)             |
| `../.dockerignore`    | Keeps `bin/`, `obj/`, `.vs/` out of the image build context        |

## Prerequisites

- [Docker Engine](https://docs.docker.com/engine/install/) 20.10+ with
  Docker Compose v2 (`docker compose`).

## Quick start

From the repository root (`../../..` relative to this folder):

```bash
# 1. (Optional) create a local env file to customise things
cp docker/.env.example docker/.env

# 2. Build and start both services
docker compose -f docker/docker-compose.yml up --build -d

# 3. Check status
docker compose -f docker/docker-compose.yml ps

# 4. View logs
docker compose -f docker/docker-compose.yml logs -f api
```

### What runs where

| Component | Container          | Host URL                          |
|-----------|--------------------|-----------------------------------|
| API       | `jdb_portfolio_api`| http://localhost:8080             |
| Swagger UI| `jdb_portfolio_api`| http://localhost:8080/swagger     |
| PostgreSQL| `jdb_portfolio_db` | localhost:5432 (db: `jdb_portfolio`) |

The Swagger UI is available at `/swagger` because the compose file sets
`ASPNETCORE_ENVIRONMENT=Development`. For a deployment you do **not** want to
expose Swagger on, remove that line (or set it to `Production`).

## Configuration

##### Database credentials

Defaults are suitable for local development (`postgres` / `postgres`,
database `jdb_portfolio`). Override them by creating `docker/.env`:

```ini
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change-me
POSTGRES_DB=jdb_portfolio
POSTGRES_PORT=5454
API_PORT=5254
```

The API's connection string is **built by `docker-compose.yml` from the
`POSTGRES_*` variables** and always targets the `db` compose service over the
internal `jdb_network`:

```
Host=db;Port=5432;Database=jdb_portfolio;Username=postgres;Password=postgres
```

> The `db` before `Port=5432` NOT `localhost`: inside a container `localhost`
> would be the API container itself. `POSTGRES_PORT` only affects the *host*-
> facing port, so it doesn't matter to the API. Put another way: the API talks
> to the containerized DB on `db:5432` regardless of the host port.

> Note: `appsettings.Development.json` contains a hardcoded connection string
> (`Host=localhost;...`) intended for running the API directly on your machine
> against a local Postgres. Inside Docker that value is **overridden** by the
> env var set in the compose file, so it is ignored here.

### Applying EF Core migrations

The compose file starts the API only after the database reports healthy, but it
does **not** auto-apply migrations. Apply them once the stack is up:

```bash
dotnet tool restore            # ensures the EF tools are available
dotnet ef database update \
  --project src/JDB_Portfolio_API.Persistence \
  --startup-project src/JDB_Portfolio_API \
  --connection "Host=localhost;Port=5432;Database=jdb_portfolio;Username=postgres;Password=postgres"
```

See `src/JDB_Portfolio_API.Persistence/Migrations/MIGRATION_GUIDE.md` for full
migration commands.

## Building / running without Compose

```bash
# Build the image (context must be the repo root)
docker build -f docker/Dockerfile -t jdb-portfolio-api .

# Run with an explicit connection string
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__JDBConnection=Host=<db-host>;Port=5432;Database=jdb_portfolio;Username=postgres;Password=postgres" \
  jdb-portfolio-api
```

## Stopping & cleaning up

```bash
# Stop containers (data volume persists)
docker compose -f docker/docker-compose.yml down

# Stop containers AND delete the PostgreSQL data volume
docker compose -f docker/docker-compose.yml down -v
```