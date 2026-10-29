using LogisticsERP.Api;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using Xunit;
namespace LogisticsERP.Jahez.Tests;
public sealed class MonthlyAccountUsageFilterTests
{
    private sealed class Usage(Guid used) : IMonthlyAccountUsageService
    {
        public int Calls { get; private set; }
        public Task<MonthlyAccountUsage> GetAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default)
        {
            Calls++; Assert.Contains(used, accountIds);
            return Task.FromResult(new MonthlyAccountUsage("2026-10", new HashSet<Guid> { used }));
        }
    }
    [Fact]
    public async Task EnrichesNestedAccountComparisonsInOneBatchAndOmitsInternalIdentity()
    {
        var id = Guid.NewGuid(); var usage = new Usage(id);
        var used = new NinjaComparisonSystemAccount(id, "1", "1", "Available", null, null, null, null, Guid.NewGuid(), null, null, null);
        var unused = used with { AccountId = Guid.NewGuid() };
        var response = new NinjaSheetOnlyAccount(new(1, "1", "Owner", "City", "Iqama"), [used, unused], 2);
        var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var result = new OkObjectResult(response);
        var context = new ResultExecutingContext(action, filters, result, new object());
        await new MonthlyAccountUsageFilter(usage).OnResultExecutionAsync(context,
            () => Task.FromResult(new ResultExecutedContext(action, filters, result, new object())));
        Assert.Equal(1, usage.Calls); Assert.Equal("Used", used.MonthlyUsageStatus); Assert.Equal("Unused", unused.MonthlyUsageStatus);
        Assert.Equal("2026-10", unused.UsageMonth);
        var json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);
        Assert.Contains("monthlyUsageStatus", json, StringComparison.Ordinal); Assert.DoesNotContain("usageAccountId", json, StringComparison.Ordinal);
    }
}
