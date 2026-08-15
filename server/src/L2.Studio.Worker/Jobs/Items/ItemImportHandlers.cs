using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class ItemImportHandlers(IDbContextFactory<GameContentDbContext> contextFactory, TimeProvider timeProvider)
{
    private static readonly C1ItemCatalog Catalog = new();

    public Task Handle(ImportC1Items message, CancellationToken token) => ImportAsync(message.RunId, token);

    private async Task ImportAsync(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(value =>
                value.Id == runId && value.Kind == ContentImportTargetValues.Items, token);
            if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !ItemImportJobValues.SupportedModes.Contains(run.Mode)) throw new InvalidOperationException("Only C1 add-missing and restore-defaults item imports are supported.");
            run.Status = ItemImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();
            await EnsureC1LookupsAsync(context, run.GameVersion, token);
            var existing = await context.Items.Include(item => item.Stats).Where(item => item.GameVersion == run.GameVersion).ToDictionaryAsync(item => item.Id, token);
            var missing = Catalog.Items.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
            context.Items.AddRange(missing.Select(definition => ToEntity(run.GameVersion, definition)));
            var restored = Array.Empty<ItemDefinition>();
            if (run.Mode == ItemImportJobValues.RestoreDefaults)
            {
                restored = Catalog.Items.Where(definition => existing.ContainsKey(definition.Id)).ToArray();
                foreach (var definition in restored) Apply(context, existing[definition.Id], definition);
            }
            run.TotalCount = Catalog.Items.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = Catalog.Items.Count - missing.Length;
            run.RestoredCount = restored.Length;
            run.Status = ItemImportJobValues.Succeeded;
            run.FinishedAt = timeProvider.GetUtcNow();
            run.LastHeartbeatAt = run.FinishedAt;
            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkFailed(runId, exception, token);
        }
    }

    private static async Task EnsureC1LookupsAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken token)
    {
        var types = await context.ItemTypes.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemTypes.AddRange(Catalog.Types.Where(value => !types.Contains(value.Name)).Select(value =>
            new ItemType { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        var actions = await context.ItemActions.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemActions.AddRange(Catalog.Actions.Where(value => !actions.Contains(value.Name)).Select(value =>
            new ItemAction { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        var bodyParts = await context.ItemBodyParts.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemBodyParts.AddRange(Catalog.BodyParts.Where(value => !bodyParts.Contains(value.Name)).Select(value =>
            new ItemBodyPart { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        var materials = await context.ItemMaterials.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemMaterials.AddRange(Catalog.Materials.Where(value => !materials.Contains(value.Name)).Select(value =>
            new ItemMaterial { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        var crystals = await context.ItemCrystalTypes.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemCrystalTypes.AddRange(Catalog.CrystalTypes.Where(value => !crystals.Contains(value.Name)).Select(value =>
            new ItemCrystalType { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        await context.SaveChangesAsync(token);
    }

    private static async Task<IReadOnlyList<string>> MissingC1LookupsAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken token)
    {
        var types = await context.ItemTypes.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var actions = await context.ItemActions.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var bodyParts = await context.ItemBodyParts.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var materials = await context.ItemMaterials.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var crystals = await context.ItemCrystalTypes.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        return MissingC1Lookups(types, actions, bodyParts, materials, crystals);
    }

    internal static IReadOnlyList<string> MissingC1Lookups(
        IReadOnlySet<string> types,
        IReadOnlySet<string> actions,
        IReadOnlySet<string> bodyParts,
        IReadOnlySet<string> materials,
        IReadOnlySet<string> crystals)
    {
        return
        [
            .. MissingLookupNames("item types", Catalog.Types.Select(definition => definition.Name), types),
            .. MissingLookupNames("item actions", Catalog.Actions.Select(definition => definition.Name), actions),
            .. MissingLookupNames("item body parts", Catalog.BodyParts.Select(definition => definition.Name), bodyParts),
            .. MissingLookupNames("item materials", Catalog.Materials.Select(definition => definition.Name), materials),
            .. MissingLookupNames("item crystal types", Catalog.CrystalTypes.Select(definition => definition.Name), crystals)
        ];
    }

    private static IEnumerable<string> MissingLookupNames(
        string label,
        IEnumerable<string> definitions,
        IReadOnlySet<string> existing)
    {
        var missing = definitions.Where(name => !existing.Contains(name)).ToArray();
        return missing.Length == 0 ? [] : [$"{label} ({string.Join(", ", missing)})"];
    }

    private static Item ToEntity(string gameVersion, ItemDefinition definition)
    {
        var item = new Item { GameVersion = gameVersion, Id = definition.Id, Name = definition.Name, ItemTypeName = definition.TypeName };
        Apply(null, item, definition);
        return item;
    }

    private static void Apply(GameContentDbContext? context, Item item, ItemDefinition definition)
    {
        item.Name = definition.Name; item.ItemTypeName = definition.TypeName; item.ItemActionName = definition.ActionName; item.ItemBodyPartName = definition.BodyPartName; item.ItemMaterialName = definition.MaterialName; item.ItemCrystalTypeName = definition.CrystalTypeName;
        item.Icon = definition.Icon; item.WeaponType = definition.WeaponType; item.ArmorType = definition.ArmorType; item.EtcItemType = definition.EtcItemType; item.DamageRange = definition.DamageRange; item.DisplayId = definition.DisplayId; item.CrystalCount = definition.CrystalCount; item.Weight = definition.Weight; item.Price = definition.Price; item.Soulshots = definition.Soulshots; item.Spiritshots = definition.Spiritshots; item.MpConsume = definition.MpConsume; item.ReducedMpConsume = definition.ReducedMpConsume; item.ReuseDelay = definition.ReuseDelay; item.RecipeId = definition.RecipeId; item.Handler = definition.Handler; item.ItemSkill = definition.ItemSkill; item.UseCondition = definition.UseCondition;
        item.ElementEnabled = definition.ElementEnabled; item.EnchantEnabled = definition.EnchantEnabled; item.ForNpc = definition.ForNpc; item.ImmediateEffect = definition.ImmediateEffect; item.IsAttackWeapon = definition.IsAttackWeapon; item.IsForceEquip = definition.IsForceEquip; item.IsDepositable = definition.IsDepositable; item.IsDestroyable = definition.IsDestroyable; item.IsDropable = definition.IsDropable; item.IsMagicWeapon = definition.IsMagicWeapon; item.IsOlyRestricted = definition.IsOlyRestricted; item.IsQuestItem = definition.IsQuestItem; item.IsSellable = definition.IsSellable; item.IsStackable = definition.IsStackable; item.IsTradable = definition.IsTradable; item.UseWeaponSkillsOnly = definition.UseWeaponSkillsOnly;
        if (definition.Stats is null)
        {
            if (item.Stats is not null && context is not null) context.ItemStats.Remove(item.Stats);
            item.Stats = null;
            return;
        }
        if (item.Stats is null)
        {
            item.Stats = ToEntity(item.GameVersion, item.Id, definition.Stats);
            if (context is not null) context.ItemStats.Add(item.Stats);
        }
        Apply(item.Stats, definition.Stats);
    }

    private static ItemStats ToEntity(string gameVersion, int itemId, ItemStatsDefinition stats)
    {
        var itemStats = new ItemStats { GameVersion = gameVersion, ItemId = itemId };
        Apply(itemStats, stats);
        return itemStats;
    }

    private static void Apply(ItemStats stats, ItemStatsDefinition definition)
    {
        stats.AccuracyCombat = definition.AccuracyCombat; stats.CriticalRate = definition.CriticalRate; stats.MagicalAttack = definition.MagicalAttack; stats.MagicalDefence = definition.MagicalDefence; stats.MaximumMp = definition.MaximumMp; stats.PhysicalAttack = definition.PhysicalAttack; stats.PhysicalAttackRange = definition.PhysicalAttackRange; stats.PhysicalAttackSpeed = definition.PhysicalAttackSpeed; stats.PhysicalDefence = definition.PhysicalDefence; stats.Evasion = definition.Evasion; stats.ShieldRate = definition.ShieldRate; stats.RandomDamage = definition.RandomDamage; stats.ShieldDefence = definition.ShieldDefence;
    }

    private async Task MarkFailed(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = ItemImportJobValues.Failed;
        run.Error = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }
}
