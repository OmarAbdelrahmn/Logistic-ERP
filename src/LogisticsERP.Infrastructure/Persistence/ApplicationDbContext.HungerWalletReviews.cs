using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Persistence;

public sealed partial class ApplicationDbContext
{
    // Invalidate in the same unit of work, including writes made through shared account services.
    private async Task SupersedeHungerWalletReviewsAsync(bool async, CancellationToken ct)
    {
        var accounts = new HashSet<Guid>();
        var platforms = new HashSet<Guid>();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            switch (entry.Entity)
            {
                case PlatformRiderAccount a: accounts.Add(a.Id); break;
                case RiderClientAssignment a: accounts.Add(a.PlatformRiderAccountId); break;
                case PlatformAccountBusinessDetails d: accounts.Add(d.PlatformRiderAccountId); break;
                case PlatformAccountDayUsage u: accounts.Add(u.PlatformRiderAccountId); break;
                case PlatformAccountDayContribution c: accounts.Add(c.PlatformRiderAccountId); break;
                case HungerQualitySnapshot s: accounts.Add(s.PlatformRiderAccountId); break;
                case HungerDailyOperation o: accounts.Add(o.PlatformRiderAccountId); break;
                // Platform-wide invoice documents and related financial events can affect account rights.
                case HungerSettlementDocument d: platforms.Add(d.ClientPlatformId); break;
                case HungerFinancialEvent e: platforms.Add(e.ClientPlatformId); break;
            }
        }
        if (accounts.Count == 0 && platforms.Count == 0) return;
        var query = Set<HungerWalletReview>().Where(r => r.Status == "Pending" && r.ResultJson == null
            && (accounts.Contains(r.PlatformRiderAccountId) || PlatformRiderAccounts.Any(a => a.Id == r.PlatformRiderAccountId && platforms.Contains(a.ClientPlatformId))));
        var pending = async ? await query.ToArrayAsync(ct) : query.ToArray();
        foreach (var review in pending.Where(r => r.Status == "Pending")) review.Status = "Superseded";
    }
}
