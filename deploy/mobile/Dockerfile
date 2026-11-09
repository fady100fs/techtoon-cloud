# ============================================================================
# Techtoon combined image: Nginx-served Flutter PWA + Techtoon.API
#
# The deployed PWA bundle is compiled Flutter web output. It is mirrored into
# Techtoon.Mobile/build/web by deploy/mobile/prepare-context.ps1 and is served
# by Nginx, while /api/* and /health are reverse-proxied to the .NET API on
# 127.0.0.1:8081. Both processes live in one container so the existing Fly app
# (techtoon-mobile) keeps its public URL and the PWA becomes same-origin with
# the API.
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Techtoon.Api/Techtoon.Api.csproj Techtoon.Api/
COPY Techtoon.Core/Techtoon.Core.csproj Techtoon.Core/
COPY Techtoon.Data/Techtoon.Data.csproj Techtoon.Data/
RUN dotnet restore Techtoon.Api/Techtoon.Api.csproj

COPY Techtoon.Api/ Techtoon.Api/
COPY Techtoon.Core/ Techtoon.Core/
COPY Techtoon.Data/ Techtoon.Data/

RUN dotnet publish Techtoon.Api/Techtoon.Api.csproj -c Release -o /app/published --no-restore

# ============================================================================
# Runtime: ASP.NET runtime + Nginx in the same image
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx curl ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && rm -f /etc/nginx/sites-enabled/default

WORKDIR /app
COPY --from=build /app/published ./

# Compiled PWA (mirrored from the live release or produced by `flutter build web`)
COPY Techtoon.Mobile/build/web/ /usr/share/nginx/html/

COPY deploy/mobile/nginx.conf /etc/nginx/conf.d/default.conf
COPY deploy/mobile/start.sh /app/start.sh
RUN chmod +x /app/start.sh

# Nginx owns the public port; the API is only reachable on loopback.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://127.0.0.1:8081 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["/app/start.sh"]
