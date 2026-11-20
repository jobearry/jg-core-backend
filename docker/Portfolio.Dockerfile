# =============================================================================
# JDB Portfolio API - Docker image
# -----------------------------------------------------------------------------
# Multi-stage build for the .NET 10 ASP.NET Core Web API.
#
# Build context MUST be the repository root (parent of this docker/ folder), so
# the solution and all src projects can be copied:
#   docker build -f docker/Portfolio.Dockerfile -t jdb-portfolio-api .
# =============================================================================

# ---- Build / publish stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy the csproj files of the project to publish and its transitive project
# references first, so NuGet restore can be cached
COPY ["Directory.Build.props", "./"]
COPY ["src/Portfolio/Portfolio.Application/*.csproj", "src/Portfolio/Portfolio.Application/"]
COPY ["src/Portfolio/Portfolio.Domain/*.csproj", "src/Portfolio/Portfolio.Domain/"]
COPY ["src/Portfolio/Portfolio.ExternalService/*.csproj", "src/Portfolio/Portfolio.ExternalService/"]
COPY ["src/Portfolio/Portfolio.Persistence/*.csproj", "src/Portfolio/Portfolio.Persistence/"]
COPY ["src/Portfolio/Portfolio.API/*.csproj", "src/Portfolio/Portfolio.API/"]

# Restore the web project and its transitive project references.
# Do NOT restore the solution: it also lists the tests/ projects, whose .csproj
# files are not (and need not be) copied into the build context, and MSB3202
# would fail the build.
RUN dotnet restore "./src/Portfolio/Portfolio.API/Portfolio.API.csproj"

# Copy the remaining source and publish the main web project
COPY ["src/", "./src/"]

RUN dotnet publish "./src/Portfolio/Portfolio.API/Portfolio.API.csproj" \
        -c Release \
        --no-restore \
        -o /app/publish

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as a non-root user for security
USER $APP_UID

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Portfolio.API.dll"]