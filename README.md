# Generic API
[![CI](https://img.shields.io/github/actions/workflow/status/jobearry/jg-generic-api/ci.yaml?branch=master)](https://github.com/jobearry/jg-generic-api/actions/workflows/ci.yaml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![C#](https://img.shields.io/badge/C%23-14-9B4F96?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Minimal%20APIs-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
[![Vertical Slice Architecture](https://img.shields.io/badge/Architecture-Vertical%20Slice-8A2BE2)](https://www.jimmybogard.com/vertical-slice-architecture/)
[![JWT](https://img.shields.io/badge/Auth-JWT%20(HMAC--SHA256)-E8590C)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI%20v1-85EA2D?logo=swagger&logoColor=black)](https://swagger.io/tools/swagger-ui/)
[![Azure App Service](https://img.shields.io/badge/Azure-App%20Service-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/products/app-service)
[![Docker](https://img.shields.io/badge/Docker-alpine-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)

Monorepo for personal projects. Currently hosts a .NET 10 Web API providing
the Auth service (and future personal backends), organized using vertical
slice architecture. Built with assistance of Github Copilot, and Opencode.

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
- `Health` - anonymous diagnostic status (`GET /api/v1/health`: version, environment, uptime).
- `Health check` - anonymous framework probe (`GET /healthz`).

Portfolio data and the Notion integration were removed — that data is served by
a separate backend now. EF Core / SQL Server are no longer used.
