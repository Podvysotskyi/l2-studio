using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Exceptions;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;

namespace L2.Studio.Api.Controllers;

[ApiController]
[Route("api/game-versions/{gameVersion}/assets/{kind}/imports")]
public sealed class AssetImportsController(IAssetImportRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AssetImportRunSummary>> Queue(
        string gameVersion, string kind, [FromBody] AssetImportRequest? request, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        AssetImportRunSummary? run;
        try
        {
            if (kind == AssetImportJobValues.MapPreviews && !string.IsNullOrWhiteSpace(request?.MapName))
            {
                var fileName = request.MapName.EndsWith(".unr", StringComparison.OrdinalIgnoreCase)
                    ? request.MapName
                    : request.MapName + ".unr";
                run = await repository.QueueSingleFileAsync(gameVersion, kind, fileName, request.Force, token);
            }
            else
            {
                run = await repository.QueueFullScanAsync(gameVersion, kind, request?.Force ?? false, token);
            }
        }
        catch (ArgumentException exception)
        {
            return ValidationError("mapName", exception.Message);
        }
        catch (AssetImportTargetNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        return run is null
            ? Conflict(new { message = $"An import for '{kind}' conflicts with an active run." })
            : Accepted($"/api/game-versions/{gameVersion}/assets/{kind}/imports/{run.Id}", run);
    }

    [HttpPost("files/{**fileName}")]
    public async Task<ActionResult<AssetImportRunSummary>> QueueFile(
        string kind,
        string gameVersion,
        string fileName,
        [FromQuery] bool force,
        CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        try
        {
            var run = await repository.QueueSingleFileAsync(gameVersion, kind, fileName, force, token);
            return run is null
                ? Conflict(new { message = $"The requested '{kind}' file conflicts with an active run." })
                : Accepted($"/api/game-versions/{gameVersion}/assets/{kind}/imports/{run.Id}", run);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["fileName"] = [exception.Message]
            }));
        }
        catch (AssetImportTargetNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("resources")]
    public async Task<ActionResult<AssetImportRunSummary>> QueueResource(
        string kind,
        string gameVersion,
        [FromBody] AssetResourceImportRequest request,
        CancellationToken token)
    {
        if (kind is not (AssetImportJobValues.Textures or AssetImportJobValues.StaticMeshes or AssetImportJobValues.Maps)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.ResourceName)) return ValidationError("resourceName", "A resource name is required.");
        if (kind is AssetImportJobValues.Textures or AssetImportJobValues.StaticMeshes && string.IsNullOrWhiteSpace(request.PackageName))
            return ValidationError("packageName", "A package name is required.");
        try
        {
            var run = await repository.QueueResourceAsync(
                gameVersion, kind, request.ResourceName, request.PackageName, request.SourceKey, request.Force, token);
            return run is null
                ? Conflict(new { message = $"The requested '{kind}' resource conflicts with an active run." })
                : Accepted($"/api/game-versions/{gameVersion}/assets/{kind}/imports/{run.Id}", run);
        }
        catch (ArgumentException exception)
        {
            return ValidationError("resourceName", exception.Message);
        }
        catch (AssetImportTargetNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("stale")]
    public async Task<ActionResult<IReadOnlyList<StaleAssetSourceSummary>>> ListStale(
        string kind, string gameVersion, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        return Ok(await repository.GetStaleAsync(gameVersion, kind, token));
    }

    [HttpPost("stale")]
    public async Task<ActionResult<AssetImportRunSummary>> QueueStale(
        string kind, string gameVersion, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var run = await repository.QueueStaleAsync(gameVersion, kind, token);
        return run is null
            ? Conflict(new { message = $"A stale rebuild for '{kind}' conflicts with an active run." })
            : Accepted($"/api/game-versions/{gameVersion}/assets/{kind}/imports/{run.Id}", run);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetImportRunSummary>>> List(
        string kind,
        string gameVersion,
        [FromQuery] int limit = 20,
        CancellationToken token = default)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        if (limit is < 1 or > 100) return ValidationError("limit", "Limit must be between 1 and 100.");
        return Ok(await repository.GetRecentAsync(gameVersion, kind, limit, token));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetImportRunSummary>> Get(string gameVersion, string kind, Guid id, CancellationToken token)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        var run = await repository.GetAsync(id, gameVersion, kind, token);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("{id:guid}/work-items")]
    public async Task<ActionResult<AssetImportWorkItemPage>> GetWorkItems(
        string kind,
        string gameVersion,
        Guid id,
        [FromQuery] string? sourceKey,
        [FromQuery] string? status,
        [FromQuery] string? query,
        [FromQuery] string? diagnosticSeverity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken token = default)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        if (page < 1) return ValidationError("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) return ValidationError("pageSize", "Page size must be between 1 and 100.");
        if (status is not null && !AssetImportJobValues.ActiveStatuses.Concat(AssetImportJobValues.WorkItemTerminalStatuses).Contains(status))
            return ValidationError("status", "Work-item status is invalid.");
        if (diagnosticSeverity is not null && diagnosticSeverity is not "warning" and not "error")
            return ValidationError("diagnosticSeverity", "Diagnostic severity must be warning or error.");
        var result = await repository.GetWorkItemsAsync(
            id, gameVersion, kind, sourceKey, status, query, diagnosticSeverity, page, pageSize, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/diagnostics")]
    public async Task<ActionResult<AssetImportDiagnosticPage>> GetDiagnostics(
        string kind,
        string gameVersion,
        Guid id,
        [FromQuery] string? sourceKey,
        [FromQuery] string? severity,
        [FromQuery] string? code,
        [FromQuery] string? stage,
        [FromQuery] string? workItemStatus,
        [FromQuery] string? query,
        [FromQuery] string? scope,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken token = default)
    {
        if (!AssetImportJobValues.SupportedKinds.Contains(kind)) return NotFound();
        if (page < 1) return ValidationError("page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) return ValidationError("pageSize", "Page size must be between 1 and 100.");
        if (severity is not null && severity is not "warning" and not "error")
            return ValidationError("severity", "Severity must be warning or error.");
        if (scope is not null && scope is not "run")
            return ValidationError("scope", "Diagnostic scope is invalid.");
        var result = await repository.GetDiagnosticsAsync(
            id, gameVersion, kind, sourceKey, severity, code, stage, workItemStatus, query, scope, page, pageSize, token);
        return result is null ? NotFound() : Ok(result);
    }

    private BadRequestObjectResult ValidationError(string key, string message) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [key] = [message] }));
}
