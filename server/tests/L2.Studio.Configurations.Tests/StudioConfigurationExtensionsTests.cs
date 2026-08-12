using L2.Studio.Configurations;
using L2.Studio.Migrations;
using L2.Studio.Repositories;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Services;
using L2.Studio.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace L2.Studio.Configurations.Tests;

public sealed class StudioConfigurationExtensionsTests
{
    [Fact]
    public async Task AddsStudioApiHostServicesAndIdentity()
    {
        var builder = CreateWebBuilder();

        var result = builder.AddStudioApi("l2-studio-api");
        await using var app = builder.Build();

        Assert.Same(builder, result);
        Assert.Equal(
            "l2-studio-api",
            app.Services.GetRequiredService<ServiceIdentity>().Name);
        Assert.NotNull(app.Services.GetRequiredService<IHttpClientFactory>());
        var registrations = app.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        Assert.Contains(registrations, item => item.Name == "postgresql");
    }

    [Fact]
    public async Task CanDisableThePostgreSqlHostHealthCheck()
    {
        var builder = CreateWebBuilder();

        builder.AddStudioApi(
            "l2-studio-api",
            new DependencyOptions { PostgreSqlRequired = false });
        await using var app = builder.Build();

        var registrations = app.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;
        Assert.DoesNotContain(registrations, item => item.Name == "postgresql");
    }

    [Fact]
    public async Task ConfiguresAllowedCorsOrigins()
    {
        var builder = CreateWebBuilder();
        builder.Configuration["Cors:AllowedOrigins:0"] = "https://studio.example.com";
        builder.Configuration["Cors:AllowedOrigins:1"] = "https://tools.example.com";

        builder.AddStudioApi("l2-studio-api");
        await using var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = Assert.IsType<CorsPolicy>(options.GetPolicy(options.DefaultPolicyName));
        Assert.Contains("https://studio.example.com", policy.Origins);
        Assert.Contains("https://tools.example.com", policy.Origins);
        Assert.True(policy.SupportsCredentials);
        Assert.Contains("*", policy.Headers);
        Assert.Contains("*", policy.Methods);
    }

    [Fact]
    public async Task MapsBothStudioHealthEndpoints()
    {
        var builder = CreateWebBuilder();
        builder.AddStudioApi(
            "l2-studio-api",
            new DependencyOptions { PostgreSqlRequired = false });
        await using var app = builder.Build();

        var result = app.MapStudioApi();

        Assert.Same(app, result);
        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        Assert.Contains("/health/live", routes);
        Assert.Contains("/health/ready", routes);
    }

    [Fact]
    public void AddsStudioWorkerIdentity()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Testing"
        });

        var result = builder.AddStudioWorker("l2-studio-worker");
        using var host = builder.Build();

        Assert.Same(builder, result);
        Assert.Equal(
            "l2-studio-worker",
            host.Services.GetRequiredService<ServiceIdentity>().Name);
    }

    [Fact]
    public void RegistersAssetStorageReconciliationPublisherOnlyForWorkerMessagingAndBuildsWorker()
    {
        var apiBuilder = CreateHostBuilder();
        apiBuilder.AddStudioApiMessaging();

        Assert.DoesNotContain(apiBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);

        var workerBuilder = CreateHostBuilder(Environments.Development);
        workerBuilder.AddStudioWorker("l2-studio-worker");
        workerBuilder.AddStudioWorkerMessaging();
        workerBuilder.Services.AddStudioWorkerApplication(workerBuilder.Configuration);

        Assert.Contains(workerBuilder.Services, HostedService<AssetStorageReconciliationPublisher>);
        using var host = workerBuilder.Build();
    }

    [Fact]
    public void RequiresAConnectionStringForStudioPersistence()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddStudioPersistence(configuration));

        Assert.Equal("ConnectionStrings:PostgreSql is required.", exception.Message);
    }

    [Fact]
    public void RegistersApiApplicationServicesAndPreservesACustomClock()
    {
        var services = new ServiceCollection();
        var clock = new FixedTimeProvider();
        services.AddSingleton<TimeProvider>(clock);

        var result = services.AddStudioApiApplication(Configuration());

        Assert.Same(services, result);
        Assert.Contains(services, Service<IContentDirectoryRepository, ContentDirectoryRepository>);
        Assert.Contains(services, Service<IAssetCatalogRepository, AssetCatalogRepository>);
        Assert.Contains(services, Service<IAssetImportRepository, AssetImportRepository>);
        Assert.Contains(services, Service<IAssetCatalogStore, AssetCatalogStore>);
        Assert.Contains(services, Service<GameVersionSeeder, GameVersionSeeder>);
        Assert.Contains(services, HostedService<GameContentInitializer>);
        using var provider = services.BuildServiceProvider();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void DoesNotRegisterGameContentInitializerForWorkerApplication()
    {
        var services = new ServiceCollection();

        services.AddStudioWorkerApplication(Configuration());

        Assert.DoesNotContain(services, HostedService<GameContentInitializer>);
    }

    [Fact]
    public void ValidatesAssetImportUrlsAndPaths()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = ConnectionString,
            ["AssetImport:SourceRootPath"] = "",
            ["AssetImport:StudioBaseUrl"] = "ftp://studio.example.com",
            ["AssetImport:MapPreviewBrowserUrl"] = "relative"
        };
        var services = new ServiceCollection();
        services.AddStudioWorkerApplication(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AssetImportOptions>>().Value);

        Assert.Contains("Asset import paths must not be empty.", exception.Failures);
        Assert.Contains("StudioBaseUrl must be an absolute HTTP URL.", exception.Failures);
        Assert.Contains("MapPreviewBrowserUrl must be an absolute HTTP URL.", exception.Failures);
    }

    private const string ConnectionString =
        "Host=localhost;Database=studio;Username=studio;Password=studio";

    private static WebApplicationBuilder CreateWebBuilder() =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(StudioConfigurationExtensionsTests).Assembly.FullName,
            EnvironmentName = "Testing"
        });

    private static HostApplicationBuilder CreateHostBuilder(string environmentName = "Testing")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        builder.Configuration["ConnectionStrings:PostgreSql"] = ConnectionString;
        return builder;
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = ConnectionString
            }).Build();

    private static bool Service<TService, TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(TService) &&
        descriptor.ImplementationType == typeof(TImplementation);

    private static bool HostedService<TImplementation>(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService) &&
        descriptor.ImplementationType == typeof(TImplementation);

    private sealed class FixedTimeProvider : TimeProvider;
}
