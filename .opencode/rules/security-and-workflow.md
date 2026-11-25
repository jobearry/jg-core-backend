# Security and workflow rules

## 1. Secrets

| Environment | Mechanism |
|---|---|
| Local | `dotnet user-secrets` on `src/JGBackend/JGBackend.csproj` (single host) |
| Docker | `.env` (git-ignored) → env vars in `docker/compose*.yaml` |
| Azure | App Service configuration / Key Vault |

Rules:

- Never commit a secret, token, connection string, or signing key — including in
  `appsettings*.json`, comments, tests, or `.http` files.
- `.env` is git-ignored at the repo root. `.env.example` contains
  placeholders only.
- Use `:` in .NET config keys, `__` in environment variables
  (`Jwt__Key`, `ConnectionStrings__JDBConnection`).
- Rotating `Jwt:Key` invalidates every issued token. Do it deliberately.
- If a secret is ever committed, rotate it — deleting the commit is not enough.

## 2. Authentication and authorization

Current design: a **single shared key** exchanged at `/api/v1/auth/login` for a
JWT carrying `sub=portfolio-api` and one `role=Admin` claim. There is no user store.

- Compare secrets with `CryptographicOperations.FixedTimeEquals` (already done).
- Reject a configured key shorter than 32 bytes (already done, returns 500 —
  correct, since it is a server misconfiguration).
- Portfolio, Notion, and current GitHub read endpoints are **anonymous by design**.
  GitHub endpoints return public repository data only, even when the server-side
  GitHub token can access private repositories. `/auth/me` and every
  `/api/v1/admin/...` write require the Admin JWT. Do not expose private GitHub
  metadata unless the user explicitly authorizes implementation of
  [`docs/plan.md`](../plan.md).
- The `Admin` policy is the fallback: a new endpoint is protected unless it opts
  out with `AllowAnonymous()`. Keep the public opt-outs explicit (KI-010).
- Do not put secrets in query strings — they end up in logs and proxies.
- When adding authorization, use policies (`RequireClaim`, `RequireRole`), not
  ad-hoc claim checks scattered across endpoints.

## 3. Transport and CORS

- `UseHttpsRedirection()` is enabled. In containers, TLS terminates at the edge;
  do not remove the redirect without replacing it.
- CORS is an explicit allow-list in configuration (`Cors:AllowedOrigins`, and
  `Cors:DevelopmentOrigins` for localhost, which is applied only in Development).
  Adding an origin is a security review item.
- `AllowCredentials()` is not enabled. Bearer tokens use the `Authorization` header, which
  CORS treats as an ordinary header; re-enabling it is only justified by a cookie-based
  flow, and that is a security review item.
- Never combine `AllowAnyOrigin()` with `AllowCredentials()`.

## 4. Error responses

- Do not leak exception messages or stack traces to clients. `ProblemDetails`
  carries a generic `detail`; the real exception goes to the log.
- `traceId` is the link between the client report and the server log — keep it.

## 5. Dependencies

- No new package without approval (AGENTS.md §10). Justify: what it replaces,
  maintenance status, transitive weight.
- Keep `PrivateAssets="all"` on design-time packages
  (`EntityFrameworkCore.Design`, `.Tools`).
- Enable Dependabot (or `dotnet list package --vulnerable` in CI) — not yet
  configured.

## 6. Git

Branch naming (matches history):

```
feature/<short-topic>      feature/recommendations
fix/<short-topic>          fix/compose-db-service
chore/<short-topic>        chore/agents-docs
docs/<short-topic>
task/<short-topic>         task/phase-3-incremental-tech-stack
```

**Branch strategy:**

- **One `feature/<topic>` branch per feature** — the integration branch that accumulates completed phases.
- **Each phase gets a `task/<phase-name>` branch** cut from the feature branch. Implement the phase on the task branch, verify, then open a PR **to the feature branch**. The feature branch receives these phase PRs.
- Do not chain task branches (task-2 cut from task-1); always cut from the current tip of the feature branch so phases integrate cleanly.
- Task branches are short-lived and deleted after their PR is merged to the feature branch.
- **Only the `feature/<topic>` branch is PR'd to `main`/`master`.** Task branches never target main directly.

Commit messages follow Conventional Commits:

```
feat(techstack): add PortfolioSkillService and skill endpoints
fix(mapping): stop dropping Period.FinishedAt
docs(agents): add architecture rules and docs index
refactor(domain): convert value objects to records
test(domain): cover Period invariants
```

Type quick-reference:

| Type | Purpose | Example from this repo |
|---|---|---|
| `feat` | New user-facing behaviour | `feat(project): create projects under an experience` |
| `fix` | Bug fix | `fix(admin): address review findings for write slices` |
| `docs` | Documentation only | `docs(plan): choose project-side experience route` |
| `refactor` | Code change, no behaviour change | `refactor(domain): convert value objects to records` |
| `test` | Adding/updating tests | `test(domain): cover Period invariants` |
| `chore` | Maintenance, tooling | `chore(ci): bump actions versions` |

- Subject: imperative mood, ≤ 72 chars, no trailing period.
- Body: explains **why**, not what (the diff shows what). Wrapped at ~72 chars.
- One logical change per commit. Do not mix formatting with behaviour.

## 7. Pull requests

- Target `main`; CI must be green.
- PR description: what changed, why, how it was verified, and any follow-up
  debt recorded in `docs/known-issues.md`.
- Include the verification command/output for anything non-obvious.
- Reviewers check, in order: dependency direction → domain invariants → tests →
  API contract → schema.

## 8. CI/CD

`.github/workflows/`:

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yaml` | every push and pull request | restore, build `Release`, `dotnet test` the whole solution |
| `deploy.yaml` | push to `master` under `src/**` or `tests/Portfolio/**` | migration gate, build, publish, `dotnet ef database update`, deploy `jdbweb` |

One deployable remains; `deploy.yaml` owns both the migration gate and the apply
step. Deploy runs log into Azure via OIDC and use `azure/webapps-deploy`. CI is
the only place the tests run, which is why every project must stay in
`JGBackend.sln`.

Never commit secrets to the workflow. Use `secrets.*` and OIDC federated
credentials (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`).

## 9. Never commit

`bin/`, `obj/`, `.vs/`, `.env`, `*.user`, `publish/`, `.DS_Store`.

Already tracked and worth a decision:

- `opencode.jsonc` is committed and holds only `$schema` and the `instructions`
  list. That is harness configuration, not machine-specific state — keep it that
  way and do not add local provider endpoints or model overrides to it.
