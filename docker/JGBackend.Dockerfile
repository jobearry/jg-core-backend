# syntax=docker/dockerfile:1

# Defaults to linux/amd64 so plain `docker build` works without BuildKit;
# Buildx/Compose set BUILDPLATFORM automatically for cross-arch builds.
ARG BUILDPLATFORM=linux/amd64

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

COPY . /source

WORKDIR /source/src/JGBackend

# This is the architecture you're building for. Buildx passes TARGETARCH
# automatically; defaults to amd64 for plain `docker build`.
ARG TARGETARCH=amd64

# Build the application.
# Buildx passes TARGETARCH automatically (amd64|arm64|aarch64); map it to
# .NET's canonical RID names. Defaults to amd64 for plain `docker build`.
RUN arch="${TARGETARCH:-amd64}"; \
    case "$arch" in \
      amd64|x86_64) rid=x64 ;; \
      arm64|aarch64) rid=arm64 ;; \
      *) rid="$arch" ;; \
    esac; \
    dotnet publish -a "$rid" --use-current-runtime --self-contained false -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install ICU globalization libraries
RUN apk add --no-cache icu-libs

# Ensure .NET uses ICU instead of invariant mode
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app .

# Run as non-privileged user
USER $APP_UID

ENTRYPOINT ["dotnet", "JGBackend.dll"]

