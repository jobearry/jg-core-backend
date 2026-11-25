# Shared plans

This file holds plans the owner has explicitly asked to discuss or defer. It is the
only plan location; the existence of a plan is not authorization to execute it.

## A. Optional authenticated GitHub visibility

**Status:** Deferred proposal. Current GitHub endpoints remain anonymous and
public-only. Do not implement or modify this plan unless the user explicitly
asks.

## Scope

Change only the GitHub read surface: repository listing, count, detail, and
language endpoints. Portfolio and Notion access, the GitHub token's server-side
storage, and the existing `/auth/login` and `/auth/me` contracts are out of
scope.

## Goal

Allow anonymous requests to keep receiving public GitHub repository data, while
valid API-JWT requests may receive private repository metadata available to the
configured GitHub token. The current public-only behavior remains in effect
unless this plan is explicitly authorized and implemented.

## Decisions needed

| Decision | Options | Recommendation | Status |
|---|---|---|---|
| Is the existing shared service JWT an acceptable gate for private repository metadata? | Use the shared JWT; introduce caller-specific identity/authorization first; keep all endpoints public-only | Keep public-only unless every holder of the shared API key is trusted with the configured GitHub token's private-repository metadata. This JWT identifies `portfolio-api`, not an individual | Open before implementation |

## Work items

### 1. Add optional API-JWT behavior

- **What:** No `Authorization` header selects the public-only behavior; a valid
  API JWT selects the authorized behavior; an invalid or expired Bearer token
  returns `401` rather than silently falling back to public data.
- **Why:** Preserve anonymous public access while making credential failures
  explicit and preventing an invalid credential from appearing to succeed.
- **How to verify:** API tests cover anonymous requests, valid-token requests,
  invalid/expired-token responses, and OpenAPI `200`/`401` metadata.
- **Risk:** The current JWT is a shared service token. It does not distinguish
  users, so every holder can access the same private repository metadata.
- **Effort:** M

### 2. Select repository visibility by caller

- **What:** Anonymous list requests use GitHub's public-user repository
  endpoint. Authorized requests may list the configured token account's
  repositories, including private ones, filtered to the requested owner.
  Repository count, detail, and language requests follow the same
  public/authorized visibility rule and continue to validate the requested
  owner.
- **Why:** Avoid exposing private metadata to anonymous callers while keeping
  the endpoint shapes consistent across all three routes.
- **How to verify:** Client/service tests assert the correct GitHub path and
  results for each caller mode, including repository counts; private
  repositories are absent anonymously, and owner mismatch or inaccessible
  repositories return `404`.
- **Risk:** The configured GitHub token may see more private repositories than
  intended. Keep it server-side, minimally scoped, and never treat the API JWT
  as proof of a user's GitHub identity.
- **Effort:** M

### 3. Update contracts and operational guidance

- **What:** Update endpoint OpenAPI metadata, API-reference and operations
  documentation, and security rules to describe optional authentication,
  private-data scope, token separation, and the shared-JWT limitation.
- **Why:** Callers and maintainers need a truthful contract before relying on
  authenticated responses.
- **How to verify:** Documentation describes anonymous and authorized behavior
  consistently; endpoint tests and the solution build pass with zero warnings.
- **Risk:** Inaccurate auth metadata can lead clients to assume private data is
  available anonymously or that a JWT represents an individual user.
- **Effort:** S

## Cheapest-first verification

1. Run focused GitHub client, service, and endpoint tests for anonymous,
   authorized, invalid-token, private-repository, and owner-mismatch cases.
2. Inspect generated Swagger metadata for the three GitHub routes.
3. Run `dotnet test JGBackend.sln`.
4. Run `dotnet build JGBackend.sln` and confirm zero warnings.

## B. Relationship endpoints

**Status:** Proposed for discussion. Phase 1 is implemented on this branch; the
remaining phases are not authorized.

### Scope

Relationship writes only. No schema change, no new package and no public write.
Routes stay under `/api/v1/admin`, require the `role=Admin` token, use the
`admin-write` limiter and the admin CORS policy. A write goes through the owning
aggregate's port; a cross-aggregate existence check uses the other slice's
repository port.

### Current coverage

| Relationship | Covered today | Gap |
|---|---|---|
| `experience` → `project` | `POST /admin/experience/{experienceId}/project` creates and attaches | Attach an existing project, detach, move, role-scoped list |
| `project` → `tech_stack` | `PUT /admin/project/{id}/tech-stacks` replaces the whole set | Incremental add/remove |
| `tech_stack` → `tech_stack_category` | Set by name in `POST`/`PUT /admin/skill`, resolved to an id | Standalone reassignment (probably not needed) |

### Decisions needed

| Decision | Options | Recommendation | Status |
|---|---|---|---|
| Attach, detach or move an existing project | A single `PUT /admin/project/{id}/experience` with `experienceId` set to an id or `null`, or paired `POST`/`DELETE` under `/admin/experience/{experienceId}/project/{projectId}` | The single project-side route: `Project` owns `experience_id`, and one contract covers attach, move and detach | Open |
| Admin list of projects for a role | Reuse `GET /api/v1/experience?includeProjects=true`, or add `GET /admin/experience/{experienceId}/project` | Reuse the public projection until the admin app proves it needs a separate raw list | Open |
| Incremental tech-stack links | Add `POST`/`DELETE /admin/project/{id}/tech-stack/{stackId}`, or keep replace-all | Keep replace-all: it is deterministic and avoids half-applied sets | Open |
| Standalone category reassignment | Add `PUT /admin/skill/{id}/category`, or keep the existing skill update | Keep the existing update; the category is already resolved by name there | Open |
| Failure semantics | `404` for an absent parent/child, `400` for a malformed or non-positive id | Use that split consistently; database outages stay `503` | Open |

### Phasing

1. **Done — phase 1:** create a project under an existing experience with
   `Project.AttachToExperience` (ADR-0003).
2. **Phase 2:** after the route-shape decision, attach/detach/move an existing
   project. Tests cover invalid ids, missing parent/child, a failed lookup and the
   three success outcomes.
3. **Phase 3:** only if the owner confirms the need, add incremental stack links
   or a role-scoped admin list.

Out of scope: schema changes, public writes, category reassignment and any change
to the anonymous read contract.

### Cheapest-first verification

1. Handler tests with hand-written fakes for every branch.
2. HTTP contract tests for status codes, `Location`, error codes, auth and Swagger tags.
3. Assert the admin rate-limit metadata and CORS preflight still cover every new route.
4. Run `dotnet build JGBackend.sln`, `dotnet test JGBackend.sln`,
   `dotnet format JGBackend.sln --verify-no-changes --no-restore` and `git diff --check`.
