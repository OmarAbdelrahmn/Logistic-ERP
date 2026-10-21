using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private static bool HasTreasuryDisplayName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && !Guid.TryParse(name, out _) && name != "اسم المستخدم غير متاح";

    private async Task<string> TreasuryUserName(Guid id, CancellationToken ct)
        => (await TreasuryUserNames([id], ct)).GetValueOrDefault(id, "اسم المستخدم غير متاح");

    private async Task<Dictionary<Guid, string>> TreasuryUserNames(Guid[] ids, CancellationToken ct)
    {
        var result = new Dictionary<Guid, string>();
        if (treasuryIdentity is not null)
        {
            var users = await treasuryIdentity.Users.IgnoreQueryFilters().AsNoTracking().Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.DisplayNameAr, x.DisplayNameEn, x.UserName }).ToArrayAsync(ct);
            foreach (var names in users)
                result[names.Id] = new[] { names.DisplayNameAr, names.DisplayNameEn, names.UserName }.FirstOrDefault(HasTreasuryDisplayName)
                    ?? "اسم المستخدم غير متاح";
        }
        return result;
    }
}
