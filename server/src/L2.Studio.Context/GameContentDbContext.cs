using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace L2.Studio.Context;

public sealed class GameContentDbContext(DbContextOptions<GameContentDbContext> options) : DbContext(options)
{
    public const string SchemaName = "content";

    public DbSet<GameVersion> GameVersions => Set<GameVersion>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<NpcStatus> NpcStatuses => Set<NpcStatus>();
    public DbSet<NpcStats> NpcStats => Set<NpcStats>();
    public DbSet<NpcStatsVitals> NpcStatsVitals => Set<NpcStatsVitals>();
    public DbSet<NpcStatsAttack> NpcStatsAttacks => Set<NpcStatsAttack>();
    public DbSet<NpcStatsDefence> NpcStatsDefences => Set<NpcStatsDefence>();
    public DbSet<NpcStatsSpeed> NpcStatsSpeeds => Set<NpcStatsSpeed>();
    public DbSet<NpcType> NpcTypes => Set<NpcType>();
    public DbSet<NpcRace> NpcRaces => Set<NpcRace>();
    public DbSet<NpcSex> NpcSexes => Set<NpcSex>();
    public DbSet<NpcLookupImportRun> NpcLookupImportRuns => Set<NpcLookupImportRun>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemStats> ItemStats => Set<ItemStats>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<ItemAction> ItemActions => Set<ItemAction>();
    public DbSet<ItemBodyPart> ItemBodyParts => Set<ItemBodyPart>();
    public DbSet<ItemMaterial> ItemMaterials => Set<ItemMaterial>();
    public DbSet<ItemCrystalType> ItemCrystalTypes => Set<ItemCrystalType>();
    public DbSet<ItemImportRun> ItemImportRuns => Set<ItemImportRun>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillIcon> SkillIcons => Set<SkillIcon>();
    public DbSet<SkillImportRun> SkillImportRuns => Set<SkillImportRun>();
    public DbSet<SkillOperateType> SkillOperateTypes => Set<SkillOperateType>();
    public DbSet<SkillTargetType> SkillTargetTypes => Set<SkillTargetType>();
    public DbSet<PlayerRace> PlayerRaces => Set<PlayerRace>();
    public DbSet<PlayerSex> PlayerSexes => Set<PlayerSex>();
    public DbSet<PlayerClass> PlayerClasses => Set<PlayerClass>();
    public DbSet<PlayerImportRun> PlayerImportRuns => Set<PlayerImportRun>();
    public DbSet<PlayerFace> PlayerFaces => Set<PlayerFace>();
    public DbSet<PlayerHairStyle> PlayerHairStyles => Set<PlayerHairStyle>();
    public DbSet<PlayerHairColor> PlayerHairColors => Set<PlayerHairColor>();
    public DbSet<AssetImportRun> AssetImportRuns => Set<AssetImportRun>();
    public DbSet<AssetImportWorkItem> AssetImportWorkItems => Set<AssetImportWorkItem>();
    public DbSet<AssetImportDiagnostic> AssetImportDiagnostics => Set<AssetImportDiagnostic>();
    public DbSet<AssetCatalog> AssetCatalogs => Set<AssetCatalog>();
    public DbSet<AssetCatalogSource> AssetCatalogSources => Set<AssetCatalogSource>();
    public DbSet<AssetCatalogGroup> AssetCatalogGroups => Set<AssetCatalogGroup>();
    public DbSet<AssetCatalogItem> AssetCatalogItems => Set<AssetCatalogItem>();
    public DbSet<AssetCatalogSourceDependency> AssetCatalogSourceDependencies => Set<AssetCatalogSourceDependency>();
    public DbSet<AssetArtifact> AssetArtifacts => Set<AssetArtifact>();
    public DbSet<AssetArtifactFile> AssetArtifactFiles => Set<AssetArtifactFile>();
    public DbSet<AssetArtifactDependency> AssetArtifactDependencies => Set<AssetArtifactDependency>();
    public DbSet<AssetRelease> AssetReleases => Set<AssetRelease>();
    public DbSet<AssetReleaseArtifact> AssetReleaseArtifacts => Set<AssetReleaseArtifact>();
    public DbSet<AssetReleaseEvent> AssetReleaseEvents => Set<AssetReleaseEvent>();
    public DbSet<AssetReleasePointer> AssetReleasePointers => Set<AssetReleasePointer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        var gameVersion = modelBuilder.Entity<GameVersion>();
        gameVersion.ToTable("game_versions");
        gameVersion.HasKey(entity => entity.Key);
        gameVersion.Property(entity => entity.Key).HasColumnName("key").HasMaxLength(32);
        gameVersion.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        gameVersion.Property(entity => entity.SourceFolder).HasColumnName("source_folder").HasMaxLength(64);
        gameVersion.Property(entity => entity.SortOrder).HasColumnName("sort_order");
        gameVersion.HasIndex(entity => entity.DisplayName).IsUnique();
        var playerRace = modelBuilder.Entity<PlayerRace>();
        playerRace.ToTable("player_races");
        playerRace.HasKey(entity => new { entity.GameVersion, entity.Id });
        playerRace.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerRace.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerRace.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerRace.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_player_races_name");

        var playerSex = modelBuilder.Entity<PlayerSex>();
        playerSex.ToTable("player_sexes");
        playerSex.HasKey(entity => new { entity.GameVersion, entity.Id });
        playerSex.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerSex.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerSex.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerSex.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_player_sexes_name");

        var playerClass = modelBuilder.Entity<PlayerClass>();
        playerClass.ToTable("player_classes");
        playerClass.HasKey(entity => new { entity.GameVersion, entity.Id, entity.PlayerSexId, entity.PlayerRaceId });
        playerClass.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerClass.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerClass.Property(entity => entity.PlayerSexId).HasColumnName("player_sex_id").ValueGeneratedNever();
        playerClass.Property(entity => entity.PlayerRaceId).HasColumnName("player_race_id").ValueGeneratedNever();
        playerClass.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerClass.Property(entity => entity.IsMage).HasColumnName("is_mage");
        playerClass.Property(entity => entity.ParentClassId).HasColumnName("parent_class_id").IsRequired(false);
        playerClass.HasIndex(entity => new { entity.GameVersion, entity.Name, entity.PlayerSexId, entity.PlayerRaceId })
            .IsUnique().HasDatabaseName("ix_player_classes_name_sex_race");
        playerClass.HasIndex(entity => entity.PlayerRaceId).HasDatabaseName("ix_player_classes_player_race_id");
        playerClass.HasIndex(entity => entity.PlayerSexId).HasDatabaseName("ix_player_classes_player_sex_id");
        playerClass.HasIndex(entity => new { entity.ParentClassId, entity.PlayerSexId, entity.PlayerRaceId })
            .HasDatabaseName("ix_player_classes_parent_sex_race");
        playerClass.HasOne(entity => entity.PlayerRace)
            .WithMany(entity => entity.PlayerClasses)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId })
            .OnDelete(DeleteBehavior.Restrict);
        playerClass.HasOne(entity => entity.PlayerSex)
            .WithMany(entity => entity.PlayerClasses)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId })
            .OnDelete(DeleteBehavior.Restrict);
        playerClass.HasOne(entity => entity.ParentClass)
            .WithMany(entity => entity.ChildClasses)
            .HasForeignKey(entity => new { entity.GameVersion, entity.ParentClassId, entity.PlayerSexId, entity.PlayerRaceId })
            .HasPrincipalKey(entity => new { entity.GameVersion, entity.Id, entity.PlayerSexId, entity.PlayerRaceId })
            .OnDelete(DeleteBehavior.Restrict);

        var playerFace = modelBuilder.Entity<PlayerFace>();
        playerFace.ToTable("player_faces");
        playerFace.HasKey(entity => new { entity.GameVersion, entity.Id, entity.PlayerSexId, entity.PlayerRaceId });
        playerFace.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerFace.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerFace.Property(entity => entity.PlayerSexId).HasColumnName("player_sex_id").ValueGeneratedNever();
        playerFace.Property(entity => entity.PlayerRaceId).HasColumnName("player_race_id").ValueGeneratedNever();
        playerFace.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerFace.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerFaces)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerFace.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerFaces)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var playerHairStyle = modelBuilder.Entity<PlayerHairStyle>();
        playerHairStyle.ToTable("player_hair_styles");
        playerHairStyle.HasKey(entity => new { entity.GameVersion, entity.Id, entity.PlayerSexId, entity.PlayerRaceId });
        playerHairStyle.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerHairStyle.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerHairStyle.Property(entity => entity.PlayerSexId).HasColumnName("player_sex_id").ValueGeneratedNever();
        playerHairStyle.Property(entity => entity.PlayerRaceId).HasColumnName("player_race_id").ValueGeneratedNever();
        playerHairStyle.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerHairStyle.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerHairStyles)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerHairStyle.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerHairStyles)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var playerHairColor = modelBuilder.Entity<PlayerHairColor>();
        playerHairColor.ToTable("player_hair_colors");
        playerHairColor.HasKey(entity => new { entity.GameVersion, entity.Id, entity.PlayerSexId, entity.PlayerRaceId });
        playerHairColor.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerHairColor.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerHairColor.Property(entity => entity.PlayerSexId).HasColumnName("player_sex_id").ValueGeneratedNever();
        playerHairColor.Property(entity => entity.PlayerRaceId).HasColumnName("player_race_id").ValueGeneratedNever();
        playerHairColor.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        playerHairColor.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerHairColors)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerHairColor.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerHairColors)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var playerImportRun = modelBuilder.Entity<PlayerImportRun>();
        playerImportRun.ToTable("player_import_runs");
        playerImportRun.HasKey(entity => entity.Id);
        playerImportRun.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        playerImportRun.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        playerImportRun.Property(entity => entity.Mode).HasColumnName("mode").HasMaxLength(32).HasDefaultValue("add_missing");
        playerImportRun.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        playerImportRun.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        playerImportRun.Property(entity => entity.StartedAt).HasColumnName("started_at");
        playerImportRun.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        playerImportRun.Property(entity => entity.TotalCount).HasColumnName("total_count");
        playerImportRun.Property(entity => entity.InsertedCount).HasColumnName("inserted_count");
        playerImportRun.Property(entity => entity.ExistingCount).HasColumnName("existing_count");
        playerImportRun.Property(entity => entity.RestoredCount).HasColumnName("restored_count");
        playerImportRun.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        playerImportRun.HasIndex(entity => new { entity.GameVersion, entity.RequestedAt }).HasDatabaseName("ix_player_import_runs_recent");
        playerImportRun.HasIndex(entity => entity.GameVersion).IsUnique().HasFilter("status IN ('queued', 'running')").HasDatabaseName("ix_player_import_runs_active");

        var assetImportRun = modelBuilder.Entity<AssetImportRun>();
        assetImportRun.ToTable("asset_import_runs");
        assetImportRun.HasKey(entity => entity.Id);
        assetImportRun.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetImportRun.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetImportRun.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(64);
        assetImportRun.Property(entity => entity.TriggerType).HasColumnName("trigger_type").HasMaxLength(32);
        assetImportRun.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        assetImportRun.Property(entity => entity.RequestedSourceKey).HasColumnName("requested_source_key").HasMaxLength(256);
        assetImportRun.Property(entity => entity.NormalizedRequestedSourceKey).HasColumnName("normalized_requested_source_key").HasMaxLength(256);
        assetImportRun.Property(entity => entity.Force).HasColumnName("force");
        assetImportRun.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        assetImportRun.Property(entity => entity.StartedAt).HasColumnName("started_at");
        assetImportRun.Property(entity => entity.DiscoveryFinishedAt).HasColumnName("discovery_finished_at");
        assetImportRun.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        assetImportRun.Property(entity => entity.DiscoveredFileCount).HasColumnName("discovered_file_count");
        assetImportRun.Property(entity => entity.CompletedFileCount).HasColumnName("completed_file_count");
        assetImportRun.Property(entity => entity.SucceededFileCount).HasColumnName("succeeded_file_count");
        assetImportRun.Property(entity => entity.WarningFileCount).HasColumnName("warning_file_count");
        assetImportRun.Property(entity => entity.FailedFileCount).HasColumnName("failed_file_count");
        assetImportRun.Property(entity => entity.ReusedFileCount).HasColumnName("reused_file_count");
        assetImportRun.Property(entity => entity.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
        assetImportRun.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.RequestedAt })
            .HasDatabaseName("ix_asset_import_runs_kind_requested");
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind }).IsUnique()
            .HasFilter("trigger_type = 'full_scan' AND status IN ('queued', 'discovering', 'running')")
            .HasDatabaseName("ix_asset_import_runs_active_full_scan_kind");
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.NormalizedRequestedSourceKey }).IsUnique()
            .HasFilter("trigger_type = 'single_file' AND status IN ('queued', 'discovering', 'running')")
            .HasDatabaseName("ix_asset_import_runs_active_single_source");

        var assetImportWorkItem = modelBuilder.Entity<AssetImportWorkItem>();
        assetImportWorkItem.ToTable("asset_import_work_items");
        assetImportWorkItem.HasKey(entity => entity.Id);
        assetImportWorkItem.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetImportWorkItem.Ignore(entity => entity.Kind);
        assetImportWorkItem.Ignore(entity => entity.TotalCount);
        assetImportWorkItem.Ignore(entity => entity.ProcessedCount);
        assetImportWorkItem.Ignore(entity => entity.SkippedCount);
        assetImportWorkItem.Ignore(entity => entity.WarningsJson);
        assetImportWorkItem.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetImportWorkItem.Property(entity => entity.RunId).HasColumnName("run_id");
        assetImportWorkItem.Property(entity => entity.ImportKind).HasColumnName("import_kind").HasMaxLength(64);
        assetImportWorkItem.Property(entity => entity.SourceKey).HasColumnName("source_key").HasMaxLength(256);
        assetImportWorkItem.Property(entity => entity.NormalizedSourceKey).HasColumnName("normalized_source_key").HasMaxLength(256);
        assetImportWorkItem.Property(entity => entity.SourcePath).HasColumnName("source_path").HasMaxLength(1024);
        assetImportWorkItem.Property(entity => entity.SourceHash).HasColumnName("source_hash").HasMaxLength(64);
        assetImportWorkItem.Property(entity => entity.ArtifactFingerprint).HasColumnName("artifact_fingerprint").HasMaxLength(64);
        assetImportWorkItem.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        assetImportWorkItem.Property(entity => entity.AttemptCount).HasColumnName("attempt_count");
        assetImportWorkItem.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        assetImportWorkItem.Property(entity => entity.StartedAt).HasColumnName("started_at");
        assetImportWorkItem.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        assetImportWorkItem.Property(entity => entity.TotalResourceCount).HasColumnName("total_resource_count");
        assetImportWorkItem.Property(entity => entity.ProcessedResourceCount).HasColumnName("processed_resource_count");
        assetImportWorkItem.Property(entity => entity.SkippedResourceCount).HasColumnName("skipped_resource_count");
        assetImportWorkItem.Property(entity => entity.WarningCount).HasColumnName("warning_count");
        assetImportWorkItem.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        assetImportWorkItem.Property(entity => entity.UnpublishedAt).HasColumnName("unpublished_at");
        assetImportWorkItem.Property(entity => entity.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
        assetImportWorkItem.HasIndex(entity => new { entity.RunId, entity.NormalizedSourceKey }).IsUnique()
            .HasDatabaseName("ix_asset_import_work_items_run_source");
        assetImportWorkItem.HasIndex(entity => new { entity.RunId, entity.Status })
            .HasDatabaseName("ix_asset_import_work_items_run_status");
        assetImportWorkItem.HasOne(entity => entity.Run).WithMany(entity => entity.WorkItems)
            .HasForeignKey(entity => entity.RunId).OnDelete(DeleteBehavior.Cascade);

        var assetImportDiagnostic = modelBuilder.Entity<AssetImportDiagnostic>();
        assetImportDiagnostic.ToTable("asset_import_diagnostics");
        assetImportDiagnostic.HasKey(entity => entity.Id);
        assetImportDiagnostic.Property(entity => entity.Id).HasColumnName("id");
        assetImportDiagnostic.Property(entity => entity.RunId).HasColumnName("run_id");
        assetImportDiagnostic.Property(entity => entity.WorkItemId).HasColumnName("work_item_id");
        assetImportDiagnostic.Property(entity => entity.Severity).HasColumnName("severity").HasMaxLength(16);
        assetImportDiagnostic.Property(entity => entity.Code).HasColumnName("code").HasMaxLength(128);
        assetImportDiagnostic.Property(entity => entity.Stage).HasColumnName("stage").HasMaxLength(64);
        assetImportDiagnostic.Property(entity => entity.SourceKey).HasColumnName("source_key").HasMaxLength(256);
        assetImportDiagnostic.Property(entity => entity.ObjectName).HasColumnName("object_name").HasMaxLength(512);
        assetImportDiagnostic.Property(entity => entity.Message).HasColumnName("message").HasMaxLength(4000);
        assetImportDiagnostic.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        assetImportDiagnostic.HasIndex(entity => new { entity.RunId, entity.Severity, entity.Code, entity.Stage })
            .HasDatabaseName("ix_asset_import_diagnostics_filters");
        assetImportDiagnostic.HasIndex(entity => entity.SourceKey)
            .HasDatabaseName("ix_asset_import_diagnostics_source_key");
        assetImportDiagnostic.HasOne(entity => entity.Run).WithMany(entity => entity.Diagnostics)
            .HasForeignKey(entity => entity.RunId).OnDelete(DeleteBehavior.Cascade);
        assetImportDiagnostic.HasOne(entity => entity.WorkItem).WithMany(entity => entity.Diagnostics)
            .HasForeignKey(entity => entity.WorkItemId).OnDelete(DeleteBehavior.Cascade);

        var assetArtifact = modelBuilder.Entity<AssetArtifact>();
        assetArtifact.ToTable("asset_artifacts");
        assetArtifact.HasKey(entity => entity.Id);
        assetArtifact.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetArtifact.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetArtifact.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(64);
        assetArtifact.Property(entity => entity.SourceKey).HasColumnName("source_key").HasMaxLength(256);
        assetArtifact.Property(entity => entity.NormalizedSourceKey).HasColumnName("normalized_source_key").HasMaxLength(256);
        assetArtifact.Property(entity => entity.SourceHash).HasColumnName("source_hash").HasMaxLength(64);
        assetArtifact.Property(entity => entity.RecipeVersion).HasColumnName("recipe_version").HasMaxLength(128);
        assetArtifact.Property(entity => entity.BuildFingerprint).HasColumnName("build_fingerprint").HasMaxLength(64);
        assetArtifact.Property(entity => entity.ContentHash).HasColumnName("content_hash").HasMaxLength(64);
        assetArtifact.Property(entity => entity.OutputRoot).HasColumnName("output_root").HasMaxLength(1024);
        assetArtifact.Property(entity => entity.SchemaVersion).HasColumnName("schema_version");
        assetArtifact.Property(entity => entity.Protocol).HasColumnName("protocol");
        assetArtifact.Property(entity => entity.FileCount).HasColumnName("file_count");
        assetArtifact.Property(entity => entity.SizeBytes).HasColumnName("size_bytes");
        assetArtifact.Property(entity => entity.IntegrityStatus).HasColumnName("integrity_status").HasMaxLength(32);
        assetArtifact.Property(entity => entity.LastVerifiedAt).HasColumnName("last_verified_at");
        assetArtifact.Property(entity => entity.PublishingWorkItemId).HasColumnName("publishing_work_item_id");
        assetArtifact.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        assetArtifact.HasIndex(entity => new
            { entity.GameVersion, entity.Kind, entity.NormalizedSourceKey, entity.BuildFingerprint })
            .IsUnique().HasDatabaseName("ix_asset_artifacts_build");
        assetArtifact.HasIndex(entity => entity.OutputRoot).IsUnique()
            .HasDatabaseName("ix_asset_artifacts_output_root");
        assetArtifact.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.IntegrityStatus })
            .HasDatabaseName("ix_asset_artifacts_integrity");
        assetArtifact.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion)
            .OnDelete(DeleteBehavior.Restrict);
        assetArtifact.HasOne(entity => entity.PublishingWorkItem).WithMany()
            .HasForeignKey(entity => entity.PublishingWorkItemId).OnDelete(DeleteBehavior.Restrict);

        var assetArtifactFile = modelBuilder.Entity<AssetArtifactFile>();
        assetArtifactFile.ToTable("asset_artifact_files");
        assetArtifactFile.HasKey(entity => entity.Id);
        assetArtifactFile.Property(entity => entity.Id).HasColumnName("id");
        assetArtifactFile.Property(entity => entity.ArtifactId).HasColumnName("artifact_id");
        assetArtifactFile.Property(entity => entity.RelativePath).HasColumnName("relative_path").HasMaxLength(1024);
        assetArtifactFile.Property(entity => entity.PublicPath).HasColumnName("public_path").HasMaxLength(2048);
        assetArtifactFile.Property(entity => entity.Role).HasColumnName("role").HasMaxLength(64);
        assetArtifactFile.Property(entity => entity.MediaType).HasColumnName("media_type").HasMaxLength(128);
        assetArtifactFile.Property(entity => entity.SizeBytes).HasColumnName("size_bytes");
        assetArtifactFile.Property(entity => entity.Sha256).HasColumnName("sha256").HasMaxLength(64);
        assetArtifactFile.HasIndex(entity => new { entity.ArtifactId, entity.RelativePath }).IsUnique()
            .HasDatabaseName("ix_asset_artifact_files_path");
        assetArtifactFile.HasOne(entity => entity.Artifact).WithMany(entity => entity.Files)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Cascade);

        var assetArtifactDependency = modelBuilder.Entity<AssetArtifactDependency>();
        assetArtifactDependency.ToTable("asset_artifact_dependencies");
        assetArtifactDependency.HasKey(entity => entity.Id);
        assetArtifactDependency.Property(entity => entity.Id).HasColumnName("id");
        assetArtifactDependency.Property(entity => entity.ArtifactId).HasColumnName("artifact_id");
        assetArtifactDependency.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(64);
        assetArtifactDependency.Property(entity => entity.DependencyKey).HasColumnName("dependency_key").HasMaxLength(512);
        assetArtifactDependency.Property(entity => entity.ResolvedArtifactId).HasColumnName("resolved_artifact_id");
        assetArtifactDependency.Property(entity => entity.ResolvedSourceKey).HasColumnName("resolved_source_key").HasMaxLength(256);
        assetArtifactDependency.Property(entity => entity.BuildFingerprint).HasColumnName("build_fingerprint").HasMaxLength(64);
        assetArtifactDependency.Property(entity => entity.IsResolved).HasColumnName("is_resolved");
        assetArtifactDependency.HasIndex(entity => new { entity.ArtifactId, entity.Kind, entity.DependencyKey }).IsUnique()
            .HasDatabaseName("ix_asset_artifact_dependencies_key");
        assetArtifactDependency.HasIndex(entity => entity.ResolvedArtifactId)
            .HasDatabaseName("ix_asset_artifact_dependencies_resolved");
        assetArtifactDependency.HasOne(entity => entity.Artifact).WithMany(entity => entity.Dependencies)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Cascade);
        assetArtifactDependency.HasOne(entity => entity.ResolvedArtifact).WithMany()
            .HasForeignKey(entity => entity.ResolvedArtifactId).OnDelete(DeleteBehavior.Restrict);

        var assetRelease = modelBuilder.Entity<AssetRelease>();
        assetRelease.ToTable("asset_releases");
        assetRelease.HasKey(entity => entity.Id);
        assetRelease.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetRelease.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetRelease.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(120);
        assetRelease.Property(entity => entity.Notes).HasColumnName("notes").HasMaxLength(4000);
        assetRelease.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        assetRelease.Property(entity => entity.SnapshotHash).HasColumnName("snapshot_hash").HasMaxLength(64);
        assetRelease.Property(entity => entity.ValidationStatus).HasColumnName("validation_status").HasMaxLength(32);
        assetRelease.Property(entity => entity.ValidationIssuesJson).HasColumnName("validation_issues_json").HasColumnType("jsonb");
        assetRelease.Property(entity => entity.ValidatedSnapshotHash).HasColumnName("validated_snapshot_hash").HasMaxLength(64);
        assetRelease.Property(entity => entity.ValidationRequestedAt).HasColumnName("validation_requested_at");
        assetRelease.Property(entity => entity.ValidatedAt).HasColumnName("validated_at");
        assetRelease.Property(entity => entity.ManifestPath).HasColumnName("manifest_path").HasMaxLength(1024);
        assetRelease.Property(entity => entity.ManifestHash).HasColumnName("manifest_hash").HasMaxLength(64);
        assetRelease.Property(entity => entity.LoginSceneFileId).HasColumnName("login_scene_file_id");
        assetRelease.Property(entity => entity.LoginCameraSequence).HasColumnName("login_camera_sequence").HasMaxLength(256);
        assetRelease.Property(entity => entity.LoginMusicFileId).HasColumnName("login_music_file_id");
        assetRelease.Property(entity => entity.PrimaryLogoFileId).HasColumnName("primary_logo_file_id");
        assetRelease.Property(entity => entity.VersionLogoFileId).HasColumnName("version_logo_file_id");
        assetRelease.Property(entity => entity.LoadingArtworkFileId).HasColumnName("loading_artwork_file_id");
        assetRelease.Property(entity => entity.CharacterSelectionSceneFileId).HasColumnName("character_selection_scene_file_id");
        assetRelease.Property(entity => entity.CharacterSelectionCameraSequence).HasColumnName("character_selection_camera_sequence").HasMaxLength(256);
        assetRelease.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        assetRelease.Property(entity => entity.UpdatedAt).HasColumnName("updated_at");
        assetRelease.Property(entity => entity.PublishedAt).HasColumnName("published_at");
        assetRelease.Property(entity => entity.RetiredAt).HasColumnName("retired_at");
        assetRelease.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique()
            .HasDatabaseName("ix_asset_releases_version_name");
        assetRelease.HasIndex(entity => new { entity.GameVersion, entity.Status, entity.CreatedAt })
            .HasDatabaseName("ix_asset_releases_version_status");
        assetRelease.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.LoginSceneFileId)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.LoginMusicFileId)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.PrimaryLogoFileId)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.VersionLogoFileId)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.LoadingArtworkFileId)
            .OnDelete(DeleteBehavior.Restrict);
        assetRelease.HasOne<AssetArtifactFile>().WithMany().HasForeignKey(entity => entity.CharacterSelectionSceneFileId)
            .OnDelete(DeleteBehavior.Restrict);

        var assetReleaseArtifact = modelBuilder.Entity<AssetReleaseArtifact>();
        assetReleaseArtifact.ToTable("asset_release_artifacts");
        assetReleaseArtifact.HasKey(entity => new { entity.ReleaseId, entity.ArtifactId });
        assetReleaseArtifact.Property(entity => entity.ReleaseId).HasColumnName("release_id");
        assetReleaseArtifact.Property(entity => entity.ArtifactId).HasColumnName("artifact_id");
        assetReleaseArtifact.Property(entity => entity.IsRoot).HasColumnName("is_root");
        assetReleaseArtifact.HasIndex(entity => entity.ArtifactId).HasDatabaseName("ix_asset_release_artifacts_artifact");
        assetReleaseArtifact.HasOne(entity => entity.Release).WithMany(entity => entity.Artifacts)
            .HasForeignKey(entity => entity.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        assetReleaseArtifact.HasOne(entity => entity.Artifact).WithMany(entity => entity.Releases)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Restrict);

        var assetReleaseEvent = modelBuilder.Entity<AssetReleaseEvent>();
        assetReleaseEvent.ToTable("asset_release_events");
        assetReleaseEvent.HasKey(entity => entity.Id);
        assetReleaseEvent.Property(entity => entity.Id).HasColumnName("id");
        assetReleaseEvent.Property(entity => entity.ReleaseId).HasColumnName("release_id");
        assetReleaseEvent.Property(entity => entity.Action).HasColumnName("action").HasMaxLength(64);
        assetReleaseEvent.Property(entity => entity.DetailsJson).HasColumnName("details_json").HasColumnType("jsonb");
        assetReleaseEvent.Property(entity => entity.OccurredAt).HasColumnName("occurred_at");
        assetReleaseEvent.HasOne(entity => entity.Release).WithMany(entity => entity.Events)
            .HasForeignKey(entity => entity.ReleaseId).OnDelete(DeleteBehavior.Cascade);

        var assetReleasePointer = modelBuilder.Entity<AssetReleasePointer>();
        assetReleasePointer.ToTable("asset_release_pointers");
        assetReleasePointer.HasKey(entity => entity.GameVersion);
        assetReleasePointer.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetReleasePointer.Property(entity => entity.DesiredReleaseId).HasColumnName("desired_release_id");
        assetReleasePointer.Property(entity => entity.PublishedReleaseId).HasColumnName("published_release_id");
        assetReleasePointer.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        assetReleasePointer.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        assetReleasePointer.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        assetReleasePointer.Property(entity => entity.PublishedAt).HasColumnName("published_at");
        assetReleasePointer.HasOne<GameVersion>().WithOne().HasForeignKey<AssetReleasePointer>(entity => entity.GameVersion)
            .OnDelete(DeleteBehavior.Cascade);
        assetReleasePointer.HasOne(entity => entity.DesiredRelease).WithMany()
            .HasForeignKey(entity => entity.DesiredReleaseId).OnDelete(DeleteBehavior.Restrict);
        assetReleasePointer.HasOne(entity => entity.PublishedRelease).WithMany()
            .HasForeignKey(entity => entity.PublishedReleaseId).OnDelete(DeleteBehavior.Restrict);

        var assetCatalog = modelBuilder.Entity<AssetCatalog>();
        assetCatalog.ToTable("asset_catalogs");
        assetCatalog.HasKey(entity => entity.Id);
        assetCatalog.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        assetCatalog.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetCatalog.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(64);
        assetCatalog.Property(entity => entity.SourceFolder).HasColumnName("source_folder").HasMaxLength(256);
        assetCatalog.Property(entity => entity.SourceHash).HasColumnName("source_hash").HasMaxLength(64);
        assetCatalog.Property(entity => entity.SchemaVersion).HasColumnName("schema_version");
        assetCatalog.Property(entity => entity.Protocol).HasColumnName("protocol");
        assetCatalog.Property(entity => entity.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        assetCatalog.Property(entity => entity.IsActive).HasColumnName("is_active");
        assetCatalog.Property(entity => entity.PublishedAt).HasColumnName("published_at");
        assetCatalog.HasIndex(entity => new { entity.GameVersion, entity.Kind })
            .IsUnique()
            .HasFilter("is_active")
            .HasDatabaseName("ix_asset_catalogs_active_kind");

        var assetCatalogSource = modelBuilder.Entity<AssetCatalogSource>();
        assetCatalogSource.ToTable("asset_catalog_sources");
        assetCatalogSource.HasKey(entity => entity.Id);
        assetCatalogSource.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        assetCatalogSource.Property(entity => entity.CatalogId).HasColumnName("catalog_id");
        assetCatalogSource.Property(entity => entity.ArtifactId).HasColumnName("artifact_id");
        assetCatalogSource.Property(entity => entity.PublishingWorkItemId).HasColumnName("publishing_work_item_id");
        assetCatalogSource.Property(entity => entity.SourceKey).HasColumnName("source_key").HasMaxLength(256);
        assetCatalogSource.Property(entity => entity.NormalizedSourceKey).HasColumnName("normalized_source_key").HasMaxLength(256);
        assetCatalogSource.Property(entity => entity.SourceHash).HasColumnName("source_hash").HasMaxLength(64);
        assetCatalogSource.Property(entity => entity.ArtifactFingerprint).HasColumnName("artifact_fingerprint").HasMaxLength(64);
        assetCatalogSource.Property(entity => entity.OutputRoot).HasColumnName("output_root").HasMaxLength(1024);
        assetCatalogSource.Property(entity => entity.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        assetCatalogSource.Property(entity => entity.ReferencedOutputRootsJson).HasColumnName("referenced_output_roots_json").HasColumnType("jsonb");
        assetCatalogSource.Property(entity => entity.PublishedAt).HasColumnName("published_at");
        assetCatalogSource.Property(entity => entity.IsStale).HasColumnName("is_stale");
        assetCatalogSource.Property(entity => entity.StaleAt).HasColumnName("stale_at");
        assetCatalogSource.Property(entity => entity.StaleReasonsJson).HasColumnName("stale_reasons_json")
            .HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        assetCatalogSource.HasIndex(entity => new { entity.CatalogId, entity.NormalizedSourceKey }).IsUnique()
            .HasDatabaseName("ix_asset_catalog_sources_catalog_source");

        var assetCatalogSourceDependency = modelBuilder.Entity<AssetCatalogSourceDependency>();
        assetCatalogSourceDependency.ToTable("asset_catalog_source_dependencies");
        assetCatalogSourceDependency.HasKey(entity => entity.Id);
        assetCatalogSourceDependency.Property(entity => entity.Id).HasColumnName("id");
        assetCatalogSourceDependency.Property(entity => entity.SourceId).HasColumnName("source_id");
        assetCatalogSourceDependency.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(64);
        assetCatalogSourceDependency.Property(entity => entity.DependencyKey).HasColumnName("dependency_key").HasMaxLength(512);
        assetCatalogSourceDependency.Property(entity => entity.ResolvedSourceKey).HasColumnName("resolved_source_key").HasMaxLength(256);
        assetCatalogSourceDependency.Property(entity => entity.ArtifactFingerprint).HasColumnName("artifact_fingerprint").HasMaxLength(64);
        assetCatalogSourceDependency.Property(entity => entity.IsResolved).HasColumnName("is_resolved");
        assetCatalogSourceDependency.HasIndex(entity => new { entity.Kind, entity.DependencyKey })
            .HasDatabaseName("ix_asset_catalog_source_dependencies_key");
        assetCatalogSourceDependency.HasIndex(entity => new { entity.Kind, entity.ResolvedSourceKey })
            .HasDatabaseName("ix_asset_catalog_source_dependencies_source");
        assetCatalogSourceDependency.HasOne(entity => entity.Source).WithMany(entity => entity.Dependencies)
            .HasForeignKey(entity => entity.SourceId).OnDelete(DeleteBehavior.Cascade);
        assetCatalogSource.HasOne(entity => entity.Catalog).WithMany(entity => entity.Sources)
            .HasForeignKey(entity => entity.CatalogId).OnDelete(DeleteBehavior.Cascade);
        assetCatalogSource.HasOne(entity => entity.Artifact).WithMany(entity => entity.Publications)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Restrict);

        var assetCatalogGroup = modelBuilder.Entity<AssetCatalogGroup>();
        assetCatalogGroup.ToTable("asset_catalog_groups");
        assetCatalogGroup.HasKey(entity => entity.Id);
        assetCatalogGroup.Property(entity => entity.Id).HasColumnName("id");
        assetCatalogGroup.Property(entity => entity.CatalogId).HasColumnName("catalog_id");
        assetCatalogGroup.Property(entity => entity.SourceId).HasColumnName("source_id");
        assetCatalogGroup.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(256);
        assetCatalogGroup.Property(entity => entity.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        assetCatalogGroup.HasIndex(entity => new { entity.CatalogId, entity.Name })
            .HasDatabaseName("ix_asset_catalog_groups_catalog_name");
        assetCatalogGroup.HasOne(entity => entity.Catalog).WithMany(entity => entity.Groups)
            .HasForeignKey(entity => entity.CatalogId).OnDelete(DeleteBehavior.Cascade);
        assetCatalogGroup.HasOne(entity => entity.Source).WithMany(entity => entity.Groups)
            .HasForeignKey(entity => entity.SourceId).OnDelete(DeleteBehavior.Cascade);

        var assetCatalogItem = modelBuilder.Entity<AssetCatalogItem>();
        assetCatalogItem.ToTable("asset_catalog_items");
        assetCatalogItem.HasKey(entity => entity.Id);
        assetCatalogItem.Property(entity => entity.Id).HasColumnName("id");
        assetCatalogItem.Property(entity => entity.CatalogId).HasColumnName("catalog_id");
        assetCatalogItem.Property(entity => entity.SourceId).HasColumnName("source_id");
        assetCatalogItem.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(256);
        assetCatalogItem.Property(entity => entity.GroupName).HasColumnName("group_name").HasMaxLength(256);
        assetCatalogItem.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        assetCatalogItem.Property(entity => entity.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        assetCatalogItem.HasIndex(entity => new { entity.CatalogId, entity.Name })
            .HasDatabaseName("ix_asset_catalog_items_catalog_name");
        assetCatalogItem.HasIndex(entity => new { entity.CatalogId, entity.GroupName, entity.Name })
            .HasDatabaseName("ix_asset_catalog_items_catalog_group_name");
        assetCatalogItem.HasIndex(entity => new { entity.CatalogId, entity.Status })
            .HasDatabaseName("ix_asset_catalog_items_catalog_status");
        assetCatalogItem.HasOne(entity => entity.Catalog).WithMany(entity => entity.Items)
            .HasForeignKey(entity => entity.CatalogId).OnDelete(DeleteBehavior.Cascade);
        assetCatalogItem.HasOne(entity => entity.Source).WithMany(entity => entity.Items)
            .HasForeignKey(entity => entity.SourceId).OnDelete(DeleteBehavior.Cascade);

        var npcType = modelBuilder.Entity<NpcType>();
        npcType.ToTable("npc_types");
        npcType.HasKey(entity => new { entity.GameVersion, entity.Name });
        npcType.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npcType.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        npcType.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);

        var npcRace = modelBuilder.Entity<NpcRace>();
        npcRace.ToTable("npc_races");
        npcRace.HasKey(entity => new { entity.GameVersion, entity.Name });
        npcRace.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npcRace.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        npcRace.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);

        var npcSex = modelBuilder.Entity<NpcSex>();
        npcSex.ToTable("npc_sexes");
        npcSex.HasKey(entity => new { entity.GameVersion, entity.Name });
        npcSex.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npcSex.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        npcSex.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);

        var npcLookupImportRun = modelBuilder.Entity<NpcLookupImportRun>();
        npcLookupImportRun.ToTable("npc_lookup_import_runs");
        npcLookupImportRun.HasKey(entity => entity.Id);
        npcLookupImportRun.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        npcLookupImportRun.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npcLookupImportRun.Property(entity => entity.Kind).HasColumnName("kind").HasMaxLength(32);
        npcLookupImportRun.Property(entity => entity.Mode).HasColumnName("mode").HasMaxLength(32)
            .HasDefaultValue("add_missing");
        npcLookupImportRun.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        npcLookupImportRun.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        npcLookupImportRun.Property(entity => entity.StartedAt).HasColumnName("started_at");
        npcLookupImportRun.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        npcLookupImportRun.Property(entity => entity.TotalCount).HasColumnName("total_count");
        npcLookupImportRun.Property(entity => entity.InsertedCount).HasColumnName("inserted_count");
        npcLookupImportRun.Property(entity => entity.ExistingCount).HasColumnName("existing_count");
        npcLookupImportRun.Property(entity => entity.RestoredCount).HasColumnName("restored_count");
        npcLookupImportRun.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        npcLookupImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.RequestedAt })
            .HasDatabaseName("ix_npc_lookup_import_runs_recent");
        npcLookupImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind })
            .IsUnique().HasFilter("status IN ('queued', 'running')")
            .HasDatabaseName("ix_npc_lookup_import_runs_active");

        var npc = modelBuilder.Entity<Npc>();
        npc.ToTable("npcs", table => table.HasCheckConstraint("ck_npcs_level", "level BETWEEN 1 AND 255"));
        npc.HasKey(entity => new { entity.GameVersion, entity.Id });
        npc.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npc.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        npc.Property(entity => entity.AppearanceId).HasColumnName("appearance_id");
        npc.Property(entity => entity.Level).HasColumnName("level");
        npc.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(100).IsRequired(false);
        npc.Property(entity => entity.NpcTypeName).HasColumnName("npc_type_name").HasMaxLength(64);
        npc.Property(entity => entity.NpcRaceName).HasColumnName("npc_race_name").HasMaxLength(64).IsRequired(false);
        npc.Property(entity => entity.NpcSexName).HasColumnName("npc_sex_name").HasMaxLength(64);
        npc.HasIndex(entity => new { entity.GameVersion, entity.NpcTypeName }).HasDatabaseName("ix_npcs_npc_type_name");
        npc.HasIndex(entity => new { entity.GameVersion, entity.NpcRaceName }).HasDatabaseName("ix_npcs_npc_race_name");
        npc.HasIndex(entity => new { entity.GameVersion, entity.NpcSexName }).HasDatabaseName("ix_npcs_npc_sex_name");
        npc.HasOne(entity => entity.NpcType)
            .WithMany(entity => entity.Npcs)
            .HasForeignKey(entity => new { entity.GameVersion, entity.NpcTypeName })
            .OnDelete(DeleteBehavior.Restrict);
        npc.HasOne(entity => entity.NpcRace)
            .WithMany(entity => entity.Npcs)
            .HasForeignKey(entity => new { entity.GameVersion, entity.NpcRaceName })
            .OnDelete(DeleteBehavior.Restrict);
        npc.HasOne(entity => entity.NpcSex)
            .WithMany(entity => entity.Npcs)
            .HasForeignKey(entity => new { entity.GameVersion, entity.NpcSexName })
            .OnDelete(DeleteBehavior.Restrict);

        var npcStatus = modelBuilder.Entity<NpcStatus>();
        npcStatus.ToTable("npc_statuses");
        npcStatus.HasKey(entity => new { entity.GameVersion, entity.NpcId });
        npcStatus.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        npcStatus.Property(entity => entity.NpcId).HasColumnName("npc_id").ValueGeneratedNever();
        npcStatus.Property(entity => entity.Attackable).HasColumnName("attackable");
        npcStatus.Property(entity => entity.Targetable).HasColumnName("targetable");
        npcStatus.Property(entity => entity.Talkable).HasColumnName("talkable");
        npcStatus.Property(entity => entity.Undying).HasColumnName("undying");
        npcStatus.Property(entity => entity.ShowName).HasColumnName("show_name");
        npcStatus.Property(entity => entity.RandomWalk).HasColumnName("random_walk");
        npcStatus.Property(entity => entity.CanMove).HasColumnName("can_move");
        npcStatus.Property(entity => entity.NoSleepMode).HasColumnName("no_sleep_mode");
        npcStatus.Property(entity => entity.CanBeSown).HasColumnName("can_be_sown");
        npcStatus.HasOne(entity => entity.Npc)
            .WithOne(entity => entity.Status)
            .HasForeignKey<NpcStatus>(entity => new { entity.GameVersion, entity.NpcId })
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureNpcStats(modelBuilder.Entity<NpcStats>(), "npc_stats", entity => entity.Stats);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsVitals>(), "npc_stats_vitals", entity => entity.StatsVitals);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsAttack>(), "npc_stats_attack", entity => entity.StatsAttack);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsDefence>(), "npc_stats_defence", entity => entity.StatsDefence);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsSpeed>(), "npc_stats_speed", entity => entity.StatsSpeed);
        var npcStats = modelBuilder.Entity<NpcStats>();
        npcStats.Property(entity => entity.Str).HasColumnName("str");
        npcStats.Property(entity => entity.Int).HasColumnName("int");
        npcStats.Property(entity => entity.Dex).HasColumnName("dex");
        npcStats.Property(entity => entity.Wit).HasColumnName("wit");
        npcStats.Property(entity => entity.Con).HasColumnName("con");
        npcStats.Property(entity => entity.Men).HasColumnName("men");
        npcStats.Property(entity => entity.HitTime).HasColumnName("hit_time");
        var npcVitals = modelBuilder.Entity<NpcStatsVitals>();
        npcVitals.Property(entity => entity.Hp).HasColumnName("hp");
        npcVitals.Property(entity => entity.HpRegen).HasColumnName("hp_regen");
        npcVitals.Property(entity => entity.Mp).HasColumnName("mp");
        npcVitals.Property(entity => entity.MpRegen).HasColumnName("mp_regen");
        var npcAttack = modelBuilder.Entity<NpcStatsAttack>();
        npcAttack.Property(entity => entity.Physical).HasColumnName("physical");
        npcAttack.Property(entity => entity.Magical).HasColumnName("magical");
        npcAttack.Property(entity => entity.Random).HasColumnName("random");
        npcAttack.Property(entity => entity.Critical).HasColumnName("critical");
        npcAttack.Property(entity => entity.Accuracy).HasColumnName("accuracy");
        npcAttack.Property(entity => entity.AttackSpeed).HasColumnName("attack_speed");
        npcAttack.Property(entity => entity.ReuseDelay).HasColumnName("reuse_delay");
        npcAttack.Property(entity => entity.Type).HasColumnName("type").HasMaxLength(16);
        npcAttack.Property(entity => entity.Range).HasColumnName("range");
        npcAttack.Property(entity => entity.Distance).HasColumnName("distance");
        npcAttack.Property(entity => entity.Width).HasColumnName("width");
        var npcDefence = modelBuilder.Entity<NpcStatsDefence>();
        npcDefence.Property(entity => entity.Physical).HasColumnName("physical");
        npcDefence.Property(entity => entity.Magical).HasColumnName("magical");
        npcDefence.Property(entity => entity.Evasion).HasColumnName("evasion");
        npcDefence.Property(entity => entity.Shield).HasColumnName("shield");
        npcDefence.Property(entity => entity.ShieldRate).HasColumnName("shield_rate");
        var npcSpeed = modelBuilder.Entity<NpcStatsSpeed>();
        npcSpeed.Property(entity => entity.WalkGround).HasColumnName("walk_ground");
        npcSpeed.Property(entity => entity.RunGround).HasColumnName("run_ground");

        var itemType = modelBuilder.Entity<ItemType>();
        itemType.ToTable("item_types");
        itemType.HasKey(entity => new { entity.GameVersion, entity.Name });
        itemType.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemType.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        itemType.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        var itemAction = modelBuilder.Entity<ItemAction>();
        itemAction.ToTable("item_actions");
        itemAction.HasKey(entity => new { entity.GameVersion, entity.Name });
        itemAction.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemAction.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        itemAction.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        var itemBodyPart = modelBuilder.Entity<ItemBodyPart>();
        itemBodyPart.ToTable("item_body_parts");
        itemBodyPart.HasKey(entity => new { entity.GameVersion, entity.Name });
        itemBodyPart.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemBodyPart.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        itemBodyPart.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        var itemMaterial = modelBuilder.Entity<ItemMaterial>();
        itemMaterial.ToTable("item_materials");
        itemMaterial.HasKey(entity => new { entity.GameVersion, entity.Name });
        itemMaterial.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemMaterial.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        itemMaterial.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);
        var itemCrystalType = modelBuilder.Entity<ItemCrystalType>();
        itemCrystalType.ToTable("item_crystal_types");
        itemCrystalType.HasKey(entity => new { entity.GameVersion, entity.Name });
        itemCrystalType.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemCrystalType.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        itemCrystalType.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(64);

        var item = modelBuilder.Entity<Item>();
        item.ToTable("items");
        item.HasKey(entity => new { entity.GameVersion, entity.Id });
        item.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        item.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        item.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(100);
        item.Property(entity => entity.ItemTypeName).HasColumnName("item_type_name").HasMaxLength(64);
        item.Property(entity => entity.ItemActionName).HasColumnName("item_action_name").HasMaxLength(64);
        item.Property(entity => entity.ItemBodyPartName).HasColumnName("item_body_part_name").HasMaxLength(64);
        item.Property(entity => entity.ItemMaterialName).HasColumnName("item_material_name").HasMaxLength(64);
        item.Property(entity => entity.ItemCrystalTypeName).HasColumnName("item_crystal_type_name").HasMaxLength(64);
        item.Property(entity => entity.Icon).HasColumnName("icon").HasMaxLength(256);
        item.Property(entity => entity.WeaponType).HasColumnName("weapon_type").HasMaxLength(64);
        item.Property(entity => entity.ArmorType).HasColumnName("armor_type").HasMaxLength(64);
        item.Property(entity => entity.EtcItemType).HasColumnName("etcitem_type").HasMaxLength(64);
        item.Property(entity => entity.DamageRange).HasColumnName("damage_range").HasMaxLength(64);
        item.Property(entity => entity.DisplayId).HasColumnName("display_id");
        item.Property(entity => entity.CrystalCount).HasColumnName("crystal_count");
        item.Property(entity => entity.Weight).HasColumnName("weight");
        item.Property(entity => entity.Price).HasColumnName("price");
        item.Property(entity => entity.Soulshots).HasColumnName("soulshots");
        item.Property(entity => entity.Spiritshots).HasColumnName("spiritshots");
        item.Property(entity => entity.MpConsume).HasColumnName("mp_consume");
        item.Property(entity => entity.ReducedMpConsume).HasColumnName("reduced_mp_consume").HasMaxLength(64);
        item.Property(entity => entity.ReuseDelay).HasColumnName("reuse_delay");
        item.Property(entity => entity.RecipeId).HasColumnName("recipe_id");
        item.Property(entity => entity.Handler).HasColumnName("handler").HasMaxLength(64);
        item.Property(entity => entity.ItemSkill).HasColumnName("item_skill").HasMaxLength(64);
        item.Property(entity => entity.UseCondition).HasColumnName("use_condition").HasMaxLength(512);
        item.Property(entity => entity.ElementEnabled).HasColumnName("element_enabled");
        item.Property(entity => entity.EnchantEnabled).HasColumnName("enchant_enabled");
        item.Property(entity => entity.ForNpc).HasColumnName("for_npc");
        item.Property(entity => entity.ImmediateEffect).HasColumnName("immediate_effect");
        item.Property(entity => entity.IsAttackWeapon).HasColumnName("is_attack_weapon");
        item.Property(entity => entity.IsForceEquip).HasColumnName("is_force_equip");
        item.Property(entity => entity.IsDepositable).HasColumnName("is_depositable");
        item.Property(entity => entity.IsDestroyable).HasColumnName("is_destroyable");
        item.Property(entity => entity.IsDropable).HasColumnName("is_dropable");
        item.Property(entity => entity.IsMagicWeapon).HasColumnName("is_magic_weapon");
        item.Property(entity => entity.IsOlyRestricted).HasColumnName("is_oly_restricted");
        item.Property(entity => entity.IsQuestItem).HasColumnName("is_questitem");
        item.Property(entity => entity.IsSellable).HasColumnName("is_sellable");
        item.Property(entity => entity.IsStackable).HasColumnName("is_stackable");
        item.Property(entity => entity.IsTradable).HasColumnName("is_tradable");
        item.Property(entity => entity.UseWeaponSkillsOnly).HasColumnName("use_weapon_skills_only");
        item.HasIndex(entity => new { entity.GameVersion, entity.Name }).HasDatabaseName("ix_items_name");
        item.HasIndex(entity => new { entity.GameVersion, entity.ItemTypeName }).HasDatabaseName("ix_items_item_type_name");
        item.HasOne(entity => entity.ItemType).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemTypeName }).OnDelete(DeleteBehavior.Restrict);
        item.HasOne(entity => entity.ItemAction).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemActionName }).OnDelete(DeleteBehavior.Restrict);
        item.HasOne(entity => entity.ItemBodyPart).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemBodyPartName }).OnDelete(DeleteBehavior.Restrict);
        item.HasOne(entity => entity.ItemMaterial).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemMaterialName }).OnDelete(DeleteBehavior.Restrict);
        item.HasOne(entity => entity.ItemCrystalType).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemCrystalTypeName }).OnDelete(DeleteBehavior.Restrict);
        var itemStats = modelBuilder.Entity<ItemStats>();
        itemStats.ToTable("item_stats");
        itemStats.HasKey(entity => new { entity.GameVersion, entity.ItemId });
        itemStats.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemStats.Property(entity => entity.ItemId).HasColumnName("item_id").ValueGeneratedNever();
        itemStats.Property(entity => entity.AccuracyCombat).HasColumnName("accuracy_combat");
        itemStats.Property(entity => entity.CriticalRate).HasColumnName("critical_rate");
        itemStats.Property(entity => entity.MagicalAttack).HasColumnName("magical_attack");
        itemStats.Property(entity => entity.MagicalDefence).HasColumnName("magical_defence");
        itemStats.Property(entity => entity.MaximumMp).HasColumnName("maximum_mp");
        itemStats.Property(entity => entity.PhysicalAttack).HasColumnName("physical_attack");
        itemStats.Property(entity => entity.PhysicalAttackRange).HasColumnName("physical_attack_range");
        itemStats.Property(entity => entity.PhysicalAttackSpeed).HasColumnName("physical_attack_speed");
        itemStats.Property(entity => entity.PhysicalDefence).HasColumnName("physical_defence");
        itemStats.Property(entity => entity.Evasion).HasColumnName("evasion");
        itemStats.Property(entity => entity.ShieldRate).HasColumnName("shield_rate");
        itemStats.Property(entity => entity.RandomDamage).HasColumnName("random_damage");
        itemStats.Property(entity => entity.ShieldDefence).HasColumnName("shield_defence");
        itemStats.HasOne(entity => entity.Item).WithOne(entity => entity.Stats).HasForeignKey<ItemStats>(entity => new { entity.GameVersion, entity.ItemId }).OnDelete(DeleteBehavior.Cascade);
        var itemImportRun = modelBuilder.Entity<ItemImportRun>();
        itemImportRun.ToTable("item_import_runs");
        itemImportRun.HasKey(entity => entity.Id);
        itemImportRun.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        itemImportRun.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        itemImportRun.Property(entity => entity.Mode).HasColumnName("mode").HasMaxLength(32).HasDefaultValue("add_missing");
        itemImportRun.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        itemImportRun.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        itemImportRun.Property(entity => entity.StartedAt).HasColumnName("started_at");
        itemImportRun.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        itemImportRun.Property(entity => entity.TotalCount).HasColumnName("total_count");
        itemImportRun.Property(entity => entity.InsertedCount).HasColumnName("inserted_count");
        itemImportRun.Property(entity => entity.ExistingCount).HasColumnName("existing_count");
        itemImportRun.Property(entity => entity.RestoredCount).HasColumnName("restored_count");
        itemImportRun.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        itemImportRun.HasIndex(entity => new { entity.GameVersion, entity.RequestedAt }).HasDatabaseName("ix_item_import_runs_recent");
        itemImportRun.HasIndex(entity => entity.GameVersion).IsUnique().HasFilter("status IN ('queued', 'running')").HasDatabaseName("ix_item_import_runs_active");

        var skillImportRun = modelBuilder.Entity<SkillImportRun>();
        skillImportRun.ToTable("skill_import_runs");
        skillImportRun.HasKey(entity => entity.Id);
        skillImportRun.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        skillImportRun.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        skillImportRun.Property(entity => entity.Mode).HasColumnName("mode").HasMaxLength(32).HasDefaultValue("add_missing");
        skillImportRun.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32);
        skillImportRun.Property(entity => entity.RequestedAt).HasColumnName("requested_at");
        skillImportRun.Property(entity => entity.StartedAt).HasColumnName("started_at");
        skillImportRun.Property(entity => entity.FinishedAt).HasColumnName("finished_at");
        skillImportRun.Property(entity => entity.TotalCount).HasColumnName("total_count");
        skillImportRun.Property(entity => entity.InsertedCount).HasColumnName("inserted_count");
        skillImportRun.Property(entity => entity.ExistingCount).HasColumnName("existing_count");
        skillImportRun.Property(entity => entity.RestoredCount).HasColumnName("restored_count");
        skillImportRun.Property(entity => entity.Error).HasColumnName("error").HasMaxLength(4000);
        skillImportRun.HasIndex(entity => new { entity.GameVersion, entity.RequestedAt }).HasDatabaseName("ix_skill_import_runs_recent");
        skillImportRun.HasIndex(entity => entity.GameVersion).IsUnique().HasFilter("status IN ('queued', 'running')").HasDatabaseName("ix_skill_import_runs_active");

        var skillIcon = modelBuilder.Entity<SkillIcon>();
        skillIcon.ToTable(
            "skill_icons",
            table => table.HasCheckConstraint("ck_skill_icons_level", "level BETWEEN 1 AND 255"));
        skillIcon.HasKey(entity => new { entity.GameVersion, entity.SkillId, entity.Level });
        skillIcon.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        skillIcon.Property(entity => entity.SkillId).HasColumnName("skill_id").ValueGeneratedNever();
        skillIcon.Property(entity => entity.Level).HasColumnName("level").ValueGeneratedNever();
        skillIcon.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);

        var skillOperateType = modelBuilder.Entity<SkillOperateType>();
        skillOperateType.ToTable("skill_operate_types");
        skillOperateType.HasKey(entity => new { entity.GameVersion, entity.Id });
        skillOperateType.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        skillOperateType.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        skillOperateType.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        skillOperateType.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_skill_operate_types_name");

        var skillTargetType = modelBuilder.Entity<SkillTargetType>();
        skillTargetType.ToTable("skill_target_types");
        skillTargetType.HasKey(entity => new { entity.GameVersion, entity.Id });
        skillTargetType.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        skillTargetType.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        skillTargetType.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(64);
        skillTargetType.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_skill_target_types_name");

        var skill = modelBuilder.Entity<Skill>();
        skill.ToTable("skills", table => table.HasCheckConstraint("ck_skills_levels", "levels BETWEEN 1 AND 255"));
        skill.HasKey(entity => new { entity.GameVersion, entity.Id });
        skill.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        skill.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        skill.Property(entity => entity.Levels).HasColumnName("levels");
        skill.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(100);
        skill.Property(entity => entity.SkillOperateTypeId).HasColumnName("skill_operate_type_id").IsRequired(false);
        skill.Property(entity => entity.SkillTargetTypeId).HasColumnName("skill_target_type_id").IsRequired(false);
        skill.HasIndex(entity => entity.SkillOperateTypeId).HasDatabaseName("ix_skills_skill_operate_type_id");
        skill.HasIndex(entity => entity.SkillTargetTypeId).HasDatabaseName("ix_skills_skill_target_type_id");
        skill.HasOne(entity => entity.SkillOperateType)
            .WithMany(entity => entity.Skills)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillOperateTypeId })
            .OnDelete(DeleteBehavior.Restrict);
        skill.HasOne(entity => entity.SkillTargetType)
            .WithMany(entity => entity.Skills)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillTargetTypeId })
            .OnDelete(DeleteBehavior.Restrict);
        skill.HasMany(entity => entity.SkillIcons)
            .WithOne(entity => entity.Skill)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillId })
            .OnDelete(DeleteBehavior.Cascade);

        playerRace.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerSex.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerClass.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerFace.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerHairStyle.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerHairColor.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        playerImportRun.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcRace.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcSex.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcLookupImportRun.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillImportRun.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npc.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcStatus.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcStats.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcVitals.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcAttack.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcDefence.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcSpeed.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemAction.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemBodyPart.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemMaterial.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemCrystalType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        item.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemStats.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemImportRun.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillOperateType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillTargetType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skill.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillIcon.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        assetImportRun.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        assetImportWorkItem.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        assetCatalog.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);

        foreach (var entityType in new[]
        {
            typeof(PlayerRace), typeof(PlayerSex), typeof(PlayerClass), typeof(PlayerImportRun), typeof(PlayerFace),
            typeof(PlayerHairStyle), typeof(PlayerHairColor), typeof(NpcType), typeof(NpcRace),
            typeof(NpcSex), typeof(Npc), typeof(NpcStatus), typeof(NpcStats), typeof(NpcStatsVitals),
            typeof(NpcStatsAttack), typeof(NpcStatsDefence), typeof(NpcStatsSpeed), typeof(NpcLookupImportRun),
            typeof(ItemType), typeof(ItemAction), typeof(ItemBodyPart), typeof(ItemMaterial), typeof(ItemCrystalType),
            typeof(Item), typeof(ItemStats), typeof(ItemImportRun), typeof(SkillImportRun),
            typeof(SkillOperateType), typeof(SkillTargetType),
            typeof(Skill), typeof(SkillIcon), typeof(AssetImportRun), typeof(AssetImportWorkItem),
            typeof(AssetCatalog)
        })
        {
            modelBuilder.Entity(entityType).Property<string>(nameof(PlayerRace.GameVersion))
                .HasDefaultValue("interlude");
        }
    }

    private static void ConfigureNpcStats<TEntity>(
        EntityTypeBuilder<TEntity> stats,
        string tableName,
        Expression<Func<Npc, TEntity?>> navigation)
        where TEntity : class, INpcStatsRecord
    {
        stats.ToTable(tableName);
        stats.HasKey(entity => new { entity.GameVersion, entity.NpcId });
        stats.Property(entity => entity.GameVersion).HasColumnName("game_version").HasMaxLength(32);
        stats.Property(entity => entity.NpcId).HasColumnName("npc_id").ValueGeneratedNever();
        stats.HasOne(entity => entity.Npc)
            .WithOne(navigation)
            .HasForeignKey<TEntity>(entity => new { entity.GameVersion, entity.NpcId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
