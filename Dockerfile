# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ECT.Graph.Api/ECT.Graph.Api.csproj ECT.Graph.Api/
RUN dotnet restore ECT.Graph.Api/ECT.Graph.Api.csproj

COPY src/ECT.Graph.Api/ ECT.Graph.Api/
RUN dotnet publish ECT.Graph.Api/ECT.Graph.Api.csproj -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ECT.Graph.Api.dll"]
