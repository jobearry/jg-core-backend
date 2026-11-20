# =============================================================================
# JDB Portfolio API - Docker image
# -----------------------------------------------------------------------------
# Multi-stage build for the .NET 10 ASP.NET Core Web API.
#
# Build context MUST be the repository root (parent of this docker/ folder), so
# the solution and all src projects can be copied:
#   docker build -f docker/Dockerfile -t jdb-portfolio-api .
# =============================================================================

# ---- Build / publish stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy the csproj files of the project to publish and its transitive project
# references first, so NuGet restore can be cached
COPY ["src/JDB_Portfolio_API.Application/*.csproj", "src/JDB_Portfolio_API.Application/"]
COPY ["src/JDB_Portfolio_API.Domain/*.csproj", "src/JDB_Portfolio_API.Domain/"]
COPY ["src/JDB_Portfolio_API.ExternalService/*.csproj", "src/JDB_Portfolio_API.ExternalService/"]
COPY ["src/JDB_Portfolio_API.Persistence/*.csproj", "src/JDB_Portfolio_API.Persistence/"]
COPY ["src/JDB_Portfolio_API/*.csproj", "src/JDB_Portfolio_API/"]

# Restore the web project and its transitive project references.
# Do NOT restore the solution: it also lists the tests/ projects, whose .csproj
# files are not (and need not be) copied into the build context, and MSB3202
# would fail the build.
RUN dotnet restore "./src/JDB_Portfolio_API/JDB_Portfolio_API.csproj"

# Copy the remaining source and publish the main web project
COPY ["src/", "./src/"]

RUN dotnet publish "./src/JDB_Portfolio_API/JDB_Portfolio_API.csproj" \
        -c Release \
        --no-restore \
        -o /app/publish

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as a non-root user for security
USER $APP_UID

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "JDB_Portfolio_API.dll"]