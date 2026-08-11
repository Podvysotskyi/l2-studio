using L2.Studio.Repositories;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Services;
using L2.Studio.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace L2.Studio.Configurations;

public static class StudioApplicationConfigurationExtensions
{
    public static IServiceCollection AddStudioApiApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStudioPersistence(configuration);
        services.AddAssetImportOptions(configuration);
        services.AddHealthChecks().AddGameContentMigrationHealthCheck();
        services.AddSingleton<IContentDirectoryRepository, ContentDirectoryRepository>();
        services.AddSingleton<IAssetCatalogRepository, AssetCatalogRepository>();
        services.AddSingleton<IAssetImportRepository, AssetImportRepository>();
        services.AddSingleton<IAssetCatalogStore, AssetCatalogStore>();
        services.TryAddTimeProvider();
        services.AddHostedService<GameContentInitializer>();
        return services;
    }

    public static IServiceCollection AddStudioWorkerApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStudioPersistence(configuration);
        services.AddAssetImportOptions(configuration);
        services.TryAddTimeProvider();
        services.AddSingleton<IAssetCatalogStore, AssetCatalogStore>();
        services.AddSingleton<IAssetImportJobProcessor, AssetImportJobProcessor>();
        return services;
    }

    private static IServiceCollection AddAssetImportOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AssetImportOptions>()
            .Bind(configuration.GetSection(AssetImportOptions.SectionName))
            .Validate(options => Paths(options).All(path => !string.IsNullOrWhiteSpace(path)), "Asset import paths must not be empty.")
            .Validate(options => IsAbsoluteHttpUrl(options.StudioBaseUrl), "StudioBaseUrl must be an absolute HTTP URL.")
            .Validate(options => IsAbsoluteHttpUrl(options.LevelPreviewBrowserUrl), "LevelPreviewBrowserUrl must be an absolute HTTP URL.")
            .ValidateOnStart();
        return services;
    }

    private static IEnumerable<string> Paths(AssetImportOptions options) =>
    [
        options.SystemTexturesSourcePath, options.TexturesSourcePath, options.MusicSourcePath,
        options.SoundsSourcePath, options.StaticMeshesSourcePath, options.LevelsSourcePath,
        options.AssetRootPath
    ];

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}
