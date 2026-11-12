# Build context: ./backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layers cache well when only code (not csproj) changes.
COPY Prixo.slnx dotnet-tools.json ./
COPY src/Prixo.Domain/Prixo.Domain.csproj src/Prixo.Domain/
COPY src/Prixo.Application/Prixo.Application.csproj src/Prixo.Application/
COPY src/Prixo.Infrastructure/Prixo.Infrastructure.csproj src/Prixo.Infrastructure/
COPY src/Prixo.Api/Prixo.Api.csproj src/Prixo.Api/
RUN dotnet restore src/Prixo.Api/Prixo.Api.csproj

COPY src/ src/
RUN dotnet publish src/Prixo.Api/Prixo.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is not in the runtime image but the HEALTHCHECK below needs it to probe /health.
# Installed as root, before dropping to the non-root user.
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

# The runtime image already ships a non-root user `app` (uid/gid 1654, also exposed as
# $APP_UID) and carries no adduser/useradd, so creating one fails with exit 127. Own the
# published files at copy time instead — the app writes uploads under App_Data at runtime.
COPY --from=build --chown=app:app /app/publish .
USER app

EXPOSE 8080
# Probes the app's real liveness endpoint. `dotnet --version` was used here previously, but
# that needs the SDK (absent from the runtime image) and always exited 155 — the container
# could never report healthy, which would break any `depends_on: service_healthy` on it.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Prixo.Api.dll"]
