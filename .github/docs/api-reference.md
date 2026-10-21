# API reference

Base path: `/api/v1` · Content type: `application/json`
OpenAPI: `/swagger` (Development only) · Document: `v1/swagger.json`

## 1. Endpoint catalogue

| Method | Path | Auth | Parameters | Returns |
|---|---|---|---|---|
| `POST` | `/api/v1/auth/login` | none | body `{ "key": "..." }` | `{ "token": "<jwt>" }` |
| `GET` | `/api/v1/auth/me` | **JWT** | — | claims grouped by type |
| `GET` | `/api/v1/experience` | none* | `includeProjects` (bool, default `false`) | `Experience[]` |
| `GET` | `/api/v1/project` | none* | `includeTools` (bool, default `false`) | `Project[]` |
| `GET` | `/api/v1/recommendation` | none* | — | `Recommendation[]` |
| `GET` | `/api/v1/skill` | none* | — | `TechStack[]` |
| `GET` | `/api/v1/overview` | none* | — | `DashboardItem[]` |
| `GET` | `/api/v1/notion/{pageId}` | none* | path `pageId` | `NotionPage[]` |
| `GET` | `/api/v1/notion/{pageId}/count` | none* | path `pageId` | `number` |

\* `RequireAuthorization()` is present but **commented out** on all portfolio and
Notion endpoints. Only `/auth/me` is actually protected. Treat the API as public.

Route nouns are **singular** (`/experience`, not `/experiences`). Keep that
convention for consistency; a rename is a breaking change.

## 2. Authentication

### `POST /api/v1/auth/login`

Compares `key` against `Jwt:Key` in constant time. On success returns a JWT with
a single `role=User` claim, valid for `Jwt:ExpirationInMinutes`.

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "key": "<shared secret>" }
```

```json
{ "token": "eyJhbGciOiJIUzI1NiIs..." }
```

| Failure | Status |
|---|---|
| key mismatch | `401` |
| configured key < 32 bytes | `500` (server misconfiguration) |

There is no `sub` claim, so `User.Identity.Name` is null. This is a service
token, not user authentication.

### `GET /api/v1/auth/me`

Returns claims from the presented token, grouped by claim type:

```json
{ "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["User"] }
```

## 3. Portfolio endpoints

### `GET /api/v1/experience?includeProjects=false`

Returns roles, newest id first. With `includeProjects=true` it eagerly loads
projects and their tech stacks (4-level include).

Response item shape **today** (domain entity serialized directly):

```json
[
  {
    "id": 3,
    "company": { "name": "Acme Corp" },
    "duration": {
      "startedAt": "2023-01-15T00:00:00",
      "finishedAt": null,
      "duration": "1234.05:06:07"
    },
    "description": null,
    "role": { "value": "Senior Developer" },
    "responsibility": null,
    "type": null,
    "projects": [
      {
        "id": 7,
        "experienceId": 3,
        "projectName": "Portfolio API",
        "description": "…",
        "contribution": "…",
        "period": null,
        "techStacks": [
          { "id": 2, "stackName": "Angular", "imgSrc": "…", "createdAt": "…", "category": "Frontend" }
        ]
      }
    ]
  }
]
```

Notes:

- `finishedAt` is **always null** (the mapping discards it), so `duration` grows
  every day for past roles. Plan item 1.3.
- Value objects appear as objects (`{ "value": … }`, `{ "name": … }`) — a direct
  symptom of having no DTO layer.
- `TimeSpan` serializes as a `c`-format string (`"1234.05:06:07"`).
- `description`, `responsibility`, `type` are not explicitly mapped; their
  population depends on AutoMapper's private-setter behaviour and is untested.

### `GET /api/v1/project?includeTools=false`

Returns projects ordered by id ascending. `includeTools=true` loads tech stacks
and their categories.

Response item: `{ id, experienceId, projectName, description, contribution, period, techStacks[] }`.
`period` is always `null` (no column, no mapping).

### `GET /api/v1/recommendation`

Returns testimonials ordered by id ascending.

```json
[
  {
    "id": 1,
    "personName": "Jane Doe",
    "description": "Great to work with.",
    "careerPosition": { "value": "Engineering Manager" },
    "postedAt": "2024-06-01T00:00:00"
  }
]
```

### `GET /api/v1/skill`

Returns tech stacks ordered by id ascending, including category name.

```json
[
  { "id": 1, "stackName": "C#", "imgSrc": "…", "createdAt": "…", "category": "Backend" }
]
```

**This endpoint does not check `IsSuccess`** — on failure it returns `200` with
an empty body. Plan item 2.5.

### `GET /api/v1/overview`

Returns three dashboard widgets. Copy is hardcoded in
`PortfolioService.GetOverview`.

```json
[
  { "title": "Projects", "subtitle": "projects across all roles", "icon": "", "count": 12, "description": "Homelabbing and App Building" },
  { "title": "Skills", "subtitle": "tools and technologies", "icon": "", "count": 30, "description": "Focusing on Angular and .NET" },
  { "title": "Recommendations", "subtitle": "satisfied clients", "icon": "", "count": 4, "description": "Collaboration and communication" }
]
```

The three counts are fetched sequentially — three round trips that could be
parallel or a single query.

## 4. Notion endpoints

### `GET /api/v1/notion/{pageId}`

Fetches `https://api.notion.com/v1/pages/{pageId}/markdown`, then parses the
markdown as XML wrapped in `<root>…</root>`, extracting `<page url="…">` nodes
with an optional `<mention-date start="…"/>`.

```json
[
  { "url": "https://…", "date": "2024-01-01", "title": "Blog post title" }
]
```

### `GET /api/v1/notion/{pageId}/count`

Same call, returns just the array length as a bare number.

Both endpoints fail with `502` if Notion errors or the parse throws.

## 5. Error behaviour

Every failure path does:

```csharp
return Results.Problem(result.Error, statusCode: 502);
```

So the client receives:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Bad Gateway",
  "status": 502,
  "detail": "Failed to get portfolio experience content: <exception message>",
  "traceId": "…"
}
```

Problems with this:

1. `502` is wrong for a database failure (`500`/`503`) and for "not found"
   (`404`).
2. The exception message is leaked to the client.
3. There is no machine-readable error code.
4. Unhandled exceptions (e.g. a mapping failure) bypass this entirely and
   produce a bare `500`.

Target: typed error kinds mapped in one place to `ProblemDetails` with correct
status codes (plan 2.2).

## 6. CORS

Allowed origins: `http://localhost:4200`,
`https://jgolimlim-portfolio.vercel.app`, `https://jgolimlim-web.vercel.app`.
Any method, any header, credentials allowed. Adding an origin is a security
change — ask first.
