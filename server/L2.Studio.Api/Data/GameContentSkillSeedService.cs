using L2.Studio.Content;
using L2.Studio.Content.Seeding;
using Microsoft.Extensions.Options;

namespace L2.Studio.Api.Data;

public sealed class GameContentSkillSeedService(
    SkillSeeder seeder,
    IOptions<GameContentOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.SeedSkills)
        {
            return;
        }

        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
