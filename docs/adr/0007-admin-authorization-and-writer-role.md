# ADR-0007 — Admin authorization and least-privilege write credential

- **Status:** Accepted
- **Date:** 2026-11-22
- **Deciders:** jthan-dev (repository owner)
- **Related:** [ADR-0003](0003-aggregate-boundary.md), [ADR-0006](0006-single-host-vertical-slices.md), [`../../rules/api.md`](../../rules/api.md), [`../known-issues.md`](../known-issues.md) (KI-010), [`../operations.md`](../operations.md)

## Context

The API is currently read-only. Public portfolio reads are anonymous by design,
and the only protected endpoint is `/auth/me`. Authentication is one shared key
exchanged at `/api/v1/auth/login` for a JWT carrying `sub=portfolio-api` and
`role=User`; Core rules require exactly one login endpoint and one token service.

The owner now wants CRUD on `experience`, `tech_stack`, `tech_stack_category`,
`project`, `recommendation`, and the `project_tech_stack` join table. That creates
two risks: a write endpoint could be left anonymous, and a runtime bug could give
the public read path write or DDL rights. The database already separates reads
(`portfolio_app`, `SELECT` only) from migrations (GitHub secret
`PRODUCTION_CONNECTION_STRING`, owner rights). There is currently no authorization
fallback policy, so a newly added endpoint would be anonymous unless explicitly
protected (KI-010).

## Decision

**We will make the existing service token an admin token, protect by default, and
add a separate DML-only database role for writes.**

- `Jwt:Key` remains the single login secret. There is still one login endpoint and
  one token service. The issued role claim changes from `User` to `Admin` and is
  written/read as the raw claim name `"role"` because inbound claim mapping is off
  (`src/JGBackend/Configuration/AuthenticationConfiguration.cs:44`).
- An `Admin` authorization policy requires an authenticated principal with
  `role=Admin`. That policy is also the **fallback** policy, so a new endpoint is
  admin-only unless it opts out.
- Public portfolio reads, `/api/v1/auth/login`, and health checks explicitly opt
  out with `.AllowAnonymous()`. `/auth/me` keeps its existing explicit
  `.RequireAuthorization()`.
- The runtime read connection stays `JDBConnection` → `portfolio_app` (`SELECT`
  only). A new `JDBConnectionWrite` connection uses `portfolio_writer`, granted
  `SELECT, INSERT, UPDATE, DELETE` on the portfolio tables and sequence usage.
  It has no DDL, no `TRUNCATE`, and no access to `__EFMigrationsHistory`.
- Migrations remain owned by GitHub Actions via `PRODUCTION_CONNECTION_STRING`.
  `Database__AutoMigrate=false` stays in production. The app never uses
  `portfolio_writer` for DDL.
- A second read/write `DbContext` is registered alongside the read-only one. Write
  ports live in the owning `Features/<Slice>/` slices; one aggregate is saved per
  transaction, with `Project` as the aggregate root (ADR-0003).
- Write endpoints live under `/api/v1/admin/...` and carry explicit
  `.RequireAuthorization("Admin")`, an `AdminWrite` rate limit, and an admin CORS
  policy in addition to the fallback.
- No audit table is introduced. The existing `project.updated_at` column is stamped
  on admin edits; the other write tables have no edit timestamp yet (KI-052).
  Failures are logged; successful writes are not.

This ADR supersedes ADR-0006 only where it recorded the service-token claim shape
as unchanged. The rest of ADR-0006 remains in force.

## Consequences

**Positive**

- Public reads keep the same anonymous wire contract while a forgotten endpoint
  fails closed with `401`/`403` instead of exposing a write route.
- One auth system and one secret: no second key to issue, rotate, or leak.
- A public-route bug can only reach `portfolio_app`; changing data requires an
  admin JWT **and** the write connection path.
- Migrations stay isolated in CI; the runtime cannot change schema.

**Negative / accepted costs**

- Existing `role=User` tokens issued before this change are rejected by admin
  routes until they expire. Public reads need no token, so they are unaffected.
- Every public endpoint must opt out of the fallback; a missed annotation returns
  `401`, which is visible but must be caught by tests.
- New tables need an explicit addition to the writer grant script: no
  `ALTER DEFAULT PRIVILEGES` is used for the writer, to avoid granting DML on
  future migration tables.
- Logs are not a durable audit trail. A real audit store is deferred.

**Follow-up work**

- Implement in phases: auth policy and fallback; writer role and Azure setting;
  write contexts and ports; admin endpoints; CORS/rate-limit policies; docs and
  verification; deploy.
- Close KI-010 when the fallback policy ships.
- Add admin endpoint documentation to `docs/api-reference.md` and the new
  connection string to `docs/operations.md`.

## Alternatives considered

| Option | Why rejected |
|---|---|
| Second admin key/secret | More rotation and storage for no extra protection: the same owner holds both keys |
| Separate user store | Far too much machinery for one personal backend |
| Grant DML to `portfolio_app` | Removes the read-only guarantee from every public endpoint |
| Reuse the migration owner at runtime | Gives the app DDL rights and breaks the CI-owned migration boundary |
| No fallback policy | New endpoints stay anonymous unless someone remembers to protect them (KI-010) |
| Admin methods on public routes | Valid, but anonymous `GET` and admin `PUT` on one route is easier to misconfigure than a separate group |
