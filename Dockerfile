# ── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies (layer-cached separately)
COPY identity-service.csproj .
RUN dotnet restore identity-service.csproj

# Copy source and publish
COPY . .
RUN dotnet publish identity-service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render injects PORT env var; bind to it via ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "identity-service.dll"]