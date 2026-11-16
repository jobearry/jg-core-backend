---
applyTo: ".github/workflows/**"
---

# CI/CD workflow guidance

- Workflows build the solution at `Generic/Generic.sln` with `dotnet-version: 10.0.x`;
  keep that path and SDK version in sync with the repo (a test project does
  not exist yet — CI restores and builds only).
- `ci.yaml` — on push/PR: restore, Release build. Keep it minimal; do not add
  packaging/deploy steps here.
- `deploy.yaml` — on push to `master`: OIDC login (azure/login) → Release
  build of `Generic/Generic.sln` → `dotnet publish Generic/Generic.API/Generic.API.csproj`
  → deploy to Azure App Service `jdbweb` (azure/webapps-deploy). The publish
  step must stay explicit (never `dotnet publish` from repo root).
- When the solution layout or projects change, update both workflows in the
  same change; `docker/` is independent of CI (see docker/Dockerfile).
- Secrets come from GitHub Actions secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
  `AZURE_SUBSCRIPTION_ID`); never inline values.
