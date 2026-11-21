# ADR-0006 — One host, one class library, vertical slices

- **Status:** Accepted
- **Date:** 2026-11-21
- **Deciders:** jthan-dev (repository owner)
- **Related:** [ADR-0001](0001-five-project-clean-architecture.md) (superseded), [`../architecture.md`](../architecture.md), [`../../rules/architecture.md`](../../rules/architecture.md), [`../operations.md`](../operations.md)

## Context

ADR-0001 split the API into five projects around a single developer, one bounded
context, and one Azure Web App. The project boundaries stayed visible, but the
cost grew: the same auth, exception handling, configuration, and rate limiting
were duplicated wherever a host was needed, and the solution ended up with two
hosts, two images, two compose stacks, and two deploy workflows for one
deployable target.

The mapper-heavy layering also concentrated defects: adding one field touched a
persistence entity, a domain entity, a mapper, a DTO, and several tests. A field
could be dropped silently by a mapper that compiled and looked plausible. The
workload is a portfolio-scale read API, not a system whose project boundaries
are being changed by multiple teams, so the ceremony no longer bought enough
visible safety to justify two pipelines and two composition roots.

## Decision

**There is one deployable: `src/JGBackend` is the only host and only composition root.**

- `src/Core` and `src/Portfolio` are class libraries referenced by the host;
  neither references the host, and no new project reference was added.
- `src/Portfolio` is organised by vertical slice under `Features/<Slice>/`,
  owning its endpoint, handler, repository port and adapter, and DTOs.
  `Shared/Common`, `Shared/Endpoints`, and `Shared/Persistence` hold only what
  more than one slice genuinely uses.
- `src/Core` owns auth, health, rate limiting, and cross-cutting infrastructure.
  One JWT lifetime setting (`Jwt:ExpirationInMinutes`) and one global exception
  handler serve the whole API.
- One Docker image (`docker/JGBackend.Dockerfile`), one compose stack, one
  deploy workflow, and one `WebApplicationFactory` fixture
  (`JGBackendApiFactory`) replace the duplicates.
- Rate-limit policy names remain in `Core.Infrastructure.RateLimitPolicies`.
  The host configures the policies and passes their names as optional arguments
  to `MapPortfolioFeatures(...)`, so `Portfolio` never needs a reference to
  `Core`.
- The public contract is unchanged: same routes under `/api/v1`, tags, operation
  names, Swagger schema names, error codes, and service-token claim shape.
- EF migrations and the context remain owned by `Portfolio`; the host calls
  `Database.Migrate()` only when `Database:AutoMigrate` is enabled. The host and
  the design-time factory share one `UserSecretsId`.

## Consequences

**Positive**

- One place to change configuration, middleware, auth, and startup; one image to
  build and one workflow to deploy.
- A feature's endpoint, handler, query, and DTO sit together, so a field change
  is local and reviewable in one diff.
- The read API projects straight from EF to DTOs where possible, removing the
  mapper that caused the `FinishedAt`-class of defect.
- Dependency direction is still reviewable in `.csproj` files and enforced by
  convention: features may use another slice's DTO or repository port, not its
  internals.

**Negative / accepted costs**

- `Portfolio` is a class library with an ASP.NET Core framework reference; it
  cannot be run or deployed on its own.
- Rate-limit policy names cross the host/module boundary as strings, trading
  compile-time constants for a clean dependency graph.
- The host and the EF design-time factory must keep the same `UserSecretsId`
  synchronized by hand.
- The rename churn makes this branch's diff large even though the HTTP contract
  is unchanged.

**Follow-up work**

- Complete the owner handoff in [`../operations.md`](../operations.md) §8:
  retire the old app registration, update the federated credential subject,
  confirm secrets, and review the surviving Web App settings.
- Track the secrets-overrides-environment precedence in KI-036 and the
  timestamp columns in `docs/known-issues.md`; neither is caused by this change.
- ADR-0002 through ADR-0005 stay in force for the boundaries they describe
  inside the new layout; only ADR-0001 is superseded.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Keep the five projects and two hosts | The duplication and double pipeline were the problem; no new safety was being bought |
| Two projects (core + host) without vertical slices | Fewer projects, but feature code stays scattered by technical layer and field changes still cross folders |
| One project with everything in folders | Loses the `Core` / `Portfolio` module boundary, which still separates shared infrastructure from portfolio data |
| One host per module sharing a solution | Two deployables and two workflows for one Azure Web App; the opposite of the goal |
