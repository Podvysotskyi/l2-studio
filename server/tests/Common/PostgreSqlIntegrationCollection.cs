using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace L2.Studio.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollection : ICollectionFixture<PostgreSqlIntegrationFixture>
{
    public const string Name = "PostgreSQL integration";
}

public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? postgres;
    private readonly string hostConnectionString;

    public PostgreSqlIntegrationFixture()
    {
        hostConnectionString = Environment.GetEnvironmentVariable("L2_STUDIO_TEST_POSTGRES") ?? string.Empty;
        if (hostConnectionString.Length > 0) return;

        postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("l2_test_host")
            .WithUsername("l2_tests")
            .WithPassword("l2_tests_password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                "pg_isready", "-U", "l2_tests", "-d", "l2_test_host"))
            .Build();
    }

    public Task InitializeAsync() => postgres?.StartAsync() ?? Task.CompletedTask;

    public Task DisposeAsync() => postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;

    public async Task<PostgreSqlDatabaseLease> CreateDatabaseAsync()
    {
        var databaseName = $"l2_test_{Guid.NewGuid():N}";
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(GetHostConnectionString())
        {
            Database = databaseName
        }.ConnectionString;
        return new PostgreSqlDatabaseLease(this, databaseName, connectionString);
    }

    internal async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)} WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        for (var attempt = 1; ; attempt++)
        {
            var connection = new NpgsqlConnection(GetHostConnectionString());
            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception exception) when (attempt < 5 && exception is NpgsqlException or IOException)
            {
                await connection.DisposeAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
        }
    }

    private string GetHostConnectionString() =>
        postgres?.GetConnectionString() ?? hostConnectionString;
}

public sealed class PostgreSqlDatabaseLease(
    PostgreSqlIntegrationFixture fixture,
    string databaseName,
    string connectionString) : IAsyncDisposable
{
    private PostgreSqlIntegrationFixture? fixture = fixture;

    public string ConnectionString { get; } = connectionString;

    public async ValueTask DisposeAsync()
    {
        var owner = Interlocked.Exchange(ref fixture, null);
        if (owner is not null)
        {
            await owner.DropDatabaseAsync(databaseName);
        }
    }
}
