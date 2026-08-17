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
            var existing = await context.Items
                .Include(item => item.Armor).Include(item => item.Weapon).Include(item => item.Arrow)
                .Include(item => item.Material).Include(item => item.Potion).Include(item => item.Recipe)
                .Include(item => item.Enchant).Include(item => item.Scroll).Include(item => item.PetCollar).Include(item => item.Etc)
                .Include(item => item.BehaviorAvailability)
                .Include(item => item.Condition).ThenInclude(condition => condition!.Player)
                .Include(item => item.AttackGeometry).Include(item => item.Skills).Include(item => item.Stats)
                .Where(item => item.GameVersion == run.GameVersion).ToDictionaryAsync(item => item.Id, token);
            var missing = Catalog.Items.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
            context.Items.AddRange(missing.Select(definition => ToEntity(run.GameVersion, definition)));
            var restored = Array.Empty<ItemDefinition>();
            if (run.Mode == ItemImportJobValues.RestoreDefaults)
            {
                restored = Catalog.Items.Where(definition => existing.ContainsKey(definition.Id)).ToArray();
                foreach (var definition in restored) Apply(context, existing[definition.Id], definition);
            }
            else
            {
                foreach (var definition in Catalog.Items.Where(definition => existing.ContainsKey(definition.Id)))
                {
                    AddMissingBehaviorAvailability(context, existing[definition.Id], definition);
                    AddMissingSkills(existing[definition.Id], definition);
                }
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
            new ItemType
            {
                GameVersion = gameVersion,
                Name = value.Name,
                DisplayName = value.DisplayName,
                ParentTypeName = value.ParentTypeName
            }));
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
        var handlers = await context.ItemHandlers.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemHandlers.AddRange(Catalog.Handlers.Where(value => !handlers.Contains(value.Name)).Select(value =>
            new ItemHandler { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
        var skillTypes = await context.ItemSkillTypes.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemSkillTypes.AddRange(Catalog.SkillTypes.Where(value => !skillTypes.Contains(value.Name)).Select(value =>
            new ItemSkillType { GameVersion = gameVersion, Name = value.Name, DisplayName = value.DisplayName }));
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
        var handlers = await context.ItemHandlers.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        var skillTypes = await context.ItemSkillTypes.Where(value => value.GameVersion == gameVersion).Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        return MissingC1Lookups(types, actions, bodyParts, materials, crystals, handlers, skillTypes);
    }

    internal static IReadOnlyList<string> MissingC1Lookups(
        IReadOnlySet<string> types,
        IReadOnlySet<string> actions,
        IReadOnlySet<string> bodyParts,
        IReadOnlySet<string> materials,
        IReadOnlySet<string> crystals,
        IReadOnlySet<string> handlers,
        IReadOnlySet<string> skillTypes)
    {
        return
        [
            .. MissingLookupNames("item types", Catalog.Types.Select(definition => definition.Name), types),
            .. MissingLookupNames("item actions", Catalog.Actions.Select(definition => definition.Name), actions),
            .. MissingLookupNames("item body parts", Catalog.BodyParts.Select(definition => definition.Name), bodyParts),
            .. MissingLookupNames("item materials", Catalog.Materials.Select(definition => definition.Name), materials),
            .. MissingLookupNames("item crystal types", Catalog.CrystalTypes.Select(definition => definition.Name), crystals),
            .. MissingLookupNames("item handlers", Catalog.Handlers.Select(definition => definition.Name), handlers),
            .. MissingLookupNames("item skill types", Catalog.SkillTypes.Select(definition => definition.Name), skillTypes)
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
        item.Name = definition.Name;
        item.ItemTypeName = definition.TypeName;
        item.ItemMaterialName = definition.MaterialName;
        item.Icon = definition.Icon;
        item.Weight = definition.Weight;
        item.Price = definition.Price;
        ApplyCondition(context, item, definition.Condition);
        ApplyFamily(context, item, definition);
        ApplyBehaviorAvailability(context, item, definition);
        RestoreSkills(context, item, definition);
        var attackGeometry = (definition as Item_WeaponDefinition)?.AttackGeometry;
        if (attackGeometry is null)
        {
            if (item.AttackGeometry is not null && context is not null) context.ItemAttackGeometries.Remove(item.AttackGeometry);
            item.AttackGeometry = null;
        }
        else if (item.AttackGeometry is null)
        {
            item.AttackGeometry = new ItemAttackGeometry
            {
                GameVersion = item.GameVersion,
                ItemId = item.Id,
                OffsetX = attackGeometry.OffsetX,
                OffsetY = attackGeometry.OffsetY,
                Radius = attackGeometry.Radius,
                Length = attackGeometry.Length
            };
            if (context is not null) context.ItemAttackGeometries.Add(item.AttackGeometry);
        }
        else
        {
            item.AttackGeometry.OffsetX = attackGeometry.OffsetX;
            item.AttackGeometry.OffsetY = attackGeometry.OffsetY;
            item.AttackGeometry.Radius = attackGeometry.Radius;
            item.AttackGeometry.Length = attackGeometry.Length;
        }
        var stats = (definition as IItemStatsDefinition)?.Stats;
        if (stats is null)
        {
            if (item.Stats is not null && context is not null) context.ItemStats.Remove(item.Stats);
            item.Stats = null;
            return;
        }
        if (item.Stats is null)
        {
            item.Stats = ToEntity(item.GameVersion, item.Id, stats);
            if (context is not null) context.ItemStats.Add(item.Stats);
        }
        Apply(item.Stats, stats);
    }

    private static void ApplyFamily(GameContentDbContext? context, Item item, ItemDefinition definition)
    {
        EnsureNoOtherFamily(item, definition);
        switch (definition)
        {
            case Item_ArmorDefinition value:
                item.Armor ??= Add(context, new Item_Armor { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Armor, value);
                break;
            case Item_WeaponDefinition value:
                item.Weapon ??= Add(context, new Item_Weapon { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Weapon, value);
                break;
            case Item_ArrowDefinition value:
                item.Arrow ??= Add(context, new Item_Arrow { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Arrow, value);
                break;
            case Item_MaterialDefinition value:
                item.Material ??= Add(context, new Item_Material { GameVersion = item.GameVersion, ItemId = item.Id });
                break;
            case Item_PotionDefinition value:
                item.Potion ??= Add(context, new Item_Potion { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Potion, value);
                break;
            case Item_RecipeDefinition value:
                item.Recipe ??= Add(context, new Item_Recipe { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Recipe, value);
                break;
            case Item_EnchantDefinition value:
                item.Enchant ??= Add(context, new Item_Enchant { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Enchant, value);
                break;
            case Item_ScrollDefinition value:
                item.Scroll ??= Add(context, new Item_Scroll { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Scroll, value);
                break;
            case Item_PetCollarDefinition value:
                item.PetCollar ??= Add(context, new Item_PetCollar { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.PetCollar, value);
                break;
            case Item_EtcDefinition value:
                item.Etc ??= Add(context, new Item_Etc { GameVersion = item.GameVersion, ItemId = item.Id });
                Apply(item.Etc, value);
                break;
        }
    }

    private static T Add<T>(GameContentDbContext? context, T entity) where T : class
    {
        if (context is not null) context.Set<T>().Add(entity);
        return entity;
    }

    private static void ApplyCondition(GameContentDbContext? context, Item item, ItemConditionDefinition? definition)
    {
        if (definition is null)
        {
            if (item.Condition is not null && context is not null) context.ItemConditions.Remove(item.Condition);
            item.Condition = null;
            return;
        }

        if (item.Condition is null)
        {
            item.Condition = new ItemCondition
            {
                GameVersion = item.GameVersion,
                ItemId = item.Id,
                Player = new ItemCondition_Player
                {
                    GameVersion = item.GameVersion,
                    ItemId = item.Id
                }
            };
            if (context is not null) context.ItemConditions.Add(item.Condition);
        }

        item.Condition.MessageId = definition.MessageId;
        item.Condition.AddName = definition.AddName;
        item.Condition.Player.IsPvpFlagged = definition.IsPvpFlagged;
        item.Condition.Player.PlayerRaces = definition.PlayerRaces;
        item.Condition.Player.PlayerCategoryTypes = definition.PlayerCategoryTypes;
    }

    private static void AddMissingBehaviorAvailability(
        GameContentDbContext context,
        Item item,
        ItemDefinition definition)
    {
        if (item.BehaviorAvailability is null) ApplyBehaviorAvailability(context, item, definition);
    }

    private static void ApplyBehaviorAvailability(
        GameContentDbContext? context,
        Item item,
        ItemDefinition definition)
    {
        item.BehaviorAvailability ??= Add(context, new ItemBehaviorAvailability
        {
            GameVersion = item.GameVersion,
            ItemId = item.Id
        });
        var behavior = item.BehaviorAvailability;
        switch (definition)
        {
            case Item_ArmorDefinition value:
                behavior.EnchantEnabled = value.EnchantEnabled; behavior.ForNpc = value.ForNpc; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = value.IsDepositable; behavior.IsDestroyable = value.IsDestroyable; behavior.IsDropable = value.IsDropable; behavior.IsOlyRestricted = null; behavior.IsSellable = value.IsSellable; behavior.IsStackable = null; behavior.IsTradable = value.IsTradable;
                break;
            case Item_WeaponDefinition value:
                behavior.EnchantEnabled = value.EnchantEnabled; behavior.ForNpc = value.ForNpc; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = value.IsDepositable; behavior.IsDestroyable = value.IsDestroyable; behavior.IsDropable = value.IsDropable; behavior.IsOlyRestricted = null; behavior.IsSellable = value.IsSellable; behavior.IsStackable = null; behavior.IsTradable = value.IsTradable;
                break;
            case Item_ArrowDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = null; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = null; behavior.IsSellable = null; behavior.IsStackable = value.IsStackable; behavior.IsTradable = null;
                break;
            case Item_MaterialDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = null; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = null; behavior.IsSellable = null; behavior.IsStackable = value.IsStackable; behavior.IsTradable = null;
                break;
            case Item_PotionDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = value.ForNpc; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = value.IsOlyRestricted; behavior.IsSellable = null; behavior.IsStackable = value.IsStackable; behavior.IsTradable = null;
                break;
            case Item_RecipeDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = null; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = value.IsDepositable; behavior.IsDestroyable = value.IsDestroyable; behavior.IsDropable = value.IsDropable; behavior.IsOlyRestricted = null; behavior.IsSellable = value.IsSellable; behavior.IsStackable = value.IsStackable; behavior.IsTradable = value.IsTradable;
                break;
            case Item_EnchantDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = null; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = value.IsOlyRestricted; behavior.IsSellable = null; behavior.IsStackable = value.IsStackable; behavior.IsTradable = null;
                break;
            case Item_ScrollDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = value.ForNpc; behavior.ImmediateEffect = null; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = value.IsOlyRestricted; behavior.IsSellable = null; behavior.IsStackable = value.IsStackable; behavior.IsTradable = null;
                break;
            case Item_PetCollarDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = null; behavior.ImmediateEffect = null; behavior.IsDepositable = null; behavior.IsDestroyable = null; behavior.IsDropable = null; behavior.IsOlyRestricted = value.IsOlyRestricted; behavior.IsSellable = null; behavior.IsStackable = null; behavior.IsTradable = null;
                break;
            case Item_EtcDefinition value:
                behavior.EnchantEnabled = null; behavior.ForNpc = value.ForNpc; behavior.ImmediateEffect = value.ImmediateEffect; behavior.IsDepositable = value.IsDepositable; behavior.IsDestroyable = value.IsDestroyable; behavior.IsDropable = value.IsDropable; behavior.IsOlyRestricted = value.IsOlyRestricted; behavior.IsSellable = value.IsSellable; behavior.IsStackable = value.IsStackable; behavior.IsTradable = value.IsTradable;
                break;
        }
    }

    private static void EnsureNoOtherFamily(Item item, ItemDefinition definition)
    {
        var existing = new object?[] { item.Armor, item.Weapon, item.Arrow, item.Material, item.Potion, item.Recipe, item.Enchant, item.Scroll, item.PetCollar, item.Etc }.Count(value => value is not null);
        if (existing > 1) throw new InvalidOperationException($"Item {item.Id} has more than one family row.");
        var matches = definition switch
        {
            Item_ArmorDefinition => item.Armor is not null,
            Item_WeaponDefinition => item.Weapon is not null,
            Item_ArrowDefinition => item.Arrow is not null,
            Item_MaterialDefinition => item.Material is not null,
            Item_PotionDefinition => item.Potion is not null,
            Item_RecipeDefinition => item.Recipe is not null,
            Item_EnchantDefinition => item.Enchant is not null,
            Item_ScrollDefinition => item.Scroll is not null,
            Item_PetCollarDefinition => item.PetCollar is not null,
            Item_EtcDefinition => item.Etc is not null,
            _ => false
        };
        if (existing == 1 && !matches) throw new InvalidOperationException($"Item {item.Id} cannot change family during restore.");
    }

    private static void Apply(Item_Armor item, Item_ArmorDefinition value)
    {
        item.ItemActionName = value.ActionName; item.ItemBodyPartName = value.BodyPartName; item.ItemCrystalTypeName = value.CrystalTypeName; item.CrystalCount = value.CrystalCount;
    }

    private static void Apply(Item_Weapon item, Item_WeaponDefinition value)
    {
        item.ItemActionName = value.ActionName; item.ItemBodyPartName = value.BodyPartName; item.ItemCrystalTypeName = value.CrystalTypeName; item.DisplayId = value.DisplayId; item.CrystalCount = value.CrystalCount; item.Soulshots = value.Soulshots; item.Spiritshots = value.Spiritshots; item.MpConsume = value.MpConsume; item.ReducedMpConsume = value.ReducedMpConsume; item.ReuseDelay = value.ReuseDelay; item.ElementEnabled = value.ElementEnabled; item.IsAttackWeapon = value.IsAttackWeapon; item.IsForceEquip = value.IsForceEquip; item.IsMagicWeapon = value.IsMagicWeapon; item.UseWeaponSkillsOnly = value.UseWeaponSkillsOnly;
    }

    private static void Apply(Item_Arrow item, Item_ArrowDefinition value)
    {
        item.ItemActionName = value.ActionName; item.ItemBodyPartName = value.BodyPartName; item.ItemCrystalTypeName = value.CrystalTypeName;
    }

    private static void Apply(Item_Potion item, Item_PotionDefinition value)
    {
        item.ItemActionName = value.ActionName; item.ReuseDelay = value.ReuseDelay; item.HandlerName = value.HandlerName;
    }

    private static void Apply(Item_Recipe item, Item_RecipeDefinition value)
    {
        item.ItemActionName = value.ActionName; item.RecipeId = value.RecipeId; item.HandlerName = value.HandlerName;
    }

    private static void Apply(Item_Enchant item, Item_EnchantDefinition value)
    {
        item.ItemActionName = value.ActionName; item.HandlerName = value.HandlerName;
    }

    private static void Apply(Item_Scroll item, Item_ScrollDefinition value)
    {
        item.ItemActionName = value.ActionName; item.HandlerName = value.HandlerName;
    }

    private static void Apply(Item_PetCollar item, Item_PetCollarDefinition value)
    {
        item.ItemActionName = value.ActionName; item.HandlerName = value.HandlerName; item.UseCondition = value.UseCondition;
    }

    private static void Apply(Item_Etc item, Item_EtcDefinition value)
    {
        item.ItemActionName = value.ActionName; item.ItemBodyPartName = value.BodyPartName; item.ItemCrystalTypeName = value.CrystalTypeName; item.DisplayId = value.DisplayId; item.ReuseDelay = value.ReuseDelay; item.HandlerName = value.HandlerName; item.ItemSkill = value.ItemSkill; item.UseCondition = value.UseCondition; item.IsQuestItem = value.IsQuestItem;
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

    private static void AddMissingSkills(Item item, ItemDefinition definition)
    {
        var definitionSkills = Skills(definition);
        var existing = item.Skills.Select(skill => (skill.SkillId, skill.SkillLevel)).ToHashSet();
        foreach (var skill in definitionSkills.Where(skill => !existing.Contains((skill.SkillId, skill.SkillLevel))))
            item.Skills.Add(ToEntity(item.GameVersion, item.Id, skill));
    }

    private static void RestoreSkills(GameContentDbContext? context, Item item, ItemDefinition definition)
    {
        var definitionSkills = Skills(definition);
        var definitions = definitionSkills.ToDictionary(skill => (skill.SkillId, skill.SkillLevel));
        foreach (var skill in item.Skills.Where(skill => !definitions.ContainsKey((skill.SkillId, skill.SkillLevel))).ToArray())
        {
            if (context is not null) context.ItemSkills.Remove(skill);
            item.Skills.Remove(skill);
        }
        foreach (var definitionSkill in definitionSkills)
        {
            var skill = item.Skills.SingleOrDefault(value => value.SkillId == definitionSkill.SkillId && value.SkillLevel == definitionSkill.SkillLevel);
            if (skill is null)
            {
                item.Skills.Add(ToEntity(item.GameVersion, item.Id, definitionSkill));
                continue;
            }
            Apply(skill, definitionSkill);
        }
    }

    private static IReadOnlyList<ItemSkillDefinition> Skills(ItemDefinition definition) =>
        (definition as IItemSkillsDefinition)?.Skills ?? [];

    private static ItemSkill ToEntity(string gameVersion, int itemId, ItemSkillDefinition definition)
    {
        var skill = new ItemSkill { GameVersion = gameVersion, ItemId = itemId, SkillId = definition.SkillId, SkillLevel = definition.SkillLevel };
        Apply(skill, definition);
        return skill;
    }

    private static void Apply(ItemSkill skill, ItemSkillDefinition definition)
    {
        skill.ItemSkillTypeName = definition.TypeName;
        skill.Chance = definition.Chance;
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
