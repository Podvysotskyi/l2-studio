using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;

namespace L2.Studio.Repositories.Interfaces;

/// <summary>
/// Persistence boundary for item definitions and the lookups they own.
/// </summary>
public interface IItemRepository
{
    Task<IReadOnlyList<ItemIconSummary>> ResolveItemIconsAsync(string gameVersion, IReadOnlyList<ItemIconReference> items, CancellationToken cancellationToken);
    Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, string family, ItemDirectoryRequest request, CancellationToken cancellationToken);
    Task<ItemDetailSummary?> GetItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken);
    Task<ItemSummary?> UpdateItemAsync(string gameVersion, string family, int id, UpdateItemRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken);
    Task<ItemConditionSummary?> UpdateItemConditionAsync(string gameVersion, string family, int itemId, UpdateItemConditionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteItemConditionAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken);
    Task<ItemSetDirectoryPage> SearchItemSetsAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken);
    Task<DirectoryPage<ItemRecipeSummary>> SearchItemRecipesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken);
    Task<DirectoryPage<ItemRecipeTypeSummary>> SearchItemRecipeTypesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken);
    Task<ItemSetSummary?> GetItemSetAsync(string gameVersion, int setId, CancellationToken cancellationToken);
    Task<ItemSetSummary?> UpdateItemSetAsync(string gameVersion, int setId, UpdateItemSetRequest request, CancellationToken cancellationToken);
    Task<ItemPrimarySkillSummary?> SetItemPrimarySkillAsync(string gameVersion, string family, int itemId, SetItemPrimarySkillRequest request, CancellationToken cancellationToken);
    Task<bool> ClearItemPrimarySkillAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken);
    Task<ItemSkillSummary?> CreateItemSkillAsync(string gameVersion, string family, int itemId, CreateItemSkillRequest request, CancellationToken cancellationToken);
    Task<ItemSkillSummary?> UpdateItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, UpdateItemSkillRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, CancellationToken cancellationToken);
    Task<DirectoryPage<ItemTypeSummary>> SearchItemTypesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken);
    Task<DirectoryPage<ItemLookupSummary>> SearchItemLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken);
    Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<bool> DeleteItemLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken);
}
