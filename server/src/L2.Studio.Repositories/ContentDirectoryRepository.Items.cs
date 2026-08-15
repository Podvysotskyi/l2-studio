using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public async Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, ItemDirectoryRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var offset = ((long)request.Page - 1) * request.PageSize;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = context.Items.AsNoTracking().Where(item => item.GameVersion == gameVersion &&
            (query == string.Empty || EF.Functions.ILike(item.Name, pattern, "\\")));
        if (request.ItemTypeName is not null) items = items.Where(item => item.ItemTypeName == request.ItemTypeName);
        if (request.ItemActionName is not null) items = items.Where(item => item.ItemActionName == request.ItemActionName);
        if (request.ItemBodyPartName is not null) items = items.Where(item => item.ItemBodyPartName == request.ItemBodyPartName);
        if (request.ItemMaterialName is not null) items = items.Where(item => item.ItemMaterialName == request.ItemMaterialName);
        if (request.ItemCrystalTypeName is not null) items = items.Where(item => item.ItemCrystalTypeName == request.ItemCrystalTypeName);
        var total = await items.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue) return new ItemDirectoryPage([], total, request.Page, request.PageSize);
        var result = await ProjectItems(items.OrderBy(item => item.Id).Skip((int)offset).Take(request.PageSize)).ToListAsync(cancellationToken);
        return new ItemDirectoryPage(result, total, request.Page, request.PageSize);
    }

    public async Task<ItemSummary?> GetItemAsync(string gameVersion, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ProjectItems(context.Items.AsNoTracking().Where(item => item.GameVersion == gameVersion && item.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ItemSummary?> UpdateItemAsync(string gameVersion, int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await context.Items.SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (item is null) return null;
        if (!await LookupExists(context, gameVersion, "item-types", request.ItemTypeName!, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-actions", request.ItemActionName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-body-parts", request.ItemBodyPartName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-materials", request.ItemMaterialName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-crystal-types", request.ItemCrystalTypeName, cancellationToken))
            throw new InvalidOperationException("One or more selected item lookup values are not available for this game version.");
        item.Name = request.Name!.Trim();
        item.ItemTypeName = request.ItemTypeName!.Trim();
        item.ItemActionName = Trim(request.ItemActionName);
        item.ItemBodyPartName = Trim(request.ItemBodyPartName);
        item.ItemMaterialName = Trim(request.ItemMaterialName);
        item.ItemCrystalTypeName = Trim(request.ItemCrystalTypeName);
        item.Icon = Trim(request.Icon);
        item.Weight = request.Weight;
        item.Price = request.Price;
        item.WeaponType = Trim(request.WeaponType);
        item.ArmorType = Trim(request.ArmorType);
        item.EtcItemType = Trim(request.EtcItemType);
        item.DamageRange = Trim(request.DamageRange);
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectItems(context.Items.AsNoTracking().Where(value => value.GameVersion == gameVersion && value.Id == id))
            .SingleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemLookupSummary>> GetItemLookupsAsync(string gameVersion, string kind, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "item-types" => await context.ItemTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion).OrderBy(value => value.Name).Select(value => new ItemLookupSummary(value.Name, value.DisplayName)).ToListAsync(cancellationToken),
            "item-actions" => await context.ItemActions.AsNoTracking().Where(value => value.GameVersion == gameVersion).OrderBy(value => value.Name).Select(value => new ItemLookupSummary(value.Name, value.DisplayName)).ToListAsync(cancellationToken),
            "item-body-parts" => await context.ItemBodyParts.AsNoTracking().Where(value => value.GameVersion == gameVersion).OrderBy(value => value.Name).Select(value => new ItemLookupSummary(value.Name, value.DisplayName)).ToListAsync(cancellationToken),
            "item-materials" => await context.ItemMaterials.AsNoTracking().Where(value => value.GameVersion == gameVersion).OrderBy(value => value.Name).Select(value => new ItemLookupSummary(value.Name, value.DisplayName)).ToListAsync(cancellationToken),
            "item-crystal-types" => await context.ItemCrystalTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion).OrderBy(value => value.Name).Select(value => new ItemLookupSummary(value.Name, value.DisplayName)).ToListAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public async Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        switch (kind)
        {
            case "item-types": return await Update(context, context.ItemTypes, gameVersion, name, displayName, cancellationToken);
            case "item-actions": return await Update(context, context.ItemActions, gameVersion, name, displayName, cancellationToken);
            case "item-body-parts": return await Update(context, context.ItemBodyParts, gameVersion, name, displayName, cancellationToken);
            case "item-materials": return await Update(context, context.ItemMaterials, gameVersion, name, displayName, cancellationToken);
            case "item-crystal-types": return await Update(context, context.ItemCrystalTypes, gameVersion, name, displayName, cancellationToken);
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static IQueryable<ItemSummary> ProjectItems(IQueryable<Item> items) => items.Select(item => new ItemSummary(
        item.Id, item.Name, item.ItemTypeName, item.ItemType.DisplayName, item.ItemActionName,
        item.ItemAction == null ? null : item.ItemAction.DisplayName, item.ItemBodyPartName,
        item.ItemBodyPart == null ? null : item.ItemBodyPart.DisplayName, item.ItemMaterialName,
        item.ItemMaterial == null ? null : item.ItemMaterial.DisplayName, item.ItemCrystalTypeName,
        item.ItemCrystalType == null ? null : item.ItemCrystalType.DisplayName, item.Icon, item.Weight, item.Price,
        item.WeaponType, item.ArmorType, item.EtcItemType, item.DamageRange,
        item.Stats == null ? null : new ItemStatsSummary(item.Stats.AccuracyCombat, item.Stats.CriticalRate,
            item.Stats.MagicalAttack, item.Stats.MagicalDefence, item.Stats.MaximumMp, item.Stats.PhysicalAttack,
            item.Stats.PhysicalAttackRange, item.Stats.PhysicalAttackSpeed, item.Stats.PhysicalDefence,
            item.Stats.Evasion, item.Stats.ShieldRate, item.Stats.RandomDamage, item.Stats.ShieldDefence)));

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<bool> LookupExists(GameContentDbContext context, string gameVersion, string kind, string? name, CancellationToken token) =>
        name is null || kind switch
        {
            "item-types" => await context.ItemTypes.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-actions" => await context.ItemActions.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-body-parts" => await context.ItemBodyParts.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-materials" => await context.ItemMaterials.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-crystal-types" => await context.ItemCrystalTypes.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            _ => false
        };

    private static async Task<ItemLookupSummary?> Update<TEntity>(GameContentDbContext context, DbSet<TEntity> set, string gameVersion, string name, string displayName, CancellationToken token)
        where TEntity : class
    {
        var entity = await set.FindAsync([gameVersion, name], token);
        if (entity is null) return null;
        typeof(TEntity).GetProperty(nameof(ItemType.DisplayName))!.SetValue(entity, displayName);
        await context.SaveChangesAsync(token);
        return new ItemLookupSummary(name, displayName);
    }
}
