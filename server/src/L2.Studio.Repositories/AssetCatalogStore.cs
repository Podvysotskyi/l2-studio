using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Repositories;

public sealed partial class AssetCatalogStore(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : IAssetCatalogStore
{
    [GeneratedRegex("/(?:textures/(?:systextures|textures)|music|sounds|staticmeshes|maps|mappreviews|scenes)/[^/]+/[0-9a-f]{64}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VersionRootPattern();

    public async Task PublishAsync(AssetCatalogPublication publication, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var workItem = await context.AssetImportWorkItems.Include(item => item.Run)
            .SingleAsync(item => item.Id == publication.WorkItemId, cancellationToken);
        if (AssetImportJobValues.TerminalStatuses.Contains(workItem.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var catalog = await context.AssetCatalogs.Include(item => item.Sources)
            .SingleOrDefaultAsync(item => item.GameVersion == publication.GameVersion &&
                item.Kind == publication.Kind && item.IsActive, cancellationToken);
        if (catalog is null)
        {
            catalog = new AssetCatalog
            {
                Id = Guid.NewGuid(),
                GameVersion = publication.GameVersion,
                Kind = publication.Kind,
                SourceFolder = publication.SourceFolder,
                SourceHash = publication.SourceHash,
                SchemaVersion = publication.SchemaVersion,
                Protocol = publication.Protocol,
                MetadataJson = publication.MetadataJson,
                IsActive = true,
                PublishedAt = publication.PublishedAt
            };
            context.AssetCatalogs.Add(catalog);
        }

        var previous = catalog.Sources.SingleOrDefault(source =>
            source.NormalizedSourceKey == publication.NormalizedSourceKey);
        var previousOutputRoot = previous?.OutputRoot;
        if (previous is not null) context.AssetCatalogSources.Remove(previous);

        var references = ExtractVersionRoots(publication.Groups, publication.Items, publication.MetadataJson);
        var source = new AssetCatalogSource
        {
            Id = Guid.NewGuid(),
            Catalog = catalog,
            PublishingWorkItemId = publication.WorkItemId,
            SourceKey = publication.SourceKey,
            NormalizedSourceKey = publication.NormalizedSourceKey,
            SourceHash = publication.SourceHash,
            OutputRoot = publication.OutputRoot,
            MetadataJson = publication.MetadataJson,
            ReferencedOutputRootsJson = JsonSerializer.Serialize(references),
            PublishedAt = publication.PublishedAt
        };
        source.Groups = publication.Groups.Select(group => new AssetCatalogGroup
        {
            Catalog = catalog,
            Source = source,
            Name = group.Name,
            MetadataJson = group.MetadataJson
        }).ToList();
        source.Items = publication.Items.Select(item => new AssetCatalogItem
        {
            Catalog = catalog,
            Source = source,
            Name = item.Name,
            GroupName = item.GroupName,
            Status = item.Status!,
            MetadataJson = item.MetadataJson
        }).ToList();
        context.AssetCatalogSources.Add(source);
        await context.SaveChangesAsync(cancellationToken);

        catalog.SourceHash = AggregateHash(catalog.Sources
            .Where(item => item.Id != previous?.Id && item.Id != source.Id)
            .Append(source)
            .Select(item => (item.NormalizedSourceKey, item.SourceHash)));
        catalog.SourceFolder = publication.SourceFolder;
        catalog.SchemaVersion = publication.SchemaVersion;
        catalog.Protocol = publication.Protocol;
        var activeSources = catalog.Sources
            .Where(item => item.Id != previous?.Id && item.Id != source.Id)
            .Append(source)
            .ToArray();
        catalog.MetadataJson = AssetCatalogMetadataAggregator.Aggregate(
            publication.Kind, activeSources.Select(item => item.MetadataJson));
        catalog.PublishedAt = publication.PublishedAt;
        workItem.Status = publication.Warnings.Count == 0
            ? AssetImportJobValues.Succeeded
            : AssetImportJobValues.SucceededWithWarnings;
        workItem.WarningCount = publication.Warnings.Count;
        workItem.FinishedAt = publication.PublishedAt;
        workItem.Error = null;
        workItem.UnpublishedAt = null;

        outbox.Enroll(context);
        await outbox.PublishAsync(new AssetImportWorkItemCompleted(workItem.RunId, workItem.Id));
        if (previousOutputRoot is not null && previousOutputRoot != publication.OutputRoot)
            await outbox.PublishAsync(new DeleteAssetVersion(previousOutputRoot, false));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    public async Task FailAsync(Guid workItemId, string error, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var workItem = await context.AssetImportWorkItems.Include(item => item.Run)
            .SingleOrDefaultAsync(item => item.Id == workItemId, cancellationToken);
        if (workItem is null || AssetImportJobValues.TerminalStatuses.Contains(workItem.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        var catalog = await context.AssetCatalogs.Include(item => item.Sources)
            .SingleOrDefaultAsync(item => item.GameVersion == workItem.GameVersion &&
                item.Kind == workItem.ImportKind && item.IsActive, cancellationToken);
        var previous = catalog?.Sources.SingleOrDefault(source => source.NormalizedSourceKey == workItem.NormalizedSourceKey);
        var previousOutputRoot = previous?.OutputRoot;
        if (previous is not null)
        {
            context.AssetCatalogSources.Remove(previous);
            workItem.UnpublishedAt = timeProvider.GetUtcNow();
        }
        if (catalog is not null)
        {
            var remaining = catalog.Sources.Where(source => source.Id != previous?.Id).ToArray();
            catalog.SourceHash = AggregateHash(remaining
                .Select(source => (source.NormalizedSourceKey, source.SourceHash)));
            catalog.MetadataJson = AssetCatalogMetadataAggregator.Aggregate(
                workItem.ImportKind, remaining.Select(source => source.MetadataJson));
            catalog.PublishedAt = timeProvider.GetUtcNow();
        }
        workItem.Status = AssetImportJobValues.Failed;
        workItem.FinishedAt = timeProvider.GetUtcNow();
        workItem.Error = error;
        context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
        {
            RunId = workItem.RunId,
            WorkItemId = workItem.Id,
            Severity = "error",
            Code = workItem.SourceHash is null ? "discovery.source_unavailable" : "conversion.failed",
            Stage = "conversion",
            SourceKey = workItem.SourceKey,
            Message = error,
            CreatedAt = timeProvider.GetUtcNow()
        });
        if (previous is not null)
        {
            context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
            {
                RunId = workItem.RunId,
                WorkItemId = workItem.Id,
                Severity = "error",
                Code = "publication.source_unpublished",
                Stage = "publication",
                SourceKey = workItem.SourceKey,
                Message = "The previously published output was removed after this re-import failed.",
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
        outbox.Enroll(context);
        await outbox.PublishAsync(new AssetImportWorkItemCompleted(workItem.RunId, workItem.Id));
        if (previousOutputRoot is not null) await outbox.PublishAsync(new DeleteAssetVersion(previousOutputRoot, true));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    private static string[] ExtractVersionRoots(
        IReadOnlyList<AssetCatalogPublicationEntry> groups,
        IReadOnlyList<AssetCatalogPublicationEntry> items,
        string metadataJson) =>
        groups.Select(item => item.MetadataJson).Concat(items.Select(item => item.MetadataJson)).Append(metadataJson)
            .SelectMany(json => VersionRootPattern().Matches(json).Select(match => match.Value.TrimStart('/')))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    internal static string AggregateHash(IEnumerable<(string SourceKey, string SourceHash)> sources)
    {
        var value = string.Join('\n', sources.OrderBy(item => item.SourceKey, StringComparer.Ordinal)
            .Select(item => $"{item.SourceKey}\0{item.SourceHash}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
