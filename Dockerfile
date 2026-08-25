FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props DenariusAI.slnx global.json ./
COPY src/DenariusAI.Domain/DenariusAI.Domain.csproj src/DenariusAI.Domain/
COPY src/DenariusAI.Application/DenariusAI.Application.csproj src/DenariusAI.Application/
COPY src/DenariusAI.Infrastructure/DenariusAI.Infrastructure.csproj src/DenariusAI.Infrastructure/
COPY src/DenariusAI.Web/DenariusAI.Web.csproj src/DenariusAI.Web/
COPY src/DenariusAI.Mcp/DenariusAI.Mcp.csproj src/DenariusAI.Mcp/
COPY tests/DenariusAI.UnitTests/DenariusAI.UnitTests.csproj tests/DenariusAI.UnitTests/
COPY tests/DenariusAI.IntegrationTests/DenariusAI.IntegrationTests.csproj tests/DenariusAI.IntegrationTests/
COPY tests/DenariusAI.McpTests/DenariusAI.McpTests.csproj tests/DenariusAI.McpTests/
RUN dotnet restore DenariusAI.slnx

FROM restore AS build
COPY . .
RUN dotnet build DenariusAI.slnx --configuration Release --no-restore
RUN dotnet test DenariusAI.slnx --configuration Release --no-build
RUN dotnet publish src/DenariusAI.Web/DenariusAI.Web.csproj --configuration Release --no-build --output /app/publish
RUN dotnet publish src/DenariusAI.Mcp/DenariusAI.Mcp.csproj --configuration Release --no-build --output /app/mcp-publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS mcp-final
WORKDIR /app
COPY --from=build /app/mcp-publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "DenariusAI.Mcp.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/keys \
    && chown $APP_UID:$APP_UID /app/keys
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DenariusAI.Web.dll"]
