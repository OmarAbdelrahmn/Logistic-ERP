using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class LegalCaseServiceTests
{
    [Fact]
    public async Task ExternalPersonCaseCreatesPartiesHistoryAndTwoScheduledReminders()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateApplicationDb();
        await using var identity = CreateIdentityDb();
        await db.Database.EnsureCreatedAsync(ct);
        await identity.Database.EnsureCreatedAsync(ct);
        var user = new TestCurrentUser();
        identity.Users.Add(new ApplicationUser
        {
            Id = user.UserId!.Value,
            UserName = "legal.manager",
            NormalizedUserName = "LEGAL.MANAGER",
            DisplayNameAr = "مسؤول القضايا",
            DisplayNameEn = "Legal Manager",
            Status = UserAccountStatus.Active
        });
        var sponsor = new Sponsor
        {
            CompanyProfileId = Guid.NewGuid(),
            EmployerIdentityNumber = "7000000001",
            RegistryNameAr = "شركة الاختبار",
            SponsorType = SponsorType.Company,
            Status = CatalogStatus.Active
        };
        db.Sponsors.Add(sponsor);
        await identity.SaveChangesAsync(ct);
        await db.SaveChangesAsync(ct);
        var service = new LegalCaseService(db, identity, user, new NoFileStorage(), new FixedTime());

        var result = await service.CreateAsync(new LegalCaseUpsertRequest(
            "CASE-001", "External", "أحمد محمد", null, null, sponsor.Id, "Claimant",
            new DateOnly(2026, 10, 10), new TimeOnly(9, 30), "Open", "مطالبة قانونية", null, user.UserId.Value), ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sponsor", result.Value!.Claimant.PartyType);
        Assert.Equal("External", result.Value.Defendant.PartyType);
        Assert.Equal("أحمد محمد", result.Value.Defendant.Name);
        Assert.Single(await db.HrLegalCaseHistory.ToArrayAsync(ct));
        Assert.Equal(3, await db.Notifications.CountAsync(ct));
        Assert.Equal(2, await db.Notifications.CountAsync(x => x.EventType == "legal_case.appointment.reminder", ct));
    }

    [Fact]
    public async Task SixthHearingFileIsRejectedBeforeStorage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateApplicationDb();
        await using var identity = CreateIdentityDb();
        await db.Database.EnsureCreatedAsync(ct);
        await identity.Database.EnsureCreatedAsync(ct);
        var user = new TestCurrentUser();
        var legalCase = new HrLegalCase
        {
            CaseNumber = "CASE-002", PersonName = "شخص", PersonType = LegalCasePersonType.External,
            SponsorId = Guid.NewGuid(), SponsorPartyRole = LegalCasePartyRole.Defendant,
            CaseDate = new DateOnly(2026, 10, 10), CaseTime = new TimeOnly(10, 0), Details = "Details",
            ResponsibleUserId = user.UserId!.Value
        };
        var hearing = new HrLegalCaseHearing
        {
            LegalCaseId = legalCase.Id, HearingNumber = 1, HearingDate = legalCase.CaseDate,
            HearingTime = legalCase.CaseTime, Details = "Hearing"
        };
        db.HrLegalCases.Add(legalCase);
        db.HrLegalCaseHearings.Add(hearing);
        for (var index = 0; index < LegalCaseService.MaximumFilesPerHearing; index++)
        {
            db.HrLegalCaseHearingFiles.Add(new HrLegalCaseHearingFile
            {
                HearingId = hearing.Id, OriginalFileName = $"{index}.pdf", StoredFileName = $"{index}.pdf",
                StoragePath = $"private/{index}.pdf", ContentType = "application/pdf", FileSizeBytes = 1,
                Sha256Checksum = new string('A', 64), UploadedByUserId = user.UserId.Value
            });
        }
        await db.SaveChangesAsync(ct);
        var storage = new NoFileStorage();
        var service = new LegalCaseService(db, identity, user, storage, new FixedTime());

        var result = await service.UploadFileAsync(legalCase.Id, hearing.Id,
            new(null, new PrivateFileUpload(Stream.Null, "sixth.pdf", "application/pdf", 1)), ct);

        Assert.True(result.IsFailure);
        Assert.Equal(LegalCaseErrors.FileLimitReached.Code, result.Error.Code);
        Assert.False(storage.StoreCalled);
    }

    [Fact]
    public void SqlServerModelContainsLegalCaseConstraintsAndIndexes()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SchemaOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var ddl = db.Database.GenerateCreateScript();
        Assert.Contains("CK_HrLegalCases_PersonReference", ddl, StringComparison.Ordinal);
        Assert.Contains("CK_HrLegalCaseHearings_Number", ddl, StringComparison.Ordinal);
        Assert.Contains("IX_HrLegalCases_CaseNumber", ddl, StringComparison.Ordinal);
        Assert.Contains("HrLegalCaseHistory", ddl, StringComparison.Ordinal);
    }

    private static ApplicationDbContext CreateApplicationDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false)).Options);
    private static IdentityDbContext CreateIdentityDb() => new(new DbContextOptionsBuilder<IdentityDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false)).Options);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "legal-case-tests";
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 19, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class NoFileStorage : IPrivateFileStorage
    {
        public bool StoreCalled { get; private set; }
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default)
        {
            StoreCalled = true;
            return Task.FromResult(Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile));
        }
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<PrivateFileDownload>(PrivateFileErrors.FileMissing));
        public void DeleteBestEffort(string storagePath) { }
    }
}
