using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Telecom;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Telecom;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PhoneSimReceiptTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatePersistsSimAndResponsibilityWithOptionalReceipt(bool includeReceipt)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = await SeedRequestAsync(db, ct);
        var storage = new TestFileStorage();
        var service = new PhoneSimService(db, new TestCurrentUser(), storage, TimeProvider.System);
        using var stream = new MemoryStream([1, 2, 3]);
        var upload = includeReceipt ? new PrivateFileUpload(stream, "receipt.pdf", "application/pdf", 3) : null;

        var result = await service.CreateAsync(request, upload, ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("Available", result.Value!.Status);
        Assert.Equal("+966555123456", result.Value.PhoneNumber);
        Assert.Equal(request.PlaceId, result.Value.PlaceId);
        Assert.Equal(includeReceipt ? 1 : 0, storage.StoreCalls);
        var sim = Assert.Single(await db.PhoneSimCards.AsNoTracking().ToArrayAsync(ct));
        var responsibility = Assert.Single(await db.PhoneSimResponsibilityChanges.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(sim.Id, responsibility.PhoneSimCardId);
        Assert.Equal(request.ResponsibleEmployeeId, responsibility.ResponsibleEmployeeId);
        if (includeReceipt)
        {
            Assert.Equal("receipt.pdf", result.Value.ReceiptForm!.OriginalFileName);
            Assert.Equal(3, result.Value.ReceiptForm.FileSizeBytes);
            Assert.Equal(storage.StoredFile.StoragePath, sim.ReceiptFormStoragePath);
        }
        else
        {
            Assert.Null(result.Value.ReceiptForm);
            Assert.Null(sim.ReceiptFormStoragePath);
            var download = await service.DownloadReceiptFormAsync(sim.Id, ct);
            Assert.Equal(PhoneSimErrors.ReceiptFormNotFound.Code, download.Error.Code);
        }
    }

    [Fact]
    public async Task CreateStillRejectsInvalidUploadedReceiptWithoutSavingSim()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var request = await SeedRequestAsync(db, ct);
        var storage = new TestFileStorage { RejectUpload = true };
        var service = new PhoneSimService(db, new TestCurrentUser(), storage, TimeProvider.System);
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.CreateAsync(request,
            new PrivateFileUpload(stream, "receipt.pdf", "application/pdf", 3), ct);

        Assert.Equal(PrivateFileErrors.InvalidFile.Code, result.Error.Code);
        Assert.Empty(await db.PhoneSimCards.ToArrayAsync(ct));
        Assert.Empty(await db.PhoneSimResponsibilityChanges.ToArrayAsync(ct));
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PhoneSimReceipts_{Guid.NewGuid():N}")
            .Options);

    private static async Task<CreatePhoneSimRequest> SeedRequestAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var employee = new Employee { FullNameAr = "SIM manager", IsEmployee = true, Status = EmployeeStatus.Active };
        var place = new Place { Name = "Main office" };
        db.AddRange(employee, place);
        await db.SaveChangesAsync(ct);
        return new CreatePhoneSimRequest("0555 123 456", null, "STC", employee.Id, place.Id, null);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }

    private sealed class TestFileStorage : IPrivateFileStorage
    {
        public int StoreCalls { get; private set; }
        public bool RejectUpload { get; init; }
        public StoredPrivateFile StoredFile { get; } = new("phone-sims/receipt.pdf", "stored.pdf", 3,
            new string('a', 64), "application/pdf", "receipt.pdf");

        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file,
            long maximumBytes, CancellationToken cancellationToken = default)
        {
            StoreCalls++;
            Assert.Equal(10 * 1024 * 1024, maximumBytes);
            return Task.FromResult(RejectUpload
                ? Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile)
                : Result.Success(StoredFile));
        }

        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType,
            string downloadFileName, long length, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No receipt file should be opened in these tests.");

        public void DeleteBestEffort(string storagePath) =>
            throw new InvalidOperationException("No receipt file should need cleanup after these tests.");
    }
}
