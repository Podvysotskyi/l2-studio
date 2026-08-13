using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IContentDirectoryRepository
{
    Task<NpcDirectoryPage> SearchNpcsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerRacesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(string gameVersion, CancellationToken cancellationToken);
}
