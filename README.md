# JG Backend

[![CI](https://img.shields.io/github/actions/workflow/status/jobearry/jg-generic-api/ci.yaml?branch=master)](https://github.com/jobearry/jg-generic-api/actions/workflows/ci.yaml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![C#](https://img.shields.io/badge/C%23-14-9B4F96?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Minimal%20APIs-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
[![JWT](https://img.shields.io/badge/Auth-JWT%20(HMAC--SHA256)-E8590C)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI%20v1-85EA2D?logo=swagger&logoColor=black)](https://swagger.io/tools/swagger-ui/)
[![Azure App Service](https://img.shields.io/badge/Azure-App%20Service-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/products/app-service)
[![Docker](https://img.shields.io/badge/Docker-alpine-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)

Monorepo for personal backends. One solution (`JGBackend.sln`), one deployable:
the `JGBackend` host composes a shared `Core` module and a `Portfolio` module.

## Tech stack

- .NET 10 minimal APIs (C#)
- PostgreSQL + EF Core (Portfolio)
- JWT authentication (shared-key login, HMAC-SHA256)
- Swashbuckle (Swagger/OpenAPI)
- Azure App Service for deployment
- Docker / Docker Compose

## Repository layout

```
src/JGBackend/     host: Program.cs, middleware pipeline, configuration wiring
src/Core/          auth, health, rate limiting, forwarded headers, shared infra
src/Portfolio/     portfolio read API (class library, vertical slices)
tests/Core/        tests for Core
tests/Portfolio/   tests for Portfolio and the composed host
docs/              human-facing docs and ADRs
.opencode/         agent harness: rules, skills, agents, commands
.github/workflows/ CI and deployment only
docker/            one Dockerfile + compose file for the single host
```

## Features

`Core` — auth and infrastructure:
- `Auth` — JWT login (`POST /api/v1/auth/login`, rate limited) and current-user claims (`GET /api/v1/auth/me`).
- `Health` — anonymous diagnostic status (`GET /api/v1/health`) and framework probe (`GET /healthz`).

`Portfolio` — read-only portfolio API:
- Experience, projects, tech stacks, recommendations, overview, Notion page content, and public GitHub repository data.

## Build, run, test

```sh
dotnet tool restore
dotnet restore JGBackend.sln
dotnet build JGBackend.sln --configuration Release
dotnet test  JGBackend.sln --configuration Release

dotnet run --project src/JGBackend
```

Configuration comes from user secrets locally and environment variables in
deployments; `.env.example` is the template for Docker. Never commit a secret.
Migrations stay in `src/Portfolio` and are applied with `src/JGBackend` as the
EF startup project (see [`docs/operations.md`](docs/operations.md)).

## Docs

- [`AGENTS.md`](AGENTS.md) — entry point for agents and contributors
- [`docs/INDEX.md`](docs/INDEX.md) — index of docs, rules, and ADRs
