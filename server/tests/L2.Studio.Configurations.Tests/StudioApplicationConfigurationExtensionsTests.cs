using L2.Studio.Configurations;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace L2.Studio.Configurations.Tests;

public sealed class StudioApplicationConfigurationExtensionsTests
{
    [Fact]
    public void Worker_application_registers_its_service_boundaries()
    {
        using var provider = CreateServices().BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAssetCatalogStore>());
        Assert.NotNull(provider.GetRequiredService<IAssetImportJobProcessor>());
    }

    [Fact]
    public void Asset_import_urls_are_validated()
    {
        using var provider = CreateServices(new Dictionary<string, string?>
        {
            ["AssetImport:StudioBaseUrl"] = "not-a-url"
        }).BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AssetImportOptions>>().Value);
    }

    private static ServiceCollection CreateServices(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = "Host=localhost;Database=l2studio;Username=l2studio;Password=test"
        };
        if (overrides is not null)
            foreach (var (key, value) in overrides) values[key] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudioWorkerApplication(configuration);
        return services;
    }
}
