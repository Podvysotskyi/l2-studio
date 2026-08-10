using System.Numerics;
using L2.Studio.Content;
using L2.Studio.Worker;
using L2.Tools.PackageReader;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Foundation.Tests;

[Collection(PostgreSqlIntegrationCollection.Name)]
public sealed class StaticMeshMaterialCatalogLoaderTests : IAsyncLifetime
{
    private readonly PostgreSqlIntegrationFixture postgres;
    private PostgreSqlDatabaseLease? database;

    public StaticMeshMaterialCatalogLoaderTests(PostgreSqlIntegrationFixture postgres) => this.postgres = postgres;

    public async Task InitializeAsync()
    {
        database = await postgres.CreateDatabaseAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (database is not null) await database.DisposeAsync();
    }

    [Fact]
    public async Task Loader_fetches_only_referenced_textures_without_tracking_catalog_entities()
    {
        await using var context = CreateContext();
        var baseTexture = new TextureMaterialReference("world", "base", "Texture");
        var opacityTexture = new TextureMaterialReference("world", "opacity", "Texture");
        var missingTexture = new TextureMaterialReference("world", "missing", "Texture");
        var rootMaterial = new TextureMaterialManifestEntry(
            "world", "root", "Shader", null, baseTexture, opacityTexture, null,
            0, 0, false, true, 128, true, true);
        var missingMaterial = new TextureMaterialManifestEntry(
            "world", "missing-root", "Shader", null, missingTexture, null, null,
            0, 0, false, false, 128, true, true);
        await PublishTexturesAsync(
            context,
            AssetImportJobValues.SystemTextures,
            [Texture("world", "BaSe", "/systextures/world/base.webp")],
            []);
        await PublishTexturesAsync(
            context,
            AssetImportJobValues.Textures,
            [
                Texture("world", "base", "/textures/world/base.webp"),
                Texture("world", "opacity", "/textures/world/opacity.webp"),
                Texture("world", "unused", "/textures/world/unused.webp")
            ],
            [rootMaterial, missingMaterial]);
        context.ChangeTracker.Clear();

        var catalog = await StaticMeshMaterialCatalogLoader.LoadAsync(
            context,
            [
                new TextureMaterialReference("WORLD", "ROOT", "Shader"),
                new TextureMaterialReference("World", "Root", "Shader"),
                new TextureMaterialReference("WORLD", "BASE", "Texture"),
                new TextureMaterialReference("WORLD", "MISSING-ROOT", "Shader")
            ],
            CancellationToken.None);
        var mesh = new UnrealStaticMesh(
            "mesh",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(
                0,
                3,
                new UnrealObjectReference("WORLD", "ROOT", "Shader"))]);
        var resolution = catalog.Resolver.Resolve(mesh, "mesh-package");
        var binding = Assert.Single(resolution.SectionMaterials);
        var missingMesh = mesh with
        {
            Sections =
            [
                new UnrealStaticMeshSection(
                    0,
                    3,
                    new UnrealObjectReference("WORLD", "MISSING-ROOT", "Shader"))
            ]
        };
        var missingResolution = catalog.Resolver.Resolve(missingMesh, "mesh-package");

        Assert.Equal(2, catalog.LoadedTextureCount);
        Assert.Equal(["-dxt.ktx"], catalog.GpuTextureFormats);
        Assert.NotNull(binding);
        Assert.Equal("/systextures/world/base.webp", binding.DiffuseUrl);
        Assert.Equal("/textures/world/opacity.webp", binding.OpacityUrl);
        Assert.Equal("unresolved", missingResolution.Status);
        Assert.Contains("world.missing", missingResolution.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static async Task PublishTexturesAsync(
        GameContentDbContext context,
        string kind,
        IReadOnlyList<TextureManifestEntry> textures,
        IReadOnlyList<TextureMaterialManifestEntry> materials) =>
        await AssetCatalogPublisher.PublishAsync(
            context,
            Guid.NewGuid(),
            kind,
            kind,
            new string(kind == AssetImportJobValues.SystemTextures ? 'a' : 'b', 64),
            4,
            121,
            textures.Select(texture => texture.PackageName).Distinct().ToArray(),
            group => group,
            textures,
            texture => texture.ObjectName,
            texture => texture.PackageName,
            texture => texture.Status,
            new TextureCatalogMetadata(materials),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

    private static TextureManifestEntry Texture(string packageName, string objectName, string url) =>
        new(packageName, objectName, url, 4, 4, "DXT1", new string('c', 64), "resolved", null);

    private GameContentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql(database!.ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GameContentDbContext.SchemaName))
            .Options;
        return new GameContentDbContext(options);
    }
}
