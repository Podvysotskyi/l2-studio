using L2.Studio.Context;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace L2.Studio.Worker;

[WolverineHandler]
public sealed class ContentImportDispatchHandler(IDbContextFactory<GameContentDbContext> contextFactory)
{
    public async Task<object?> Handle(RunContentImport message, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.ContentImportRuns.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == message.JobId, cancellationToken);
        if (run is null || ImportJobValues.TerminalStatuses.Contains(run.Status)) return null;
        return (run.GameVersion, run.Kind) switch
        {
            ("c1", ContentImportTargetValues.Items) => new ImportC1Items(run.Id),
            ("c1", ContentImportTargetValues.ItemTypes) => new ImportC1ItemTypes(run.Id),
            ("c1", ContentImportTargetValues.ItemActions) => new ImportC1ItemActions(run.Id),
            ("c1", ContentImportTargetValues.ItemBodyParts) => new ImportC1ItemBodyParts(run.Id),
            ("c1", ContentImportTargetValues.ItemMaterials) => new ImportC1ItemMaterials(run.Id),
            ("c1", ContentImportTargetValues.ItemCrystalTypes) => new ImportC1ItemCrystalTypes(run.Id),
            ("c1", ContentImportTargetValues.ItemHandlers) => new ImportC1ItemHandlers(run.Id),
            ("c1", ContentImportTargetValues.ItemSkillTypes) => new ImportC1ItemSkillTypes(run.Id),
            ("c1", ContentImportTargetValues.Npcs) => new ImportC1Npcs(run.Id),
            ("c1", ContentImportTargetValues.NpcTypes) => new ImportC1NpcTypes(run.Id),
            ("c4", ContentImportTargetValues.NpcTypes) => new ImportC4NpcTypes(run.Id),
            ("interlude", ContentImportTargetValues.NpcTypes) => new ImportInterludeNpcTypes(run.Id),
            ("c1", ContentImportTargetValues.NpcRaces) => new ImportC1NpcRaces(run.Id),
            ("c4", ContentImportTargetValues.NpcRaces) => new ImportC4NpcRaces(run.Id),
            ("interlude", ContentImportTargetValues.NpcRaces) => new ImportInterludeNpcRaces(run.Id),
            ("c1", ContentImportTargetValues.NpcSexes) => new ImportC1NpcSexes(run.Id),
            ("c4", ContentImportTargetValues.NpcSexes) => new ImportC4NpcSexes(run.Id),
            ("interlude", ContentImportTargetValues.NpcSexes) => new ImportInterludeNpcSexes(run.Id),
            ("c1", ContentImportTargetValues.PlayerRaces) => new ImportC1PlayerRaces(run.Id),
            ("c1", ContentImportTargetValues.PlayerSexes) => new ImportC1PlayerSexes(run.Id),
            ("c1", ContentImportTargetValues.PlayerClasses) => new ImportC1PlayerClasses(run.Id),
            ("c1", ContentImportTargetValues.PlayerFaces) => new ImportC1PlayerFaces(run.Id),
            ("c1", ContentImportTargetValues.PlayerHairStyles) => new ImportC1PlayerHairStyles(run.Id),
            ("c1", ContentImportTargetValues.PlayerHairColors) => new ImportC1PlayerHairColors(run.Id),
            ("c1", ContentImportTargetValues.Skills) => new ImportC1Skills(run.Id),
            ("c1", ContentImportTargetValues.SkillOperateTypes) => new ImportC1SkillOperateTypes(run.Id),
            ("c1", ContentImportTargetValues.SkillTargetTypes) => new ImportC1SkillTargetTypes(run.Id),
            _ => throw new InvalidOperationException($"Unsupported content import target '{run.GameVersion}/{run.Kind}'.")
        };
    }
}
