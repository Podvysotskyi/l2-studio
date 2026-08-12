using L2.Studio.Contracts;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions")]
public sealed class GameVersionsController(IGameVersionRepository versions) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GameVersionSummary>> List(CancellationToken token) => versions.ListAsync(token);
}
