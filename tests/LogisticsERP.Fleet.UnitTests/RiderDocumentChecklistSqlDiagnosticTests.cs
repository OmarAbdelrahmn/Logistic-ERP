using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class RiderDocumentChecklistSqlDiagnosticTests
{
    [Fact]
    public async Task ExistingRiderChecklistCanBeReadFromSqlServer()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGISTICS_HR_DOCUMENT_SQL");
        Assert.SkipUnless(
            !string.IsNullOrWhiteSpace(connectionString),
            "Set LOGISTICS_HR_DOCUMENT_SQL to run this read-only SQL diagnostic test.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var cancellationToken = TestContext.Current.CancellationToken;
        var riderProfileId = await dbContext.RiderProfiles.AsNoTracking()
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Assert.SkipWhen(riderProfileId is null, "The configured database has no active rider profile.");

        var service = new EmployeeDocumentService(
            dbContext,
            new DiagnosticCurrentUser(),
            new UnusedFileStorage(),
            TimeProvider.System);

        var result = await service.GetRiderDocumentChecklistAsync(
            riderProfileId!.Value,
            cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    private sealed class DiagnosticCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => "rider-document-sql-diagnostic";
    }

    private sealed class UnusedFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(
            string relativeDirectory,
            PrivateFileUpload file,
            long maximumBytes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile));

        public Task<Result<PrivateFileDownload>> OpenReadAsync(
            string storagePath,
            string contentType,
            string downloadFileName,
            long length,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<PrivateFileDownload>(PrivateFileErrors.FileMissing));

        public void DeleteBestEffort(string storagePath)
        {
        }
    }
}
