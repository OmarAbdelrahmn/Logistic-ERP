using System.Reflection;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class PlatformAccountErrorResponseTests
{
    [Fact]
    public void AccountContractsAndRoutesExposeOnlyTheExistingSponsor()
    {
        foreach (var type in new[]
        {
            typeof(SimplePlatformAccountUpsertRequest), typeof(SimplePlatformAccountResponse),
            typeof(PlatformAccountUpsertRequest), typeof(PlatformAccountResponse), typeof(PlatformRiderAccount)
        })
        {
            Assert.NotNull(type.GetProperty("SponsorId"));
            Assert.Null(type.GetProperty("DashboardSponsorId"));
        }

        foreach (var method in new[]
        {
            typeof(PlatformAccountsController).GetMethod("GetAll")!,
            typeof(PlatformOperationsController).GetMethod("Accounts")!
        })
        {
            Assert.Contains(method.GetParameters(), parameter => parameter.Name == "sponsorId");
            Assert.DoesNotContain(method.GetParameters(), parameter => parameter.Name == "dashboardSponsorId");
        }
    }

    [Theory]
    [InlineData("$.platformId", "platformId", "UUID")]
    [InlineData("$.startDate", "startDate", "yyyy-MM-dd")]
    [InlineData("Code", "code", "قيمة نصية")]
    [InlineData("", "body", "JSON")]
    public void InvalidJsonInputReturnsActionableFieldErrors(string modelKey, string field, string requirement)
    {
        var context = new ActionContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Request.Path = "/api/platform-accounts";
        context.ModelState.AddModelError(modelKey, "Internal parser exception that must not appear in the response");
        var type = typeof(PlatformAccountsController).Assembly.GetType(
            "LogisticsERP.Api.ErrorHandling.PlatformAccountValidationProblem", throwOnError: true)!;

        var response = Assert.IsType<BadRequestObjectResult>(type.GetMethod("Create")!.Invoke(null, [context]));
        var problem = Assert.IsType<ProblemDetails>(response.Value);

        Assert.Equal(400, response.StatusCode);
        Assert.Equal("platform.account_invalid_field", problem.Extensions["errorCode"]);
        Assert.Equal(field, problem.Extensions["field"]);
        Assert.Contains(requirement, problem.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Internal parser", problem.Detail, StringComparison.Ordinal);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(problem.Detail, Assert.Single(errors[field]));
    }

    [Fact]
    public void MissingAccountSponsorSurvivesProblemDetailsMapping()
    {
        var error = PlatformAccountErrors.Required("sponsorId", "الكفيل");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/platform-accounts";
        var type = typeof(PlatformAccountsController).Assembly.GetType(
            "LogisticsERP.Api.ErrorHandling.ResultExtensions", throwOnError: true)!;
        var method = type.GetMethod("ToProblem", BindingFlags.Public | BindingFlags.Static)!;

        var response = Assert.IsType<ObjectResult>(method.Invoke(null, [Result.Failure(error), context, null]));
        var problem = Assert.IsType<ProblemDetails>(response.Value);

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(error.Description, problem.Detail);
        Assert.Equal(error.Code, problem.Extensions["errorCode"]);
        Assert.Equal("sponsorId", problem.Extensions["field"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(error.Description, Assert.Single(errors["sponsorId"]));
    }
}
