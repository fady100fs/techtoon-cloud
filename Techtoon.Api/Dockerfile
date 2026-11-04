# ============================================================================
# Techtoon.Api - Dockerfile for Cloud Deployment
# Multi-stage build: build SDK image -> runtime image (small)
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore (cached layer)
COPY Techtoon.Api/Techtoon.Api.csproj Techtoon.Api/
COPY Techtoon.Core/Techtoon.Core.csproj Techtoon.Core/
COPY Techtoon.Data/Techtoon.Data.csproj Techtoon.Data/
RUN dotnet restore Techtoon.Api/Techtoon.Api.csproj

# Copy all source code
COPY Techtoon.Api/ Techtoon.Api/
COPY Techtoon.Core/ Techtoon.Core/
COPY Techtoon.Data/ Techtoon.Data/

# Build and publish
RUN dotnet publish Techtoon.Api/Techtoon.Api.csproj -c Release -o /app/published --no-restore

# ============================================================================
# Runtime stage (small image, no SDK)
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/published .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Techtoon.Api.dll"]