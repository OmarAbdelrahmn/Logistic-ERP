using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class InitialApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateSequence(
                name: "AuditEntrySequence",
                schema: "audit");

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR [audit].[AuditEntrySequence]"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupportAccessGrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    CurrentHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientPlatforms",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoAssetKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPlatforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyProfile",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LegalNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CommercialRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnifiedNationalNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    AddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LogoAssetKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultLocale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NextEmployeeSequence = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SuspensionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SuspendedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SuspendedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfile", x => x.Id);
                    table.CheckConstraint("CK_CompanyProfile_SingleRow", "[Id] = '019c18d5-62e1-7000-8000-000000000001'");
                });

            migrationBuilder.CreateTable(
                name: "DatasetVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetVersions", x => x.Id);
                    table.CheckConstraint("CK_DatasetVersions_Version", "[Version] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppliesToSponsoredInternal = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToOutsideRider = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToRiderProfile = table.Column<bool>(type: "bit", nullable: false),
                    RequiresNumber = table.Column<bool>(type: "bit", nullable: false),
                    RequiresIssueDate = table.Column<bool>(type: "bit", nullable: false),
                    RequiresExpiryDate = table.Column<bool>(type: "bit", nullable: false),
                    RequiresFile = table.Column<bool>(type: "bit", nullable: false),
                    AllowedMimeTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FullNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    CurrentRelationshipType = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportVersion = table.Column<int>(type: "int", nullable: false),
                    ScopeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilterSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    IncludesSensitiveValues = table.Column<bool>(type: "bit", nullable: false),
                    SensitiveExportReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArtifactPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ArtifactChecksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    ArtifactSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ArtifactExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                    table.CheckConstraint("CK_ExportJobs_ProgressPercentage", "[ProgressPercentage] >= 0 AND [ProgressPercentage] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "GlobalCities",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegionAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegionEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalCities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresBalance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresHrDocuments = table.Column<bool>(type: "bit", nullable: false),
                    RequiresExitReentryVisa = table.Column<bool>(type: "bit", nullable: false),
                    MaximumCalendarDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                    table.CheckConstraint("CK_LeaveTypes_MaximumCalendarDays", "[MaximumCalendarDays] IS NULL OR [MaximumCalendarDays] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BodyAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeepLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScopeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VisibleAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDefinitions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiresHousingScope = table.Column<bool>(type: "bit", nullable: false),
                    RequiresClientScope = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    IsHighTrust = table.Column<bool>(type: "bit", nullable: false),
                    GrantabilityRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    ReplacementKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViews",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnOrderJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Density = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AppliesToEmployees = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToHousing = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToClientContracts = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToPlatformAccounts = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientContracts",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalBusinessAccountId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientContracts", x => x.Id);
                    table.CheckConstraint("CK_ClientContracts_DateRange", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_ClientContracts_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRequirements",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipType = table.Column<int>(type: "int", nullable: true),
                    AppliesToRiderProfile = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ReminderOffsetsDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRequirements", x => x.Id);
                    table.CheckConstraint("CK_DocumentRequirements_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_DocumentRequirements_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "platform",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsenceComplianceCases",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbsenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrentPath = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReportedToAuthoritiesDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AuthorityReportReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ExitOrOutageDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExitVisaNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RemovalDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAbsenceComplianceCases", x => x.Id);
                    table.CheckConstraint("CK_EmployeeAbsenceComplianceCases_Deadline", "([CurrentPath] = 1 AND [RemovalDeadline] >= [ReportedToAuthoritiesDate]) OR ([CurrentPath] = 2 AND [RemovalDeadline] >= [ExitOrOutageDate])");
                    table.CheckConstraint("CK_EmployeeAbsenceComplianceCases_PathData", "([CurrentPath] = 1 AND [ReportedToAuthoritiesDate] IS NOT NULL AND [ExitOrOutageDate] IS NULL) OR ([CurrentPath] = 2 AND [ExitOrOutageDate] IS NOT NULL AND [ReportedToAuthoritiesDate] IS NULL)");
                    table.ForeignKey(
                        name: "FK_EmployeeAbsenceComplianceCases_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRelationshipPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipType = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRelationshipPeriods", x => x.Id);
                    table.CheckConstraint("CK_EmployeeRelationshipPeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_EmployeeRelationshipPeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeStatusPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeStatusPeriods", x => x.Id);
                    table.CheckConstraint("CK_EmployeeStatusPeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_EmployeeStatusPeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutsideRiderDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalityCountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    AlternateContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlternateContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    EngagementReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EngagementNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutsideRiderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutsideRiderDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Housing",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    TotalCapacity = table.Column<int>(type: "int", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OpenedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Housing", x => x.Id);
                    table.CheckConstraint("CK_Housing_DateRange", "[ClosedDate] IS NULL OR [OpenedDate] IS NULL OR [ClosedDate] >= [OpenedDate]");
                    table.CheckConstraint("CK_Housing_TotalCapacity", "[TotalCapacity] > 0");
                    table.ForeignKey(
                        name: "FK_Housing_GlobalCities_CityId",
                        column: x => x.CityId,
                        principalSchema: "platform",
                        principalTable: "GlobalCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatingCities",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GlobalCityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnabledFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DisabledAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingCities", x => x.Id);
                    table.CheckConstraint("CK_OperatingCities_DateRange", "[DisabledAt] IS NULL OR [DisabledAt] >= [EnabledFrom]");
                    table.ForeignKey(
                        name: "FK_OperatingCities_GlobalCities_GlobalCityId",
                        column: x => x.GlobalCityId,
                        principalSchema: "platform",
                        principalTable: "GlobalCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeJobTitlePeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeJobTitlePeriods", x => x.Id);
                    table.CheckConstraint("CK_EmployeeJobTitlePeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_EmployeeJobTitlePeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeJobTitlePeriods_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalSchema: "app",
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveApprovalWorkflows",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelationshipType = table.Column<int>(type: "int", nullable: true),
                    AppliesToRider = table.Column<bool>(type: "bit", nullable: true),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApprovalWorkflows", x => x.Id);
                    table.CheckConstraint("CK_LeaveApprovalWorkflows_DateRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_LeaveApprovalWorkflows_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_LeaveApprovalWorkflows_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveApprovalWorkflows_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "app",
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTags_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientContractTags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientContractTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientContractTags_ClientContracts_ClientContractId",
                        column: x => x.ClientContractId,
                        principalSchema: "app",
                        principalTable: "ClientContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientContractTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRiderAccounts",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedExternalAccountId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OwnershipNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OperationalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRiderAccounts", x => x.Id);
                    table.CheckConstraint("CK_PlatformRiderAccounts_DateRange", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId",
                        column: x => x.ClientContractId,
                        principalSchema: "app",
                        principalTable: "ClientContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformRiderAccounts_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsenceComplianceCaseEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeAbsenceComplianceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAbsenceComplianceCaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsenceComplianceCaseEvents_EmployeeAbsenceComplianceCases_EmployeeAbsenceComplianceCaseId",
                        column: x => x.EmployeeAbsenceComplianceCaseId,
                        principalSchema: "app",
                        principalTable: "EmployeeAbsenceComplianceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeStatusChangeRequests",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    RequestedStatus = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultingStatusPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeStatusChangeRequests", x => x.Id);
                    table.CheckConstraint("CK_EmployeeStatusChangeRequests_StatusChanged", "[FromStatus] <> [RequestedStatus]");
                    table.ForeignKey(
                        name: "FK_EmployeeStatusChangeRequests_EmployeeStatusPeriods_ResultingStatusPeriodId",
                        column: x => x.ResultingStatusPeriodId,
                        principalSchema: "app",
                        principalTable: "EmployeeStatusPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeStatusChangeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingResidencePeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    MoveInReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MoveOutReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DestinationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CapacityOverrideUsed = table.Column<bool>(type: "bit", nullable: false),
                    CapacityOverrideReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousingResidencePeriods", x => x.Id);
                    table.CheckConstraint("CK_HousingResidencePeriods_CapacityOverrideReason", "[CapacityOverrideUsed] = 0 OR [CapacityOverrideReason] IS NOT NULL");
                    table.CheckConstraint("CK_HousingResidencePeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_HousingResidencePeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HousingResidencePeriods_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingSupervisorPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupervisorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignmentReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousingSupervisorPeriods", x => x.Id);
                    table.CheckConstraint("CK_HousingSupervisorPeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_HousingSupervisorPeriods_Employees_SupervisorEmployeeId",
                        column: x => x.SupervisorEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HousingSupervisorPeriods_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingTags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousingTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HousingTags_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HousingTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveApprovalWorkflowSteps",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveApprovalWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequiredPermissionKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ScopeSource = table.Column<int>(type: "int", nullable: false),
                    AllowsReturnForChanges = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCommentOnApproval = table.Column<bool>(type: "bit", nullable: false),
                    TargetResponseHours = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApprovalWorkflowSteps", x => x.Id);
                    table.CheckConstraint("CK_LeaveApprovalWorkflowSteps_Sequence", "[Sequence] > 0");
                    table.CheckConstraint("CK_LeaveApprovalWorkflowSteps_TargetHours", "[TargetResponseHours] IS NULL OR [TargetResponseHours] > 0");
                    table.ForeignKey(
                        name: "FK_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflows_LeaveApprovalWorkflowId",
                        column: x => x.LeaveApprovalWorkflowId,
                        principalSchema: "app",
                        principalTable: "LeaveApprovalWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CalendarDays = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DestinationCountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    ContactPhoneDuringLeave = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalWorkflowVersion = table.Column<int>(type: "int", nullable: true),
                    ApprovalWorkflowSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentApprovalStepKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentApprovalStepSequence = table.Column<int>(type: "int", nullable: true),
                    HrStatus = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelatedClientContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequests_CalendarDays", "[CalendarDays] = DATEDIFF(DAY, [StartDate], [EndDate]) + 1");
                    table.CheckConstraint("CK_LeaveRequests_DateRange", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_LeaveRequests_ExpectedReturn", "[ExpectedReturnDate] >= [EndDate]");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_ClientContracts_RelatedClientContractId",
                        column: x => x.RelatedClientContractId,
                        principalSchema: "app",
                        principalTable: "ClientContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveApprovalWorkflows_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "app",
                        principalTable: "LeaveApprovalWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "app",
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccountCredentialVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Nonce = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    AuthenticationTag = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    KeyVersion = table.Column<int>(type: "int", nullable: false),
                    RotatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RotatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupersededVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAccountCredentialVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAccountCredentialVersions_PlatformAccountCredentialVersions_SupersededVersionId",
                        column: x => x.SupersededVersionId,
                        principalSchema: "app",
                        principalTable: "PlatformAccountCredentialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountCredentialVersions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRiderAccountTags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRiderAccountTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRiderAccountTags_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformRiderAccountTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveApprovalDecisions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepSequence = table.Column<int>(type: "int", nullable: false),
                    RequiredPermissionKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ReturnedToStepKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthorizationSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApprovalDecisions", x => x.Id);
                    table.CheckConstraint("CK_LeaveApprovalDecisions_StepSequence", "[StepSequence] > 0");
                    table.ForeignKey(
                        name: "FK_LeaveApprovalDecisions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "app",
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveCancellationRequests",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveCancellationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveCancellationRequests_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "app",
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveDateChangeRequests",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveDateChangeRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveDateChangeRequests_PreviousRange", "[PreviousEndDate] >= [PreviousStartDate]");
                    table.CheckConstraint("CK_LeaveDateChangeRequests_RequestedRange", "[RequestedEndDate] >= [RequestedStartDate]");
                    table.ForeignKey(
                        name: "FK_LeaveDateChangeRequests_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "app",
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NormalizedDocumentNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IssuingCountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.CheckConstraint("CK_EmployeeDocuments_DateRange", "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "platform",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocumentVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreviewStoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocumentVersions", x => x.Id);
                    table.CheckConstraint("CK_EmployeeDocumentVersions_FileSize", "[FileSizeBytes] > 0");
                    table.CheckConstraint("CK_EmployeeDocumentVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_EmployeeDocumentVersions_EmployeeDocumentVersions_SupersededVersionId",
                        column: x => x.SupersededVersionId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDocumentVersions_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderProfiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiderStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RiderEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PreferredCityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicenseDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderProfiles", x => x.Id);
                    table.CheckConstraint("CK_RiderProfiles_DateRange", "[RiderEndDate] IS NULL OR [RiderStartDate] IS NULL OR [RiderEndDate] >= [RiderStartDate]");
                    table.ForeignKey(
                        name: "FK_RiderProfiles_EmployeeDocuments_LicenseDocumentId",
                        column: x => x.LicenseDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderProfiles_GlobalCities_PreferredCityId",
                        column: x => x.PreferredCityId,
                        principalSchema: "platform",
                        principalTable: "GlobalCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsoredInternalDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NationalityCountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    SecondaryPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    ProfilePhotoDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaritalStatus = table.Column<int>(type: "int", nullable: true),
                    DependentsCount = table.Column<int>(type: "int", nullable: true),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EducationDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Profession = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HomeAddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HomeAddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmergencyContactRelationship = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProbationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SponsorLegalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentJobTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsoredInternalDetails", x => x.Id);
                    table.CheckConstraint("CK_SponsoredInternalDetails_ContractRange", "[ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate]");
                    table.CheckConstraint("CK_SponsoredInternalDetails_Dependents", "[DependentsCount] IS NULL OR [DependentsCount] >= 0");
                    table.ForeignKey(
                        name: "FK_SponsoredInternalDetails_EmployeeDocuments_ProfilePhotoDocumentId",
                        column: x => x.ProfilePhotoDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsoredInternalDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsoredInternalDetails_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsoredInternalDetails_JobTitles_CurrentJobTitleId",
                        column: x => x.CurrentJobTitleId,
                        principalSchema: "app",
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderClientAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OperationalAgreementReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OperationalAgreementNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WasBackdated = table.Column<bool>(type: "bit", nullable: false),
                    BackdatedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderClientAssignments", x => x.Id);
                    table.CheckConstraint("CK_RiderClientAssignments_BackdatedReason", "[WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL");
                    table.CheckConstraint("CK_RiderClientAssignments_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_RiderClientAssignments_ClientContracts_ClientContractId",
                        column: x => x.ClientContractId,
                        principalSchema: "app",
                        principalTable: "ClientContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderClientAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderAssignmentEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderAssignmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderAssignmentEvents_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestDocuments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDocuments", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequestDocuments_DateRange", "[ExpiresOn] IS NULL OR [IssuedOn] IS NULL OR [ExpiresOn] >= [IssuedOn]");
                    table.ForeignKey(
                        name: "FK_LeaveRequestDocuments_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "app",
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestDocumentVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDocumentVersions", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequestDocumentVersions_FileSize", "[FileSizeBytes] > 0");
                    table.CheckConstraint("CK_LeaveRequestDocumentVersions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_LeaveRequestDocumentVersions_LeaveRequestDocumentVersions_SupersededVersionId",
                        column: x => x.SupersededVersionId,
                        principalSchema: "app",
                        principalTable: "LeaveRequestDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequestDocumentVersions_LeaveRequestDocuments_LeaveRequestDocumentId",
                        column: x => x.LeaveRequestDocumentId,
                        principalSchema: "app",
                        principalTable: "LeaveRequestDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "CompanyProfile",
                columns: new[] { "Id", "Code", "CommercialRegistrationNumber", "ContactEmail", "ContactPhone", "CreatedAtUtc", "CreatedByUserId", "DefaultLocale", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DisplayNameAr", "DisplayNameEn", "IsDeleted", "LegalNameAr", "LegalNameEn", "LogoAssetKey", "NextEmployeeSequence", "Status", "SuspendedAtUtc", "SuspendedByUserId", "SuspensionReason", "TimeZoneId", "UnifiedNationalNumber", "UpdatedAtUtc", "UpdatedByUserId", "VatNumber" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000001"), "ALBAWABA", null, null, "", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ar", null, null, null, "البوابة للخدمات اللوجستية", "Al Bawaba Logistics", false, "البوابة للخدمات اللوجستية", "Al Bawaba Logistics Services", null, 1L, 1, null, null, null, "Asia/Riyadh", null, null, null, null });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "GlobalCities",
                columns: new[] { "Id", "Code", "CountryCode", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DisplayOrder", "IsDeleted", "Latitude", "Longitude", "NameAr", "NameEn", "RegionAr", "RegionEn", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000002"), "JEDDAH", "SA", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, 1, false, 21.4858m, 39.1925m, "جدة", "Jeddah", "منطقة مكة المكرمة", "Makkah Region", 1, null, null });

            migrationBuilder.InsertData(
                schema: "app",
                table: "OperatingCities",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DisabledAt", "EnabledFrom", "GlobalCityId", "IsDeleted", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, new DateOnly(2026, 1, 1), new Guid("019c18d5-62e1-7000-8000-000000000002"), false, 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorUserId_OccurredAtUtc",
                schema: "audit",
                table: "AuditEntries",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_EntityId_OccurredAtUtc",
                schema: "audit",
                table: "AuditEntries",
                columns: new[] { "EntityType", "EntityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EventId",
                schema: "audit",
                table: "AuditEntries",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAtUtc",
                schema: "audit",
                table: "AuditEntries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Sequence",
                schema: "audit",
                table: "AuditEntries",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientContracts_ClientPlatformId_ExternalBusinessAccountId",
                schema: "app",
                table: "ClientContracts",
                columns: new[] { "ClientPlatformId", "ExternalBusinessAccountId" },
                unique: true,
                filter: "[ExternalBusinessAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContracts_ClientPlatformId_Status",
                schema: "app",
                table: "ClientContracts",
                columns: new[] { "ClientPlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientContracts_Code",
                schema: "app",
                table: "ClientContracts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientContracts_IsDeleted",
                schema: "app",
                table: "ClientContracts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContractTags_ClientContractId_TagId",
                schema: "app",
                table: "ClientContractTags",
                columns: new[] { "ClientContractId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientContractTags_IsDeleted",
                schema: "app",
                table: "ClientContractTags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContractTags_TagId",
                schema: "app",
                table: "ClientContractTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPlatforms_Code",
                schema: "platform",
                table: "ClientPlatforms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientPlatforms_IsDeleted",
                schema: "platform",
                table: "ClientPlatforms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfile_Code",
                schema: "platform",
                table: "CompanyProfile",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfile_IsDeleted",
                schema: "platform",
                table: "CompanyProfile",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetVersions_IsDeleted",
                schema: "app",
                table: "DatasetVersions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetVersions_ModuleKey",
                schema: "app",
                table: "DatasetVersions",
                column: "ModuleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_DocumentTypeId_RelationshipType_AppliesToRiderProfile_EffectiveFrom",
                schema: "app",
                table: "DocumentRequirements",
                columns: new[] { "DocumentTypeId", "RelationshipType", "AppliesToRiderProfile", "EffectiveFrom" },
                unique: true,
                filter: "[RelationshipType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_IsDeleted",
                schema: "app",
                table: "DocumentRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_Status_EffectiveTo",
                schema: "app",
                table: "DocumentRequirements",
                columns: new[] { "Status", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_Code",
                schema: "platform",
                table: "DocumentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_IsDeleted",
                schema: "platform",
                table: "DocumentTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceComplianceCaseEvents_EmployeeAbsenceComplianceCaseId_OccurredAtUtc",
                schema: "app",
                table: "EmployeeAbsenceComplianceCaseEvents",
                columns: new[] { "EmployeeAbsenceComplianceCaseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceComplianceCases_CaseNumber",
                schema: "app",
                table: "EmployeeAbsenceComplianceCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceComplianceCases_EmployeeId",
                schema: "app",
                table: "EmployeeAbsenceComplianceCases",
                column: "EmployeeId",
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4) AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceComplianceCases_IsDeleted",
                schema: "app",
                table: "EmployeeAbsenceComplianceCases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceComplianceCases_Status_RemovalDeadline",
                schema: "app",
                table: "EmployeeAbsenceComplianceCases",
                columns: new[] { "Status", "RemovalDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_CurrentVersionId",
                schema: "app",
                table: "EmployeeDocuments",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_DocumentTypeId_NormalizedDocumentNumber",
                schema: "app",
                table: "EmployeeDocuments",
                columns: new[] { "DocumentTypeId", "NormalizedDocumentNumber" },
                unique: true,
                filter: "[NormalizedDocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId_DocumentTypeId_Status",
                schema: "app",
                table: "EmployeeDocuments",
                columns: new[] { "EmployeeId", "DocumentTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_IsDeleted",
                schema: "app",
                table: "EmployeeDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentVersions_EmployeeDocumentId_VersionNumber",
                schema: "app",
                table: "EmployeeDocumentVersions",
                columns: new[] { "EmployeeDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentVersions_Sha256Checksum",
                schema: "app",
                table: "EmployeeDocumentVersions",
                column: "Sha256Checksum");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentVersions_SupersededVersionId",
                schema: "app",
                table: "EmployeeDocumentVersions",
                column: "SupersededVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_EmployeeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_JobTitleId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRelationshipPeriods_EmployeeId",
                schema: "app",
                table: "EmployeeRelationshipPeriods",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRelationshipPeriods_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "EmployeeRelationshipPeriods",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CurrentStatus",
                schema: "app",
                table: "Employees",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                schema: "app",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsDeleted",
                schema: "app",
                table: "Employees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NormalizedNameAr",
                schema: "app",
                table: "Employees",
                column: "NormalizedNameAr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NormalizedNameEn",
                schema: "app",
                table: "Employees",
                column: "NormalizedNameEn");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PrimaryPhone",
                schema: "app",
                table: "Employees",
                column: "PrimaryPhone");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusChangeRequests_EmployeeId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "EmployeeId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusChangeRequests_IsDeleted",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusChangeRequests_RequestNumber",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusChangeRequests_ResultingStatusPeriodId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "ResultingStatusPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusChangeRequests_Status_RequestedAtUtc",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusPeriods_EmployeeId",
                schema: "app",
                table: "EmployeeStatusPeriods",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatusPeriods_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "EmployeeStatusPeriods",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTags_EmployeeId_TagId",
                schema: "app",
                table: "EmployeeTags",
                columns: new[] { "EmployeeId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTags_IsDeleted",
                schema: "app",
                table: "EmployeeTags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTags_TagId",
                schema: "app",
                table: "EmployeeTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_IsDeleted",
                schema: "app",
                table: "ExportJobs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_RequestedByUserId_RequestedAtUtc",
                schema: "app",
                table: "ExportJobs",
                columns: new[] { "RequestedByUserId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status_RequestedAtUtc",
                schema: "app",
                table: "ExportJobs",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalCities_Code",
                schema: "platform",
                table: "GlobalCities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalCities_IsDeleted",
                schema: "platform",
                table: "GlobalCities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalCities_NameAr_NameEn",
                schema: "platform",
                table: "GlobalCities",
                columns: new[] { "NameAr", "NameEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Housing_CityId",
                schema: "app",
                table: "Housing",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Housing_Code",
                schema: "app",
                table: "Housing",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Housing_IsDeleted",
                schema: "app",
                table: "Housing",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Housing_Status_CityId",
                schema: "app",
                table: "Housing",
                columns: new[] { "Status", "CityId" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingResidencePeriods_EmployeeId",
                schema: "app",
                table: "HousingResidencePeriods",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HousingResidencePeriods_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "HousingResidencePeriods",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingResidencePeriods_HousingId_EffectiveFrom",
                schema: "app",
                table: "HousingResidencePeriods",
                columns: new[] { "HousingId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingSupervisorPeriods_HousingId",
                schema: "app",
                table: "HousingSupervisorPeriods",
                column: "HousingId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HousingSupervisorPeriods_HousingId_EffectiveFrom",
                schema: "app",
                table: "HousingSupervisorPeriods",
                columns: new[] { "HousingId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingSupervisorPeriods_SupervisorEmployeeId",
                schema: "app",
                table: "HousingSupervisorPeriods",
                column: "SupervisorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingTags_HousingId_TagId",
                schema: "app",
                table: "HousingTags",
                columns: new[] { "HousingId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HousingTags_IsDeleted",
                schema: "app",
                table: "HousingTags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HousingTags_TagId",
                schema: "app",
                table: "HousingTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_Code",
                schema: "app",
                table: "JobTitles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_IsDeleted",
                schema: "app",
                table: "JobTitles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalDecisions_DecidedByUserId_DecidedAtUtc",
                schema: "app",
                table: "LeaveApprovalDecisions",
                columns: new[] { "DecidedByUserId", "DecidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalDecisions_LeaveRequestId_StepSequence_DecidedAtUtc",
                schema: "app",
                table: "LeaveApprovalDecisions",
                columns: new[] { "LeaveRequestId", "StepSequence", "DecidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflows_ClientPlatformId",
                schema: "app",
                table: "LeaveApprovalWorkflows",
                column: "ClientPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflows_Code_Version",
                schema: "app",
                table: "LeaveApprovalWorkflows",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflows_IsDeleted",
                schema: "app",
                table: "LeaveApprovalWorkflows",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflows_LeaveTypeId",
                schema: "app",
                table: "LeaveApprovalWorkflows",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflows_Status_Priority_LeaveTypeId_RelationshipType_AppliesToRider_ClientPlatformId",
                schema: "app",
                table: "LeaveApprovalWorkflows",
                columns: new[] { "Status", "Priority", "LeaveTypeId", "RelationshipType", "AppliesToRider", "ClientPlatformId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflowSteps_IsDeleted",
                schema: "app",
                table: "LeaveApprovalWorkflowSteps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflowId_Sequence",
                schema: "app",
                table: "LeaveApprovalWorkflowSteps",
                columns: new[] { "LeaveApprovalWorkflowId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflowId_StepKey",
                schema: "app",
                table: "LeaveApprovalWorkflowSteps",
                columns: new[] { "LeaveApprovalWorkflowId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveApprovalWorkflowSteps_RequiredPermissionKey",
                schema: "app",
                table: "LeaveApprovalWorkflowSteps",
                column: "RequiredPermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCancellationRequests_IsDeleted",
                schema: "app",
                table: "LeaveCancellationRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCancellationRequests_LeaveRequestId",
                schema: "app",
                table: "LeaveCancellationRequests",
                column: "LeaveRequestId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveCancellationRequests_LeaveRequestId_RequestedAtUtc",
                schema: "app",
                table: "LeaveCancellationRequests",
                columns: new[] { "LeaveRequestId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDateChangeRequests_IsDeleted",
                schema: "app",
                table: "LeaveDateChangeRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDateChangeRequests_LeaveRequestId",
                schema: "app",
                table: "LeaveDateChangeRequests",
                column: "LeaveRequestId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDateChangeRequests_LeaveRequestId_RequestedAtUtc",
                schema: "app",
                table: "LeaveDateChangeRequests",
                columns: new[] { "LeaveRequestId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocuments_CurrentVersionId",
                schema: "app",
                table: "LeaveRequestDocuments",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocuments_IsDeleted",
                schema: "app",
                table: "LeaveRequestDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocuments_LeaveRequestId_Kind",
                schema: "app",
                table: "LeaveRequestDocuments",
                columns: new[] { "LeaveRequestId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocumentVersions_LeaveRequestDocumentId_VersionNumber",
                schema: "app",
                table: "LeaveRequestDocumentVersions",
                columns: new[] { "LeaveRequestDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocumentVersions_Sha256Checksum",
                schema: "app",
                table: "LeaveRequestDocumentVersions",
                column: "Sha256Checksum");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDocumentVersions_SupersededVersionId",
                schema: "app",
                table: "LeaveRequestDocumentVersions",
                column: "SupersededVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApprovalWorkflowId",
                schema: "app",
                table: "LeaveRequests",
                column: "ApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_StartDate_EndDate",
                schema: "app",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_HrStatus_StartDate",
                schema: "app",
                table: "LeaveRequests",
                columns: new[] { "HrStatus", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_IsDeleted",
                schema: "app",
                table: "LeaveRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                schema: "app",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_RelatedClientContractId",
                schema: "app",
                table: "LeaveRequests",
                column: "RelatedClientContractId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_RequestNumber",
                schema: "app",
                table: "LeaveRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status_CurrentApprovalStepKey_SubmittedAtUtc",
                schema: "app",
                table: "LeaveRequests",
                columns: new[] { "Status", "CurrentApprovalStepKey", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                schema: "app",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_IsDeleted",
                schema: "app",
                table: "LeaveTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ExpiresAtUtc",
                schema: "app",
                table: "Notifications",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsDeleted",
                schema: "app",
                table: "Notifications",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_DeduplicationKey",
                schema: "app",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "DeduplicationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_ReadAtUtc_VisibleAtUtc",
                schema: "app",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "ReadAtUtc", "VisibleAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingCities_GlobalCityId",
                schema: "app",
                table: "OperatingCities",
                column: "GlobalCityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatingCities_IsDeleted",
                schema: "app",
                table: "OperatingCities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OutsideRiderDetails_EmployeeId",
                schema: "app",
                table: "OutsideRiderDetails",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutsideRiderDetails_IsDeleted",
                schema: "app",
                table: "OutsideRiderDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDefinitions_Category_DisplayOrder",
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Category", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDefinitions_IsDeleted",
                schema: "platform",
                table: "PermissionDefinitions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDefinitions_Key",
                schema: "platform",
                table: "PermissionDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountCredentialVersions_PlatformRiderAccountId_KeyVersion",
                schema: "app",
                table: "PlatformAccountCredentialVersions",
                columns: new[] { "PlatformRiderAccountId", "KeyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountCredentialVersions_PlatformRiderAccountId_RotatedAtUtc",
                schema: "app",
                table: "PlatformAccountCredentialVersions",
                columns: new[] { "PlatformRiderAccountId", "RotatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountCredentialVersions_SupersededVersionId",
                schema: "app",
                table: "PlatformAccountCredentialVersions",
                column: "SupersededVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_ClientContractId_Status",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_NormalizedExternalAccountId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientPlatformId", "NormalizedExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_Code",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_IsDeleted",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccountTags_IsDeleted",
                schema: "app",
                table: "PlatformRiderAccountTags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccountTags_PlatformRiderAccountId_TagId",
                schema: "app",
                table: "PlatformRiderAccountTags",
                columns: new[] { "PlatformRiderAccountId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccountTags_TagId",
                schema: "app",
                table: "PlatformRiderAccountTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderAssignmentEvents_RiderClientAssignmentId_OccurredAtUtc",
                schema: "app",
                table: "RiderAssignmentEvents",
                columns: new[] { "RiderClientAssignmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_ClientContractId_Status",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "ClientContractId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_EmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_IsDeleted",
                schema: "app",
                table: "RiderClientAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_PlatformRiderAccountId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "PlatformRiderAccountId",
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_EmployeeId",
                schema: "app",
                table: "RiderProfiles",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_IsDeleted",
                schema: "app",
                table: "RiderProfiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles",
                column: "LicenseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_PreferredCityId",
                schema: "app",
                table: "RiderProfiles",
                column: "PreferredCityId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_Status",
                schema: "app",
                table: "RiderProfiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_IsDeleted",
                schema: "app",
                table: "SavedViews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_UserId_ModuleKey_Name",
                schema: "app",
                table: "SavedViews",
                columns: new[] { "UserId", "ModuleKey", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentJobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_EmployeeId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_IsDeleted",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_ManagerEmployeeId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_ProfilePhotoDocumentId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "ProfilePhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Code",
                schema: "app",
                table: "Tags",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsDeleted",
                schema: "app",
                table: "Tags",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_EmployeeDocumentVersions_CurrentVersionId",
                schema: "app",
                table: "EmployeeDocuments",
                column: "CurrentVersionId",
                principalSchema: "app",
                principalTable: "EmployeeDocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequestDocuments_LeaveRequestDocumentVersions_CurrentVersionId",
                schema: "app",
                table: "LeaveRequestDocuments",
                column: "CurrentVersionId",
                principalSchema: "app",
                principalTable: "LeaveRequestDocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientContracts_ClientPlatforms_ClientPlatformId",
                schema: "app",
                table: "ClientContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveApprovalWorkflows_ClientPlatforms_ClientPlatformId",
                schema: "app",
                table: "LeaveApprovalWorkflows");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_ClientContracts_RelatedClientContractId",
                schema: "app",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_DocumentTypes_DocumentTypeId",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_Employees_EmployeeId",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Employees_EmployeeId",
                schema: "app",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_EmployeeDocumentVersions_CurrentVersionId",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestDocuments_LeaveRequests_LeaveRequestId",
                schema: "app",
                table: "LeaveRequestDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequestDocuments_LeaveRequestDocumentVersions_CurrentVersionId",
                schema: "app",
                table: "LeaveRequestDocuments");

            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ClientContractTags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "CompanyProfile",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "DatasetVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DocumentRequirements",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeAbsenceComplianceCaseEvents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeJobTitlePeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeRelationshipPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeStatusChangeRequests",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeTags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExportJobs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingResidencePeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingSupervisorPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingTags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveApprovalDecisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveApprovalWorkflowSteps",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveCancellationRequests",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveDateChangeRequests",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "app");

            migrationBuilder.DropTable(
                name: "OperatingCities",
                schema: "app");

            migrationBuilder.DropTable(
                name: "OutsideRiderDetails",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PermissionDefinitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PlatformAccountCredentialVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlatformRiderAccountTags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderAssignmentEvents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SavedViews",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SponsoredInternalDetails",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeAbsenceComplianceCases",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeStatusPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Housing",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderClientAssignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobTitles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlatformRiderAccounts",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderProfiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "GlobalCities",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ClientPlatforms",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ClientContracts",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DocumentTypes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeDocumentVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveApprovalWorkflows",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveTypes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveRequestDocumentVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "LeaveRequestDocuments",
                schema: "app");

            migrationBuilder.DropSequence(
                name: "AuditEntrySequence",
                schema: "audit");
        }
    }
}
