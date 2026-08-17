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
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Item_Armor> ItemArmor => Set<Item_Armor>();
    public DbSet<Item_Weapon> ItemWeapons => Set<Item_Weapon>();
    public DbSet<Item_Arrow> ItemArrows => Set<Item_Arrow>();
    public DbSet<Item_Material> ItemMaterialDefinitions => Set<Item_Material>();
    public DbSet<Item_Potion> ItemPotions => Set<Item_Potion>();
    public DbSet<Item_Recipe> ItemRecipes => Set<Item_Recipe>();
    public DbSet<Item_Enchant> ItemEnchants => Set<Item_Enchant>();
    public DbSet<Item_Scroll> ItemScrolls => Set<Item_Scroll>();
    public DbSet<Item_PetCollar> ItemPetCollars => Set<Item_PetCollar>();
    public DbSet<Item_Etc> ItemEtc => Set<Item_Etc>();
    public DbSet<ItemBehaviorAvailability> ItemBehaviorAvailabilities => Set<ItemBehaviorAvailability>();
    public DbSet<ItemCondition> ItemConditions => Set<ItemCondition>();
    public DbSet<ItemCondition_Player> ItemConditionPlayers => Set<ItemCondition_Player>();
    public DbSet<ItemSet> ItemSets => Set<ItemSet>();
    public DbSet<ItemSetBodyPart> ItemSetBodyParts => Set<ItemSetBodyPart>();
    public DbSet<ItemSetSkill> ItemSetSkills => Set<ItemSetSkill>();
    public DbSet<ItemSetStats> ItemSetStats => Set<ItemSetStats>();
    public DbSet<ItemAttackGeometry> ItemAttackGeometries => Set<ItemAttackGeometry>();
    public DbSet<ItemSkill> ItemSkills => Set<ItemSkill>();
    public DbSet<ItemStats> ItemStats => Set<ItemStats>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<ItemAction> ItemActions => Set<ItemAction>();
    public DbSet<ItemBodyPart> ItemBodyParts => Set<ItemBodyPart>();
    public DbSet<ItemMaterial> ItemMaterials => Set<ItemMaterial>();
    public DbSet<ItemCrystalType> ItemCrystalTypes => Set<ItemCrystalType>();
    public DbSet<ItemHandler> ItemHandlers => Set<ItemHandler>();
    public DbSet<ItemSkillType> ItemSkillTypes => Set<ItemSkillType>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillIcon> SkillIcons => Set<SkillIcon>();
    public DbSet<SkillOperateType> SkillOperateTypes => Set<SkillOperateType>();
    public DbSet<SkillTargetType> SkillTargetTypes => Set<SkillTargetType>();
    public DbSet<PlayerRace> PlayerRaces => Set<PlayerRace>();
    public DbSet<PlayerSex> PlayerSexes => Set<PlayerSex>();
    public DbSet<PlayerClass> PlayerClasses => Set<PlayerClass>();
    public DbSet<PlayerFace> PlayerFaces => Set<PlayerFace>();
    public DbSet<PlayerHairStyle> PlayerHairStyles => Set<PlayerHairStyle>();
    public DbSet<PlayerHairColor> PlayerHairColors => Set<PlayerHairColor>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ContentImportRun> ContentImportRuns => Set<ContentImportRun>();
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
        gameVersion.HasIndex(entity => entity.DisplayName).IsUnique();
        var playerRace = modelBuilder.Entity<PlayerRace>();
        playerRace.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_player_races_name");

        var playerSex = modelBuilder.Entity<PlayerSex>();
        playerSex.HasIndex(entity => new { entity.GameVersion, entity.Name }).IsUnique().HasDatabaseName("ix_player_sexes_name");

        var playerClass = modelBuilder.Entity<PlayerClass>();
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
        playerFace.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerFaces)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerFace.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerFaces)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var playerHairStyle = modelBuilder.Entity<PlayerHairStyle>();
        playerHairStyle.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerHairStyles)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerHairStyle.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerHairStyles)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var playerHairColor = modelBuilder.Entity<PlayerHairColor>();
        playerHairColor.HasOne(entity => entity.PlayerRace).WithMany(entity => entity.PlayerHairColors)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerRaceId }).OnDelete(DeleteBehavior.Restrict);
        playerHairColor.HasOne(entity => entity.PlayerSex).WithMany(entity => entity.PlayerHairColors)
            .HasForeignKey(entity => new { entity.GameVersion, entity.PlayerSexId }).OnDelete(DeleteBehavior.Restrict);

        var importJob = modelBuilder.Entity<ImportJob>();
        importJob.HasDiscriminator(entity => entity.Category)
            .HasValue<ContentImportRun>("content")
            .HasValue<AssetImportRun>("asset");
        importJob.HasIndex(entity => new { entity.GameVersion, entity.RequestedAt })
            .HasDatabaseName("ix_import_jobs_recent");
        importJob.HasIndex(entity => new { entity.GameVersion, entity.Category, entity.Kind, entity.RequestedAt })
            .HasDatabaseName("ix_import_jobs_target_recent");
        importJob.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion)
            .OnDelete(DeleteBehavior.Restrict);

        var contentImportRun = modelBuilder.Entity<ContentImportRun>();
        contentImportRun.Property(entity => entity.Mode).HasDefaultValue("add_missing");
        contentImportRun.HasIndex(entity => new { entity.GameVersion, entity.ConcurrencyKey }).IsUnique()
            .HasFilter("category = 'content' AND status IN ('queued', 'running')")
            .HasDatabaseName("ix_import_jobs_active_content_target");

        var assetImportRun = modelBuilder.Entity<AssetImportRun>();
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.RequestedAt })
            .HasDatabaseName("ix_asset_import_runs_kind_requested");
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind }).IsUnique()
            .HasFilter("trigger_type = 'full_scan' AND status IN ('queued', 'discovering', 'running')")
            .HasDatabaseName("ix_asset_import_runs_active_full_scan_kind");
        assetImportRun.HasIndex(entity => new { entity.GameVersion, entity.Kind, entity.NormalizedRequestedSourceKey }).IsUnique()
            .HasFilter("trigger_type = 'single_file' AND status IN ('queued', 'discovering', 'running')")
            .HasDatabaseName("ix_asset_import_runs_active_single_source");

        var assetImportWorkItem = modelBuilder.Entity<AssetImportWorkItem>();
        assetImportWorkItem.HasIndex(entity => new { entity.RunId, entity.NormalizedSourceKey }).IsUnique()
            .HasDatabaseName("ix_asset_import_work_items_run_source");
        assetImportWorkItem.HasIndex(entity => new { entity.RunId, entity.Status })
            .HasDatabaseName("ix_asset_import_work_items_run_status");
        assetImportWorkItem.HasOne(entity => entity.Run).WithMany(entity => entity.WorkItems)
            .HasForeignKey(entity => entity.RunId).OnDelete(DeleteBehavior.Cascade);

        var assetImportDiagnostic = modelBuilder.Entity<AssetImportDiagnostic>();
        assetImportDiagnostic.HasIndex(entity => new { entity.RunId, entity.Severity, entity.Code, entity.Stage })
            .HasDatabaseName("ix_asset_import_diagnostics_filters");
        assetImportDiagnostic.HasIndex(entity => entity.SourceKey)
            .HasDatabaseName("ix_asset_import_diagnostics_source_key");
        assetImportDiagnostic.HasOne(entity => entity.Run).WithMany(entity => entity.Diagnostics)
            .HasForeignKey(entity => entity.RunId).OnDelete(DeleteBehavior.Cascade);
        assetImportDiagnostic.HasOne(entity => entity.WorkItem).WithMany(entity => entity.Diagnostics)
            .HasForeignKey(entity => entity.WorkItemId).OnDelete(DeleteBehavior.Cascade);

        var assetArtifact = modelBuilder.Entity<AssetArtifact>();
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
        assetArtifactFile.HasIndex(entity => new { entity.ArtifactId, entity.RelativePath }).IsUnique()
            .HasDatabaseName("ix_asset_artifact_files_path");
        assetArtifactFile.HasOne(entity => entity.Artifact).WithMany(entity => entity.Files)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Cascade);

        var assetArtifactDependency = modelBuilder.Entity<AssetArtifactDependency>();
        assetArtifactDependency.HasIndex(entity => new { entity.ArtifactId, entity.Kind, entity.DependencyKey }).IsUnique()
            .HasDatabaseName("ix_asset_artifact_dependencies_key");
        assetArtifactDependency.HasIndex(entity => entity.ResolvedArtifactId)
            .HasDatabaseName("ix_asset_artifact_dependencies_resolved");
        assetArtifactDependency.HasOne(entity => entity.Artifact).WithMany(entity => entity.Dependencies)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Cascade);
        assetArtifactDependency.HasOne(entity => entity.ResolvedArtifact).WithMany()
            .HasForeignKey(entity => entity.ResolvedArtifactId).OnDelete(DeleteBehavior.Restrict);

        var assetRelease = modelBuilder.Entity<AssetRelease>();
        assetRelease.Property(entity => entity.ValidationIssuesJson).HasColumnType("jsonb");
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
        assetReleaseArtifact.HasIndex(entity => entity.ArtifactId).HasDatabaseName("ix_asset_release_artifacts_artifact");
        assetReleaseArtifact.HasOne(entity => entity.Release).WithMany(entity => entity.Artifacts)
            .HasForeignKey(entity => entity.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        assetReleaseArtifact.HasOne(entity => entity.Artifact).WithMany(entity => entity.Releases)
            .HasForeignKey(entity => entity.ArtifactId).OnDelete(DeleteBehavior.Restrict);

        var assetReleaseEvent = modelBuilder.Entity<AssetReleaseEvent>();
        assetReleaseEvent.Property(entity => entity.DetailsJson).HasColumnType("jsonb");
        assetReleaseEvent.HasOne(entity => entity.Release).WithMany(entity => entity.Events)
            .HasForeignKey(entity => entity.ReleaseId).OnDelete(DeleteBehavior.Cascade);

        var assetReleasePointer = modelBuilder.Entity<AssetReleasePointer>();
        assetReleasePointer.HasOne<GameVersion>().WithOne().HasForeignKey<AssetReleasePointer>(entity => entity.GameVersion)
            .OnDelete(DeleteBehavior.Cascade);
        assetReleasePointer.HasOne(entity => entity.DesiredRelease).WithMany()
            .HasForeignKey(entity => entity.DesiredReleaseId).OnDelete(DeleteBehavior.Restrict);
        assetReleasePointer.HasOne(entity => entity.PublishedRelease).WithMany()
            .HasForeignKey(entity => entity.PublishedReleaseId).OnDelete(DeleteBehavior.Restrict);

        var assetCatalog = modelBuilder.Entity<AssetCatalog>();
        assetCatalog.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
        assetCatalog.HasIndex(entity => new { entity.GameVersion, entity.Kind })
            .IsUnique()
            .HasFilter("is_active")
            .HasDatabaseName("ix_asset_catalogs_active_kind");

        var assetCatalogSource = modelBuilder.Entity<AssetCatalogSource>();
        assetCatalogSource.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
        assetCatalogSource.Property(entity => entity.ReferencedOutputRootsJson).HasColumnType("jsonb");
        assetCatalogSource.Property(entity => entity.StaleReasonsJson)
            .HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        assetCatalogSource.HasIndex(entity => new { entity.CatalogId, entity.NormalizedSourceKey }).IsUnique()
            .HasDatabaseName("ix_asset_catalog_sources_catalog_source");

        var assetCatalogSourceDependency = modelBuilder.Entity<AssetCatalogSourceDependency>();
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
        assetCatalogGroup.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
        assetCatalogGroup.HasIndex(entity => new { entity.CatalogId, entity.Name })
            .HasDatabaseName("ix_asset_catalog_groups_catalog_name");
        assetCatalogGroup.HasOne(entity => entity.Catalog).WithMany(entity => entity.Groups)
            .HasForeignKey(entity => entity.CatalogId).OnDelete(DeleteBehavior.Cascade);
        assetCatalogGroup.HasOne(entity => entity.Source).WithMany(entity => entity.Groups)
            .HasForeignKey(entity => entity.SourceId).OnDelete(DeleteBehavior.Cascade);

        var assetCatalogItem = modelBuilder.Entity<AssetCatalogItem>();
        assetCatalogItem.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
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

        var npcRace = modelBuilder.Entity<NpcRace>();

        var npcSex = modelBuilder.Entity<NpcSex>();

        var npc = modelBuilder.Entity<Npc>();
        npc.ToTable("npcs", table => table.HasCheckConstraint("ck_npcs_level", "level BETWEEN 1 AND 255"));
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
        npcStatus.HasOne(entity => entity.Npc)
            .WithOne(entity => entity.Status)
            .HasForeignKey<NpcStatus>(entity => new { entity.GameVersion, entity.NpcId })
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureNpcStats(modelBuilder.Entity<NpcStats>(), entity => entity.Stats);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsVitals>(), entity => entity.StatsVitals);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsAttack>(), entity => entity.StatsAttack);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsDefence>(), entity => entity.StatsDefence);
        ConfigureNpcStats(modelBuilder.Entity<NpcStatsSpeed>(), entity => entity.StatsSpeed);
        var npcStats = modelBuilder.Entity<NpcStats>();
        var npcVitals = modelBuilder.Entity<NpcStatsVitals>();
        var npcAttack = modelBuilder.Entity<NpcStatsAttack>();
        var npcDefence = modelBuilder.Entity<NpcStatsDefence>();
        var npcSpeed = modelBuilder.Entity<NpcStatsSpeed>();

        var itemType = modelBuilder.Entity<ItemType>();
        var itemAction = modelBuilder.Entity<ItemAction>();
        var itemBodyPart = modelBuilder.Entity<ItemBodyPart>();
        var itemMaterial = modelBuilder.Entity<ItemMaterial>();
        var itemCrystalType = modelBuilder.Entity<ItemCrystalType>();
        var itemHandler = modelBuilder.Entity<ItemHandler>();
        var itemSkillType = modelBuilder.Entity<ItemSkillType>();

        itemType.HasIndex(entity => new { entity.GameVersion, entity.ParentTypeName })
            .HasDatabaseName("ix_item_types_parent_type_name");
        itemType.HasOne(entity => entity.ParentType).WithMany(entity => entity.ChildTypes)
            .HasForeignKey(entity => new { entity.GameVersion, entity.ParentTypeName })
            .OnDelete(DeleteBehavior.Restrict);

        var item = modelBuilder.Entity<Item>();
        item.HasIndex(entity => new { entity.GameVersion, entity.Name }).HasDatabaseName("ix_items_name");
        item.HasIndex(entity => new { entity.GameVersion, entity.ItemTypeName }).HasDatabaseName("ix_items_item_type_name");
        item.HasOne(entity => entity.ItemType).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemTypeName }).OnDelete(DeleteBehavior.Restrict);
        item.HasOne(entity => entity.ItemMaterial).WithMany(entity => entity.Items).HasForeignKey(entity => new { entity.GameVersion, entity.ItemMaterialName }).OnDelete(DeleteBehavior.Restrict);
        ConfigureItemFamily(modelBuilder.Entity<Item_Armor>(), entity => entity.Armor);
        ConfigureItemFamily(modelBuilder.Entity<Item_Weapon>(), entity => entity.Weapon);
        ConfigureItemFamily(modelBuilder.Entity<Item_Arrow>(), entity => entity.Arrow);
        ConfigureItemFamily(modelBuilder.Entity<Item_Material>(), entity => entity.Material);
        ConfigureItemFamily(modelBuilder.Entity<Item_Potion>(), entity => entity.Potion);
        ConfigureItemFamily(modelBuilder.Entity<Item_Recipe>(), entity => entity.Recipe);
        ConfigureItemFamily(modelBuilder.Entity<Item_Enchant>(), entity => entity.Enchant);
        ConfigureItemFamily(modelBuilder.Entity<Item_Scroll>(), entity => entity.Scroll);
        ConfigureItemFamily(modelBuilder.Entity<Item_PetCollar>(), entity => entity.PetCollar);
        ConfigureItemFamily(modelBuilder.Entity<Item_Etc>(), entity => entity.Etc);
        var itemBehaviorAvailability = modelBuilder.Entity<ItemBehaviorAvailability>();
        itemBehaviorAvailability.HasOne(entity => entity.Item).WithOne(entity => entity.BehaviorAvailability)
            .HasForeignKey<ItemBehaviorAvailability>(entity => new { entity.GameVersion, entity.ItemId })
            .OnDelete(DeleteBehavior.Cascade);
        var itemCondition = modelBuilder.Entity<ItemCondition>();
        itemCondition.HasOne(entity => entity.Item).WithOne(entity => entity.Condition)
            .HasForeignKey<ItemCondition>(entity => new { entity.GameVersion, entity.ItemId })
            .OnDelete(DeleteBehavior.Cascade);
        var itemConditionPlayer = modelBuilder.Entity<ItemCondition_Player>();
        itemConditionPlayer.HasOne(entity => entity.Condition).WithOne(entity => entity.Player)
            .HasForeignKey<ItemCondition_Player>(entity => new { entity.GameVersion, entity.ItemId })
            .OnDelete(DeleteBehavior.Cascade);
        var itemSet = modelBuilder.Entity<ItemSet>();
        var itemSetBodyPart = modelBuilder.Entity<ItemSetBodyPart>();
        itemSetBodyPart.HasOne(entity => entity.ItemSet).WithMany(entity => entity.BodyParts)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SetId }).OnDelete(DeleteBehavior.Cascade);
        itemSetBodyPart.HasOne(entity => entity.BodyPart).WithMany()
            .HasForeignKey(entity => new { entity.GameVersion, entity.BodyPartName }).OnDelete(DeleteBehavior.Restrict);
        var itemSetSkill = modelBuilder.Entity<ItemSetSkill>();
        itemSetSkill.HasOne(entity => entity.ItemSet).WithMany(entity => entity.Skills)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SetId }).OnDelete(DeleteBehavior.Cascade);
        itemSetSkill.HasOne(entity => entity.Skill).WithMany()
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillId }).OnDelete(DeleteBehavior.Restrict);
        var itemSetStats = modelBuilder.Entity<ItemSetStats>();
        itemSetStats.HasOne(entity => entity.ItemSet).WithOne(entity => entity.Stats)
            .HasForeignKey<ItemSetStats>(entity => new { entity.GameVersion, entity.SetId }).OnDelete(DeleteBehavior.Cascade);
        ConfigureItemLookups(modelBuilder.Entity<Item_Armor>());
        ConfigureItemLookups(modelBuilder.Entity<Item_Weapon>());
        ConfigureItemLookups(modelBuilder.Entity<Item_Arrow>());
        ConfigureActionAndHandler(modelBuilder.Entity<Item_Potion>());
        ConfigureActionAndHandler(modelBuilder.Entity<Item_Recipe>());
        ConfigureActionAndHandler(modelBuilder.Entity<Item_Enchant>());
        ConfigureActionAndHandler(modelBuilder.Entity<Item_Scroll>());
        ConfigureActionAndHandler(modelBuilder.Entity<Item_PetCollar>());
        ConfigureItemLookups(modelBuilder.Entity<Item_Etc>());
        modelBuilder.Entity<Item_Etc>().HasOne(entity => entity.ItemHandler).WithMany().HasForeignKey(entity => new { entity.GameVersion, entity.HandlerName }).OnDelete(DeleteBehavior.Restrict);
        var itemAttackGeometry = modelBuilder.Entity<ItemAttackGeometry>();
        itemAttackGeometry.HasOne(entity => entity.Item).WithOne(entity => entity.AttackGeometry).HasForeignKey<ItemAttackGeometry>(entity => new { entity.GameVersion, entity.ItemId }).OnDelete(DeleteBehavior.Cascade);
        var itemSkill = modelBuilder.Entity<ItemSkill>();
        itemSkill.HasIndex(entity => new { entity.GameVersion, entity.ItemSkillTypeName }).HasDatabaseName("ix_item_skills_type_name");
        itemSkill.HasOne(entity => entity.Item).WithMany(entity => entity.Skills).HasForeignKey(entity => new { entity.GameVersion, entity.ItemId }).OnDelete(DeleteBehavior.Cascade);
        itemSkill.HasOne(entity => entity.ItemSkillType).WithMany(entity => entity.ItemSkills).HasForeignKey(entity => new { entity.GameVersion, entity.ItemSkillTypeName }).OnDelete(DeleteBehavior.Restrict);
        var itemStats = modelBuilder.Entity<ItemStats>();
        itemStats.HasOne(entity => entity.Item).WithOne(entity => entity.Stats).HasForeignKey<ItemStats>(entity => new { entity.GameVersion, entity.ItemId }).OnDelete(DeleteBehavior.Cascade);
        var skillIcon = modelBuilder.Entity<SkillIcon>();
        skillIcon.ToTable(
            "skill_icons",
            table => table.HasCheckConstraint("ck_skill_icons_level", "level BETWEEN 1 AND 255"));

        var skillOperateType = modelBuilder.Entity<SkillOperateType>();

        var skillTargetType = modelBuilder.Entity<SkillTargetType>();

        var skill = modelBuilder.Entity<Skill>();
        skill.ToTable("skills", table => table.HasCheckConstraint("ck_skills_levels", "levels BETWEEN 1 AND 255"));
        skill.HasIndex(entity => new { entity.GameVersion, entity.SkillOperateTypeName })
            .HasDatabaseName("ix_skills_skill_operate_type_name");
        skill.HasIndex(entity => new { entity.GameVersion, entity.SkillTargetTypeName })
            .HasDatabaseName("ix_skills_skill_target_type_name");
        skill.HasOne(entity => entity.SkillOperateType)
            .WithMany(entity => entity.Skills)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillOperateTypeName })
            .OnDelete(DeleteBehavior.Restrict);
        skill.HasOne(entity => entity.SkillTargetType)
            .WithMany(entity => entity.Skills)
            .HasForeignKey(entity => new { entity.GameVersion, entity.SkillTargetTypeName })
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
        npcType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcRace.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        npcSex.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
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
        itemHandler.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSkillType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        item.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemBehaviorAvailability.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemCondition.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemConditionPlayer.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSet.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSetBodyPart.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSetSkill.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSetStats.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemSkill.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        itemStats.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillOperateType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillTargetType.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skill.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        skillIcon.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        assetImportWorkItem.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);
        assetCatalog.HasOne<GameVersion>().WithMany().HasForeignKey(entity => entity.GameVersion).OnDelete(DeleteBehavior.Restrict);

        foreach (var entityType in new[]
        {
            typeof(PlayerRace), typeof(PlayerSex), typeof(PlayerClass), typeof(PlayerFace),
            typeof(PlayerHairStyle), typeof(PlayerHairColor), typeof(NpcType), typeof(NpcRace),
            typeof(NpcSex), typeof(Npc), typeof(NpcStatus), typeof(NpcStats), typeof(NpcStatsVitals),
            typeof(NpcStatsAttack), typeof(NpcStatsDefence), typeof(NpcStatsSpeed),
            typeof(ItemType), typeof(ItemAction), typeof(ItemBodyPart), typeof(ItemMaterial), typeof(ItemCrystalType),
            typeof(ItemHandler), typeof(ItemSkillType), typeof(Item), typeof(Item_Armor), typeof(Item_Weapon), typeof(Item_Arrow), typeof(Item_Material),
            typeof(Item_Potion), typeof(Item_Recipe), typeof(Item_Enchant), typeof(Item_Scroll), typeof(Item_PetCollar), typeof(Item_Etc),
            typeof(ItemBehaviorAvailability),
            typeof(ItemCondition), typeof(ItemCondition_Player),
            typeof(ItemSet), typeof(ItemSetBodyPart), typeof(ItemSetSkill), typeof(ItemSetStats),
            typeof(ItemAttackGeometry), typeof(ItemSkill), typeof(ItemStats), typeof(ContentImportRun),
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
        Expression<Func<Npc, TEntity?>> navigation)
        where TEntity : class, INpcStatsRecord
    {
        stats.HasOne(entity => entity.Npc)
            .WithOne(navigation)
            .HasForeignKey<TEntity>(entity => new { entity.GameVersion, entity.NpcId })
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureItemFamily<TEntity>(
        EntityTypeBuilder<TEntity> family,
        Expression<Func<Item, TEntity?>> navigation)
        where TEntity : class
    {
        family.HasOne<Item>("Item")
            .WithOne(navigation)
            .HasForeignKey<TEntity>("GameVersion", "ItemId")
            .OnDelete(DeleteBehavior.Cascade);
        family.HasOne<GameVersion>().WithMany().HasForeignKey("GameVersion").OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureItemLookups<TEntity>(EntityTypeBuilder<TEntity> family)
        where TEntity : class
    {
        family.HasOne<ItemAction>("ItemAction").WithMany()
            .HasForeignKey("GameVersion", "ItemActionName").OnDelete(DeleteBehavior.Restrict);
        family.HasOne<ItemBodyPart>("ItemBodyPart").WithMany()
            .HasForeignKey("GameVersion", "ItemBodyPartName").OnDelete(DeleteBehavior.Restrict);
        family.HasOne<ItemCrystalType>("ItemCrystalType").WithMany()
            .HasForeignKey("GameVersion", "ItemCrystalTypeName").OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureActionAndHandler<TEntity>(EntityTypeBuilder<TEntity> family)
        where TEntity : class
    {
        family.HasOne<ItemAction>("ItemAction").WithMany()
            .HasForeignKey("GameVersion", "ItemActionName").OnDelete(DeleteBehavior.Restrict);
        family.HasOne<ItemHandler>("ItemHandler").WithMany()
            .HasForeignKey("GameVersion", "HandlerName").OnDelete(DeleteBehavior.Restrict);
    }
}
