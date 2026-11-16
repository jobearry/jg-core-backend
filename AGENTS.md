# Repository guidance

## Project overview

This repository contains a .NET 10 ASP.NET Core Web API organized using
vertical slice architecture as a solution of three projects:

- `Generic/Generic.API` hosts the API, the application composition root
  (`Program.cs`), startup configuration (Swagger, JWT auth, CORS), and the
  Dockerfile entry point.
- `Generic/Generic.Features` contains the feature slices. Each slice owns its
  endpoints (minimal APIs via `MapXxxEndpoints`), handlers, DTOs, and mapping
  logic. Currently: `Auth/` (JWT login and `me`). New features should be
  added as new slice folders here.
- `Generic/Generic.Infra` contains shared infrastructure concerns
  (configuration binding, external clients, persistence, cross-cutting
  middleware helpers) as they are introduced. Currently: `JwtOptions`,
  `GlobalExceptionHandler` (RFC 7807 ProblemDetails), and DI wiring in
  `DependencyInjection.AddInfrastructure`.

Keep dependencies flowing in one direction: `Generic/Generic.API` →
`Generic/Generic.Features` → `Generic/Generic.Infra`. Slices should not
reference the host project, and cross-cutting infrastructure belongs in
`Generic/Generic.Infra` rather than inside a slice.

The previous portfolio CRUD/Notion implementation was removed; an external
backend owns that data now. EF Core, SQL Server, and the Azure Entra
integration are no longer part of the working tree. The full previous
implementation (EF context, migrations, repositories) is preserved in git
history — retrieve it via `git show <pre-restructure-commit>:<path>` (e.g.
`git show HEAD~1:Portfolio_API.DataAccess/Contexts/JDBContext.cs`).

## Build and run

Run commands from the repository root:

```sh
dotnet restore Generic/Generic.sln
dotnet build Generic/Generic.sln --configuration Release --no-restore
dotnet run --project Generic/Generic.API/Generic.API.csproj
```

There is currently no automated test project. The GitHub Actions CI workflow
restores and builds the solution; it does not run tests.

Docker files are under `docker/`. Compose and Docker builds use the repository
root as their build context:

```sh
docker compose --project-directory . -f docker/compose.yaml up --build
docker build -f docker/Dockerfile -t myapp .
```

## Change guidance

- Target .NET 10 and preserve the projects' nullable reference type and
  implicit using settings.
- New features go in `Generic/Generic.Features/<FeatureName>/` as self-contained
  slices: minimal-API endpoint mapping, request/response DTOs, handler or
  service logic, and mapping all live together in the slice.
- Cross-cutting infrastructure (configuration binding, HTTP clients,
  persistence) goes in `Generic/Generic.Infra` and is wired up via
  `AddInfrastructure` in the host's `Program.cs`; slices are registered via
  `AddFeatures`.
- Slices expose minimal-API endpoints via a `Map<Slice>Endpoints(this
  IEndpointRouteBuilder)` extension; the host maps every slice with a single
  `app.MapFeatures()` call (see `Generic/Generic.Features/DependencyInjection.cs`).
  There is no controller/ApplicationPart mechanism — controllers were removed.
- Cross-cutting request handling: unhandled exceptions become RFC 7807
  ProblemDetails via `GlobalExceptionHandler`; `/healthz` is an anonymous
  health-check endpoint; `POST /api/v1/auth/login` is rate limited
  (fixed window, 5 requests/minute per IP) via the built-in rate limiter.
- Keep HTTP handling in slices, startup/composition in `Generic/Generic.API`, and
  shared infrastructure in `Generic/Generic.Infra`.
- Do not commit credentials or local environment files. External service
  credentials may require local configuration from `.env.example`.
- When changing build or deployment behavior, keep `.github/workflows/ci.yaml`
  and `.github/workflows/deploy.yaml` aligned with the solution; both build
  `Generic/Generic.sln`, which now contains only the three projects above.
