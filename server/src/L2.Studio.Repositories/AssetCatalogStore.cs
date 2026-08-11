using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed class AssetCatalogStore(IDbContextFactory<GameContentDbContext> contextFactory) : IAssetCatalogStore
{
    public async Task PublishAsync(AssetCatalogPublication publication, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.AssetCatalogs.Where(catalog => catalog.Kind == publication.Kind && catalog.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(catalog => catalog.IsActive, false), cancellationToken);
        context.AssetCatalogs.Add(new AssetCatalog
        {
            Id = publication.Id,
            Kind = publication.Kind,
            SourceFolder = publication.SourceFolder,
            SourceHash = publication.SourceHash,
            SchemaVersion = publication.SchemaVersion,
            Protocol = publication.Protocol,
            MetadataJson = publication.MetadataJson,
            IsActive = true,
            PublishedAt = publication.PublishedAt,
            Groups = publication.Groups.Select(group => new AssetCatalogGroup
            {
                Name = group.Name,
                MetadataJson = group.MetadataJson
            }).ToList(),
            Items = publication.Items.Select(item => new AssetCatalogItem
            {
                Name = item.Name,
                GroupName = item.GroupName,
                Status = item.Status!,
                MetadataJson = item.MetadataJson
            }).ToList()
        });
        await context.SaveChangesAsync(cancellationToken);
        await context.AssetCatalogs
            .Where(catalog => catalog.Kind == publication.Kind && !catalog.IsActive && catalog.Id != publication.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
