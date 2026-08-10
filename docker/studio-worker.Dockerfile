FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS publish

WORKDIR /workspace
COPY . .
RUN dotnet publish server/L2.Studio.Worker/L2.Studio.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    --property:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet"]
