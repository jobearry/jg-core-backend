# Core — module guidance

`src/Core` is the shared module: authentication, health, and cross-cutting
infrastructure used by the host.

## Layout

```
Features/Auth/          login + me endpoints, JWT token service, options
Features/Health/        diagnostic health endpoint
Features/DependencyInjection.cs   AddFeatures() / MapFeatures()
Infrastructure/         JWT options, rate-limit policy names, exception handler
Infrastructure/DependencyInjection.cs   AddInfrastructure()
```

## Rules

- **Auth is the single auth system.** There must be exactly one login endpoint
  (`/api/v1/auth/login`) and one token service. Portfolio's duplicate auth is
  removed during the single-host merge; do not add a second one.
- The signing key is also the login secret. Compare it in fixed time, require at
  least 32 bytes, and never log it.
- Token validation checks issuer, audience, lifetime, and signing key.
- Rate limiting is per-IP, fixed window, 5 requests/minute on login. Extend
  policies here, not in the host.
- Forwarded-header handling lives in the host; Core may read `RemoteIpAddress`
  but must not re-implement proxy trust.
- No EF Core and no database dependencies.

## Tests

Put tests in `tests/Core/`. Today the auth-related tests still live in
`tests/Portfolio/Portfolio.Api.Tests` because Portfolio owns the current host;
move them when the merge lands.

## Verification

```sh
dotnet build src/Core/Core.csproj -c Release
dotnet test JGBackend.sln -c Release
```
