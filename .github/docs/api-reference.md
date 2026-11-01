# API reference

Base path: `/api/v1` · Content type: `application/json`
OpenAPI: `/swagger` (Development only, or `Swagger:Enabled=true`) · Document: `v1/swagger.json`

## 1. Endpoint catalogue

| Method | Path | Auth | Parameters | Returns |
|---|---|---|---|---|
| `POST` | `/api/v1/auth/login` | none | body `{ "key": "..." }` | `{ "token": "<jwt>" }` |
| `GET` | `/api/v1/auth/me` | **JWT** | — | claims grouped by type |
| `GET` | `/api/v1/experience` | none* | `includeProjects` (bool, default `false`) | `ExperienceDto[]` |
| `GET` | `/api/v1/project` | none* | `includeTools` (bool, default `false`) | `ProjectDto[]` |
| `GET` | `/api/v1/recommendation` | none* | — | `RecommendationDto[]` |
| `GET` | `/api/v1/skill` | none* | — | `TechStackDto[]` |
| `GET` | `/api/v1/overview` | none* | — | `DashboardItemDto[]` |
| `GET` | `/api/v1/notion/{pageId}` | none* | path `pageId` | `NotionPageDto[]` |
| `GET` | `/api/v1/notion/{pageId}/count` | none* | path `pageId` | `number` |
| `GET` | `/api/v1/github/owner/{owner}/repositories` | none | path `owner` | `GithubRepositoryDto[]` |
| `GET` | `/api/v1/github/repository/{owner}/{repository}/languages` | none | path `owner`, `repository` | `GithubRepositoryLanguagesDto` |
| `GET` | `/api/v1/github/owner/{owner}/repositories/{repositoryId}` | none | path `owner`, `repositoryId` (long) | `GithubRepositoryDto` |

Portfolio, Notion, and current GitHub read endpoints are anonymous by design.
The GitHub endpoints return public repository data only; possible authenticated
access to private repository data is tracked in
[plan 002](../plans/002-github-visibility-auth.md).

Route nouns are **singular** (`/experience`, not `/experiences`). Keep that
convention for consistency; a rename is a breaking change.

## 2. Authentication

**Portfolio, Notion, and current GitHub read endpoints are public by design.**
The GitHub endpoints are limited to public repository data even though the
server-configured GitHub token may have broader upstream access. Optional
authenticated access to private repository data is not implemented; see
[plan 002](../plans/002-github-visibility-auth.md).

What exists is a **service-token flow**, not a user login: one shared key, no user store,
and a token that says so.

### `POST /api/v1/auth/login`

Compares `key` against `Jwt:Key` in constant time. On success returns a JWT with
`sub=portfolio-api` and `role=User`, valid for `Jwt:ExpirationInMinutes`.

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "key": "<shared secret>" }
```

```json
{ "token": "eyJhbGciOiJIUzI1NiIs..." }
```

| Failure | Status | `code` |
|---|---|---|
| blank key | `400` | `validation_failed` |
| key mismatch | `401` | `invalid_credentials` |
| configured key < 32 bytes | `500` | `server_misconfigured` |

The wrong-key message never says which part was wrong, and the length check runs only
*after* the key matches, so an anonymous caller cannot learn the length of the secret
from the response.

`sub` is the literal `portfolio-api`: the token represents the service, because there is
one shared key rather than a person. `JwtBearer` is configured with
`NameClaimType = "sub"` and `MapInboundClaims = false`, so `User.Identity.Name` is
populated.

### `GET /api/v1/auth/me`

Requires a token. Returns claims from the presented token, grouped by claim type:

```json
{
  "sub": ["portfolio-api"],
  "role": ["User"]
}
```

## 3. Portfolio endpoints

Responses are **DTOs** (`src/JDB_Portfolio_API.Application/DTO`), not domain
entities (ADR-0004). The domain model can now change without changing
the wire contract.

### `GET /api/v1/experience?includeProjects=false`

Returns roles, newest id first. With `includeProjects=true` it eagerly loads
projects and their tech stacks.

```json
[
  {
    "id": 4,
    "company": "Applied ICT Inc.",
    "role": "Software Engineer",
    "startedAt": "2026-06-25T00:00:00",
    "finishedAt": null,
    "durationMonths": 3,
    "description": "Transitioned into Software Engineer role",
    "responsibility": "Collaborate and/or maintain …",
    "type": "Corporate",
    "projects": [
      {
        "id": 7,
        "projectName": "Portfolio API",
        "description": "…",
        "contribution": "…",
        "techStacks": [
          { "id": 2, "stackName": "Angular", "imgSrc": "…", "category": "Frontend" }
        ]
      }
    ]
  }
]
```

Notes:

- `company` and `role` are **strings**. The value objects are flattened at the
  boundary; the wire no longer carries `{ "name": … }` or `{ "value": … }`.
- `company` is **never `null`**. A role always names a company; if you cannot
  disclose the client, use a truthful stand-in such as `"Self-employed"` or
  `"Contract — NDA"`. The column is `NOT NULL` with a non-blank `CHECK`
  (migration `20261022150752_RequireCompanyName`), so the guarantee is
  enforced by the database rather than by the mapper.
- `role` is **nullable**. `"role": null` means the role is deliberately not
  declared — it is not an error, and it does not affect the rest of the list.
  (Before 2026-10-22 a blank role threw inside the mapper and returned `500` for
  every experience; see plan item 1.5.) The asymmetry with `company` is
  intentional: "not disclosed" is a real answer for a *role*, not for an employer.
- `durationMonths` is whole **calendar** months, `int?`:
  - between `startedAt` and `finishedAt` when the role has ended,
  - between `startedAt` and *now* while the role is ongoing,
  - `null` when `startedAt` is unknown.
  A partial month at the end does not count, so `2020-01-31 → 2020-02-28` is `0`.
  It replaces the old `duration` field, which serialized a `TimeSpan` as
  `"1234.05:06:07"`.
- `finishedAt` is `null` **only for a role that is still ongoing**. A role that has
  ended carries its end date. The column was added on 2026-10-22 (migration
  `20261022064321_AddExperienceFinishedAt`); the existing rows still need their
  end dates backfilled, which is why past roles currently show a growing
  `durationMonths`.

### `GET /api/v1/project?includeTools=false`

Returns projects ordered by id ascending. `includeTools=true` loads tech stacks
and their categories.

Response item: `{ id, projectName, description, contribution, techStacks[] }`.

Two keys are deliberately absent:

- **`experienceId`** (removed 2026-10-22): a project is its own aggregate
  (ADR-0003, decision D3), and the link back to the role is internal.
- **`period`** (removed 2026-10-22): it had no column and no mapping,
  so it was always `null`. A project has no dates of its own — those belong to
  the role it sits under.

`techStacks[]` items are `{ id, stackName, imgSrc, category }` — no `createdAt`.

### `GET /api/v1/recommendation`

Returns testimonials ordered by id ascending.

```json
[
  {
    "id": 1,
    "personName": "Jane Doe",
    "description": "Great to work with.",
    "careerPosition": "Engineering Manager",
    "postedAt": "2024-06-01T00:00:00"
  }
]
```

`careerPosition` is a **string** and is **nullable**. `"careerPosition": null`
means the recommender does not want their title published; it is a supported
state, not missing data.

### `GET /api/v1/skill`

Returns tech stacks ordered by id ascending, including category name.

```json
[
  { "id": 1, "stackName": "C#", "imgSrc": "…", "category": "Backend" }
]
```

`createdAt` is **gone** — a persistence audit column no client asked
for.

On failure, this endpoint uses the same typed error mapping as the other
portfolio endpoints; a database failure returns `503` with
`database_unavailable`.

### `GET /api/v1/overview`

Returns one counter per dashboard tile. The `key` is the contract; the client owns the
label, icon, order and link, so adding a counter is additive and reordering them is free.

```json
[
  { "key": "projects", "count": 12 },
  { "key": "skills", "count": 30 },
  { "key": "recommendations", "count": 4 }
]
```

The three counts are still fetched sequentially — one scoped `DbContext` cannot run the
queries concurrently — and each one materialises rows rather than issuing `COUNT(*)`
(KI-046). Unknown keys must be ignored by the client.

### Breaking change — 2026-10-22

Every change a client has to absorb, in one place:

| Key | Before | After | Endpoint |
|---|---|---|---|
| `company` | `{ "name": "…" }` | `"Applied ICT Inc."` | `/experience` |
| `role` | `{ "value": "…" }` | `"Software Engineer"` | `/experience` |
| `duration.startedAt` | nested in `duration` | **`startedAt`**, top level | `/experience` |
| `duration.finishedAt` | nested in `duration` | **`finishedAt`**, top level | `/experience` |
| `duration.duration` | `"1231.05:06:07"` | **removed** → use `durationMonths` | `/experience` |
| `durationMonths` | — | **new**, `int?` calendar months | `/experience` |
| `experienceId` | `3` | **removed** | `/project`, nested `projects[]` |
| `period` | `null` | **removed** | `/project` |
| `techStacks[].createdAt` | ISO timestamp | **removed** | `/project` |
| `careerPosition` | `{ "value": "…" }` | `"Engineer"` | `/recommendation` |
| `createdAt` | ISO timestamp | **removed** | `/skill` |

Field **names** on `/overview` and `/notion/{pageId}` are unchanged.

Nothing in this table became non-nullable: every field that was previously absent
or `null` is still optional. The disruptive part is the `duration` object being
flattened.

One field did gain a guarantee later the same day: **`company` can no longer be
`null`** (migration `20261022150752_RequireCompanyName`). No response
changed shape — every stored row already had a company — but a client that
branches on `company === null` can drop that branch. `role` is still nullable.


## 4. Notion endpoints

### `GET /api/v1/notion/{pageId}`

Fetches `https://api.notion.com/v1/pages/{pageId}/markdown`, then tolerantly
extracts `<page url="…">` fragments and optional `<mention-date start="…"/>`
values from the returned markdown. Malformed fragments are skipped.

```json
[
  { "url": "https://…", "date": "2024-01-01", "title": "Blog post title" }
]
```

### `GET /api/v1/notion/{pageId}/count`

Same call, returns just the array length as a bare number.

Both endpoints fail with `502` if Notion errors or the parse throws — with
`code` set to `notion_unavailable` or `notion_invalid_response` (see §6).

## 5. GitHub endpoints

These read endpoints are anonymous and call GitHub using the server-configured
`Github:Token`. They return public repositories only: listing uses GitHub's
public-user repository endpoint, and the repository detail and language routes
check repository visibility before returning data. The GitHub token stays
server-side and is never sent to the browser. The API JWT is not required.

### `GET /api/v1/github/owner/{owner}/repositories`

Lists public repositories for `{owner}` using GitHub's `/users/{owner}/repos`
endpoint. Results are filtered case-insensitively by owner and private entries
are excluded. The client follows GitHub's page size of 100. Repository entries
are portfolio-owned DTOs, not GitHub's nested vendor response.

### `GET /api/v1/github/repository/{owner}/{repository}/languages`

Returns language byte counts only for a public repository. Private or unknown
repositories return `404`. The response's `languages` object has language names
as keys and GitHub byte counts as values:

```json
{
  "languages": { "C#": 1234, "JavaScript": 567 }
}
```

### `GET /api/v1/github/owner/{owner}/repositories/{repositoryId}`

Returns the same repository DTO using the numeric repository ID, only when it
belongs to the specified owner. A repository that does not exist or belongs to
another owner returns `404`.

An unknown, private, or wrong-owner repository returns `404` with code
`github_repository_not_found`. GitHub endpoints do not require an API JWT.
Other GitHub request failures return `502` with `code` `github_unavailable`;
unreadable responses return `502` with `code` `github_invalid_response`.

## 6. Error behaviour

Application failures are translated to `ProblemDetails` by
`Endpoints/ApiResult.cs`. Unhandled exceptions use `GlobalExceptionHandler`.
Endpoints do not pick status codes; they call `result.ToHttpResult()`. The
response carries a **machine-readable `code`** alongside the human-readable
`detail`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.11",
  "title": "Service temporarily unavailable",
  "status": 503,
  "detail": "Failed to get portfolio experience content. Please try again later.",
  "code": "database_unavailable",
  "traceId": "…"
}
```

Clients should branch on `status`, and use `code` to tell two failures with the
same status apart.

### Status map

| `ErrorKind` | Status | `code` | When |
|---|---|---|---|
| `NotFound` | `404` | `not_found`, `github_repository_not_found` | a record or GitHub repository that was asked for does not exist |
| `Validation` | `400` | `validation_failed` | the caller sent something invalid |
| `Unauthorized` | `401` | `invalid_credentials`, `unauthorized` | a missing or wrong credential |
| `Conflict` | `409` | `conflict` | the request conflicts with current state |
| `Infrastructure` | `503` | `database_unavailable` | our own PostgreSQL is unreachable |
| `Upstream` | `502` | `notion_unavailable`, `notion_invalid_response`, `github_unavailable`, `github_invalid_response` | Notion or GitHub failed, or returned something unreadable |
| `Unexpected` | `500` | `unexpected_error` | a bug |

Why the two dependency kinds are different statuses: a database outage is
**ours**, and `503` tells a client to retry later. Notion failing is an
**upstream** problem, and `502 Bad Gateway` is literally what that code means.
Collapsing them would lose the one distinction that changes a client's reaction.

`Unexpected` is deliberately not `503`. A `NullReferenceException` is a defect;
reporting it as an outage would teach clients to retry something that can never
succeed.

### Exception messages are not published

`detail` never contains `exception.Message`. Npgsql messages can include host
names, database names, usernames and SQL. The client gets a stable sentence plus
a code; the diagnostic detail goes to the application log instead.

This is a **change** from the previous behaviour, where the raw exception text
was returned to the caller.

### Unhandled exceptions

Anything that escapes an endpoint — model binding, dependency injection, a bug the
services did not classify — is caught by `GlobalExceptionHandler` and answered with
the same shape and the same `500 → unexpected_error`. There is no path that returns a
bodyless 500.

One exception: if the **client disconnected** before the response was ready,
`OperationCanceledException` is answered with `499 Client Closed Request` and logged at
Debug, not Error. A client navigating away is not a server failure.

The exception detail is written to the application log, where it belongs.

## 7. CORS

An explicit allow-list, configured rather than hardcoded:

| Key | Applied when | Default |
|---|---|---|
| `Cors:AllowedOrigins` | every environment | `https://jgolimlim-portfolio.vercel.app`, `https://jgolimlim-web.vercel.app` |
| `Cors:DevelopmentOrigins` | `Development` only | `http://localhost:4200` |

A new domain, or a Vercel preview deployment, is an environment variable —
`Cors__AllowedOrigins__2=https://my-branch-git-team.vercel.app` — not a code edit and a
redeploy. Adding an origin is still a security change: ask first.

The policy is `GET, POST` and `Content-Type, Authorization`. There is **no
`AllowCredentials()`**: bearer tokens travel in the `Authorization` header, which CORS
treats as an ordinary request header, and nothing in this API sets a cookie. If the
frontend ever moves to cookie auth or calls with `withCredentials: true`, that flag has
to come back in the same change.

Allowing localhost only in Development is deliberate: otherwise any page served from a
local port could read the deployed API. The tests in `CorsPolicyTests` pin both that and
the absence of the credentials flag.

**CORS is not access control.** It tells *browsers* whether JS from an origin may read a
response; `curl`, Postman and server-side clients ignore it entirely. What keeps the
portfolio data public is the anonymity decision in §2, not this list.
