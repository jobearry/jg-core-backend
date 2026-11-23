# Repository guidance for agents

Monorepo for personal backends. One solution (`JGBackend.sln`), one product, one
deployable: the `JGBackend` host composes the `Core` and `Portfolio` modules.

## Layout

```
src/JGBackend/     host: Program.cs, middleware pipeline, configuration wiring
src/Core/          auth, health, rate limiting, forwarded headers, shared infra
src/Portfolio/     portfolio read API (class library, vertical slices)
tests/Core/        tests for Core
tests/Portfolio/   tests for Portfolio and the composed host
docs/              human-facing docs and ADRs
.opencode/         OpenCode harness: rules, skills, agents, commands
.github/workflows/ CI and deployment only
docker/            one Dockerfile + compose file for the single host
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
```

EF Core commands (tool manifest is at the repo root; Portfolio owns the context
and migrations, the host is the startup project):

```sh
dotnet ef migrations add <Name> \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/JGBackend/JGBackend.csproj
```

Docker (build context is always the repo root):

```sh
docker build -f docker/JGBackend.Dockerfile .
docker compose --project-directory . -f docker/compose.yaml up --build
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

## Current state

Phases 1–3 are complete. `src/JGBackend` is the sole composition root and
deployable. It composes `Core` (auth, health, cross-cutting infrastructure) and
`Portfolio` (read-only vertical slices in a class library). Configuration, the
JWT lifetime, and rate-limit policies are unified on the host; there is one
deploy workflow and one Docker image. Portfolio hosts no HTTP pipeline of its
own.

## Non-negotiables

- No secret, connection string, or token in source, tests, or logs.
- No new NuGet package without asking.
- Ask before applying migrations, changing the schema, rewriting git history, or
  starting long-running processes.
- If you find unrelated debt, record it in `docs/known-issues.md`; do not fix it
  silently.
