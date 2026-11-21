# Docker guidance

All images build from the repository root. Never set the build context to
`docker/` or to a project directory.

```sh
docker build -f docker/JGBackend.Dockerfile -t jg-backend .
docker build -f docker/Portfolio.Dockerfile -t jg-portfolio .
docker compose -f docker/compose.yaml up --build
docker compose -f docker/compose.portfolio.yaml up --build
```

## Current state

Two images exist during the restructure:

| File | App |
|---|---|
| `docker/JGBackend.Dockerfile` | `src/JGBackend` |
| `docker/Portfolio.Dockerfile` | `src/Portfolio/Portfolio.csproj` |
| `docker/compose.yaml` | JGBackend |
| `docker/compose.portfolio.yaml` | Portfolio + external PostgreSQL |

Phase 4 collapses these into one image and one compose file.

## Rules

- Multi-stage builds only; runtime images use the ASP.NET base image and run as
  the non-root `$APP_UID` user.
- Never bake secrets, `.env`, or connection strings into an image. Pass them at
  runtime.
- Keep `.dockerignore` aligned with the build context; it excludes `.git`, `bin`,
  `obj`, `.env*` (except `.env.example`), and the agent harness.
- Alpine is used for the JGBackend image; keep `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`
  paired with `icu-libs` if globalization is needed.
