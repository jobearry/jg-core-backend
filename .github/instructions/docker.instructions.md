---
applyTo: "docker/**,**/Dockerfile*"
---

# Docker guidance

Docker builds use the repository root as the build context; do not move or
rename `Generic/Generic.API` without updating `docker/Dockerfile`.

- Images: `mcr.microsoft.com/dotnet/sdk:10.0-alpine` (build) and
  `aspnet:10.0-alpine` (runtime). Keep them in step with the target framework.
- Plain `docker build` must work (no buildx/BuildKit required): `BUILDPLATFORM`
  and `TARGETARCH` have defaults; the shell case in the build stage maps
  `amd64`/`aarch64` to .NET RIDs. If you install buildx locally you get the
  automatic args either way.
- Compose (`docker/compose.yaml`) passes `Jwt__*` and `DOTNET_*` from `.env`;
  the SQL Server service was intentionally removed (no EF/DB in this repo).
- Do not add secrets into the image; runtime config via environment variables
  only. `.dockerignore` keeps `bin/`, `obj/`, `.git` out of the context.
