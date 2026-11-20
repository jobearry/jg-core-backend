# CI/CD guidance

Workflows live here only because GitHub Actions requires `.github/workflows/`.
The agent harness lives in `AGENTS.md` and `.opencode/`.

## Files

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yaml` | every push and PR | restore, build, and test `JGBackend.sln` |
| `deploy.yaml` | push to `master` under `src/JGBackend/**` or `src/Core/**` | deploy JGBackend to `jdbweb` |
| `deploy-portfolio.yaml` | push to `master` under `src/Portfolio/**` | check migrations, build, apply migrations, deploy to `jdb-portfolio-api` |

Phase 4 collapses the two deploy workflows into one target.

## Rules

- CI must stay path-agnostic: it builds the whole solution on every change.
- Deploy workflows must stay path-filtered so an app only ships when its own code
  changes.
- Portfolio deploys apply EF migrations before swapping the app. Never remove the
  guard that fails when `PRODUCTION_CONNECTION_STRING` is missing.
- Use the `master` branch. Do not deploy from pull requests.
- Authentication is OIDC via `azure/login`; no publish profiles or client
  secrets in the repo.
- Required repository secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
  `AZURE_SUBSCRIPTION_ID`, and `PRODUCTION_CONNECTION_STRING` (Portfolio only).
- Keep `dotnet-version` aligned with `Directory.Build.props` and the projects
  (`net10.0`).
- Do not add `--no-build` to a publish step unless the exact same configuration
  was built earlier in the job.
