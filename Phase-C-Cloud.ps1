# ============================================================================
# Phase C: Cloud Deployment Files for Techtoon.Api
# Creates: Dockerfile, fly.toml, .dockerignore, render.yaml, appsettings.Production.json
# ============================================================================

 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $apiDir = Join-Path $root "Techtoon.Api"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== Phase C: Cloud Deployment Files ===" -ForegroundColor Cyan
Write-Host ""

# 1. Dockerfile (multi-stage build for small image)
Write-Host "[1/5] Creating Dockerfile..." -ForegroundColor Yellow
 $dockerfile = @'
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
'@
WF "$apiDir\Dockerfile" $dockerfile

# 2. .dockerignore (exclude unnecessary files)
Write-Host "[2/5] Creating .dockerignore..." -ForegroundColor Yellow
 $dockerignore = @'
bin/
obj/
*.user
*.suo
.vs/
.idea/
*.log
appsettings.Development.json
*.http
Properties/launchSettings.json
'@
WF "$apiDir\.dockerignore" $dockerignore

# 3. fly.toml (Fly.io configuration)
Write-Host "[3/5] Creating fly.toml..." -ForegroundColor Yellow
 $flyToml = @'
# ============================================================================
# Fly.io configuration for Techtoon.Api
# ============================================================================
app = "techtoon-api"
primary_region = "lhr"  # London (closest to Neon EU-West)

[build]
  dockerfile = "Techtoon.Api/Dockerfile"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = false
  auto_start_machines = true
  min_machines_running = 1  # Keep always running (no sleep)

  [http_service.concurrency]
    type = "requests"
    hard_limit = 250
    soft_limit = 200

[[http_service.checks]]
  interval = "30s"
  timeout = "10s"
  grace_period = "10s"
  method = "GET"
  path = "/health"

[vm]
  memory = "512mb"
  cpu_kind = "shared"
  cpus = 1
'@
WF "$root\fly.toml" $flyToml

# 4. render.yaml (Render.com alternative - backup option)
Write-Host "[4/5] Creating render.yaml..." -ForegroundColor Yellow
 $renderYaml = @'
# Render.com configuration (backup option if Fly.io doesn't work)
services:
  - type: web
    name: techtoon-api
    runtime: docker
    plan: free
    dockerfilePath: Techtoon.Api/Dockerfile
    healthCheckPath: /health
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://0.0.0.0:$PORT
      - key: ConnectionStrings__DefaultConnection
        sync: false
      - key: Jwt__Secret
        sync: false
      - key: Cors__AllowedOrigins__0
        sync: false
'@
WF "$root\render.yaml" $renderYaml

# 5. appsettings.Production.json (no secrets - env vars only)
Write-Host "[5/5] Creating appsettings.Production.json..." -ForegroundColor Yellow
 $appsettingsProd = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "Techtoon",
    "Audience": "TechtoonClients",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
'@
WF "$apiDir\appsettings.Production.json" $appsettingsProd

Write-Host ""
Write-Host "=== Phase C Files Created ===" -ForegroundColor Green
Write-Host ""
Write-Host "Files created:" -ForegroundColor Cyan
Write-Host "  - Techtoon.Api/Dockerfile"
Write-Host "  - Techtoon.Api/.dockerignore"
Write-Host "  - fly.toml (root)"
Write-Host "  - render.yaml (root)"
Write-Host "  - Techtoon.Api/appsettings.Production.json"
Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Yellow
Write-Host "1. Install flyctl:   irm https://fly.io/install.ps1 | iex"
Write-Host "2. Login:            flyctl auth login"
Write-Host "3. Create app:       flyctl launch --no-deploy"
Write-Host "4. Set secrets:      flyctl secrets set ConnectionStrings__DefaultConnection='...' Jwt__Secret='...'"
Write-Host "5. Deploy:           flyctl deploy"