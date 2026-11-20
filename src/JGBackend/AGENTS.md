# JGBackend — host guidance

`src/JGBackend` is the composition root and executable. It wires middleware and
service registrations; it owns no business logic.

## What belongs here

- `Program.cs` — service registration and the middleware pipeline.
- `Configuration/` — Swagger, CORS, authentication, rate limiting, forwarded headers.
- `appsettings*.json` — host-level configuration only.
- `Properties/`, `JGBackend.http` — local run and manual requests.

Put features in `src/Core` or `src/Portfolio`, not here.

## Pipeline order

Keep this order; it is load-bearing:

1. `UseForwardedHeaders()` — restores the real client IP and scheme behind Azure.
2. `UseExceptionHandler()` — turns unhandled exceptions into `ProblemDetails`.
3. `UseCors(...)` — must wrap the rate limiter so 429 responses keep CORS headers.
4. `UseHttpsRedirection()`.
5. `UseRateLimiter()`.
6. `UseAuthentication()` / `UseAuthorization()`.
7. Map endpoints.

## Rules

- Authentication is JWT bearer, HMAC-SHA256, shared-key login. The key must be at
  least 32 bytes or startup fails.
- Rate-limit policies are defined in `src/Core/Infrastructure/RateLimitPolicies.cs`
  and applied on endpoints, not globally.
- CORS origins are an explicit allow-list. Do not add wildcards or credentials
  unless the task asks for it.
- No database, no EF Core, no persistence here.

## Verification

```sh
dotnet build src/JGBackend/JGBackend.csproj -c Release
dotnet run --project src/JGBackend
```
