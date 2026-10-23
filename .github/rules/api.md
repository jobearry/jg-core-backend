# API rules (ASP.NET Core Minimal APIs)

## 1. Shape of an endpoint

```csharp
public static class PortfolioProjectEndpoints
{
    public static void MapPortfolioProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/project", async (
                PortfolioProjectService service,
                bool includeTools = false,
                CancellationToken ct = default) =>
            {
                var result = await service.GetAllProjectsAsync(includeTools, ct);
                return result.ToHttpResult();
            })
            .WithName("GetProjects")
            .WithTags("Portfolio")
            .Produces<List<ProjectDto>>()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
```

Rules:

- One static class per area, `MapXxxEndpoints(this IEndpointRouteBuilder)`.
- Register a single `app.MapGroup("/api/v1")` in `Program.cs` and map groups
  under it. Do **not** hand-spell `"/api/v1/..."` in each endpoint (plan 2.5).
- Bind services by parameter injection, not `[FromServices]` attributes.
- Every endpoint takes `CancellationToken`.
- Always translate the `Result` — never return `result.Value` unconditionally
  (`PortfolioSkillEndpoints` currently does; plan 2.5).

## 2. Routes

- Lowercase, kebab-case, **singular** resource nouns to match the existing
  surface: `/experience`, `/project`, `/recommendation`, `/skill`, `/overview`.
  Consistency beats REST purism here; do not rename without a version bump.
- Query parameters for filtering: `?includeProjects=true`.
- Path parameters for identity: `/notion/{pageId}`.
- Version in the path: `/api/v1`. A breaking change means `/api/v2`, not a
  silent change.

## 3. Responses

| Situation | Status | Body |
|---|---|---|
| Success, collection | `200` | JSON array |
| Success, created | `201` | resource + `Location` header |
| Validation failure | `400` | `ValidationProblemDetails` |
| Unauthenticated | `401` | empty |
| Unauthorised | `403` | empty |
| Not found | `404` | `ProblemDetails` |
| Conflict | `409` | `ProblemDetails` |
| Unhandled / database failure | `500` | `ProblemDetails` |
| Dependency unavailable (Notion down) | `503` | `ProblemDetails` |

**Never `502`.** `502 Bad Gateway` describes a reverse proxy receiving an
invalid response from an upstream. This API *is* the upstream. The current code
returns 502 for everything (plan 2.2).

## 4. DTOs

- Endpoints accept and return **DTOs only**. No `Domain.Entities`, no
  `Domain.ValueObjects`, no `Persistence.Entities`.
- DTOs live in `Application/DTO`, are `sealed record`s, and are named `*Dto`.
- Do not expose audit fields (`CreatedAt`, `UpdatedAt`) or foreign keys unless
  the client genuinely needs them.
- Do not make a DTO a `record` with `init` setters if it is also used for request
  binding with validation attributes — choose one role per DTO.

## 5. Errors

- One `Result<T> → IResult` mapper (extension method) used by every endpoint.
- Error kinds map to status codes in exactly one place.
- Problem responses include `type`, `title`, `status`, `detail`, and `traceId`
  (ASP.NET adds `traceId` automatically via `ProblemDetails`).

## 6. Validation

Minimal APIs do **not** run `[ApiController]`-style model validation. Choose one
approach and apply it everywhere:

1. A validation filter / `IEndpointFilter`, or
2. FluentValidation + a filter, or
3. Explicit checks returning `Results.ValidationProblem(...)`.

`AuthDto` is validated by an explicit guard in `AuthEndpoints` (2.5): a missing or blank
`Key` returns `400 + validation_failed`. If another input DTO appears, follow the same
shape — `Error.Validation(...)` → `400 + validation_failed` — rather than introducing a
second error contract.

## 7. OpenAPI

- Swagger is Development-only by default. `Swagger:Enabled=true` exposes it in a
  deployment for a smoke test; keep it unset otherwise.
- Every endpoint carries `.WithName(...)`, `.WithTags(...)`, `.Produces<T>(...)`,
  and `.ProducesProblem(...)`.
- Document auth requirements with `.RequireAuthorization()` **only if it is
  actually enforced** — today only `/auth/me` carries it.
- The Swagger UI is served at `/swagger`; the document is `v1/swagger.json`.

## 8. Authentication

- `/api/v1/auth/login` exchanges a shared key for a JWT. This is a *service*
  token, not user authentication: one shared key, no user store, and a token
  carrying `sub=portfolio-api` and one role claim.
- The portfolio read endpoints are **anonymous by design** — they feed a public CV
  site. Only `/auth/me` requires a token. Adding `RequireAuthorization()` to the
  others is a breaking change for the frontend, not a hardening tweak (2.5).
- Do not rename the endpoint or change the claims shape without an ADR; the
  service-token decision is recorded in 2.5 and `docs/api-reference.md` §2.
- Never accept a secret in a query string.

## 9. CORS

The allow-list is configuration (`Cors:AllowedOrigins`, plus `Cors:DevelopmentOrigins`
which is applied only in Development), read by `ConfigurationSettings.AddCorsOrigins`.
Adding an origin is a security change — ask first. Never `AllowAnyOrigin()` together with
`AllowCredentials()`, and do not add `AllowCredentials()` back without a cookie-based flow
that needs it.

## 10. Checklist for a new endpoint

- [ ] Route under the `/api/v1` group
- [ ] Request DTO with validation
- [ ] Response DTO (not a domain type)
- [ ] `CancellationToken` threaded to the repository
- [ ] `Result` mapped to the correct status code
- [ ] `.WithName` / `.Produces` / `.ProducesProblem`
- [ ] Integration test: success + one failure
- [ ] `docs/api-reference.md` updated
