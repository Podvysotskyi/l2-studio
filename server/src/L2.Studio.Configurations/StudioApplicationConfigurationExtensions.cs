using L2.Studio.Migrations;
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
        services.AddSingleton<IGameVersionRepository, GameVersionRepository>();
        services.AddSingleton<IAssetCatalogRepository, AssetCatalogRepository>();
        services.AddScoped<IAssetReleaseRepository, AssetReleaseRepository>();
        services.AddScoped<IAssetImportRepository, AssetImportRepository>();
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IAssetCatalogStore, AssetCatalogStore>();
        services.AddSingleton<GameVersionSeeder>();
        services.TryAddTimeProvider();
        services.AddHostedService<GameContentInitializer>();
        services.AddHostedService<ImportJobAbandonmentService>();
        return services;
    }

    public static IServiceCollection AddStudioWorkerApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStudioPersistence(configuration);
        services.AddAssetImportOptions(configuration);
        services.TryAddTimeProvider();
        services.AddScoped<IAssetCatalogStore, AssetCatalogStore>();
        services.AddScoped<IAssetReleaseRepository, AssetReleaseRepository>();
        services.AddScoped<IAssetImportWorkItemProcessor, AssetImportJobProcessor>();
        return services;
    }

    private static IServiceCollection AddAssetImportOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AssetImportOptions>()
            .Bind(configuration.GetSection(AssetImportOptions.SectionName))
            .Validate(options => Paths(options).All(path => !string.IsNullOrWhiteSpace(path)), "Asset import paths must not be empty.")
            .Validate(options => DistinctStorageRoots(options),
                "Published assets, generated work, and source snapshots must use distinct paths.")
            .Validate(options => IsAbsoluteHttpUrl(options.StudioBaseUrl), "StudioBaseUrl must be an absolute HTTP URL.")
            .Validate(options => IsAbsoluteHttpUrl(options.MapPreviewBrowserUrl), "MapPreviewBrowserUrl must be an absolute HTTP URL.")
            .Validate(options => IsAbsoluteHttpUrl(options.MapPreviewAssetBaseUrl), "MapPreviewAssetBaseUrl must be an absolute HTTP URL.")
            .Validate(options => options.AbandonedRunTimeout > TimeSpan.Zero, "AbandonedRunTimeout must be positive.")
            .ValidateOnStart();
        return services;
    }

    private static IEnumerable<string> Paths(AssetImportOptions options) =>
    [
        options.SourceRootPath, options.AssetRootPath, options.AssetWorkRootPath,
        options.SourceSnapshotRootPath
    ];

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    private static bool DistinctStorageRoots(AssetImportOptions options)
    {
        var roots = new[]
        {
            options.AssetRootPath, options.AssetWorkRootPath, options.SourceSnapshotRootPath
        }.Select(Path.GetFullPath).ToArray();
        return roots.Distinct(StringComparer.Ordinal).Count() == roots.Length &&
            !IsContained(roots[0], roots[2]) && !IsContained(roots[2], roots[0]);
    }

    private static bool IsContained(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        return relative == "." || (!Path.IsPathRooted(relative) &&
            !relative.StartsWith("..", StringComparison.Ordinal));
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}
