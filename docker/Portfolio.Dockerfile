# =============================================================================
# JDB Portfolio API - Docker image
# -----------------------------------------------------------------------------
# Multi-stage build for the .NET 10 ASP.NET Core Web API.
#
# Build context MUST be the repository root (parent of this docker/ folder), so
# the shared build props and the Portfolio project can be copied:
#   docker build -f docker/Portfolio.Dockerfile -t jdb-portfolio-api .
# =============================================================================

# ---- Build / publish stage ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy the build props and the single Portfolio project first, so NuGet restore
# can be cached.
COPY ["Directory.Build.props", "./"]
COPY ["src/Portfolio/Portfolio.csproj", "src/Portfolio/"]

# Restore the web project only. Do NOT restore the solution: it also lists the
# tests/ projects, whose .csproj files are not copied into the build context.
RUN dotnet restore "./src/Portfolio/Portfolio.csproj"

# Copy the remaining source and publish the main web project
COPY ["src/", "./src/"]

RUN dotnet publish "./src/Portfolio/Portfolio.csproj" \
        -c Release \
        --no-restore \
        -o /app/publish

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as a non-root user for security
USER $APP_UID

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Portfolio.dll"]