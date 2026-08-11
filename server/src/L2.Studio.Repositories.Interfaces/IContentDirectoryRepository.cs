using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IContentDirectoryRepository
{
    Task<NpcDirectoryPage> SearchNpcsAsync(string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerRacesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(CancellationToken cancellationToken);
    Task<SkillDirectoryPage> SearchSkillsAsync(string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(CancellationToken cancellationToken);
}
