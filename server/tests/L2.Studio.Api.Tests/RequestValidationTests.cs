using L2.Studio.Api.Filters;
using L2.Studio.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace L2.Studio.Api.Tests;

public sealed class RequestValidationTests
{
    [Fact]
    public void ValidatesAllDirectoryRequestBoundsTogether()
    {
        var request = new DirectoryRequest(new string('a', 101), 0, 101);
        var context = CreateContext(request);

        new ValidateDirectoryRequestAttribute().OnActionExecuting(context);

        var problem = Problem(context);
        Assert.Equal(
            "Search terms must contain 100 characters or fewer.",
            Assert.Single(problem.Errors["query"]));
        Assert.Equal("Page must be at least 1.", Assert.Single(problem.Errors["page"]));
        Assert.Equal(
            "Page size must be between 1 and 100.",
            Assert.Single(problem.Errors["pageSize"]));
    }

    [Fact]
    public void AcceptsDirectoryRequestsAtTheirLimits()
    {
        var context = CreateContext(new DirectoryRequest(new string('a', 100), 1, 100));

        new ValidateDirectoryRequestAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void ValidatesNpcDirectoryFilters()
    {
        var request = new NpcDirectoryRequest(
            NpcTypeName: new string('t', 65),
            NpcRaceName: "HUMANOID",
            WithoutRace: true,
            NpcSexName: new string('s', 65));
        var context = CreateContext(request);

        new ValidateDirectoryRequestAttribute().OnActionExecuting(context);

        var problem = Problem(context);
        Assert.Equal("NPC type filters must contain 64 characters or fewer.", Assert.Single(problem.Errors["npcTypeName"]));
        Assert.Equal("Choose either a specific NPC race or no race.", Assert.Single(problem.Errors["withoutRace"]));
        Assert.Equal("NPC sex filters must contain 64 characters or fewer.", Assert.Single(problem.Errors["npcSexName"]));
    }

    [Fact]
    public void ValidatesAssetCatalogPageBounds()
    {
        var context = CreateContext(new AssetCatalogRequest(Page: 0, PageSize: 501));

        new ValidateAssetCatalogRequestAttribute().OnActionExecuting(context);

        var problem = Problem(context);
        Assert.Equal("Page must be positive.", Assert.Single(problem.Errors["page"]));
        Assert.Equal(
            "Page size must be between 1 and 500.",
            Assert.Single(problem.Errors["pageSize"]));
    }

    [Fact]
    public void IgnoresActionsWithoutTheValidatedRequestType()
    {
        var context = CreateContext("not a request");

        new ValidateDirectoryRequestAttribute().OnActionExecuting(context);
        new ValidateAssetCatalogRequestAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    private static ValidationProblemDetails Problem(ActionExecutingContext context) =>
        Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(context.Result).Value);

    private static ActionExecutingContext CreateContext(object request)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = request },
            new object());
    }
}
