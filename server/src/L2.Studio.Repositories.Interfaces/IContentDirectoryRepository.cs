using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;

namespace L2.Studio.Repositories.Interfaces;

public interface IContentDirectoryRepository
{
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
    Task<NpcDirectoryPage> SearchNpcsAsync(string gameVersion, NpcDirectoryRequest request, CancellationToken cancellationToken);
    Task<NpcSpawnWorldMap> GetNpcSpawnWorldMapAsync(string gameVersion, CancellationToken cancellationToken);
    Task<NpcSummary?> GetNpcAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<NpcSummary?> UpdateNpcAsync(string gameVersion, int id, string name, short level, string npcTypeName, string? npcRaceName, string npcSexName, CancellationToken cancellationToken);
    Task<bool> DeleteNpcAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<DirectoryPage<NpcLookupSummary>> SearchNpcLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken);
    Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<bool> DeleteNpcLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<PlayerClassSummary?> UpdatePlayerClassAsync(string gameVersion, int id, UpdatePlayerClassRequest request, CancellationToken cancellationToken);
    Task<bool> DeletePlayerClassAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<DirectoryPage<PlayerLookupSummary>> SearchPlayerLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken);
    Task<PlayerLookupSummary?> UpdatePlayerLookupNameAsync(string gameVersion, string kind, int id, string name, CancellationToken cancellationToken);
    Task<bool> DeletePlayerLookupAsync(string gameVersion, string kind, int id, CancellationToken cancellationToken);
    Task<DirectoryPage<PlayerAppearanceSummary>> SearchPlayerAppearancesAsync(string gameVersion, string kind, PlayerAppearanceDirectoryRequest request, CancellationToken cancellationToken);
    Task<PlayerAppearanceSummary?> UpdatePlayerAppearanceNameAsync(string gameVersion, string kind, int id, int playerRaceId, int playerSexId, string name, CancellationToken cancellationToken);
    Task<bool> DeletePlayerAppearanceAsync(string gameVersion, string kind, int id, int playerRaceId, int playerSexId, CancellationToken cancellationToken);
    Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<SkillSummary?> GetSkillAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<SkillSummary?> UpdateSkillAsync(string gameVersion, int id, UpdateSkillRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteSkillAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<DirectoryPage<SkillLookupSummary>> SearchSkillLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken);
    Task<SkillLookupSummary?> UpdateSkillLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<bool> DeleteSkillLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken);
}
