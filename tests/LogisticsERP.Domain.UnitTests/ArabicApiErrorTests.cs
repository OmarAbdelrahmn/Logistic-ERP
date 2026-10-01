using System.Reflection;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class ArabicApiErrorTests
{
    [Fact]
    public void DeclaredServiceErrorsHaveArabicDescriptions()
    {
        var assemblies = new[] { typeof(FleetErrors).Assembly, typeof(DependencyInjection).Assembly };
        var checkedCount = 0;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
                             .Where(field => field.FieldType == typeof(OperationError)))
                {
                    var error = Assert.IsType<OperationError>(field.GetValue(null));
                    if (error.Type == ErrorType.None)
                    {
                        continue;
                    }

                    checkedCount++;
                    Assert.True(error.Description.Any(character => character is >= '\u0600' and <= '\u06FF'),
                        $"{type.FullName}.{field.Name} has no Arabic description.");
                }
            }
        }

        Assert.True(checkedCount > 100, $"Only {checkedCount} service errors were checked.");
    }

    [Fact]
    public async Task InvalidSparePartUsageReturnsArabicProblemDetails()
    {
        var controller = new SparePartController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Request.Path = "/api/sparepart/spare-parts";

        var response = await controller.PostUsages(default, null!, CancellationToken.None);

        var problem = Assert.IsType<ProblemDetails>(Assert.IsType<BadRequestObjectResult>(response).Value);
        Assert.Equal("طلب غير صالح", problem.Title);
        Assert.Equal("حدد تاريخ استخدام قطع الغيار.", problem.Detail);
        Assert.Equal("api.invalid_request", problem.Extensions["errorCode"]);
    }
}
