---
applyTo: "Generic/Generic.API/**"
---

# Generic.API — host project guidance

This project is the composition root only. It wires services and the HTTP
pipeline; it contains **no** feature logic.

- Keep `Program.cs` thin: builder → service registration → pipeline → run.
  Each startup concern belongs in `Generic/Generic.API/Configuration/` as a
  `AddXxx(...)` / `UseXxx(...)` extension method (see `Configuration/`).
- `Configuration/SwaggerConfiguration.cs` — OpenAPI v1 only (no OAuth, no v2),
  Bearer JWT security definition + global security requirement.
- `Configuration/AuthenticationConfiguration.cs` — binds `JwtOptions`, registers
  `AddJwtBearer` with issuer/audience/lifetime/issuer-signing-key validation.
  Fails fast at startup with an actionable message when `Jwt` is missing or
  the key is shorter than 32 bytes — never let a `null` `JwtOptions` through.
  Development ships a loudly-labelled insecure key in `appsettings.Development.json`;
  Production requires real `Jwt__*` environment variables.
- `Configuration/CorsConfiguration.cs` — `AllowSPA` policy
  (`http://localhost:4200` + the production Vercel origins).
- `Configuration/RateLimitConfiguration.cs` — named fixed-window policies from
  `RateLimitPolicies`; `AuthLogin` = 5 requests/minute/IP, rejects with 429.
- `Configuration` files must stay one-concern-per-file; do not reintroduce a
  single monolithic `ConfigurationSettings.cs`.
- The host maps all slices via one `app.MapFeatures()` call plus
  `app.MapHealthChecks("/healthz")`. Never register slice controllers or
  application parts — there are no controllers.
- Unhandled exceptions flow through `GlobalExceptionHandler`
  (`UseExceptionHandler()` first in the pipeline). Do not catch-and-log in
  slices as a substitute.
