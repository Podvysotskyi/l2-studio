using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;

namespace L2.Studio.Repositories;

public sealed partial class ContentDirectoryRepository
{
    public Task<IReadOnlyList<ItemIconSummary>> ResolveItemIconsAsync(string gameVersion, IReadOnlyList<ItemIconReference> items, CancellationToken cancellationToken) => itemRepository.ResolveItemIconsAsync(gameVersion, items, cancellationToken);
    public Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, string family, ItemDirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemsAsync(gameVersion, family, request, cancellationToken);
    public Task<ItemDetailSummary?> GetItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken) => itemRepository.GetItemAsync(gameVersion, family, id, cancellationToken);
    public Task<ItemSummary?> UpdateItemAsync(string gameVersion, string family, int id, UpdateItemRequest request, CancellationToken cancellationToken) => itemRepository.UpdateItemAsync(gameVersion, family, id, request, cancellationToken);
    public Task<bool> DeleteItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken) => itemRepository.DeleteItemAsync(gameVersion, family, id, cancellationToken);
    public Task<ItemConditionSummary?> UpdateItemConditionAsync(string gameVersion, string family, int itemId, UpdateItemConditionRequest request, CancellationToken cancellationToken) => itemRepository.UpdateItemConditionAsync(gameVersion, family, itemId, request, cancellationToken);
    public Task<bool> DeleteItemConditionAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken) => itemRepository.DeleteItemConditionAsync(gameVersion, family, itemId, cancellationToken);
    public Task<ItemSetDirectoryPage> SearchItemSetsAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemSetsAsync(gameVersion, request, cancellationToken);
    public Task<DirectoryPage<ItemRecipeSummary>> SearchItemRecipesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemRecipesAsync(gameVersion, request, cancellationToken);
    public Task<DirectoryPage<ItemRecipeTypeSummary>> SearchItemRecipeTypesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemRecipeTypesAsync(gameVersion, request, cancellationToken);
    public Task<ItemSetSummary?> GetItemSetAsync(string gameVersion, int setId, CancellationToken cancellationToken) => itemRepository.GetItemSetAsync(gameVersion, setId, cancellationToken);
    public Task<ItemSetSummary?> UpdateItemSetAsync(string gameVersion, int setId, UpdateItemSetRequest request, CancellationToken cancellationToken) => itemRepository.UpdateItemSetAsync(gameVersion, setId, request, cancellationToken);
    public Task<ItemPrimarySkillSummary?> SetItemPrimarySkillAsync(string gameVersion, string family, int itemId, SetItemPrimarySkillRequest request, CancellationToken cancellationToken) => itemRepository.SetItemPrimarySkillAsync(gameVersion, family, itemId, request, cancellationToken);
    public Task<bool> ClearItemPrimarySkillAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken) => itemRepository.ClearItemPrimarySkillAsync(gameVersion, family, itemId, cancellationToken);
    public Task<ItemSkillSummary?> CreateItemSkillAsync(string gameVersion, string family, int itemId, CreateItemSkillRequest request, CancellationToken cancellationToken) => itemRepository.CreateItemSkillAsync(gameVersion, family, itemId, request, cancellationToken);
    public Task<ItemSkillSummary?> UpdateItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, UpdateItemSkillRequest request, CancellationToken cancellationToken) => itemRepository.UpdateItemSkillAsync(gameVersion, family, itemId, skillId, skillLevel, request, cancellationToken);
    public Task<bool> DeleteItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, CancellationToken cancellationToken) => itemRepository.DeleteItemSkillAsync(gameVersion, family, itemId, skillId, skillLevel, cancellationToken);
    public Task<DirectoryPage<ItemTypeSummary>> SearchItemTypesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemTypesAsync(gameVersion, request, cancellationToken);
    public Task<DirectoryPage<ItemLookupSummary>> SearchItemLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken) => itemRepository.SearchItemLookupsAsync(gameVersion, kind, request, cancellationToken);
    public Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken) => itemRepository.UpdateItemLookupDisplayNameAsync(gameVersion, kind, name, displayName, cancellationToken);
    public Task<bool> DeleteItemLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken) => itemRepository.DeleteItemLookupAsync(gameVersion, kind, name, cancellationToken);
}
