# JGBackend — Docker

Image for the `src/JGBackend` host.

### Building and running your application

From the repository root:

```sh
docker compose --project-directory . -f docker/compose.yaml up --build
```

Your application will be available at http://localhost:5031.

### Deploying your application to the cloud

From the repository root:

```sh
docker build -f docker/JGBackend.Dockerfile -t jg-backend .
```

If your cloud uses a different CPU architecture than your development machine
(e.g. you are on a Mac M1 and your cloud provider is amd64), build for that
platform:

```sh
docker build --platform=linux/amd64 -f docker/JGBackend.Dockerfile -t jg-backend .
```

Then push it to your registry, e.g. `docker push myregistry.com/jg-backend`.

Consult Docker's [getting started](https://docs.docker.com/go/get-started-sharing/)
docs for more detail on building and pushing.

### References
* [Docker's .NET guide](https://docs.docker.com/language/dotnet/)
* The [dotnet-docker](https://github.com/dotnet/dotnet-docker/tree/main/samples)
  repository has many relevant samples and docs.
