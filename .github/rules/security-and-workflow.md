# Security and workflow rules

## 1. Secrets

| Environment | Mechanism |
|---|---|
| Local | `dotnet user-secrets` on `src/JDB_Portfolio_API` |
| Docker | `docker/.env` (git-ignored) → env vars in `docker-compose.yml` |
| Azure | App Service configuration / Key Vault |

Rules:

- Never commit a secret, token, connection string, or signing key — including in
  `appsettings*.json`, comments, tests, or `.http` files.
- `.env` is git-ignored at the repo root. `docker/.env.example` contains
  placeholders only.
- Use `:` in .NET config keys, `__` in environment variables
  (`Jwt__Key`, `ConnectionStrings__JDBConnection`).
- Rotating `Jwt:Key` invalidates every issued token. Do it deliberately.
- If a secret is ever committed, rotate it — deleting the commit is not enough.

## 2. Authentication and authorization

Current design: a **single shared key** exchanged at `/api/v1/auth/login` for a
JWT containing one `role=User` claim. There is no user store and no `sub` claim.

- Compare secrets with `CryptographicOperations.FixedTimeEquals` (already done).
- Reject a configured key shorter than 32 bytes (already done, returns 500 —
  correct, since it is a server misconfiguration).
- `RequireAuthorization()` is currently commented out on every portfolio
  endpoint. This must be resolved one way or the other (plan 2.5): either
  enforce it, or document the API as public and stop pretending.
- Do not put secrets in query strings — they end up in logs and proxies.
- When adding authorization, use policies (`RequireClaim`, `RequireRole`), not
  ad-hoc claim checks scattered across endpoints.

## 3. Transport and CORS

- `UseHttpsRedirection()` is enabled. In containers, TLS terminates at the edge;
  do not remove the redirect without replacing it.
- CORS is an explicit allow-list in `ConfigurationSettings.AddCorsOrigins`.
  Adding an origin is a security review item.
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
```

Commit messages follow Conventional Commits:

```
feat(techstack): add PortfolioSkillService and skill endpoints
fix(mapping): stop dropping Period.FinishedAt
docs(agents): add architecture rules and docs index
refactor(domain): convert value objects to records
test(domain): cover Period invariants
```

- Subject in imperative mood, ≤ 72 chars, no trailing period.
- Body explains **why**, not what (the diff shows what).
- One logical change per commit. Do not mix formatting with behaviour.

## 7. Pull requests

- Target `main`; CI must be green.
- PR description: what changed, why, how it was verified, and any follow-up
  debt recorded in `docs/known-issues.md`.
- Include the verification command/output for anything non-obvious.
- Reviewers check, in order: dependency direction → domain invariants → tests →
  API contract → schema.

## 8. CI/CD

`.github/workflows/main_jdb-portfolio-api.yml`:

- Triggers on push to `main` and manual dispatch.
- Restores, builds `Release`, publishes, logs into Azure via OIDC, deploys.
- **Does not run tests** — because there are none. Add a `dotnet test` step in
  the same PR that adds the first test project.

Never commit secrets to the workflow. Use `secrets.*` and OIDC federated
credentials (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`).

## 9. Never commit

`bin/`, `obj/`, `.vs/`, `docker/.env`, `*.user`, `publish/`, `.DS_Store`.

Already tracked and worth a decision:

- `.vscode/settings.json` is committed and only pins the default solution
  (`dotnet.defaultSolution`). That is harmless and useful — keep it, but do not
  add machine-specific paths or extensions to it.
- `opencode.json` is committed and points at `http://127.0.0.1:8081/v1`. Local
  endpoints are not secrets, but they are machine-specific; either keep it as a
  documented local default or git-ignore it.
