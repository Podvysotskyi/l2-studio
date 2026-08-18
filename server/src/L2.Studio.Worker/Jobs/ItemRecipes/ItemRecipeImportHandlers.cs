using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class ItemRecipeImportHandlers(IDbContextFactory<GameContentDbContext> contextFactory, TimeProvider timeProvider)
{
    private static readonly C1ItemRecipeCatalog Catalog = new();

    public Task Handle(ImportC1ItemRecipes message, CancellationToken token) => ImportAsync(message.RunId, token);

    private async Task ImportAsync(Guid runId, CancellationToken token)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(token);
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var run = await context.ContentImportRuns.SingleOrDefaultAsync(value =>
                value.Id == runId && value.Kind == ContentImportTargetValues.ItemRecipes, token);
            if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
            if (run.GameVersion != "c1" || !ItemImportJobValues.SupportedModes.Contains(run.Mode))
                throw new InvalidOperationException("Only C1 add-missing and restore-defaults item-recipe imports are supported.");

            run.Status = ItemImportJobValues.Running;
            run.StartedAt ??= timeProvider.GetUtcNow();
            run.LastHeartbeatAt = timeProvider.GetUtcNow();

            await EnsureTypesAsync(context, run.GameVersion, token);
            var existing = await context.CraftingRecipes
                .Include(value => value.Ingredients)
                .Include(value => value.Productions)
                .Include(value => value.StatUse)
                .Where(value => value.GameVersion == run.GameVersion)
                .ToDictionaryAsync(value => value.Id, token);
            var missing = Catalog.Recipes.Where(definition => !existing.ContainsKey(definition.Id)).ToArray();
            context.CraftingRecipes.AddRange(missing.Select(definition => ToEntity(run.GameVersion, definition)));

            var restored = Array.Empty<ItemRecipeDefinition>();
            if (run.Mode == ItemImportJobValues.RestoreDefaults)
            {
                restored = Catalog.Recipes.Where(definition => existing.ContainsKey(definition.Id)).ToArray();
                foreach (var definition in restored) Restore(context, existing[definition.Id], definition);
            }

            run.TotalCount = Catalog.Recipes.Count;
            run.InsertedCount = missing.Length;
            run.ExistingCount = Catalog.Recipes.Count - missing.Length;
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

    private static async Task EnsureTypesAsync(GameContentDbContext context, string gameVersion, CancellationToken token)
    {
        var existing = await context.ItemRecipeTypes.Where(value => value.GameVersion == gameVersion)
            .Select(value => value.Name).ToHashSetAsync(StringComparer.Ordinal, token);
        context.ItemRecipeTypes.AddRange(Catalog.Types.Where(value => !existing.Contains(value.Name))
            .Select(value => new ItemRecipeType { GameVersion = gameVersion, Name = value.Name }));
    }

    private static ItemRecipe ToEntity(string gameVersion, ItemRecipeDefinition definition)
    {
        var recipe = new ItemRecipe
        {
            GameVersion = gameVersion,
            Id = definition.Id,
            Name = definition.Name,
            ItemRecipeTypeName = definition.ItemRecipeTypeName
        };
        Apply(recipe, definition);
        return recipe;
    }

    private static void Restore(GameContentDbContext context, ItemRecipe recipe, ItemRecipeDefinition definition)
    {
        context.ItemRecipeIngredients.RemoveRange(recipe.Ingredients);
        context.ItemRecipeProductions.RemoveRange(recipe.Productions);
        if (recipe.StatUse is not null) context.ItemRecipeStatUses.Remove(recipe.StatUse);
        recipe.Ingredients.Clear();
        recipe.Productions.Clear();
        recipe.StatUse = null;
        Apply(recipe, definition);
    }

    private static void Apply(ItemRecipe recipe, ItemRecipeDefinition definition)
    {
        recipe.Name = definition.Name;
        recipe.ItemRecipeTypeName = definition.ItemRecipeTypeName;
        recipe.CraftLevel = definition.CraftLevel;
        recipe.SuccessRate = definition.SuccessRate;
        foreach (var ingredient in definition.Ingredients)
            recipe.Ingredients.Add(new ItemRecipeIngredient
            {
                GameVersion = recipe.GameVersion,
                ItemRecipeId = recipe.Id,
                ItemId = ingredient.ItemId,
                Count = ingredient.Count
            });
        foreach (var production in definition.Productions)
            recipe.Productions.Add(new ItemRecipeProduction
            {
                GameVersion = recipe.GameVersion,
                ItemRecipeId = recipe.Id,
                ItemId = production.ItemId,
                Count = production.Count
            });
        recipe.StatUse = new ItemRecipeStatUse
        {
            GameVersion = recipe.GameVersion,
            ItemRecipeId = recipe.Id,
            Mp = definition.StatUse.Mp,
            Hp = definition.StatUse.Hp
        };
    }

    private async Task MarkFailed(Guid runId, Exception exception, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(value => value.Id == runId, token);
        if (run is null || ItemImportJobValues.TerminalStatuses.Contains(run.Status)) return;
        run.Status = ItemImportJobValues.Failed;
        var error = exception.ToString();
        run.Error = error[..Math.Min(error.Length, 4000)];
        run.FinishedAt = timeProvider.GetUtcNow();
        run.LastHeartbeatAt = run.FinishedAt;
        await context.SaveChangesAsync(token);
    }
}
