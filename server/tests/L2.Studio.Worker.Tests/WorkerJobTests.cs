using L2.Studio.Configurations;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Attributes;
using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class WorkerJobTests
{
    [Fact]
    public void DefinesVersionSpecificNpcLookupCatalogs()
    {
        NpcLookupCatalog c1 = new C1NpcLookupCatalog();
        NpcLookupCatalog c4 = new C4NpcLookupCatalog();
        NpcLookupCatalog interlude = new InterludeNpcLookupCatalog();

        Assert.Equal(28, c1.Types.Count);
        Assert.Equal(46, c4.Types.Count);
        Assert.Equal(48, interlude.Types.Count);
        Assert.Equal(21, c1.Races.Count);
        Assert.Equal(22, c4.Races.Count);
        Assert.Equal(22, interlude.Races.Count);
        Assert.Equal(["MALE", "FEMALE", "ETC"], c1.Sexes.Select(item => item.Name));
        Assert.Equal(c1.Sexes, c4.Sexes);
        Assert.Equal(c1.Sexes, interlude.Sexes);
        Assert.DoesNotContain(c1.Races, item => item.Name == "DIVINE");
        Assert.Contains(c4.Races, item => item.Name == "DIVINE");
        Assert.DoesNotContain(interlude.Races, item => item.Name == "NONE");
    }

    [Theory]
    [InlineData("SIEGE_WEAPON", "Siege Weapon")]
    [InlineData("HUMAN", "Human")]
    [InlineData("VillageMasterFighter", "Village Master Fighter")]
    [InlineData("VillageMasterDElf", "Village Master Dark Elf")]
    [InlineData("mixed_case", "Mixed Case")]
    public void GeneratesFriendlyNpcLookupNames(string source, string expected) =>
        Assert.Equal(expected, NpcLookupCatalog.FriendlyName(source));

    [Fact]
    public void KeepsEveryWolverineHandlerInWorker()
    {
        var workerAssembly = typeof(NpcLookupImportHandlers).Assembly;
        var handlerTypes = workerAssembly.GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(WolverineHandlerAttribute), inherit: false).Length > 0)
            .ToArray();

        Assert.Equal(6, handlerTypes.Length);
        Assert.All(handlerTypes, type => Assert.Equal("L2.Studio.Worker", type.Namespace));
        Assert.DoesNotContain(typeof(AssetImportJobProcessor).Assembly.GetTypes(), type =>
            type.GetCustomAttributes(typeof(WolverineHandlerAttribute), inherit: false).Length > 0);
    }

    [Fact]
    public void AggregatesRunCountsAndWarningsByTerminalFile()
    {
        var run = Run(
            Item(AssetImportJobValues.Succeeded),
            Item(AssetImportJobValues.SucceededWithWarnings, warnings: 2),
            Item(AssetImportJobValues.Failed),
            Item(AssetImportJobValues.Running));

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(3, run.CompletedFileCount);
        Assert.Equal(2, run.SucceededFileCount);
        Assert.Equal(1, run.WarningFileCount);
        Assert.Equal(1, run.FailedFileCount);
    }

    [Fact]
    public void ResetsRunCountsWhenNoWorkItemsExist()
    {
        var run = Run();
        run.CompletedFileCount = 10;
        run.SucceededFileCount = 9;
        run.WarningFileCount = 8;
        run.FailedFileCount = 7;

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(0, run.CompletedFileCount);
        Assert.Equal(0, run.SucceededFileCount);
        Assert.Equal(0, run.WarningFileCount);
        Assert.Equal(0, run.FailedFileCount);
    }

    [Fact]
    public void FinalizesOnlyActiveRunsAfterDiscoveryAndAllWorkCompletes()
    {
        var run = Run(Item(AssetImportJobValues.Succeeded), Item(AssetImportJobValues.Reused));
        run.DiscoveredFileCount = 2;

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.False(AssetImportRunHandlers.IsReadyToFinalize(run));

        run.DiscoveryFinishedAt = DateTimeOffset.UtcNow;

        Assert.True(AssetImportRunHandlers.IsReadyToFinalize(run));

        run.Status = AssetImportJobValues.Succeeded;

        Assert.False(AssetImportRunHandlers.IsReadyToFinalize(run));
    }

    [Fact]
    public void RegistersWorkerJobsAndReconciliationPublisher()
    {
        var apiBuilder = CreateHostBuilder();
        apiBuilder.AddStudioApiMessaging();
        Assert.DoesNotContain(apiBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);

        var workerBuilder = CreateHostBuilder(Environments.Development);
        workerBuilder.AddStudioWorker("l2-studio-worker");
        workerBuilder.AddStudioWorkerJobs();
        workerBuilder.Services.AddStudioWorkerApplication(workerBuilder.Configuration);

        Assert.Contains(workerBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);
        using var host = workerBuilder.Build();
    }

    private static AssetImportRun Run(params AssetImportWorkItem[] workItems) => new()
    {
        Id = Guid.NewGuid(),
        Kind = AssetImportJobValues.Textures,
        TriggerType = AssetImportJobValues.FullScan,
        Status = AssetImportJobValues.Running,
        RequestedAt = DateTimeOffset.UtcNow,
        WorkItems = workItems
    };

    private static AssetImportWorkItem Item(string status, int warnings = 0) => new()
    {
        Id = Guid.NewGuid(),
        ImportKind = AssetImportJobValues.Textures,
        SourceKey = $"{Guid.NewGuid():N}.utx",
        NormalizedSourceKey = Guid.NewGuid().ToString("N"),
        SourcePath = "/tmp/source.utx",
        Status = status,
        WarningCount = warnings,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static HostApplicationBuilder CreateHostBuilder(string environmentName = "Testing")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        builder.Configuration["ConnectionStrings:PostgreSql"] =
            "Host=localhost;Database=studio;Username=studio;Password=studio";
        return builder;
    }

    private static bool HostedService<TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService) &&
        descriptor.ImplementationType == typeof(TImplementation);
}
