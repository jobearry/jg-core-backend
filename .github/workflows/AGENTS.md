# CI/CD guidance

Workflows live here only because GitHub Actions requires `.github/workflows/`.
The agent harness lives in `AGENTS.md` and `.opencode/`.

## Files

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yaml` | every push and PR | restore, build, and test `JGBackend.sln` |
| `deploy.yaml` | push to `master` under `src/**` or `tests/Portfolio/**` | check migrations, build, publish, apply EF migrations, deploy `jdbweb` |

One deployable remains. `deploy.yaml` owns the migration gate and apply step that
used to live in the retired `deploy-portfolio.yaml`.

## Rules

- CI must stay path-agnostic: it builds the whole solution on every change.
- The deploy workflow is path-filtered so it ships when host or module code changes.
- The migration gate fails when the model changed without a migration, and the
  apply step fails when `PRODUCTION_CONNECTION_STRING` is missing. Never remove
  either guard.
- Use the `master` branch. Do not deploy from pull requests.
- Authentication is OIDC via `azure/login`; no publish profiles or client
  secrets in the repo.
- Required repository secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
  `AZURE_SUBSCRIPTION_ID`, and `PRODUCTION_CONNECTION_STRING`.
- Keep `dotnet-version` aligned with `Directory.Build.props` and the projects
  (`net10.0`).
- Do not add `--no-build` to a publish step unless the exact same configuration
  was built earlier in the job.
