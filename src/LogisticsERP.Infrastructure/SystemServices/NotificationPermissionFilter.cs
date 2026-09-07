using System.Linq.Expressions;
using System.Text.Json;
using LogisticsERP.Domain.Entities.System;

namespace LogisticsERP.Infrastructure.SystemServices;

internal static class NotificationPermissionFilter
{
    // Keys come from the server's permission catalog. Match complete JSON string tokens, not partial keys.
    // OR means any one audience permission is sufficient. Filtering runs in SQL before paging/counting.
    internal static IQueryable<Notification> Apply(IQueryable<Notification> query, IReadOnlyList<string> effectivePermissions, bool includePersonal)
    {
        var item = Expression.Parameter(typeof(Notification), "notification");
        var json = Expression.Property(item, nameof(Notification.AudiencePermissionKeysJson));
        var isNull = Expression.Equal(json, Expression.Constant(null, typeof(string)));
        Expression predicate = includePersonal ? isNull : Expression.Constant(false);
        foreach (var key in effectivePermissions)
        {
            var contains = Expression.Call(json, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(JsonSerializer.Serialize(key)));
            predicate = Expression.OrElse(predicate, Expression.AndAlso(Expression.Not(isNull), contains));
        }
        return query.Where(Expression.Lambda<Func<Notification, bool>>(predicate, item));
    }
}
