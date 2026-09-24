using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleRegistrationTransitionApiTests
{
    [Fact]
    public async Task MissingIstimaraReturnsFieldSpecificProblemDetails()
    {
        var controller = new VehiclesController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var form = new VehicleRegistrationTransitionForm
        {
            PlateNumberAr = "س ب 5149",
            PlateNumberEn = "GBN 5149",
            EffectiveAtUtc = DateTimeOffset.UtcNow,
            Reason = "Convert to public transport",
            RowVersion = "current-version"
        };

        var response = await controller.TransitionToPublic(Guid.CreateVersion7(), form, CancellationToken.None);

        var problem = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(response).Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(FleetErrors.TransitionIstimaraInvalid.Code, problem.Title);
        Assert.Equal("istimara", problem.Extensions["field"]);
        Assert.Equal(FleetErrors.TransitionIstimaraInvalid.Description, problem.Detail);
    }
}
