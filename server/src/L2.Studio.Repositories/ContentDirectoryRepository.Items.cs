using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.EntityFrameworkCore;
using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public async Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, string family, ItemDirectoryRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var offset = ((long)request.Page - 1) * request.PageSize;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = FamilyItems(context.Items.AsNoTracking().Where(item => item.GameVersion == gameVersion &&
            (query == string.Empty || EF.Functions.ILike(item.Name, pattern, "\\"))), family);
        if (request.ItemTypeName is not null)
            items = items.Where(item => item.ItemTypeName == request.ItemTypeName ||
                item.ItemType.ParentTypeName == request.ItemTypeName);
        if (request.ItemActionName is not null) items = items.Where(item =>
            item.Armor!.ItemActionName == request.ItemActionName || item.Weapon!.ItemActionName == request.ItemActionName ||
            item.Arrow!.ItemActionName == request.ItemActionName || item.Potion!.ItemActionName == request.ItemActionName ||
            item.Recipe!.ItemActionName == request.ItemActionName || item.Enchant!.ItemActionName == request.ItemActionName ||
            item.Scroll!.ItemActionName == request.ItemActionName || item.PetCollar!.ItemActionName == request.ItemActionName ||
            item.Etc!.ItemActionName == request.ItemActionName);
        if (request.ItemBodyPartName is not null) items = items.Where(item =>
            item.Armor!.ItemBodyPartName == request.ItemBodyPartName || item.Weapon!.ItemBodyPartName == request.ItemBodyPartName ||
            item.Arrow!.ItemBodyPartName == request.ItemBodyPartName || item.Etc!.ItemBodyPartName == request.ItemBodyPartName);
        if (request.ItemMaterialName is not null) items = items.Where(item => item.ItemMaterialName == request.ItemMaterialName);
        if (request.ItemCrystalTypeName is not null) items = items.Where(item =>
            item.Armor!.ItemCrystalTypeName == request.ItemCrystalTypeName || item.Weapon!.ItemCrystalTypeName == request.ItemCrystalTypeName ||
            item.Arrow!.ItemCrystalTypeName == request.ItemCrystalTypeName || item.Etc!.ItemCrystalTypeName == request.ItemCrystalTypeName);
        if (request.HandlerName is not null) items = items.Where(item =>
            item.Potion!.HandlerName == request.HandlerName || item.Recipe!.HandlerName == request.HandlerName ||
            item.Enchant!.HandlerName == request.HandlerName || item.Scroll!.HandlerName == request.HandlerName ||
            item.PetCollar!.HandlerName == request.HandlerName || item.Etc!.HandlerName == request.HandlerName);
        var total = await items.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue) return new ItemDirectoryPage([], total, request.Page, request.PageSize);
        var result = await ProjectItems(items.OrderBy(item => item.Id).Skip((int)offset).Take(request.PageSize), context.Skills.AsNoTracking()).ToListAsync(cancellationToken);
        return new ItemDirectoryPage(result, total, request.Page, request.PageSize);
    }

    public async Task<ItemDetailSummary?> GetItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await ProjectItemDetails(
                FamilyItems(context.Items.AsNoTracking().Where(value => value.GameVersion == gameVersion && value.Id == id), family),
                context.Skills.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
        if (item?.Properties.ItemSkill is not { } primarySkill) return item;

        if (!TryParsePrimarySkill(primarySkill, out var skillId, out var skillLevel))
            return item with { PrimarySkill = new ItemPrimarySkillSummary(primarySkill, null, null, null) };

        var skillName = await context.Skills.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion && value.Id == skillId)
            .Select(value => value.Name)
            .SingleOrDefaultAsync(cancellationToken);
        return item with { PrimarySkill = new ItemPrimarySkillSummary(primarySkill, skillId, skillLevel, skillName) };
    }

    public async Task<ItemPrimarySkillSummary?> SetItemPrimarySkillAsync(
        string gameVersion,
        string family,
        int itemId,
        SetItemPrimarySkillRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (family != ItemFamilyValues.Etc) return null;
        var item = await context.ItemEtc.SingleOrDefaultAsync(
            value => value.GameVersion == gameVersion && value.ItemId == itemId,
            cancellationToken);
        if (item is null) return null;

        var skill = await context.Skills.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion && value.Id == request.SkillId)
            .Select(value => new { value.Name, value.Levels })
            .SingleOrDefaultAsync(cancellationToken);
        if (skill is null || request.SkillLevel > skill.Levels)
            throw new InvalidOperationException("The selected skill and level are not available for this game version.");

        item.ItemSkill = $"{request.SkillId}-{request.SkillLevel}";
        await context.SaveChangesAsync(cancellationToken);
        return new ItemPrimarySkillSummary(item.ItemSkill, request.SkillId, request.SkillLevel, skill.Name);
    }

    public async Task<bool> ClearItemPrimarySkillAsync(
        string gameVersion,
        string family,
        int itemId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (family != ItemFamilyValues.Etc) return false;
        var item = await context.ItemEtc.SingleOrDefaultAsync(
            value => value.GameVersion == gameVersion && value.ItemId == itemId,
            cancellationToken);
        if (item is null) return false;
        item.ItemSkill = null;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ItemSkillSummary?> CreateItemSkillAsync(
        string gameVersion,
        string family,
        int itemId,
        CreateItemSkillRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!ItemFamilyValues.SkillFamilies.Contains(family)) return null;
        var itemExists = await FamilyItems(context.Items, family).AnyAsync(
            value => value.GameVersion == gameVersion && value.Id == itemId,
            cancellationToken);
        if (!itemExists) return null;
        await ValidateItemSkillAsync(context, gameVersion, request.SkillId, request.SkillLevel, request.ItemSkillTypeName, cancellationToken);

        var exists = await context.ItemSkills.AnyAsync(value =>
            value.GameVersion == gameVersion && value.ItemId == itemId && value.SkillId == request.SkillId &&
            value.SkillLevel == request.SkillLevel, cancellationToken);
        if (exists) throw new ItemSkillConflictException();

        context.ItemSkills.Add(new ItemSkill
        {
            GameVersion = gameVersion,
            ItemId = itemId,
            SkillId = request.SkillId,
            SkillLevel = request.SkillLevel,
            ItemSkillTypeName = Trim(request.ItemSkillTypeName),
            Chance = request.Chance
        });
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectItemSkills(context.ItemSkills.AsNoTracking().Where(value =>
                value.GameVersion == gameVersion && value.ItemId == itemId && value.SkillId == request.SkillId &&
                value.SkillLevel == request.SkillLevel), context.Skills.AsNoTracking())
            .SingleAsync(cancellationToken);
    }

    public async Task<ItemSkillSummary?> UpdateItemSkillAsync(
        string gameVersion,
        string family,
        int itemId,
        int skillId,
        short skillLevel,
        UpdateItemSkillRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!ItemFamilyValues.SkillFamilies.Contains(family) || !await FamilyItems(context.Items, family).AnyAsync(value => value.GameVersion == gameVersion && value.Id == itemId, cancellationToken)) return null;
        var itemSkill = await context.ItemSkills.SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.ItemId == itemId && value.SkillId == skillId &&
            value.SkillLevel == skillLevel, cancellationToken);
        if (itemSkill is null) return null;
        if (request.ItemSkillTypeName is not null && !await context.ItemSkillTypes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == request.ItemSkillTypeName, cancellationToken))
            throw new InvalidOperationException("The selected item skill type is not available for this game version.");

        itemSkill.ItemSkillTypeName = Trim(request.ItemSkillTypeName);
        itemSkill.Chance = request.Chance;
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectItemSkills(context.ItemSkills.AsNoTracking().Where(value =>
                value.GameVersion == gameVersion && value.ItemId == itemId && value.SkillId == skillId &&
                value.SkillLevel == skillLevel), context.Skills.AsNoTracking())
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> DeleteItemSkillAsync(
        string gameVersion,
        string family,
        int itemId,
        int skillId,
        short skillLevel,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!ItemFamilyValues.SkillFamilies.Contains(family) || !await FamilyItems(context.Items, family).AnyAsync(value => value.GameVersion == gameVersion && value.Id == itemId, cancellationToken)) return false;
        var itemSkill = await context.ItemSkills.SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.ItemId == itemId && value.SkillId == skillId &&
            value.SkillLevel == skillLevel, cancellationToken);
        if (itemSkill is null) return false;
        context.ItemSkills.Remove(itemSkill);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ItemSummary?> UpdateItemAsync(string gameVersion, string family, int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await FamilyItems(context.Items.Include(value => value.AttackGeometry)
                .Include(value => value.Armor).Include(value => value.Weapon).Include(value => value.Arrow)
                .Include(value => value.Potion).Include(value => value.Recipe).Include(value => value.Enchant)
                .Include(value => value.Scroll).Include(value => value.PetCollar).Include(value => value.Etc), family)
            .SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (item is null) return null;
        if (!await LookupExists(context, gameVersion, "item-actions", request.ItemActionName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-body-parts", request.ItemBodyPartName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-materials", request.ItemMaterialName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-crystal-types", request.ItemCrystalTypeName, cancellationToken) ||
            !await LookupExists(context, gameVersion, "item-handlers", request.HandlerName, cancellationToken))
            throw new InvalidOperationException("One or more selected item lookup values are not available for this game version.");
        item.Name = request.Name!.Trim();
        item.ItemMaterialName = Trim(request.ItemMaterialName);
        item.Icon = Trim(request.Icon);
        item.Weight = request.Weight;
        item.Price = request.Price;
        ApplyEditableFamilyFields(item, request);
        if (family != ItemFamilyValues.Weapon && request.AttackGeometry is not null)
            throw new InvalidOperationException("Attack geometry is available only for weapon definitions.");
        if (family == ItemFamilyValues.Weapon)
        {
            if (request.AttackGeometry is null)
            {
                if (item.AttackGeometry is not null) context.ItemAttackGeometries.Remove(item.AttackGeometry);
                item.AttackGeometry = null;
            }
            else if (item.AttackGeometry is null)
            {
                item.AttackGeometry = new ItemAttackGeometry
                {
                    GameVersion = item.GameVersion,
                    ItemId = item.Id,
                    OffsetX = request.AttackGeometry.OffsetX,
                    OffsetY = request.AttackGeometry.OffsetY,
                    Radius = request.AttackGeometry.Radius,
                    Length = request.AttackGeometry.Length
                };
                context.ItemAttackGeometries.Add(item.AttackGeometry);
            }
            else
            {
                item.AttackGeometry.OffsetX = request.AttackGeometry.OffsetX;
                item.AttackGeometry.OffsetY = request.AttackGeometry.OffsetY;
                item.AttackGeometry.Radius = request.AttackGeometry.Radius;
                item.AttackGeometry.Length = request.AttackGeometry.Length;
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        return await ProjectItems(FamilyItems(context.Items.AsNoTracking().Where(value => value.GameVersion == gameVersion && value.Id == id), family), context.Skills.AsNoTracking())
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> DeleteItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await FamilyItems(context.Items, family).SingleOrDefaultAsync(value =>
            value.GameVersion == gameVersion && value.Id == id, cancellationToken);
        if (item is null) return false;
        context.Items.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ItemConditionSummary?> UpdateItemConditionAsync(
        string gameVersion,
        string family,
        int itemId,
        UpdateItemConditionRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await FamilyItems(context.Items.Include(value => value.Condition).ThenInclude(value => value!.Player), family)
            .SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.Id == itemId, cancellationToken);
        if (item is null) return null;

        item.Condition ??= new ItemCondition
        {
            GameVersion = gameVersion,
            ItemId = itemId,
            Player = new ItemCondition_Player { GameVersion = gameVersion, ItemId = itemId }
        };
        item.Condition.MessageId = request.MessageId;
        item.Condition.AddName = request.AddName;
        item.Condition.Player.IsPvpFlagged = request.IsPvpFlagged;
        item.Condition.Player.PlayerRaces = JoinTokens(request.PlayerRaces);
        item.Condition.Player.PlayerCategoryTypes = JoinTokens(request.PlayerCategoryTypes);
        await context.SaveChangesAsync(cancellationToken);
        return ToSummary(item.Condition);
    }

    public async Task<bool> DeleteItemConditionAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await FamilyItems(context.Items.Include(value => value.Condition), family)
            .SingleOrDefaultAsync(value => value.GameVersion == gameVersion && value.Id == itemId, cancellationToken);
        if (item?.Condition is null) return false;
        context.ItemConditions.Remove(item.Condition);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DirectoryPage<ItemTypeSummary>> SearchItemTypesAsync(
        string gameVersion,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await PageAsync(context.ItemTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
            (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\")))
            .OrderBy(value => value.ParentTypeName == null ? 0 : 1).ThenBy(value => value.ParentTypeName).ThenBy(value => value.Name)
            .Select(value => new ItemTypeSummary(value.Name, value.DisplayName, value.ParentTypeName,
                value.ParentType == null ? null : value.ParentType.DisplayName)), request.Page, request.PageSize, cancellationToken);
    }

    public async Task<DirectoryPage<ItemLookupSummary>> SearchItemLookupsAsync(
        string gameVersion,
        string kind,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return kind switch
        {
            "item-actions" => await PageAsync(context.ItemActions.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "item-body-parts" => await PageAsync(context.ItemBodyParts.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "item-materials" => await PageAsync(context.ItemMaterials.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "item-crystal-types" => await PageAsync(context.ItemCrystalTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "item-handlers" => await PageAsync(context.ItemHandlers.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
            "item-skill-types" => await PageAsync(context.ItemSkillTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion &&
                (query == string.Empty || EF.Functions.ILike(value.Name, pattern, "\\") || EF.Functions.ILike(value.DisplayName, pattern, "\\"))).OrderBy(value => value.Name)
                .Select(value => new ItemLookupSummary(value.Name, value.DisplayName)), request.Page, request.PageSize, cancellationToken),
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
            case "item-handlers": return await Update(context, context.ItemHandlers, gameVersion, name, displayName, cancellationToken);
            case "item-skill-types": return await Update(context, context.ItemSkillTypes, gameVersion, name, displayName, cancellationToken);
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public async Task<bool> DeleteItemLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var usageCount = kind switch
        {
            "item-types" =>
                await context.Items.CountAsync(item => item.GameVersion == gameVersion && item.ItemTypeName == name, cancellationToken) +
                await context.ItemTypes.CountAsync(item => item.GameVersion == gameVersion && item.ParentTypeName == name, cancellationToken),
            "item-actions" => await CountActions(context, gameVersion, name, cancellationToken),
            "item-body-parts" => await CountBodyParts(context, gameVersion, name, cancellationToken),
            "item-materials" => await context.Items.CountAsync(item => item.GameVersion == gameVersion && item.ItemMaterialName == name, cancellationToken),
            "item-crystal-types" => await CountCrystalTypes(context, gameVersion, name, cancellationToken),
            "item-handlers" => await CountHandlers(context, gameVersion, name, cancellationToken),
            "item-skill-types" => await context.ItemSkills.CountAsync(item => item.GameVersion == gameVersion && item.ItemSkillTypeName == name, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (usageCount > 0)
            throw new ContentDeleteConflictException(kind == "item-types" ? "item types or definitions" : "item definitions", usageCount);

        var deleted = kind switch
        {
            "item-types" => await Delete(context.ItemTypes, gameVersion, name, cancellationToken),
            "item-actions" => await Delete(context.ItemActions, gameVersion, name, cancellationToken),
            "item-body-parts" => await Delete(context.ItemBodyParts, gameVersion, name, cancellationToken),
            "item-materials" => await Delete(context.ItemMaterials, gameVersion, name, cancellationToken),
            "item-crystal-types" => await Delete(context.ItemCrystalTypes, gameVersion, name, cancellationToken),
            "item-handlers" => await Delete(context.ItemHandlers, gameVersion, name, cancellationToken),
            "item-skill-types" => await Delete(context.ItemSkillTypes, gameVersion, name, cancellationToken),
            _ => false
        };
        if (!deleted) return false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<ItemSummary> ProjectItems(IQueryable<Item> items, IQueryable<Skill> skills) => items.Select(item => new ItemSummary(
        item.Id, item.Name, item.ItemTypeName, item.ItemType.DisplayName, item.ItemType.ParentTypeName,
        item.ItemType.ParentType == null ? null : item.ItemType.ParentType.DisplayName,
        item.Armor != null ? item.Armor.ItemActionName : item.Weapon != null ? item.Weapon.ItemActionName : item.Arrow != null ? item.Arrow.ItemActionName : item.Potion != null ? item.Potion.ItemActionName : item.Recipe != null ? item.Recipe.ItemActionName : item.Enchant != null ? item.Enchant.ItemActionName : item.Scroll != null ? item.Scroll.ItemActionName : item.PetCollar != null ? item.PetCollar.ItemActionName : item.Etc != null ? item.Etc.ItemActionName : null,
        item.Armor != null ? (item.Armor.ItemAction == null ? null : item.Armor.ItemAction.DisplayName) : item.Weapon != null ? (item.Weapon.ItemAction == null ? null : item.Weapon.ItemAction.DisplayName) : item.Arrow != null ? (item.Arrow.ItemAction == null ? null : item.Arrow.ItemAction.DisplayName) : item.Potion != null ? (item.Potion.ItemAction == null ? null : item.Potion.ItemAction.DisplayName) : item.Recipe != null ? (item.Recipe.ItemAction == null ? null : item.Recipe.ItemAction.DisplayName) : item.Enchant != null ? (item.Enchant.ItemAction == null ? null : item.Enchant.ItemAction.DisplayName) : item.Scroll != null ? (item.Scroll.ItemAction == null ? null : item.Scroll.ItemAction.DisplayName) : item.PetCollar != null ? (item.PetCollar.ItemAction == null ? null : item.PetCollar.ItemAction.DisplayName) : item.Etc != null && item.Etc.ItemAction != null ? item.Etc.ItemAction.DisplayName : null,
        item.Armor != null ? item.Armor.ItemBodyPartName : item.Weapon != null ? item.Weapon.ItemBodyPartName : item.Arrow != null ? item.Arrow.ItemBodyPartName : item.Etc != null ? item.Etc.ItemBodyPartName : null,
        item.Armor != null ? (item.Armor.ItemBodyPart == null ? null : item.Armor.ItemBodyPart.DisplayName) : item.Weapon != null ? (item.Weapon.ItemBodyPart == null ? null : item.Weapon.ItemBodyPart.DisplayName) : item.Arrow != null ? (item.Arrow.ItemBodyPart == null ? null : item.Arrow.ItemBodyPart.DisplayName) : item.Etc != null && item.Etc.ItemBodyPart != null ? item.Etc.ItemBodyPart.DisplayName : null,
        item.ItemMaterialName, item.ItemMaterial == null ? null : item.ItemMaterial.DisplayName,
        item.Armor != null ? item.Armor.ItemCrystalTypeName : item.Weapon != null ? item.Weapon.ItemCrystalTypeName : item.Arrow != null ? item.Arrow.ItemCrystalTypeName : item.Etc != null ? item.Etc.ItemCrystalTypeName : null,
        item.Armor != null ? (item.Armor.ItemCrystalType == null ? null : item.Armor.ItemCrystalType.DisplayName) : item.Weapon != null ? (item.Weapon.ItemCrystalType == null ? null : item.Weapon.ItemCrystalType.DisplayName) : item.Arrow != null ? (item.Arrow.ItemCrystalType == null ? null : item.Arrow.ItemCrystalType.DisplayName) : item.Etc != null && item.Etc.ItemCrystalType != null ? item.Etc.ItemCrystalType.DisplayName : null,
        item.Icon, item.Weight, item.Price,
        item.Potion != null ? item.Potion.HandlerName : item.Recipe != null ? item.Recipe.HandlerName : item.Enchant != null ? item.Enchant.HandlerName : item.Scroll != null ? item.Scroll.HandlerName : item.PetCollar != null ? item.PetCollar.HandlerName : item.Etc != null ? item.Etc.HandlerName : null,
        item.Potion != null ? (item.Potion.ItemHandler == null ? null : item.Potion.ItemHandler.DisplayName) : item.Recipe != null ? (item.Recipe.ItemHandler == null ? null : item.Recipe.ItemHandler.DisplayName) : item.Enchant != null ? (item.Enchant.ItemHandler == null ? null : item.Enchant.ItemHandler.DisplayName) : item.Scroll != null ? (item.Scroll.ItemHandler == null ? null : item.Scroll.ItemHandler.DisplayName) : item.PetCollar != null ? (item.PetCollar.ItemHandler == null ? null : item.PetCollar.ItemHandler.DisplayName) : item.Etc != null && item.Etc.ItemHandler != null ? item.Etc.ItemHandler.DisplayName : null,
        item.Skills.OrderBy(value => value.SkillId).ThenBy(value => value.SkillLevel).Select(value => new ItemSkillSummary(
            value.SkillId, value.SkillLevel,
            skills.Where(skill => skill.GameVersion == value.GameVersion && skill.Id == value.SkillId).Select(skill => skill.Name).FirstOrDefault(),
            value.ItemSkillTypeName, value.ItemSkillType == null ? null : value.ItemSkillType.DisplayName, value.Chance)).ToArray(),
        item.AttackGeometry == null ? null : new ItemAttackGeometrySummary(item.AttackGeometry.OffsetX,
            item.AttackGeometry.OffsetY, item.AttackGeometry.Radius, item.AttackGeometry.Length),
        item.Stats == null ? null : new ItemStatsSummary(item.Stats.AccuracyCombat, item.Stats.CriticalRate,
            item.Stats.MagicalAttack, item.Stats.MagicalDefence, item.Stats.MaximumMp, item.Stats.PhysicalAttack,
            item.Stats.PhysicalAttackRange, item.Stats.PhysicalAttackSpeed, item.Stats.PhysicalDefence,
            item.Stats.Evasion, item.Stats.ShieldRate, item.Stats.RandomDamage, item.Stats.ShieldDefence)));

    private static IQueryable<ItemDetailSummary> ProjectItemDetails(IQueryable<Item> items, IQueryable<Skill> skills) => items.Select(item => new ItemDetailSummary(
        new ItemSummary(
            item.Id, item.Name, item.ItemTypeName, item.ItemType.DisplayName, item.ItemType.ParentTypeName,
            item.ItemType.ParentType == null ? null : item.ItemType.ParentType.DisplayName,
            item.Armor != null ? item.Armor.ItemActionName : item.Weapon != null ? item.Weapon.ItemActionName : item.Arrow != null ? item.Arrow.ItemActionName : item.Potion != null ? item.Potion.ItemActionName : item.Recipe != null ? item.Recipe.ItemActionName : item.Enchant != null ? item.Enchant.ItemActionName : item.Scroll != null ? item.Scroll.ItemActionName : item.PetCollar != null ? item.PetCollar.ItemActionName : item.Etc != null ? item.Etc.ItemActionName : null,
            item.Armor != null ? (item.Armor.ItemAction == null ? null : item.Armor.ItemAction.DisplayName) : item.Weapon != null ? (item.Weapon.ItemAction == null ? null : item.Weapon.ItemAction.DisplayName) : item.Arrow != null ? (item.Arrow.ItemAction == null ? null : item.Arrow.ItemAction.DisplayName) : item.Potion != null ? (item.Potion.ItemAction == null ? null : item.Potion.ItemAction.DisplayName) : item.Recipe != null ? (item.Recipe.ItemAction == null ? null : item.Recipe.ItemAction.DisplayName) : item.Enchant != null ? (item.Enchant.ItemAction == null ? null : item.Enchant.ItemAction.DisplayName) : item.Scroll != null ? (item.Scroll.ItemAction == null ? null : item.Scroll.ItemAction.DisplayName) : item.PetCollar != null ? (item.PetCollar.ItemAction == null ? null : item.PetCollar.ItemAction.DisplayName) : item.Etc != null && item.Etc.ItemAction != null ? item.Etc.ItemAction.DisplayName : null,
            item.Armor != null ? item.Armor.ItemBodyPartName : item.Weapon != null ? item.Weapon.ItemBodyPartName : item.Arrow != null ? item.Arrow.ItemBodyPartName : item.Etc != null ? item.Etc.ItemBodyPartName : null,
            item.Armor != null ? (item.Armor.ItemBodyPart == null ? null : item.Armor.ItemBodyPart.DisplayName) : item.Weapon != null ? (item.Weapon.ItemBodyPart == null ? null : item.Weapon.ItemBodyPart.DisplayName) : item.Arrow != null ? (item.Arrow.ItemBodyPart == null ? null : item.Arrow.ItemBodyPart.DisplayName) : item.Etc != null && item.Etc.ItemBodyPart != null ? item.Etc.ItemBodyPart.DisplayName : null,
            item.ItemMaterialName, item.ItemMaterial == null ? null : item.ItemMaterial.DisplayName,
            item.Armor != null ? item.Armor.ItemCrystalTypeName : item.Weapon != null ? item.Weapon.ItemCrystalTypeName : item.Arrow != null ? item.Arrow.ItemCrystalTypeName : item.Etc != null ? item.Etc.ItemCrystalTypeName : null,
            item.Armor != null ? (item.Armor.ItemCrystalType == null ? null : item.Armor.ItemCrystalType.DisplayName) : item.Weapon != null ? (item.Weapon.ItemCrystalType == null ? null : item.Weapon.ItemCrystalType.DisplayName) : item.Arrow != null ? (item.Arrow.ItemCrystalType == null ? null : item.Arrow.ItemCrystalType.DisplayName) : item.Etc != null && item.Etc.ItemCrystalType != null ? item.Etc.ItemCrystalType.DisplayName : null,
            item.Icon, item.Weight, item.Price,
            item.Potion != null ? item.Potion.HandlerName : item.Recipe != null ? item.Recipe.HandlerName : item.Enchant != null ? item.Enchant.HandlerName : item.Scroll != null ? item.Scroll.HandlerName : item.PetCollar != null ? item.PetCollar.HandlerName : item.Etc != null ? item.Etc.HandlerName : null,
            item.Potion != null ? (item.Potion.ItemHandler == null ? null : item.Potion.ItemHandler.DisplayName) : item.Recipe != null ? (item.Recipe.ItemHandler == null ? null : item.Recipe.ItemHandler.DisplayName) : item.Enchant != null ? (item.Enchant.ItemHandler == null ? null : item.Enchant.ItemHandler.DisplayName) : item.Scroll != null ? (item.Scroll.ItemHandler == null ? null : item.Scroll.ItemHandler.DisplayName) : item.PetCollar != null ? (item.PetCollar.ItemHandler == null ? null : item.PetCollar.ItemHandler.DisplayName) : item.Etc != null && item.Etc.ItemHandler != null ? item.Etc.ItemHandler.DisplayName : null,
            item.Skills.OrderBy(value => value.SkillId).ThenBy(value => value.SkillLevel).Select(value => new ItemSkillSummary(
                value.SkillId, value.SkillLevel,
                skills.Where(skill => skill.GameVersion == value.GameVersion && skill.Id == value.SkillId)
                    .Select(skill => skill.Name).FirstOrDefault(),
                value.ItemSkillTypeName, value.ItemSkillType == null ? null : value.ItemSkillType.DisplayName,
                value.Chance)).ToArray(),
            item.AttackGeometry == null ? null : new ItemAttackGeometrySummary(item.AttackGeometry.OffsetX,
                item.AttackGeometry.OffsetY, item.AttackGeometry.Radius, item.AttackGeometry.Length),
            item.Stats == null ? null : new ItemStatsSummary(item.Stats.AccuracyCombat, item.Stats.CriticalRate,
                item.Stats.MagicalAttack, item.Stats.MagicalDefence, item.Stats.MaximumMp, item.Stats.PhysicalAttack,
                item.Stats.PhysicalAttackRange, item.Stats.PhysicalAttackSpeed, item.Stats.PhysicalDefence,
                item.Stats.Evasion, item.Stats.ShieldRate, item.Stats.RandomDamage, item.Stats.ShieldDefence)),
        new ItemPropertiesSummary(
            item.Weapon != null ? item.Weapon.DisplayId : item.Etc != null ? item.Etc.DisplayId : null,
            item.Armor != null ? item.Armor.CrystalCount : item.Weapon != null ? item.Weapon.CrystalCount : null,
            item.Weapon == null ? null : item.Weapon.Soulshots, item.Weapon == null ? null : item.Weapon.Spiritshots,
            item.Weapon == null ? null : item.Weapon.MpConsume, item.Weapon == null ? null : item.Weapon.ReducedMpConsume,
            item.Weapon != null ? item.Weapon.ReuseDelay : item.Potion != null ? item.Potion.ReuseDelay : item.Etc != null ? item.Etc.ReuseDelay : null,
            item.Recipe == null ? null : item.Recipe.RecipeId, item.Etc == null ? null : item.Etc.ItemSkill,
            item.PetCollar != null ? item.PetCollar.UseCondition : item.Etc != null ? item.Etc.UseCondition : null,
            item.Weapon == null ? null : item.Weapon.ElementEnabled,
            item.Weapon == null ? null : item.Weapon.IsAttackWeapon, item.Weapon == null ? null : item.Weapon.IsForceEquip,
            item.Weapon == null ? null : item.Weapon.IsMagicWeapon,
            item.Etc == null ? null : item.Etc.IsQuestItem,
            item.Weapon == null ? null : item.Weapon.UseWeaponSkillsOnly),
        item.BehaviorAvailability == null ? null : new ItemBehaviorAvailabilitySummary(
            item.BehaviorAvailability.EnchantEnabled,
            item.BehaviorAvailability.ForNpc,
            item.BehaviorAvailability.ImmediateEffect,
            item.BehaviorAvailability.IsDepositable,
            item.BehaviorAvailability.IsDestroyable,
            item.BehaviorAvailability.IsDropable,
            item.BehaviorAvailability.IsOlyRestricted,
            item.BehaviorAvailability.IsSellable,
            item.BehaviorAvailability.IsStackable,
            item.BehaviorAvailability.IsTradable),
        null,
        item.Condition == null ? null : new ItemConditionSummary(
            item.Condition.MessageId,
            item.Condition.AddName,
            item.Condition.Player.IsPvpFlagged,
            item.Condition.Player.PlayerRaces,
            item.Condition.Player.PlayerCategoryTypes)));

    private static ItemConditionSummary ToSummary(ItemCondition condition) => new(
        condition.MessageId,
        condition.AddName,
        condition.Player.IsPvpFlagged,
        condition.Player.PlayerRaces,
        condition.Player.PlayerCategoryTypes);

    private static string? JoinTokens(IReadOnlyList<string>? values)
    {
        var tokens = values?.Select(value => value.Trim().ToUpperInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray() ?? [];
        return tokens.Length == 0 ? null : string.Join(',', tokens);
    }

    private static IQueryable<ItemSkillSummary> ProjectItemSkills(
        IQueryable<ItemSkill> itemSkills,
        IQueryable<Skill> skills) => itemSkills.Select(value => new ItemSkillSummary(
            value.SkillId,
            value.SkillLevel,
            skills.Where(skill => skill.GameVersion == value.GameVersion && skill.Id == value.SkillId)
                .Select(skill => skill.Name).FirstOrDefault(),
            value.ItemSkillTypeName,
            value.ItemSkillType == null ? null : value.ItemSkillType.DisplayName,
            value.Chance));

    private static async Task ValidateItemSkillAsync(
        GameContentDbContext context,
        string gameVersion,
        int skillId,
        short skillLevel,
        string? itemSkillTypeName,
        CancellationToken token)
    {
        var skill = await context.Skills.AsNoTracking().SingleOrDefaultAsync(
            value => value.GameVersion == gameVersion && value.Id == skillId,
            token);
        if (skill is null || skillLevel > skill.Levels)
            throw new InvalidOperationException("The selected skill and level are not available for this game version.");
        if (itemSkillTypeName is not null && !await context.ItemSkillTypes.AnyAsync(value =>
                value.GameVersion == gameVersion && value.Name == itemSkillTypeName, token))
            throw new InvalidOperationException("The selected item skill type is not available for this game version.");
    }

    private static bool TryParsePrimarySkill(string value, out int skillId, out short skillLevel)
    {
        var parts = value.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[0], out skillId) && skillId > 0 &&
            short.TryParse(parts[1], out skillLevel) && skillLevel > 0)
            return true;
        skillId = 0;
        skillLevel = 0;
        return false;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<Item> FamilyItems(IQueryable<Item> items, string family) => family switch
    {
        ItemFamilyValues.Armor => items.Where(item => item.Armor != null),
        ItemFamilyValues.Weapon => items.Where(item => item.Weapon != null),
        ItemFamilyValues.Arrow => items.Where(item => item.Arrow != null),
        ItemFamilyValues.Material => items.Where(item => item.Material != null),
        ItemFamilyValues.Potion => items.Where(item => item.Potion != null),
        ItemFamilyValues.Recipe => items.Where(item => item.Recipe != null),
        ItemFamilyValues.Enchant => items.Where(item => item.Enchant != null),
        ItemFamilyValues.Scroll => items.Where(item => item.Scroll != null),
        ItemFamilyValues.PetCollar => items.Where(item => item.PetCollar != null),
        ItemFamilyValues.Etc => items.Where(item => item.Etc != null),
        _ => items.Where(_ => false)
    };

    private static void ApplyEditableFamilyFields(Item item, UpdateItemRequest request)
    {
        var action = Trim(request.ItemActionName);
        var bodyPart = Trim(request.ItemBodyPartName);
        var crystalType = Trim(request.ItemCrystalTypeName);
        var handler = Trim(request.HandlerName);
        if (item.Armor is not null) { item.Armor.ItemActionName = action; item.Armor.ItemBodyPartName = bodyPart; item.Armor.ItemCrystalTypeName = crystalType; }
        else if (item.Weapon is not null) { item.Weapon.ItemActionName = action; item.Weapon.ItemBodyPartName = bodyPart; item.Weapon.ItemCrystalTypeName = crystalType; }
        else if (item.Arrow is not null) { item.Arrow.ItemActionName = action; item.Arrow.ItemBodyPartName = bodyPart; item.Arrow.ItemCrystalTypeName = crystalType; }
        else if (item.Potion is not null) { item.Potion.ItemActionName = action; item.Potion.HandlerName = handler; }
        else if (item.Recipe is not null) { item.Recipe.ItemActionName = action; item.Recipe.HandlerName = handler; }
        else if (item.Enchant is not null) { item.Enchant.ItemActionName = action; item.Enchant.HandlerName = handler; }
        else if (item.Scroll is not null) { item.Scroll.ItemActionName = action; item.Scroll.HandlerName = handler; }
        else if (item.PetCollar is not null) { item.PetCollar.ItemActionName = action; item.PetCollar.HandlerName = handler; }
        else if (item.Etc is not null) { item.Etc.ItemActionName = action; item.Etc.ItemBodyPartName = bodyPart; item.Etc.ItemCrystalTypeName = crystalType; item.Etc.HandlerName = handler; }
    }

    private static async Task<int> CountActions(GameContentDbContext context, string gameVersion, string name, CancellationToken token) =>
        await context.ItemArmor.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemWeapons.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemArrows.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemPotions.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemRecipes.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemEnchants.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemScrolls.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemPetCollars.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token) +
        await context.ItemEtc.CountAsync(item => item.GameVersion == gameVersion && item.ItemActionName == name, token);

    private static async Task<int> CountBodyParts(GameContentDbContext context, string gameVersion, string name, CancellationToken token) =>
        await context.ItemArmor.CountAsync(item => item.GameVersion == gameVersion && item.ItemBodyPartName == name, token) +
        await context.ItemWeapons.CountAsync(item => item.GameVersion == gameVersion && item.ItemBodyPartName == name, token) +
        await context.ItemArrows.CountAsync(item => item.GameVersion == gameVersion && item.ItemBodyPartName == name, token) +
        await context.ItemEtc.CountAsync(item => item.GameVersion == gameVersion && item.ItemBodyPartName == name, token);

    private static async Task<int> CountCrystalTypes(GameContentDbContext context, string gameVersion, string name, CancellationToken token) =>
        await context.ItemArmor.CountAsync(item => item.GameVersion == gameVersion && item.ItemCrystalTypeName == name, token) +
        await context.ItemWeapons.CountAsync(item => item.GameVersion == gameVersion && item.ItemCrystalTypeName == name, token) +
        await context.ItemArrows.CountAsync(item => item.GameVersion == gameVersion && item.ItemCrystalTypeName == name, token) +
        await context.ItemEtc.CountAsync(item => item.GameVersion == gameVersion && item.ItemCrystalTypeName == name, token);

    private static async Task<int> CountHandlers(GameContentDbContext context, string gameVersion, string name, CancellationToken token) =>
        await context.ItemPotions.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token) +
        await context.ItemRecipes.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token) +
        await context.ItemEnchants.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token) +
        await context.ItemScrolls.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token) +
        await context.ItemPetCollars.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token) +
        await context.ItemEtc.CountAsync(item => item.GameVersion == gameVersion && item.HandlerName == name, token);

    private static async Task<bool> LookupExists(GameContentDbContext context, string gameVersion, string kind, string? name, CancellationToken token) =>
        name is null || kind switch
        {
            "item-types" => await context.ItemTypes.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-actions" => await context.ItemActions.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-body-parts" => await context.ItemBodyParts.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-materials" => await context.ItemMaterials.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-crystal-types" => await context.ItemCrystalTypes.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
            "item-handlers" => await context.ItemHandlers.AnyAsync(value => value.GameVersion == gameVersion && value.Name == name, token),
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

    private static async Task<bool> Delete<TEntity>(DbSet<TEntity> set, string gameVersion, string name, CancellationToken token)
        where TEntity : class
    {
        var entity = await set.FindAsync([gameVersion, name], token);
        if (entity is null) return false;
        set.Remove(entity);
        return true;
    }
}
