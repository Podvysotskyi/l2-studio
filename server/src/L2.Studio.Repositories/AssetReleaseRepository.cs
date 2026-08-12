using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Repositories;

public sealed class AssetReleaseRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider) : IAssetReleaseRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AssetReleasePage> ListAsync(
        string gameVersion, string? status, int page, int pageSize, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var query = context.AssetReleases.AsNoTracking().Where(item => item.GameVersion == gameVersion);
        var pointer = await context.AssetReleasePointers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion, token);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var publishedId = pointer?.PublishedReleaseId;
            query = status == "active"
                ? query.Where(item => item.Id == publishedId)
                : query.Where(item => item.Status == status && item.Id != publishedId);
        }
        var total = await query.LongCountAsync(token);
        var releases = await query.Include(item => item.Artifacts).ThenInclude(item => item.Artifact)
            .OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new AssetReleasePage(releases.Select(item => Summary(item, pointer)).ToArray(), total, page, pageSize);
    }

    public async Task<AssetReleaseDetail?> GetAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        return await DetailAsync(context, gameVersion, id, token);
    }

    public async Task<AssetReleaseDetail> CreateAsync(
        string gameVersion, CreateAssetReleaseRequest request, CancellationToken token)
    {
        ValidateName(request.Name);
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var now = timeProvider.GetUtcNow();
        var (roots, artifacts) = await SnapshotAsync(context, gameVersion, token);
        var release = new AssetRelease
        {
            Id = Guid.NewGuid(), GameVersion = gameVersion, Name = request.Name.Trim(), Notes = NormalizeNotes(request.Notes),
            SnapshotHash = SnapshotHash(artifacts), CreatedAt = now, UpdatedAt = now
        };
        release.Artifacts = artifacts.Select(artifact => new AssetReleaseArtifact
            { ReleaseId = release.Id, ArtifactId = artifact.Id, IsRoot = roots.Contains(artifact.Id) }).ToArray();
        release.Events.Add(Event(release.Id, "created", now));
        context.AssetReleases.Add(release);
        await context.SaveChangesAsync(token);
        return (await DetailAsync(context, gameVersion, release.Id, token))!;
    }

    public async Task<AssetReleaseDetail?> CloneAsync(
        string gameVersion, Guid id, CreateAssetReleaseRequest request, CancellationToken token)
    {
        ValidateName(request.Name);
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var source = await context.AssetReleases.AsNoTracking().Include(item => item.Artifacts)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, token);
        if (source is null) return null;
        var now = timeProvider.GetUtcNow();
        var release = new AssetRelease
        {
            Id = Guid.NewGuid(), GameVersion = gameVersion, Name = request.Name.Trim(), Notes = NormalizeNotes(request.Notes),
            SnapshotHash = source.SnapshotHash, CreatedAt = now, UpdatedAt = now,
            LoginSceneFileId = source.LoginSceneFileId, LoginCameraSequence = source.LoginCameraSequence,
            LoginMusicFileId = source.LoginMusicFileId, PrimaryLogoFileId = source.PrimaryLogoFileId,
            VersionLogoFileId = source.VersionLogoFileId, LoadingArtworkFileId = source.LoadingArtworkFileId,
            CharacterSelectionSceneFileId = source.CharacterSelectionSceneFileId,
            CharacterSelectionCameraSequence = source.CharacterSelectionCameraSequence
        };
        release.Artifacts = source.Artifacts.Select(item => new AssetReleaseArtifact
            { ReleaseId = release.Id, ArtifactId = item.ArtifactId, IsRoot = item.IsRoot }).ToArray();
        release.Events.Add(Event(release.Id, "cloned", now, new { sourceReleaseId = source.Id }));
        context.AssetReleases.Add(release);
        await context.SaveChangesAsync(token);
        return await DetailAsync(context, gameVersion, release.Id, token);
    }

    public async Task<AssetReleaseDetail?> UpdateAsync(
        string gameVersion, Guid id, UpdateAssetReleaseRequest request, CancellationToken token)
    {
        ValidateName(request.Name);
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        RequireDraft(release);
        release.Name = request.Name.Trim();
        release.Notes = NormalizeNotes(request.Notes);
        release.LoginSceneFileId = request.LoginSceneFileId;
        release.LoginCameraSequence = Normalize(request.LoginCameraSequence);
        release.LoginMusicFileId = request.LoginMusicFileId;
        release.PrimaryLogoFileId = request.PrimaryLogoFileId;
        release.VersionLogoFileId = request.VersionLogoFileId;
        release.LoadingArtworkFileId = request.LoadingArtworkFileId;
        release.CharacterSelectionSceneFileId = request.CharacterSelectionSceneFileId;
        release.CharacterSelectionCameraSequence = Normalize(request.CharacterSelectionCameraSequence);
        Invalidate(release);
        release.Events.Add(Event(release.Id, "updated", release.UpdatedAt));
        await context.SaveChangesAsync(token);
        return await DetailAsync(context, gameVersion, id, token);
    }

    public async Task<AssetReleaseDetail?> RefreshAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.Include(item => item.Artifacts)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        RequireDraft(release);
        var (roots, artifacts) = await SnapshotAsync(context, gameVersion, token);
        context.AssetReleaseArtifacts.RemoveRange(release.Artifacts);
        release.Artifacts = artifacts.Select(artifact => new AssetReleaseArtifact
            { ReleaseId = release.Id, ArtifactId = artifact.Id, IsRoot = roots.Contains(artifact.Id) }).ToArray();
        release.SnapshotHash = SnapshotHash(artifacts);
        var fileIds = artifacts.SelectMany(item => item.Files).Select(item => item.Id).ToHashSet();
        if (release.LoginSceneFileId is long loginScene && !fileIds.Contains(loginScene)) release.LoginSceneFileId = null;
        if (release.LoginMusicFileId is long music && !fileIds.Contains(music)) release.LoginMusicFileId = null;
        if (release.PrimaryLogoFileId is long primary && !fileIds.Contains(primary)) release.PrimaryLogoFileId = null;
        if (release.VersionLogoFileId is long versionLogo && !fileIds.Contains(versionLogo)) release.VersionLogoFileId = null;
        if (release.LoadingArtworkFileId is long artwork && !fileIds.Contains(artwork)) release.LoadingArtworkFileId = null;
        if (release.CharacterSelectionSceneFileId is long characterScene && !fileIds.Contains(characterScene))
            release.CharacterSelectionSceneFileId = null;
        Invalidate(release);
        release.Events.Add(Event(release.Id, "snapshot_refreshed", release.UpdatedAt));
        await context.SaveChangesAsync(token);
        return await DetailAsync(context, gameVersion, id, token);
    }

    public async Task<bool> DeleteDraftAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return false;
        RequireDraft(release);
        context.AssetReleases.Remove(release);
        await context.SaveChangesAsync(token);
        return true;
    }

    public async Task<AssetReleaseDetail?> QueueValidationAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        RequireDraft(release);
        release.ValidationStatus = "queued";
        release.ValidationIssuesJson = "[]";
        release.ValidationRequestedAt = timeProvider.GetUtcNow();
        release.UpdatedAt = release.ValidationRequestedAt.Value;
        release.Events.Add(Event(release.Id, "validation_queued", release.UpdatedAt));
        outbox.Enroll(context);
        await outbox.PublishAsync(new ValidateAssetRelease(release.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(token);
        return await GetAsync(gameVersion, id, token);
    }

    public async Task ValidateAsync(Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.Include(item => item.Artifacts).ThenInclude(item => item.Artifact)
            .ThenInclude(item => item.Files).Include(item => item.Artifacts).ThenInclude(item => item.Artifact)
            .ThenInclude(item => item.Dependencies).SingleOrDefaultAsync(item => item.Id == id, token);
        if (release is null || release.Status != "draft" || release.ValidationStatus is not ("queued" or "running")) return;
        if (release.ValidationStatus == "queued")
        {
            release.ValidationStatus = "running";
            await context.SaveChangesAsync(token);
        }
        var issues = new List<AssetReleaseValidationIssue>();
        var root = Path.GetFullPath(options.Value.AssetRootPath);
        foreach (var item in release.Artifacts)
        {
            var artifact = item.Artifact;
            var healthy = await VerifyAsync(root, artifact, token);
            artifact.IntegrityStatus = healthy ? "healthy" : Directory.Exists(ContainedPath(root, artifact.OutputRoot)) ? "corrupt" : "missing";
            artifact.LastVerifiedAt = timeProvider.GetUtcNow();
            if (!healthy) issues.Add(new("artifact_integrity", null, $"{artifact.Kind}/{artifact.SourceKey} is {artifact.IntegrityStatus}."));
            foreach (var dependency in artifact.Dependencies.Where(dependency => !dependency.IsResolved ||
                         dependency.ResolvedArtifactId is null || !release.Artifacts.Any(candidate => candidate.ArtifactId == dependency.ResolvedArtifactId)))
                issues.Add(new("unresolved_dependency", null,
                    $"{artifact.Kind}/{artifact.SourceKey} is missing {dependency.Kind}/{dependency.DependencyKey}."));
        }
        var files = release.Artifacts.SelectMany(item => item.Artifact.Files).ToDictionary(item => item.Id);
        ValidateFile(files, release.LoginSceneFileId, "loginSceneFileId", "application/json", true, issues);
        ValidateFile(files, release.CharacterSelectionSceneFileId, "characterSelectionSceneFileId", "application/json", true, issues);
        ValidateFile(files, release.LoginMusicFileId, "loginMusicFileId", "audio/", false, issues);
        ValidateFile(files, release.PrimaryLogoFileId, "primaryLogoFileId", "image/", false, issues);
        ValidateFile(files, release.LoadingArtworkFileId, "loadingArtworkFileId", "image/", false, issues);
        if (release.VersionLogoFileId is not null)
            ValidateFile(files, release.VersionLogoFileId, "versionLogoFileId", "image/", false, issues);
        ValidateCamera(root, files, release.LoginSceneFileId, release.LoginCameraSequence, "loginCameraSequence", issues);
        ValidateCamera(root, files, release.CharacterSelectionSceneFileId, release.CharacterSelectionCameraSequence,
            "characterSelectionCameraSequence", issues);
        release.ValidationStatus = issues.Count == 0 ? "valid" : "invalid";
        release.ValidationIssuesJson = JsonSerializer.Serialize(issues, JsonOptions);
        release.ValidatedSnapshotHash = release.SnapshotHash;
        release.ValidatedAt = timeProvider.GetUtcNow();
        release.UpdatedAt = release.ValidatedAt.Value;
        release.Events.Add(Event(release.Id, issues.Count == 0 ? "validation_succeeded" : "validation_failed",
            release.UpdatedAt, new { issueCount = issues.Count }));
        await context.SaveChangesAsync(token);
    }

    public async Task<AssetReleaseDetail?> PublishAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.Include(item => item.Artifacts).ThenInclude(item => item.Artifact)
            .ThenInclude(item => item.Files).SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        RequireDraft(release);
        if (release.ValidationStatus != "valid" || release.ValidatedSnapshotHash != release.SnapshotHash)
            throw new InvalidOperationException("The current release snapshot must pass validation before publication.");
        var files = release.Artifacts.SelectMany(item => item.Artifact.Files).ToDictionary(item => item.Id);
        var now = timeProvider.GetUtcNow();
        var relativeDirectory = Path.Combine("versions", gameVersion, "releases", release.Id.ToString("D")).Replace('\\', '/');
        var manifestPath = $"/{relativeDirectory}/client-manifest.json";
        var manifest = new
        {
            schemaVersion = 2,
            releaseId = release.Id,
            releaseName = release.Name,
            gameVersion,
            artifactSetFingerprint = release.SnapshotHash,
            publishedAt = now,
            loginScreen = new
            {
                sceneManifestPath = PathOf(files, release.LoginSceneFileId),
                cameraSequence = release.LoginCameraSequence,
                musicPath = PathOf(files, release.LoginMusicFileId),
                primaryLogoPath = PathOf(files, release.PrimaryLogoFileId),
                versionLogoPath = PathOf(files, release.VersionLogoFileId),
                loadingArtworkPath = PathOf(files, release.LoadingArtworkFileId)
            },
            characterSelection = new
            {
                sceneManifestPath = PathOf(files, release.CharacterSelectionSceneFileId),
                cameraSequence = release.CharacterSelectionCameraSequence
            }
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var target = ContainedPath(Path.GetFullPath(options.Value.AssetRootPath), relativeDirectory);
        var staging = Path.Combine(Path.GetFullPath(options.Value.AssetWorkRootPath), "releases", release.Id.ToString("D"));
        Directory.CreateDirectory(staging);
        await File.WriteAllBytesAsync(Path.Combine(staging, "client-manifest.json"), bytes, token);
        await File.WriteAllTextAsync(Path.Combine(staging, ".l2-release-version"), hash, token);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (Directory.Exists(target))
        {
            var existing = await File.ReadAllBytesAsync(Path.Combine(target, "client-manifest.json"), token);
            if (!SHA256.HashData(existing).SequenceEqual(SHA256.HashData(bytes)))
                throw new InvalidDataException("The immutable release path already contains different content.");
            Directory.Delete(staging, true);
        }
        else Directory.Move(staging, target);
        release.Status = "published";
        release.ManifestPath = manifestPath;
        release.ManifestHash = hash;
        release.PublishedAt = now;
        release.UpdatedAt = now;
        release.Events.Add(Event(release.Id, "published", now, new { manifestPath, manifestHash = hash }));
        await context.SaveChangesAsync(token);
        return await DetailAsync(context, gameVersion, id, token);
    }

    public async Task<AssetReleaseDetail?> QueueActivationAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        if (release.Status != "published") throw new InvalidOperationException("Only a published release can be activated.");
        var now = timeProvider.GetUtcNow();
        var pointer = await context.AssetReleasePointers.SingleOrDefaultAsync(item => item.GameVersion == gameVersion, token);
        if (pointer is null)
        {
            pointer = new AssetReleasePointer { GameVersion = gameVersion };
            context.AssetReleasePointers.Add(pointer);
        }
        pointer.DesiredReleaseId = id;
        pointer.Status = "pending";
        pointer.Error = null;
        pointer.RequestedAt = now;
        release.Events.Add(Event(id, pointer.PublishedReleaseId is null ? "activation_queued" : "rollback_queued", now,
            new { previousReleaseId = pointer.PublishedReleaseId }));
        outbox.Enroll(context);
        await outbox.PublishAsync(new ActivateAssetRelease(gameVersion, id));
        await outbox.SaveChangesAndFlushMessagesAsync(token);
        return await GetAsync(gameVersion, id, token);
    }

    public async Task ActivateAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var pointer = await context.AssetReleasePointers.SingleOrDefaultAsync(item => item.GameVersion == gameVersion, token);
        if (pointer?.DesiredReleaseId != id) return;
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id && item.Status == "published", token);
        if (release?.ManifestPath is null) return;
        var publicRoot = Path.GetFullPath(options.Value.AssetRootPath);
        if (!File.Exists(ContainedPath(publicRoot, release.ManifestPath.TrimStart('/'))))
            throw new FileNotFoundException("The published release manifest is missing.", release.ManifestPath);
        try
        {
            var now = timeProvider.GetUtcNow();
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1, gameVersion, releaseId = id,
                releaseManifestPath = release.ManifestPath, activatedAt = now
            }, JsonOptions);
            var versionRoot = ContainedPath(publicRoot, Path.Combine("versions", gameVersion));
            Directory.CreateDirectory(versionRoot);
            var temporary = Path.Combine(versionRoot, $".current.{Guid.NewGuid():N}.tmp");
            await File.WriteAllBytesAsync(temporary, bytes, token);
            File.Move(temporary, Path.Combine(versionRoot, "current.json"), true);
            pointer.PublishedReleaseId = id;
            pointer.Status = "active";
            pointer.Error = null;
            pointer.PublishedAt = now;
            release.Events.Add(Event(id, "activated", now));
            await context.SaveChangesAsync(token);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            pointer.Status = "failed";
            pointer.Error = exception.Message;
            await context.SaveChangesAsync(token);
            throw;
        }
    }

    public async Task<AssetReleaseDetail?> RetireAsync(string gameVersion, Guid id, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var release = await context.AssetReleases.SingleOrDefaultAsync(
            item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        if (release.Status != "published") throw new InvalidOperationException("Only a published release can be retired.");
        if (await context.AssetReleasePointers.AnyAsync(item => item.GameVersion == gameVersion &&
                (item.DesiredReleaseId == id || item.PublishedReleaseId == id), token))
            throw new InvalidOperationException("The active or pending release cannot be retired.");
        var now = timeProvider.GetUtcNow();
        release.Status = "retired";
        release.RetiredAt = now;
        release.UpdatedAt = now;
        release.Events.Add(Event(id, "retired", now));
        await context.SaveChangesAsync(token);
        return await DetailAsync(context, gameVersion, id, token);
    }

    public async Task<AssetReleaseResourcePage?> SearchResourcesAsync(
        string gameVersion, Guid id, string type, string query, int page, int pageSize, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        if (!await context.AssetReleases.AnyAsync(item => item.GameVersion == gameVersion && item.Id == id, token)) return null;
        var files = context.AssetArtifactFiles.AsNoTracking().Where(file =>
            file.Artifact.Releases.Any(item => item.ReleaseId == id));
        files = type switch
        {
            "scene" => files.Where(file => file.Artifact.Kind == "scenes" && file.Role == "manifest"),
            "audio" => files.Where(file => file.MediaType.StartsWith("audio/")),
            "image" => files.Where(file => file.MediaType.StartsWith("image/")),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Resource type is invalid.")
        };
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{EscapeLike(query.Trim())}%";
            files = files.Where(file => EF.Functions.ILike(file.Artifact.SourceKey, pattern, "\\") ||
                EF.Functions.ILike(file.RelativePath, pattern, "\\"));
        }
        var total = await files.LongCountAsync(token);
        var values = await files.Include(file => file.Artifact).OrderBy(file => file.Artifact.SourceKey)
            .ThenBy(file => file.RelativePath).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var root = Path.GetFullPath(options.Value.AssetRootPath);
        var items = values.Select(file => new AssetReleaseResourceOption(
            file.Id, file.ArtifactId, file.Artifact.Kind, file.Artifact.SourceKey,
            $"{file.Artifact.SourceKey} · {file.RelativePath}", file.PublicPath, file.MediaType,
            type == "scene" ? CameraSequences(ContainedPath(root, file.PublicPath.TrimStart('/'))) : [])).ToArray();
        return new AssetReleaseResourcePage(items, total, page, pageSize);
    }

    private async Task<AssetReleaseDetail?> DetailAsync(
        GameContentDbContext context, string gameVersion, Guid id, CancellationToken token)
    {
        var release = await context.AssetReleases.AsNoTracking().Include(item => item.Artifacts)
            .ThenInclude(item => item.Artifact).Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion && item.Id == id, token);
        if (release is null) return null;
        var pointer = await context.AssetReleasePointers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion, token);
        var ids = EntryFileIds(release).ToArray();
        var paths = await context.AssetArtifactFiles.AsNoTracking().Where(file => ids.Contains(file.Id))
            .ToDictionaryAsync(file => file.Id, file => file.PublicPath, token);
        return new AssetReleaseDetail(
            Summary(release, pointer),
            new AssetReleaseEntrypoints(
                release.LoginSceneFileId, PathOf(paths, release.LoginSceneFileId), release.LoginCameraSequence,
                release.LoginMusicFileId, PathOf(paths, release.LoginMusicFileId),
                release.PrimaryLogoFileId, PathOf(paths, release.PrimaryLogoFileId),
                release.VersionLogoFileId, PathOf(paths, release.VersionLogoFileId),
                release.LoadingArtworkFileId, PathOf(paths, release.LoadingArtworkFileId),
                release.CharacterSelectionSceneFileId, PathOf(paths, release.CharacterSelectionSceneFileId),
                release.CharacterSelectionCameraSequence),
            JsonSerializer.Deserialize<AssetReleaseValidationIssue[]>(release.ValidationIssuesJson, JsonOptions) ?? [],
            release.Artifacts.OrderBy(item => item.Artifact.Kind).ThenBy(item => item.Artifact.SourceKey)
                .Select(item => new AssetReleaseArtifactSummary(item.ArtifactId, item.Artifact.Kind,
                    item.Artifact.SourceKey, item.Artifact.BuildFingerprint, item.Artifact.IntegrityStatus,
                    item.Artifact.SizeBytes, item.IsRoot)).ToArray(),
            release.Events.OrderByDescending(item => item.OccurredAt)
                .Select(item => new AssetReleaseEventSummary(item.Id, item.Action, item.OccurredAt)).ToArray(),
            pointer?.Status ?? "inactive", pointer?.Error);
    }

    private static AssetReleaseSummary Summary(AssetRelease release, AssetReleasePointer? pointer)
    {
        var active = pointer?.PublishedReleaseId == release.Id;
        return new AssetReleaseSummary(release.Id, release.Name, release.Notes, active ? "active" : release.Status,
            release.ValidationStatus, release.SnapshotHash, release.Artifacts.Count(item => item.IsRoot),
            release.Artifacts.Count, release.Artifacts.Sum(item => item.Artifact.SizeBytes), release.ManifestPath,
            release.ManifestHash, release.CreatedAt, release.UpdatedAt, release.PublishedAt, release.RetiredAt,
            active, pointer?.DesiredReleaseId == release.Id);
    }

    private static async Task<(HashSet<Guid> Roots, AssetArtifact[] Artifacts)> SnapshotAsync(
        GameContentDbContext context, string gameVersion, CancellationToken token)
    {
        var roots = (await context.AssetCatalogSources.AsNoTracking().Where(source =>
                source.Catalog.GameVersion == gameVersion && source.Catalog.IsActive && !source.IsStale &&
                source.Artifact.IntegrityStatus == "healthy")
            .Select(source => source.ArtifactId).Distinct().ToListAsync(token)).ToHashSet();
        var artifacts = await context.AssetArtifacts.AsNoTracking().Include(item => item.Dependencies)
            .Include(item => item.Files).Where(item => item.GameVersion == gameVersion).ToArrayAsync(token);
        var byId = artifacts.ToDictionary(item => item.Id);
        var included = new HashSet<Guid>(roots);
        var queue = new Queue<Guid>(roots);
        while (queue.TryDequeue(out var id) && byId.TryGetValue(id, out var artifact))
            foreach (var dependency in artifact.Dependencies.Where(item => item.ResolvedArtifactId is not null))
                if (included.Add(dependency.ResolvedArtifactId!.Value)) queue.Enqueue(dependency.ResolvedArtifactId.Value);
        return (roots, artifacts.Where(item => included.Contains(item.Id)).ToArray());
    }

    private static string SnapshotHash(IEnumerable<AssetArtifact> artifacts)
    {
        var canonical = string.Join('\n', artifacts.OrderBy(item => item.Id)
            .Select(item => $"{item.Id:D}|{item.BuildFingerprint}|{item.ContentHash}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static async Task<bool> VerifyAsync(string root, AssetArtifact artifact, CancellationToken token)
    {
        var artifactRoot = ContainedPath(root, artifact.OutputRoot);
        var marker = Path.Combine(artifactRoot, ".l2-asset-version");
        if (!Directory.Exists(artifactRoot) || !File.Exists(marker) ||
            !string.Equals((await File.ReadAllTextAsync(marker, token)).Trim(), artifact.BuildFingerprint, StringComparison.Ordinal))
            return false;
        foreach (var file in artifact.Files)
        {
            var path = ContainedPath(artifactRoot, file.RelativePath);
            if (!File.Exists(path) || new FileInfo(path).Length != file.SizeBytes) return false;
            await using var stream = File.OpenRead(path);
            if (!string.Equals(Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, token)), file.Sha256,
                    StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static void ValidateFile(
        IReadOnlyDictionary<long, AssetArtifactFile> files, long? id, string field, string mediaType, bool exact,
        ICollection<AssetReleaseValidationIssue> issues)
    {
        if (id is null) { issues.Add(new("required", field, "Select a resource.")); return; }
        if (!files.TryGetValue(id.Value, out var file)) { issues.Add(new("outside_snapshot", field, "The resource is not in this release.")); return; }
        if (exact ? file.MediaType != mediaType : !file.MediaType.StartsWith(mediaType, StringComparison.Ordinal))
            issues.Add(new("invalid_media_type", field, $"Expected {mediaType}."));
    }

    private static void ValidateCamera(
        string root, IReadOnlyDictionary<long, AssetArtifactFile> files, long? fileId, string? camera,
        string field, ICollection<AssetReleaseValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(camera)) { issues.Add(new("required", field, "Select a camera sequence.")); return; }
        if (fileId is null || !files.TryGetValue(fileId.Value, out var file)) return;
        var sequences = CameraSequences(ContainedPath(root, file.PublicPath.TrimStart('/')));
        if (!sequences.Contains(camera, StringComparer.Ordinal))
            issues.Add(new("camera_not_found", field, $"Camera sequence '{camera}' is not present in the scene."));
    }

    private static string[] CameraSequences(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            if (!document.RootElement.TryGetProperty("sceneManagers", out var managers)) return [];
            return managers.EnumerateArray().Select(item => item.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().Distinct().Order().ToArray();
        }
        catch (IOException) { return []; }
        catch (JsonException) { return []; }
    }

    private void Invalidate(AssetRelease release)
    {
        release.ValidationStatus = "not_validated";
        release.ValidationIssuesJson = "[]";
        release.ValidatedSnapshotHash = null;
        release.ValidatedAt = null;
        release.UpdatedAt = timeProvider.GetUtcNow();
    }

    private static void RequireDraft(AssetRelease release)
    {
        if (release.Status != "draft") throw new InvalidOperationException("Only draft releases can be changed.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
            throw new ArgumentException("Release name must contain between 1 and 120 characters.", nameof(name));
    }

    private static AssetReleaseEvent Event(Guid releaseId, string action, DateTimeOffset occurredAt, object? details = null) =>
        new() { ReleaseId = releaseId, Action = action, OccurredAt = occurredAt,
            DetailsJson = JsonSerializer.Serialize(details ?? new { }, JsonOptions) };
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeNotes(string? value) => Normalize(value) is { Length: > 4000 } normalized ? normalized[..4000] : Normalize(value);
    private static IEnumerable<long> EntryFileIds(AssetRelease release) => new long?[]
        { release.LoginSceneFileId, release.LoginMusicFileId, release.PrimaryLogoFileId, release.VersionLogoFileId,
            release.LoadingArtworkFileId, release.CharacterSelectionSceneFileId }.Where(item => item is not null).Select(item => item!.Value);
    private static string? PathOf(IReadOnlyDictionary<long, AssetArtifactFile> files, long? id) =>
        id is long value && files.TryGetValue(value, out var file) ? file.PublicPath : null;
    private static string? PathOf(IReadOnlyDictionary<long, string> files, long? id) =>
        id is long value && files.TryGetValue(value, out var path) ? path : null;
    private static string ContainedPath(string root, string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException("Release paths must be relative.");
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var check = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(check) || check.StartsWith("..", StringComparison.Ordinal))
            throw new InvalidDataException("Release path escaped the configured root.");
        return path;
    }
    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
