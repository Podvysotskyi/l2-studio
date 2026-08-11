using L2.Studio.Configurations;
using L2.Studio.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(ServiceIdentity identity, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("info")]
    public SystemInfo GetInfo() => new(identity.Name, StudioHostConfigurationExtensions.BuildVersion(), environment.EnvironmentName);
}
