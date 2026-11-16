---
applyTo: "Generic/Generic.Infra/**"
---

# Generic.Infra — shared infrastructure guidance

Shared, cross-cutting infrastructure only. Slices must not reach sideways for
infra; if something is used by exactly one slice, it belongs in that slice.

- Register everything via `DependencyInjection.AddInfrastructure(IServiceCollection, IConfiguration)`;
  the host calls this in `Program.cs`. No service locator usage.
- Current contents: `Options/JwtOptions.cs` (bound from the `Jwt` section),
  `GlobalExceptionHandler` (RFC 7807 ProblemDetails for unhandled exceptions),
  `RateLimitPolicies` (names of the host's named rate-limit policies).
- Add shared persistence, external HTTP clients, or options types here — with
  an `AddXxx` extension wired into `AddInfrastructure` — but only when a
  second slice needs them. No speculative base classes or generic helpers.
- Keep `Generic.Infra` free of HTTP/endpoint concerns; it may depend on the
  ASP.NET Core shared framework but must not reference `Generic.API` or
  `Generic.Features`.
