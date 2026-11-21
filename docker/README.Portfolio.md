# Portfolio — Docker

Containerizes the **Portfolio API** (`src/Portfolio/Portfolio.csproj`, .NET 10
ASP.NET Core Web API) with Docker Compose.

The stack runs the **API only**. PostgreSQL is external: point the API at a
server you run yourself or a hosted one, through `.env` at the repository root.

## What's in here

| File                    | Purpose                                                     |
|-------------------------|-------------------------------------------------------------|
| `Portfolio.Dockerfile`  | Multi-stage build (build → publish) for the API image       |
| `compose.portfolio.yaml`| Runs the `api` service                                      |
| `../.env.example`       | Template for environment variables (credentials, ports)     |
| `../.dockerignore`      | Keeps `bin/`, `obj/`, `.vs/` out of the image build context |

## Prerequisites

- [Docker Engine](https://docs.docker.com/engine/install/) 20.10+ with
  Docker Compose v2 (`docker compose`).
- A reachable PostgreSQL instance.

## Quick start

From the repository root:

```bash
# 1. Create your env file and fill in the connection string
cp .env.example .env

# 2. Build and start
docker compose -f docker/compose.portfolio.yaml up --build -d

# 3. Check status
docker compose -f docker/compose.portfolio.yaml ps

# 4. View logs
docker compose -f docker/compose.portfolio.yaml logs -f api
```

### What runs where

| Component  | Container           | Host URL                          |
|------------|---------------------|-----------------------------------|
| API        | `jdb_portfolio_api` | http://localhost:5254             |
| Swagger UI | `jdb_portfolio_api` | http://localhost:5254/swagger     |

The Swagger UI appears because `ASPNETCORE_ENVIRONMENT=Development`. For a
deployment you do not want to expose it — set the environment to `Production`
in `.env`.

## Configuration

### Database

The connection string is passed through in full, with no `Host=db` shortcut,
because there is no `db` service:

```ini
ConnectionStrings__JDBConnection=Host=host.docker.internal;Port=5432;Database=jg-portfolio;Username=postgres;Password=postgres;Ssl Mode=Disable
```

`host.docker.internal` reaches a PostgreSQL running on the host — the compose
file maps that name to the host gateway, which works on Linux as well as Docker
Desktop. Inside a container `localhost` would be the API container itself.

For a hosted database, use its host and whatever SSL settings it requires
(for example `Ssl Mode=Require`).

`Program.cs` calls `Database.Migrate()` on startup when `Database:AutoMigrate`
is enabled, so the API applies pending migrations itself and will not boot if
the connection string is unreachable.

### JWT and Notion

`Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Notion__Token`, and
`Notion__Version` bind the app's `Jwt:*` and `Notion:*` sections.
`Jwt__Key` must be at least 32 bytes.

## Building / running without Compose

```bash
# Build the image (context must be the repo root)
docker build -f docker/Portfolio.Dockerfile -t jdb-portfolio-api .

# Run with an explicit connection string
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__JDBConnection=Host=<db-host>;Port=5432;Database=jg-portfolio;Username=postgres;Password=postgres" \
  -e "Jwt__Key=<32+ bytes>" -e "Jwt__Issuer=jdb_api" -e "Jwt__Audience=PortfolioApiUsers" \
  -e "Notion__Token=secret_…" \
  jdb-portfolio-api
```

## Stopping

```bash
docker compose -f docker/compose.portfolio.yaml down
```

There is no data volume to remove: the database lives outside this stack.

See [`docs/database.md`](../docs/database.md) for migration commands.
