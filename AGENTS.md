# Repository guidance for agents

Monorepo for personal backends. One solution (`JGBackend.sln`), one product.
Today it produces two deployables; the target is a single modular-monolith host.

## Layout

```
src/JGBackend/     host: Program.cs, middleware pipeline, configuration wiring
src/Core/          auth, health, rate limiting, forwarded headers, shared infra
src/Portfolio/     portfolio read API (vertical slices in one project)
tests/Core/        tests for Core
tests/Portfolio/   tests for Portfolio
docs/              human-facing docs and ADRs
.opencode/         OpenCode harness: rules, skills, agents, commands
.github/workflows/ CI and deployment only
docker/            one Dockerfile + compose file per app (until the merge)
```

Read order: this file → the nearest `AGENTS.md` for the area → `.opencode/rules/`
for the area you touch → `docs/` for context.

## Build, run, test

```sh
dotnet tool restore
dotnet restore JGBackend.sln
dotnet build JGBackend.sln --configuration Release
dotnet test  JGBackend.sln --configuration Release

dotnet run --project src/JGBackend
dotnet run --project src/Portfolio/Portfolio.csproj
```

EF Core commands (tool manifest is at the repo root):

```sh
dotnet ef migrations add <Name> \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/Portfolio/Portfolio.csproj
```

Docker (build context is always the repo root):

```sh
docker build -f docker/JGBackend.Dockerfile .
docker build -f docker/Portfolio.Dockerfile .
docker compose -f docker/compose.yaml up --build
docker compose -f docker/compose.portfolio.yaml up --build
```

## Conventions

- .NET 10 (`net10.0`), nullable reference types, implicit usings.
- ASP.NET Core minimal APIs; features are vertical slices under `Features/`.
- `Directory.Build.props` turns warnings into errors. Keep the build at zero warnings.
- Dependencies point inward: `src/JGBackend` → `src/Core` / `src/Portfolio`.
  Modules never reference the host.
- Never commit secrets. `.env.example` is the template; real values come from
  environment variables, user secrets, or the deployment platform.
- Never edit an applied EF migration. Add a new one.
- Tests ship with the change; new behavior starts with a failing test.
- Keep responses short and cite file paths and line numbers.

## Current phase

Phases 1–2 are complete: the repo is restructured and Portfolio is now one
project organised by vertical slice. It is still its own host and still uses its
own auth. The single-host merge (phase 3) will:

- make `src/JGBackend` compose both modules,
- delete Portfolio's duplicate auth and use `Core`,
- unify token lifetime on `Jwt:ExpirationInMinutes`,
- add rate-limit policies for the Notion and GitHub passthroughs,
- retire the second Web App.

Do not assume the merge has happened. When a task touches auth or host wiring,
read `docs/plan.md` only if the user explicitly asks for it.

## Non-negotiables

- No secret, connection string, or token in source, tests, or logs.
- No new NuGet package without asking.
- Ask before applying migrations, changing the schema, rewriting git history, or
  starting long-running processes.
- If you find unrelated debt, record it in `docs/known-issues.md`; do not fix it
  silently.
