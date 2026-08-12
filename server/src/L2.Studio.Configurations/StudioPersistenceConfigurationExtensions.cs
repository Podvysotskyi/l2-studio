using L2.Studio.Context;
using L2.Studio.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace L2.Studio.Configurations;

public static class StudioPersistenceConfigurationExtensions
{
    public static IServiceCollection AddStudioPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
        services.AddPooledDbContextFactory<GameContentDbContext>(options => options.UseNpgsql(
            connectionString,
            postgres =>
            {
                postgres.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.FullName);
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", GameContentDbContext.SchemaName);
            }));
        return services;
    }

    public static IHealthChecksBuilder AddGameContentMigrationHealthCheck(this IHealthChecksBuilder checks) =>
        checks.AddCheck<GameContentMigrationHealthCheck>("game-content-migrations", tags: ["ready"]);
}

public sealed class GameContentMigrationHealthCheck(
    IDbContextFactory<GameContentDbContext> contextFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
            var pending = await database.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pending.ToArray();
            return migrations.Length == 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Pending game content migrations: {string.Join(", ", migrations)}");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Game content migration state could not be checked.", exception);
        }
    }
}
