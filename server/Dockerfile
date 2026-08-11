# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS build

WORKDIR /workspace
COPY global.json ./
COPY server/ server/
RUN dotnet restore server/L2.Studio.slnx
RUN dotnet build server/L2.Studio.slnx --configuration Release --no-restore -m:1

FROM build AS publish-api
RUN dotnet publish server/src/L2.Studio.Api/L2.Studio.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false

FROM build AS publish-worker
RUN dotnet publish server/src/L2.Studio.Worker/L2.Studio.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false

FROM build AS server-tests
ENV L2_STUDIO_TEST_POSTGRES="Host=postgres-test;Port=5432;Database=l2-studio-test;Username=l2_test;Password=test-secret"
CMD ["dotnet", "test", "server/L2.Studio.slnx", "--configuration", "Release", "--no-build", "--no-restore"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.2 AS api-production
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish-api --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "L2.Studio.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.2 AS worker-production
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production
COPY --from=publish-worker --chown=$APP_UID:$APP_UID /app/publish .
COPY --chmod=755 server/docker/worker-entrypoint.sh /usr/local/bin/l2-studio-worker
RUN mkdir -p /workspace/assets && chown $APP_UID:$APP_UID /workspace/assets
ENTRYPOINT ["l2-studio-worker"]
