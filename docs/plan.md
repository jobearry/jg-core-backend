# Optional authenticated GitHub visibility

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
