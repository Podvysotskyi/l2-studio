using L2.Studio.Configurations;
using L2.Studio.Context;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace L2.Studio.Tests;

public sealed class PostgreSqlAssetImportTests
{
    [Fact]
    public async Task CreatesRunAndDurableOutboxThenWorkerDiscoversAndFinalizesIt()
    {
        var baseConnection = Environment.GetEnvironmentVariable("L2_STUDIO_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;

        var databaseName = $"l2_studio_test_{Guid.NewGuid():N}";
        var testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName }.ConnectionString;
        await CreateDatabaseAsync(baseConnection, databaseName);
        var sourceRoot = Path.Combine(Path.GetTempPath(), databaseName);
        Directory.CreateDirectory(sourceRoot);
        try
        {
            var settings = new HostApplicationBuilderSettings { EnvironmentName = "Testing" };
            var builder = Host.CreateApplicationBuilder(settings);
            builder.Configuration.AddInMemoryCollection(Configuration(testConnection, sourceRoot));
            builder.AddStudioApiMessaging();
            builder.Services.AddStudioApiApplication(builder.Configuration);
            using var host = builder.Build();
            await host.StartAsync();
            try
            {
                await using var scope = host.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAssetImportRepository>();
                var run = await repository.QueueFullScanAsync(AssetImportJobValues.Textures, CancellationToken.None);
                Assert.NotNull(run);
                Assert.Null(await repository.QueueFullScanAsync(
                    AssetImportJobValues.Textures, CancellationToken.None));

                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GameContentDbContext>>();
                await using var context = await factory.CreateDbContextAsync();
                Assert.Equal(1, await context.AssetImportRuns.CountAsync());
                await using var connection = new NpgsqlConnection(testConnection);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(
                    """
                    SELECT
                        (SELECT count(*) FROM l2_messaging_api.wolverine_outgoing_envelopes) +
                        (SELECT count(*) FROM l2_messaging.wolverine_queue_l2_asset_import_control)
                    """, connection);
                Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);

                var workerBuilder = Host.CreateApplicationBuilder(settings);
                workerBuilder.Configuration.AddInMemoryCollection(Configuration(testConnection, sourceRoot));
                workerBuilder.AddStudioWorkerMessaging();
                workerBuilder.Services.AddStudioWorkerApplication(workerBuilder.Configuration);
                using var worker = workerBuilder.Build();
                await worker.StartAsync();
                try
                {
                    var completed = await WaitForRunAsync(factory, run.Id);
                    Assert.Equal(AssetImportJobValues.Succeeded, completed.Status);
                    Assert.Equal(0, completed.DiscoveredFileCount);
                }
                finally
                {
                    await worker.StopAsync();
                }
            }
            finally
            {
                await host.StopAsync();
            }
        }
        finally
        {
            Directory.Delete(sourceRoot, recursive: true);
            NpgsqlConnection.ClearAllPools();
            await DropDatabaseAsync(baseConnection, databaseName);
        }
    }

    private static async Task<L2.Studio.Context.Entities.AssetImportRun> WaitForRunAsync(
        IDbContextFactory<GameContentDbContext> factory,
        Guid runId)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < timeout)
        {
            await using var context = await factory.CreateDbContextAsync();
            var run = await context.AssetImportRuns.AsNoTracking().SingleAsync(item => item.Id == runId);
            if (AssetImportJobValues.TerminalStatuses.Contains(run.Status)) return run;
            await Task.Delay(100);
        }
        throw new TimeoutException("The Worker did not finalize the import run.");
    }

    private static Dictionary<string, string?> Configuration(string connection, string sourceRoot) => new()
    {
        ["ConnectionStrings:PostgreSql"] = connection,
        ["GameContent:RunMigrations"] = "true",
        ["GameContent:SeedNpcLookups"] = "false",
        ["GameContent:SeedPlayerLookups"] = "false",
        ["GameContent:SeedPlayerClasses"] = "false",
        ["GameContent:SeedPlayerAppearances"] = "false",
        ["GameContent:SeedNpcs"] = "false",
        ["GameContent:SeedSkills"] = "false",
        ["AssetImport:SystemTexturesSourcePath"] = sourceRoot,
        ["AssetImport:TexturesSourcePath"] = sourceRoot,
        ["AssetImport:MusicSourcePath"] = sourceRoot,
        ["AssetImport:SoundsSourcePath"] = sourceRoot,
        ["AssetImport:StaticMeshesSourcePath"] = sourceRoot,
        ["AssetImport:LevelsSourcePath"] = sourceRoot,
        ["AssetImport:AssetRootPath"] = sourceRoot,
        ["AssetImport:StudioBaseUrl"] = "http://localhost:3001",
        ["AssetImport:LevelPreviewBrowserUrl"] = "http://localhost:9222"
    };

    private static async Task CreateDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
