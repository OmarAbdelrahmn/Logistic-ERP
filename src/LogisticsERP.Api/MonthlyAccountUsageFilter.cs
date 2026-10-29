using System.Collections;
using System.Reflection;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace LogisticsERP.Api;
// Enrich only declared account representations, including nested comparison/detail DTOs.
public sealed class MonthlyAccountUsageFilter(IMonthlyAccountUsageService usage) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: { } value, StatusCode: null or >= 200 and < 300 })
        {
            var accounts = new List<IMonthlyAccountUsageResponse>();
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            void Visit(object? item)
            {
                if (item is null || item is string || item.GetType().IsValueType || !seen.Add(item)) return;
                if (item is IMonthlyAccountUsageResponse account) accounts.Add(account);
                if (item is IEnumerable items) { foreach (var child in items) Visit(child); return; }
                if (item.GetType().Assembly != typeof(IMonthlyAccountUsageResponse).Assembly) return;
                foreach (var property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (property.CanRead && property.GetIndexParameters().Length == 0) Visit(property.GetValue(item));
            }
            Visit(value);
            if (accounts.Count > 0)
            {
                var monthly = await usage.GetAsync(accounts.Select(a => a.UsageAccountId).Distinct().ToArray(), context.HttpContext.RequestAborted);
                foreach (var account in accounts)
                {
                    account.UsageMonth = monthly.UsageMonth;
                    account.MonthlyUsageStatus = monthly.UsedAccountIds.Contains(account.UsageAccountId) ? "Used" : "Unused";
                }
            }
        }
        await next();
    }
}
