using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class JahezTreasuryConfigurations : IEntityTypeConfiguration<TreasurySubAccount>,
    IEntityTypeConfiguration<TreasuryOwnership>, IEntityTypeConfiguration<TreasuryPosting>,
    IEntityTypeConfiguration<TreasuryState>, IEntityTypeConfiguration<TreasuryHandoverLine>,
    IEntityTypeConfiguration<PetroAppEvidence>, IEntityTypeConfiguration<TreasuryBalanceVerification>,
    IEntityTypeConfiguration<TreasuryHandoverPosting>
{
    public void Configure(EntityTypeBuilder<TreasurySubAccount> b)
    {
        b.ConfigureAuditable("TreasurySubAccounts", "jahez_v3");
        b.ToTable(t => t.UseSqlOutputClause(false));
        b.Property(x => x.Code).HasMaxLength(100);
        b.Property(x => x.NameAr).HasMaxLength(200);
        b.Property(x => x.NameEn).HasMaxLength(200);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SponsorId, x.Location, x.Purpose }).IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsPosting] = 1 AND [IsDeleted] = 0");
        b.ToTable(t => t.HasCheckConstraint("CK_TreasuryAccount_Classification",
            "[Location] BETWEEN 1 AND 4 AND [Purpose] BETWEEN 1 AND 3 AND [AllowedDirections] BETWEEN 1 AND 3 AND ([IsPosting]=0 OR [SponsorId] IS NOT NULL) AND [IsDeleted]=0"));
        b.HasData(TreasurySeeds.Accounts());
    }

    public void Configure(EntityTypeBuilder<TreasuryOwnership> b)
    {
        b.ConfigureHistory("Ownership", "jahez_v3");
        b.Property(x => x.SponsorIdentifier).HasMaxLength(100);
        b.Property(x => x.SponsorNameAr).HasMaxLength(200);
        b.Property(x => x.Evidence).HasMaxLength(2000);
        b.HasIndex(x => new { x.Source, x.SourceId }).IsUnique();
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformAccountId, x.SponsorId });
    }

    public void Configure(EntityTypeBuilder<TreasuryPosting> b)
    {
        b.ConfigureHistory("Postings", "jahez_v3");
        b.ToTable(t => t.UseSqlOutputClause(false));
        b.Property(x => x.ActorNameAr).HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.SubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezMoneyMovement>().WithMany().HasForeignKey(x => x.MoneyMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JahezCashboxHandover>().WithMany().HasForeignKey(x => x.CashboxHandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasuryPosting>().WithMany().HasForeignKey(x => x.ReversesPostingId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ReversesPostingId).IsUnique().HasFilter("[ReversesPostingId] IS NOT NULL");
        b.HasIndex(x => new { x.MoneyMovementId, x.Revision }).IsUnique().HasFilter("[MoneyMovementId] IS NOT NULL AND [Kind] = 1");
        b.HasIndex(x => new { x.SubAccountId, x.OccurredAtUtc, x.Id });
        b.HasIndex(x => new { x.SponsorId, x.OccurredAtUtc });
        b.HasIndex(x => x.BatchId);
        b.ToTable(t => t.HasCheckConstraint("CK_TreasuryPosting_Amount", "[Amount]<>0 AND [Kind] BETWEEN 1 AND 5 AND [Revision]>=0"));
    }

    public void Configure(EntityTypeBuilder<TreasuryState> b)
    {
        b.ConfigureAuditable("State", "jahez_v3");
        b.ToTable(t => t.HasCheckConstraint("CK_TreasuryState_Singleton", $"[Id]='{TreasuryState.SingletonId}' AND [NextHandoverNumber]>0 AND [IsDeleted]=0"));
        b.HasData(new TreasuryState { Id = TreasuryState.SingletonId, CreatedAtUtc = TreasurySeeds.SeededAt });
    }

    public void Configure(EntityTypeBuilder<TreasuryHandoverLine> b)
    {
        b.ConfigureHistory("HandoverLines", "jahez_v3");
        b.Property(x => x.AccountCode).HasMaxLength(100);
        b.Property(x => x.AccountNameAr).HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.SponsorIdentifier).HasMaxLength(100);
        b.Property(x => x.SponsorNameAr).HasMaxLength(200);
        b.HasOne<JahezCashboxHandover>().WithMany().HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.SubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.HandoverId, x.SubAccountId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PetroAppEvidence> b)
    {
        b.ConfigureHistory("PetroEvidence", "jahez_v3");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.AllocatedCollections).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PetroAppEvidence>().WithMany().HasForeignKey(x => x.ReplacesId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SponsorId, x.Reference }).IsUnique();
        b.HasIndex(x => x.ReplacesId).IsUnique().HasFilter("[ReplacesId] IS NOT NULL");
        b.HasIndex(x => new { x.SponsorId, x.BusinessDate });
        b.ToTable(t => t.HasCheckConstraint("CK_PetroEvidence_Amounts", "[Amount]>=0 AND [AllocatedCollections]>=0 AND [Kind] IN(1,2) AND ([Kind]=1 OR [AllocatedCollections]=0)"));
    }

    public void Configure(EntityTypeBuilder<TreasuryBalanceVerification> b)
    {
        b.ConfigureHistory("BalanceVerifications", "jahez_v3");
        b.Property(x => x.VerifiedBalance).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.SubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SubAccountId, x.VerifiedAtUtc });
    }

    public void Configure(EntityTypeBuilder<TreasuryHandoverPosting> b)
    {
        b.ConfigureHistory("HandoverPostings", "jahez_v3");
        b.HasOne<JahezCashboxHandover>().WithMany().HasForeignKey(x => x.HandoverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasuryPosting>().WithMany().HasForeignKey(x => x.PostingId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.HandoverId, x.PostingId }).IsUnique();
        b.HasIndex(x => x.PostingId);
    }
}

internal static class TreasurySeeds
{
    internal static readonly DateTimeOffset SeededAt = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
    internal static IReadOnlyList<TreasurySubAccount> Accounts()
    {
        var result = new List<TreasurySubAccount>();
        var sequence = 10;
        TreasurySubAccount Add(string code, string ar, string en, TreasuryLocation location, TreasuryPurpose purpose,
            Guid? sponsor = null, Guid? parent = null, TreasuryDirections directions = TreasuryDirections.Dual)
        {
            var account = new TreasurySubAccount { Id = Guid.Parse($"019d0000-0000-7000-8000-{sequence++:D12}"),
                Code = code, NameAr = ar, NameEn = en, Location = location, Purpose = purpose, SponsorId = sponsor,
                ParentId = parent, AllowedDirections = directions, IsPosting = sponsor.HasValue,
                IsDefault = sponsor.HasValue, CreatedAtUtc = SeededAt };
            result.Add(account); return account;
        }
        var supervisor = Add("SUP", "عهدة مشرف جاهز", "Jahez supervisor custody", TreasuryLocation.Supervisor, TreasuryPurpose.Combined);
        var central = Add("MAIN", "الخزينة المركزية", "Central treasury", TreasuryLocation.Central, TreasuryPurpose.Combined);
        var bank = Add("BANK", "البنك", "Bank", TreasuryLocation.Bank, TreasuryPurpose.Combined);
        var petro = Add("PETRO", "بترو آب - مقاصة التحصيل", "PetroApp collection clearing", TreasuryLocation.PetroApp, TreasuryPurpose.Combined);
        var fees = Add("MAIN.FEES", "رسوم البوابا وإكسبرس", "Al-Bwaba and Express fees", TreasuryLocation.Central, TreasuryPurpose.AccountFee, parent: central.Id);
        var sharedBank = Add("BANK.BWEXP", "بنك البوابا وإكسبرس", "Al-Bwaba and Express bank", TreasuryLocation.Bank, TreasuryPurpose.Combined, parent: bank.Id);
        (Guid Id, string Code, string Ar)[] sponsors = [(Sponsor.AlBawabaNextCompanyId, "BW", "البوابا"),
            (Sponsor.ExpressGateId, "EX", "إكسبرس"), (Sponsor.AlBawabaCommercialEstablishmentId, "TR", "التجارية")];
        foreach (var s in sponsors)
        {
            Add($"SUP.{s.Code}.SET", $"عهدة تصفيات {s.Ar}", $"{s.Code} settlement custody", TreasuryLocation.Supervisor, TreasuryPurpose.Settlement, s.Id, supervisor.Id);
            Add($"SUP.{s.Code}.FEE", $"عهدة رسوم {s.Ar}", $"{s.Code} fee custody", TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee, s.Id, supervisor.Id, TreasuryDirections.InOnly);
            if (s.Code == "TR") Add("MAIN.TR", "صندوق التجارية", "Trading central treasury", TreasuryLocation.Central, TreasuryPurpose.Combined, s.Id, central.Id);
            else
            {
                Add($"MAIN.{s.Code}.FEE", $"رسوم {s.Ar}", $"{s.Code} central fees", TreasuryLocation.Central, TreasuryPurpose.AccountFee, s.Id, fees.Id, TreasuryDirections.InOnly);
                Add($"MAIN.{s.Code}.SET", $"تصفيات {s.Ar}", $"{s.Code} central settlements", TreasuryLocation.Central, TreasuryPurpose.Settlement, s.Id, central.Id);
            }
            Add($"BANK.{s.Code}", $"بنك {s.Ar}", $"{s.Code} bank", TreasuryLocation.Bank, TreasuryPurpose.Combined, s.Id, s.Code == "TR" ? bank.Id : sharedBank.Id, TreasuryDirections.OutOnly);
            Add($"PETRO.{s.Code}", $"بترو آب {s.Ar}", $"{s.Code} PetroApp clearing", TreasuryLocation.PetroApp, TreasuryPurpose.Combined, s.Id, petro.Id, TreasuryDirections.InOnly);
        }
        return result;
    }
}
