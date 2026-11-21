# Docker guidance

One image, built from the repository root. Never set the build context to
`docker/` or to a project directory.

```sh
docker build -f docker/JGBackend.Dockerfile -t jg-backend .
docker compose -f docker/compose.yaml up --build
```

The `JGBackend` image serves the Core and Portfolio features from the single
host. The root `.dockerignore` is the one in effect.

## Rules

- Multi-stage builds only; the runtime image uses the ASP.NET base image and runs
  as the non-root `$APP_UID` user.
- Never bake secrets, `.env`, or connection strings into an image. Pass them at
  runtime.
- Keep the root `.dockerignore` aligned with the build context; it excludes
  `.git`, `bin`, `obj`, `.env*` (except `.env.example`), and the agent harness.
- Alpine is used for the image; keep `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`
  paired with `icu-libs` if globalization is needed.
