# jg-generic-api (monorepo)

Monorepo for personal projects. Currently hosts a .NET 10 Web API providing
the Auth service (and future personal backends), organized using vertical
slice architecture. Built with assistance of Github Copilot, and Ollama (Qwen2.5-Coder:7b).

## Tech stack
- .NET 10 Web API (C#)
- Vertical slice architecture (host + feature slices)
- JWT authentication
- Swashbuckle (Swagger/OpenAPI)
- Azure App Service for deployment
- Docker / Docker Compose

## Repository layout
- `Generic/` — the .NET solution (`Generic.sln`), buildable and deployable on its own
  - `Generic.API/` — host project: `Program.cs`, Swagger/auth/CORS wiring, `Generic.API.http`
  - `Generic.Features/` — feature slices (currently `Auth/`)
  - `Generic.Infra/` — shared infrastructure (configuration binding, DI wiring)
- `docker/` — Dockerfile and compose file (build context is repo root)
- `.github/` — CI (restore/build `Generic/Generic.sln`) and deploy workflows

## Current Features
- `Auth` - JWT login (`POST /api/v1/auth/login`, rate limited) and current-user claims (`GET /api/v1/auth/me`).
- `Health check` - `GET /healthz` (anonymous).

Portfolio data and the Notion integration were removed — that data is served by
a separate backend now. EF Core / SQL Server are no longer used.
