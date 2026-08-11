using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using L2.Studio.Context;
using Microsoft.EntityFrameworkCore.Migrations;

namespace L2.Studio.Migrations;

public sealed class GameContentDbContextFactory : IDesignTimeDbContextFactory<GameContentDbContext>
{
    public GameContentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql")
            ?? "Host=localhost;Port=5432;Database=l2-studio;Username=l2;Password=secret";
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                postgres.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.FullName);
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", GameContentDbContext.SchemaName);
            })
            .Options;
        return new GameContentDbContext(options);
    }
}
