---
applyTo: "Generic/Generic.Features/**"
---

# Generic.Features — vertical slice guidance

Each folder under `Generic/Generic.Features/<FeatureName>/` is a self-contained
slice: endpoint mapping, DTOs, and handler/service logic live together.

- HTTP surface is minimal APIs only. A slice exposes a
  `Map<Slice>Endpoints(this IEndpointRouteBuilder)` extension; the host calls
  it via `app.MapFeatures()`. Register DI in `DependencyInjection.cs`
  (`AddFeatures`) and endpoint mapping alongside it (`MapFeatures`).
- Route groups: `api/v1/<feature>`; tag with `.WithTags("Feature")`; add
  `.WithSummary(...)`/`.WithName(...)` for Swagger metadata.
- Auth slices: use `.RequireAuthorization()` for protected endpoints; apply
  named rate-limit policies from `RateLimitPolicies` for sensitive routes
  (`AuthLogin` on `login`).
- Validation lives in the endpoint (or a small local helper); expected
  failures return `Results.Problem(...)` (RFC 7807) with the right status
  code. Do not introduce a validation library until a second slice needs it.
- DTOs and mapping are co-located in the slice. No shared DTO project; do not
  reference `Generic.Infra` from a slice for anything other than shared
  options/cross-cutting helpers deliberately exposed for reuse.
- Never reference `Generic.API` from a slice; slices must remain host-agnostic.
- Add DTOs with `required` members or default values; keep token/claims shape
  stable unless the API version is deliberately revved.
