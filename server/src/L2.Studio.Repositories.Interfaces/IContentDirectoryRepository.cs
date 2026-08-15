using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;

namespace L2.Studio.Repositories.Interfaces;

public interface IContentDirectoryRepository
{
    Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, ItemDirectoryRequest request, CancellationToken cancellationToken);
    Task<ItemSummary?> GetItemAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<ItemSummary?> UpdateItemAsync(string gameVersion, int id, UpdateItemRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ItemLookupSummary>> GetItemLookupsAsync(string gameVersion, string kind, CancellationToken cancellationToken);
    Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<NpcDirectoryPage> SearchNpcsAsync(string gameVersion, NpcDirectoryRequest request, CancellationToken cancellationToken);
    Task<NpcSummary?> GetNpcAsync(string gameVersion, int id, CancellationToken cancellationToken);
    Task<NpcSummary?> UpdateNpcAsync(string gameVersion, int id, string name, short level, string npcTypeName, string? npcRaceName, string npcSexName, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerRacesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerFacesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairStylesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairColorsAsync(string gameVersion, CancellationToken cancellationToken);
    Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(string gameVersion, CancellationToken cancellationToken);
}
