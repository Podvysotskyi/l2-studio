using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public async Task<DirectoryPage<ItemRecipeSummary>> SearchItemRecipesAsync(
        string gameVersion,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var isId = int.TryParse(query, out var id);
        var offset = ((long)request.Page - 1) * request.PageSize;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var recipes = context.CraftingRecipes.AsNoTracking()
            .Where(value => value.GameVersion == gameVersion);
        if (query != string.Empty)
        {
            var matchingItems = context.Items.AsNoTracking().Where(item => item.GameVersion == gameVersion &&
                ((isId && item.Id == id) || EF.Functions.ILike(item.Name, pattern, "\\")));
            recipes = recipes.Where(value =>
                (isId && value.Id == id) ||
                EF.Functions.ILike(value.Name, pattern, "\\") ||
                value.Ingredients.Any(ingredient =>
                    (isId && ingredient.ItemId == id) || matchingItems.Any(item => item.Id == ingredient.ItemId)) ||
                value.Productions.Any(production =>
                    (isId && production.ItemId == id) || matchingItems.Any(item => item.Id == production.ItemId)));
        }

        var total = await recipes.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue)
            return new DirectoryPage<ItemRecipeSummary>([], total, request.Page, request.PageSize);
        var items = await ProjectItemRecipes(
                recipes.OrderBy(value => value.Id).Skip((int)offset).Take(request.PageSize),
                context.Items.AsNoTracking())
            .ToListAsync(cancellationToken);
        return new DirectoryPage<ItemRecipeSummary>(items, total, request.Page, request.PageSize);
    }

    public async Task<DirectoryPage<ItemRecipeTypeSummary>> SearchItemRecipeTypesAsync(
        string gameVersion,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var pattern = $"%{EscapeLikePattern(query)}%";
        var offset = ((long)request.Page - 1) * request.PageSize;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var types = context.ItemRecipeTypes.AsNoTracking().Where(value => value.GameVersion == gameVersion);
        if (query != string.Empty)
            types = types.Where(value => EF.Functions.ILike(value.Name, pattern, "\\"));

        var total = await types.LongCountAsync(cancellationToken);
        if (offset > int.MaxValue)
            return new DirectoryPage<ItemRecipeTypeSummary>([], total, request.Page, request.PageSize);
        var items = await types.OrderBy(value => value.Name)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Select(value => new ItemRecipeTypeSummary(value.Name, value.Recipes.Count))
            .ToListAsync(cancellationToken);
        return new DirectoryPage<ItemRecipeTypeSummary>(items, total, request.Page, request.PageSize);
    }

    private static IQueryable<ItemRecipeSummary> ProjectItemRecipes(
        IQueryable<ItemRecipe> recipes,
        IQueryable<Item> items) => recipes.Select(value => new ItemRecipeSummary(
        value.Id,
        value.Name,
        value.ItemRecipeTypeName,
        value.CraftLevel,
        value.SuccessRate,
        value.StatUse == null ? null : new ItemRecipeStatUseSummary(value.StatUse.Mp, value.StatUse.Hp),
        value.Ingredients.OrderBy(ingredient => ingredient.ItemId).Select(ingredient => new ItemRecipeItemSummary(
            ingredient.ItemId,
            items.Where(item => item.GameVersion == value.GameVersion && item.Id == ingredient.ItemId)
                .Select(item => item.Name).FirstOrDefault(),
            ingredient.Count)).ToArray(),
        value.Productions.OrderBy(production => production.ItemId).Select(production => new ItemRecipeItemSummary(
            production.ItemId,
            items.Where(item => item.GameVersion == value.GameVersion && item.Id == production.ItemId)
                .Select(item => item.Name).FirstOrDefault(),
            production.Count)).ToArray()));
}
