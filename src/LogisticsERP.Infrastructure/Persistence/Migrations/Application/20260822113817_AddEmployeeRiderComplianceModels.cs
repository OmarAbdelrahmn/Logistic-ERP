using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1825 // EF scaffolds zero-length rowversion defaults.

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEmployeeRiderComplianceModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_Employees_EmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderProfiles_EmployeeDocuments_LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsoredInternalDetails_JobTitles_CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropIndex(
                name: "IX_RiderProfiles_LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.RenameColumn(
                name: "SponsorLegalReference",
                schema: "app",
                table: "SponsoredInternalDetails",
                newName: "LegacySponsorReference");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentSponsorId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "ActualEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_RiderClientAssignments_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom");

            migrationBuilder.RenameIndex(
                name: "IX_RiderClientAssignments_EmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "IX_RiderClientAssignments_ActualEmployeeId");

            migrationBuilder.RenameColumn(
                name: "EndedByUserId",
                schema: "app",
                table: "HousingSupervisorPeriods",
                newName: "ClosedByUserId");

            migrationBuilder.RenameColumn(
                name: "EndedByUserId",
                schema: "app",
                table: "HousingResidencePeriods",
                newName: "ClosedByUserId");

            migrationBuilder.AddColumn<int>(
                name: "BillingMode",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RegisteredEmployeeId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationType",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "app",
                table: "HousingSupervisorPeriods",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "app",
                table: "HousingSupervisorPeriods",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "app",
                table: "HousingResidencePeriods",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "app",
                table: "HousingResidencePeriods",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeStatusPeriods",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeStatusPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeStatusPeriods",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryPhone",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedNameEn",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "FullNameEn",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalityCountryCode",
                schema: "app",
                table: "Employees",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeRelationshipPeriods",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeRelationshipPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeRelationshipPeriods",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "MaxFileSizeBytes",
                schema: "platform",
                table: "DocumentTypes",
                type: "bigint",
                nullable: false,
                defaultValue: 10485760L);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RiderProfiles_Id_EmployeeId",
                schema: "app",
                table: "RiderProfiles",
                columns: new[] { "Id", "EmployeeId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformRiderAccounts_Id_ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "Id", "ClientContractId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ClientContracts_Id_ClientPlatformId",
                schema: "app",
                table: "ClientContracts",
                columns: new[] { "Id", "ClientPlatformId" });

            migrationBuilder.CreateTable(
                name: "DriverLicenseCategories",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_DriverLicenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceCompanies",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProviderRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
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
                    table.PrimaryKey("PK_InsuranceCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalWorkTypes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_OperationalWorkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResidencyProfessions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_ResidencyProfessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiderCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedCardNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    ValidityCycle = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_RiderCards", x => x.Id);
                    table.CheckConstraint("CK_RiderCards_DateRange", "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_RiderCards_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderCards_RiderCards_PreviousCardId",
                        column: x => x.PreviousCardId,
                        principalSchema: "app",
                        principalTable: "RiderCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderCards_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderHealthCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CardNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CardNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: false),
                    CardType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_RiderHealthCards", x => x.Id);
                    table.CheckConstraint("CK_RiderHealthCards_DateRange", "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_RiderHealthCards_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderHealthCards_RiderHealthCards_PreviousCardId",
                        column: x => x.PreviousCardId,
                        principalSchema: "app",
                        principalTable: "RiderHealthCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderHealthCards_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerIdentityNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegistryNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistryNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CommercialRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnifiedNationalNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SponsorType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ActiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    AddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_Sponsors", x => x.Id);
                    table.CheckConstraint("CK_Sponsors_ActiveRange", "[ActiveTo] IS NULL OR [ActiveFrom] IS NULL OR [ActiveTo] >= [ActiveFrom]");
                    table.ForeignKey(
                        name: "FK_Sponsors_CompanyProfile_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalSchema: "platform",
                        principalTable: "CompanyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDriverLicenses",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverLicenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LicenseNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    LicenseNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BookingStatus = table.Column<int>(type: "int", nullable: false),
                    IssuanceStatus = table.Column<int>(type: "int", nullable: false),
                    LicenseStatus = table.Column<int>(type: "int", nullable: false),
                    PreviousLicenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDriverLicenses", x => x.Id);
                    table.CheckConstraint("CK_EmployeeDriverLicenses_DateRange", "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_EmployeeDriverLicenses_DriverLicenseCategories_DriverLicenseCategoryId",
                        column: x => x.DriverLicenseCategoryId,
                        principalSchema: "app",
                        principalTable: "DriverLicenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDriverLicenses_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDriverLicenses_EmployeeDriverLicenses_PreviousLicenseId",
                        column: x => x.PreviousLicenseId,
                        principalSchema: "app",
                        principalTable: "EmployeeDriverLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDriverLicenses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePlanLevels",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    NetworkName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CoverageClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnualCoverageLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeductiblePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_InsurancePlanLevels", x => x.Id);
                    table.UniqueConstraint("AK_InsurancePlanLevels_Id_InsuranceCompanyId", x => new { x.Id, x.InsuranceCompanyId });
                    table.CheckConstraint("CK_InsurancePlanLevels_AnnualLimit", "[AnnualCoverageLimit] IS NULL OR [AnnualCoverageLimit] >= 0");
                    table.CheckConstraint("CK_InsurancePlanLevels_DateRange", "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_InsurancePlanLevels_Deductible", "[DeductiblePercentage] IS NULL OR ([DeductiblePercentage] >= 0 AND [DeductiblePercentage] <= 100)");
                    table.CheckConstraint("CK_InsurancePlanLevels_Rank", "[Rank] >= 0");
                    table.ForeignKey(
                        name: "FK_InsurancePlanLevels_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalSchema: "app",
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobTitleOperationalWorkTypes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalWorkTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_JobTitleOperationalWorkTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitleOperationalWorkTypes_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalSchema: "app",
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobTitleOperationalWorkTypes_OperationalWorkTypes_OperationalWorkTypeId",
                        column: x => x.OperationalWorkTypeId,
                        principalSchema: "app",
                        principalTable: "OperationalWorkTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePromissoryNotes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NoteNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedNoteNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryCompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EmployeePromissoryNotes", x => x.Id);
                    table.CheckConstraint("CK_EmployeePromissoryNotes_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_EmployeePromissoryNotes_DateRange", "[DueDate] IS NULL OR [DueDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_EmployeePromissoryNotes_CompanyProfile_BeneficiaryCompanyProfileId",
                        column: x => x.BeneficiaryCompanyProfileId,
                        principalSchema: "platform",
                        principalTable: "CompanyProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePromissoryNotes_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePromissoryNotes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePromissoryNotes_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeResidencyPermits",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResidencyProfessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermitNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PermitNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    PermitNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousPermitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeResidencyPermits", x => x.Id);
                    table.CheckConstraint("CK_EmployeeResidencyPermits_DateRange", "[IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_EmployeeResidencyPermits_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeResidencyPermits_EmployeeResidencyPermits_PreviousPermitId",
                        column: x => x.PreviousPermitId,
                        principalSchema: "app",
                        principalTable: "EmployeeResidencyPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeResidencyPermits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeResidencyPermits_ResidencyProfessions_ResidencyProfessionId",
                        column: x => x.ResidencyProfessionId,
                        principalSchema: "app",
                        principalTable: "ResidencyProfessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeResidencyPermits_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSponsorshipPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSponsorshipPeriods", x => x.Id);
                    table.CheckConstraint("CK_EmployeeSponsorshipPeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_EmployeeSponsorshipPeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsorshipPeriods_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccountRegistrations",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatingCityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PlatformAccountRegistrations", x => x.Id);
                    table.CheckConstraint("CK_PlatformAccountRegistrations_ActivationRange", "[ActivatedAtUtc] IS NULL OR [RequestedAtUtc] IS NULL OR [ActivatedAtUtc] >= [RequestedAtUtc]");
                    table.CheckConstraint("CK_PlatformAccountRegistrations_Registration", "([RegistrationType] = 1 AND [SponsorId] IS NOT NULL) OR ([RegistrationType] = 2 AND [SponsorId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_ClientContracts_ClientContractId_ClientPlatformId",
                        columns: x => new { x.ClientContractId, x.ClientPlatformId },
                        principalSchema: "app",
                        principalTable: "ClientContracts",
                        principalColumns: new[] { "Id", "ClientPlatformId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_Employees_RegisteredEmployeeId",
                        column: x => x.RegisteredEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_OperatingCities_OperatingCityId",
                        column: x => x.OperatingCityId,
                        principalSchema: "app",
                        principalTable: "OperatingCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_RiderProfiles_RiderProfileId_RegisteredEmployeeId",
                        columns: x => new { x.RiderProfileId, x.RegisteredEmployeeId },
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumns: new[] { "Id", "EmployeeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountRegistrations_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeMedicalInsurancePolicies",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsurancePlanLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PolicyNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    PolicyNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                    MemberNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MemberNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    MemberNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeMedicalInsurancePolicies", x => x.Id);
                    table.CheckConstraint("CK_EmployeeMedicalInsurancePolicies_DateRange", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_EmployeeMedicalInsurancePolicies_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeMedicalInsurancePolicies_EmployeeMedicalInsurancePolicies_PreviousPolicyId",
                        column: x => x.PreviousPolicyId,
                        principalSchema: "app",
                        principalTable: "EmployeeMedicalInsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeMedicalInsurancePolicies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeMedicalInsurancePolicies_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalSchema: "app",
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeMedicalInsurancePolicies_InsurancePlanLevels_InsurancePlanLevelId_InsuranceCompanyId",
                        columns: x => new { x.InsurancePlanLevelId, x.InsuranceCompanyId },
                        principalSchema: "app",
                        principalTable: "InsurancePlanLevels",
                        principalColumns: new[] { "Id", "InsuranceCompanyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "DocumentTypes",
                columns: new[] { "Id", "AllowedMimeTypes", "AppliesToOutsideRider", "AppliesToRiderProfile", "AppliesToSponsoredInternal", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "IsDeleted", "MaxFileSizeBytes", "NameAr", "NameEn", "RequiresExpiryDate", "RequiresFile", "RequiresIssueDate", "RequiresNumber", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-8000-000000000030"), "application/pdf,image/jpeg,image/png", false, false, true, "RESIDENCY_PERMIT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "الإقامة", "Residency Permit", true, true, true, true, 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000031"), "application/pdf,image/jpeg,image/png", true, true, true, "DRIVER_LICENSE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "رخصة القيادة", "Driver License", true, true, true, true, 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000032"), "application/pdf,image/jpeg,image/png", true, true, true, "RIDER_CARD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "بطاقة السائق", "Rider Card", true, true, true, true, 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000033"), "application/pdf,image/jpeg,image/png", true, true, true, "HEALTH_CARD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "البطاقة الصحية", "Health Card", true, true, true, true, 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000034"), "application/pdf,image/jpeg,image/png", true, false, true, "PROMISSORY_NOTE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "سند الأمر", "Promissory Note", false, true, true, true, 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000035"), "application/pdf,image/jpeg,image/png", true, true, true, "MEDICAL_INSURANCE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "التأمين الطبي", "Medical Insurance", true, true, true, true, 1, null, null }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "DriverLicenseCategories",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "NameAr", "NameEn", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-8000-000000000020"), "LIGHT_TRANSPORT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "نقل خفيف", "Light Transport", 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000021"), "MOTORCYCLE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "دراجة نارية", "Motorcycle", 1, null, null }
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "GlobalCities",
                columns: new[] { "Id", "Code", "CountryCode", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DisplayOrder", "IsDeleted", "Latitude", "Longitude", "NameAr", "NameEn", "RegionAr", "RegionEn", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000004"), "RIYADH", "SA", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, 2, false, 24.7136m, 46.6753m, "الرياض", "Riyadh", "منطقة الرياض", "Riyadh Region", 1, null, null });

            migrationBuilder.InsertData(
                schema: "app",
                table: "OperationalWorkTypes",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "NameAr", "NameEn", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-8000-000000000010"), "ADMIN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "إداري", "Administrative", 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000011"), "CAR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "سيارة", "Car", 1, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000012"), "MOTORCYCLE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "دراجة نارية", "Motorcycle", 1, null, null }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "OperatingCities",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DisabledAt", "EnabledFrom", "GlobalCityId", "IsDeleted", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, new DateOnly(2026, 1, 1), new Guid("019c18d5-62e1-7000-8000-000000000004"), false, 1, null, null });

            migrationBuilder.Sql(
                """
                EXEC(N'
                UPDATE employee
                SET employee.HireDate = sponsored.HireDate,
                    employee.NationalityCountryCode = sponsored.NationalityCountryCode
                FROM [app].[Employees] AS employee
                INNER JOIN [app].[SponsoredInternalDetails] AS sponsored ON sponsored.EmployeeId = employee.Id;

                UPDATE employee
                SET employee.NationalityCountryCode = outsideRider.NationalityCountryCode
                FROM [app].[Employees] AS employee
                INNER JOIN [app].[OutsideRiderDetails] AS outsideRider ON outsideRider.EmployeeId = employee.Id
                WHERE employee.NationalityCountryCode IS NULL;

                UPDATE [app].[EmployeeJobTitlePeriods]
                SET OperatingCityId = ''019c18d5-62e1-7000-8000-000000000003'',
                    OperationalWorkTypeId = ''019c18d5-62e1-7000-8000-000000000010'';

                INSERT INTO [app].[EmployeeJobTitlePeriods]
                    (Id, EmployeeId, JobTitleId, OperationalWorkTypeId, OperatingCityId, EffectiveFrom,
                     EffectiveTo, Reason, ChangedByUserId, CreatedAtUtc, CreatedByUserId, ClosedAtUtc, ClosedByUserId)
                SELECT NEWID(), sponsored.EmployeeId, sponsored.CurrentJobTitleId,
                       ''019c18d5-62e1-7000-8000-000000000010'',
                       ''019c18d5-62e1-7000-8000-000000000003'',
                       COALESCE(sponsored.HireDate, CAST(sponsored.CreatedAtUtc AS date), CAST(SYSUTCDATETIME() AS date)),
                       NULL, N''Migrated from the previous current job-title reference.'',
                       ''00000000-0000-0000-0000-000000000000'', SYSUTCDATETIME(), NULL, NULL, NULL
                FROM [app].[SponsoredInternalDetails] AS sponsored
                WHERE sponsored.CurrentJobTitleId IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [app].[EmployeeJobTitlePeriods] AS period
                      WHERE period.EmployeeId = sponsored.EmployeeId AND period.EffectiveTo IS NULL
                  );

                INSERT INTO [app].[EmployeeDriverLicenses]
                    (Id, EmployeeId, DriverLicenseCategoryId, BookingStatus, IssuanceStatus, LicenseStatus,
                     PreviousLicenseId, IsCurrent, EmployeeDocumentId, Notes, CreatedAtUtc, IsDeleted)
                SELECT NEWID(), rider.EmployeeId, ''019c18d5-62e1-7000-8000-000000000020'',
                       6, 3, 2, NULL, 1, rider.LicenseDocumentId,
                       N''Migrated from RiderProfile.LicenseDocumentId.'', SYSUTCDATETIME(), 0
                FROM [app].[RiderProfiles] AS rider
                WHERE rider.LicenseDocumentId IS NOT NULL;

                UPDATE [app].[PlatformRiderAccounts]
                SET BillingMode = 1,
                    OperatingCityId = ''019c18d5-62e1-7000-8000-000000000003'',
                    RegistrationType = 2,
                    SponsorId = NULL;
                ');
                """);

            migrationBuilder.DropIndex(
                name: "IX_SponsoredInternalDetails_CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropColumn(
                name: "CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropColumn(
                name: "HireDate",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropColumn(
                name: "NationalityCountryCode",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropColumn(
                name: "LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "NationalityCountryCode",
                schema: "app",
                table: "OutsideRiderDetails");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_PlatformRiderAccountId_ClientContractId",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "PlatformRiderAccountId", "ClientContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "ActualEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_ClientContractId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientContractId", "ClientPlatformId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_SponsorId_RegistrationType",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "OperatingCityId", "SponsorId", "RegistrationType" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_Status",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "RegisteredEmployeeId", "ClientPlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "SponsorId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlatformRiderAccounts_Registration",
                schema: "app",
                table: "PlatformRiderAccounts",
                sql: "([RegistrationType] = 1 AND [SponsorId] IS NOT NULL) OR ([RegistrationType] = 2 AND [SponsorId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_OperatingCityId_OperationalWorkTypeId_JobTitleId_EffectiveTo",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                columns: new[] { "OperatingCityId", "OperationalWorkTypeId", "JobTitleId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "OperationalWorkTypeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentTypes_MaxFileSize",
                schema: "platform",
                table: "DocumentTypes",
                sql: "[MaxFileSizeBytes] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicenseCategories_Code",
                schema: "app",
                table: "DriverLicenseCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicenseCategories_IsDeleted",
                schema: "app",
                table: "DriverLicenseCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_DriverLicenseCategoryId",
                schema: "app",
                table: "EmployeeDriverLicenses",
                column: "DriverLicenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_EmployeeDocumentId",
                schema: "app",
                table: "EmployeeDriverLicenses",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_EmployeeId_DriverLicenseCategoryId",
                schema: "app",
                table: "EmployeeDriverLicenses",
                columns: new[] { "EmployeeId", "DriverLicenseCategoryId" },
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_EmployeeId_DriverLicenseCategoryId_LicenseStatus",
                schema: "app",
                table: "EmployeeDriverLicenses",
                columns: new[] { "EmployeeId", "DriverLicenseCategoryId", "LicenseStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_IsDeleted",
                schema: "app",
                table: "EmployeeDriverLicenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_LicenseNumberLookupHash",
                schema: "app",
                table: "EmployeeDriverLicenses",
                column: "LicenseNumberLookupHash",
                filter: "[LicenseNumberLookupHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_PreviousLicenseId",
                schema: "app",
                table: "EmployeeDriverLicenses",
                column: "PreviousLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_EmployeeDocumentId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_EmployeeId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "EmployeeId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_InsuranceCompanyId_Status_EndDate",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                columns: new[] { "InsuranceCompanyId", "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_InsurancePlanLevelId_InsuranceCompanyId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                columns: new[] { "InsurancePlanLevelId", "InsuranceCompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_IsDeleted",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_MemberNumberLookupHash",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "MemberNumberLookupHash",
                filter: "[MemberNumberLookupHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_PolicyNumberLookupHash",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "PolicyNumberLookupHash",
                filter: "[PolicyNumberLookupHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_PreviousPolicyId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                column: "PreviousPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePromissoryNotes_BeneficiaryCompanyProfileId_NormalizedNoteNumber",
                schema: "app",
                table: "EmployeePromissoryNotes",
                columns: new[] { "BeneficiaryCompanyProfileId", "NormalizedNoteNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePromissoryNotes_EmployeeDocumentId",
                schema: "app",
                table: "EmployeePromissoryNotes",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePromissoryNotes_EmployeeId_Status",
                schema: "app",
                table: "EmployeePromissoryNotes",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePromissoryNotes_IsDeleted",
                schema: "app",
                table: "EmployeePromissoryNotes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePromissoryNotes_SponsorId",
                schema: "app",
                table: "EmployeePromissoryNotes",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_EmployeeDocumentId",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_EmployeeId",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "EmployeeId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_IsDeleted",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_PermitNumberLookupHash",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "PermitNumberLookupHash",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_PreviousPermitId",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "PreviousPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_ResidencyProfessionId",
                schema: "app",
                table: "EmployeeResidencyPermits",
                column: "ResidencyProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeResidencyPermits_SponsorId_Status_ExpiryDate",
                schema: "app",
                table: "EmployeeResidencyPermits",
                columns: new[] { "SponsorId", "Status", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsorshipPeriods_EmployeeId",
                schema: "app",
                table: "EmployeeSponsorshipPeriods",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsorshipPeriods_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "EmployeeSponsorshipPeriods",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsorshipPeriods_SponsorId_Status_EffectiveTo",
                schema: "app",
                table: "EmployeeSponsorshipPeriods",
                columns: new[] { "SponsorId", "Status", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_Code",
                schema: "app",
                table: "InsuranceCompanies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_IsDeleted",
                schema: "app",
                table: "InsuranceCompanies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_ProviderRegistrationNumber",
                schema: "app",
                table: "InsuranceCompanies",
                column: "ProviderRegistrationNumber",
                unique: true,
                filter: "[ProviderRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceCompanies_Status_NameAr",
                schema: "app",
                table: "InsuranceCompanies",
                columns: new[] { "Status", "NameAr" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlanLevels_InsuranceCompanyId_Code",
                schema: "app",
                table: "InsurancePlanLevels",
                columns: new[] { "InsuranceCompanyId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlanLevels_InsuranceCompanyId_Status_Rank",
                schema: "app",
                table: "InsurancePlanLevels",
                columns: new[] { "InsuranceCompanyId", "Status", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlanLevels_IsDeleted",
                schema: "app",
                table: "InsurancePlanLevels",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitleOperationalWorkTypes_IsDeleted",
                schema: "app",
                table: "JobTitleOperationalWorkTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitleOperationalWorkTypes_JobTitleId_OperationalWorkTypeId",
                schema: "app",
                table: "JobTitleOperationalWorkTypes",
                columns: new[] { "JobTitleId", "OperationalWorkTypeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitleOperationalWorkTypes_OperationalWorkTypeId",
                schema: "app",
                table: "JobTitleOperationalWorkTypes",
                column: "OperationalWorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalWorkTypes_Code",
                schema: "app",
                table: "OperationalWorkTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalWorkTypes_IsDeleted",
                schema: "app",
                table: "OperationalWorkTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_ClientContractId_ClientPlatformId",
                schema: "app",
                table: "PlatformAccountRegistrations",
                columns: new[] { "ClientContractId", "ClientPlatformId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_ClientPlatformId",
                schema: "app",
                table: "PlatformAccountRegistrations",
                column: "ClientPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_IsDeleted",
                schema: "app",
                table: "PlatformAccountRegistrations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_OperatingCityId_SponsorId_RegistrationType_Status",
                schema: "app",
                table: "PlatformAccountRegistrations",
                columns: new[] { "OperatingCityId", "SponsorId", "RegistrationType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_PlatformRiderAccountId",
                schema: "app",
                table: "PlatformAccountRegistrations",
                column: "PlatformRiderAccountId",
                unique: true,
                filter: "[PlatformRiderAccountId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_RegisteredEmployeeId_ClientPlatformId_Status",
                schema: "app",
                table: "PlatformAccountRegistrations",
                columns: new[] { "RegisteredEmployeeId", "ClientPlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_RiderProfileId_RegisteredEmployeeId",
                schema: "app",
                table: "PlatformAccountRegistrations",
                columns: new[] { "RiderProfileId", "RegisteredEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountRegistrations_SponsorId",
                schema: "app",
                table: "PlatformAccountRegistrations",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidencyProfessions_Code",
                schema: "app",
                table: "ResidencyProfessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidencyProfessions_IsDeleted",
                schema: "app",
                table: "ResidencyProfessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResidencyProfessions_NameAr",
                schema: "app",
                table: "ResidencyProfessions",
                column: "NameAr");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_EmployeeDocumentId",
                schema: "app",
                table: "RiderCards",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_IsDeleted",
                schema: "app",
                table: "RiderCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_NormalizedCardNumber",
                schema: "app",
                table: "RiderCards",
                column: "NormalizedCardNumber",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_PreviousCardId",
                schema: "app",
                table: "RiderCards",
                column: "PreviousCardId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_RiderProfileId_CardType",
                schema: "app",
                table: "RiderCards",
                columns: new[] { "RiderProfileId", "CardType" },
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_RiderProfileId_CardType_Status",
                schema: "app",
                table: "RiderCards",
                columns: new[] { "RiderProfileId", "CardType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_CardNumberLookupHash",
                schema: "app",
                table: "RiderHealthCards",
                column: "CardNumberLookupHash",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_EmployeeDocumentId",
                schema: "app",
                table: "RiderHealthCards",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_IsDeleted",
                schema: "app",
                table: "RiderHealthCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_PreviousCardId",
                schema: "app",
                table: "RiderHealthCards",
                column: "PreviousCardId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_RiderProfileId_CardType",
                schema: "app",
                table: "RiderHealthCards",
                columns: new[] { "RiderProfileId", "CardType" },
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_RiderProfileId_CardType_Status",
                schema: "app",
                table: "RiderHealthCards",
                columns: new[] { "RiderProfileId", "CardType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_CommercialRegistrationNumber",
                schema: "app",
                table: "Sponsors",
                column: "CommercialRegistrationNumber",
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_CompanyProfileId",
                schema: "app",
                table: "Sponsors",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_EmployerIdentityNumber",
                schema: "app",
                table: "Sponsors",
                column: "EmployerIdentityNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_IsDeleted",
                schema: "app",
                table: "Sponsors",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_Status_RegistryNameAr",
                schema: "app",
                table: "Sponsors",
                columns: new[] { "Status", "RegistryNameAr" });

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeJobTitlePeriods_OperatingCities_OperatingCityId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeJobTitlePeriods_OperationalWorkTypes_OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "OperationalWorkTypeId",
                principalSchema: "app",
                principalTable: "OperationalWorkTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientContractId", "ClientPlatformId" },
                principalSchema: "app",
                principalTable: "ClientContracts",
                principalColumns: new[] { "Id", "ClientPlatformId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_Employees_RegisteredEmployeeId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "RegisteredEmployeeId",
                principalSchema: "app",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_OperatingCities_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "SponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_Employees_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "ActualEmployeeId",
                principalSchema: "app",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId_ClientContractId",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "PlatformRiderAccountId", "ClientContractId" },
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumns: new[] { "Id", "ClientContractId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "ActualEmployeeId" },
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumns: new[] { "Id", "EmployeeId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsoredInternalDetails_Sponsors_CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentSponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeJobTitlePeriods_OperatingCities_OperatingCityId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeJobTitlePeriods_OperationalWorkTypes_OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_Employees_RegisteredEmployeeId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_OperatingCities_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_Employees_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId_ClientContractId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_SponsoredInternalDetails_Sponsors_CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropTable(
                name: "EmployeeDriverLicenses",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeMedicalInsurancePolicies",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeePromissoryNotes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeResidencyPermits",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeSponsorshipPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobTitleOperationalWorkTypes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlatformAccountRegistrations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderCards",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderHealthCards",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DriverLicenseCategories",
                schema: "app");

            migrationBuilder.DropTable(
                name: "InsurancePlanLevels",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ResidencyProfessions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "OperationalWorkTypes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Sponsors",
                schema: "app");

            migrationBuilder.DropTable(
                name: "InsuranceCompanies",
                schema: "app");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RiderProfiles_Id_EmployeeId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_PlatformRiderAccountId_ClientContractId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlatformRiderAccounts_Id_ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_ClientContractId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_SponsorId_RegistrationType",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_Status",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlatformRiderAccounts_Registration",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeJobTitlePeriods_OperatingCityId_OperationalWorkTypeId_JobTitleId_EffectiveTo",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeJobTitlePeriods_OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentTypes_MaxFileSize",
                schema: "platform",
                table: "DocumentTypes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ClientContracts_Id_ClientPlatformId",
                schema: "app",
                table: "ClientContracts");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000032"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "app",
                table: "OperatingCities",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "GlobalCities",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000004"));

            migrationBuilder.DropColumn(
                name: "BillingMode",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "RegisteredEmployeeId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "RegistrationType",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "app",
                table: "HousingSupervisorPeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "app",
                table: "HousingSupervisorPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "app",
                table: "HousingResidencePeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "app",
                table: "HousingResidencePeriods");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeStatusPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeStatusPeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeStatusPeriods");

            migrationBuilder.DropColumn(
                name: "HireDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NationalityCountryCode",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeRelationshipPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeRelationshipPeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeRelationshipPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropColumn(
                name: "OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "app",
                table: "EmployeeJobTitlePeriods");

            migrationBuilder.DropColumn(
                name: "MaxFileSizeBytes",
                schema: "platform",
                table: "DocumentTypes");

            migrationBuilder.RenameColumn(
                name: "LegacySponsorReference",
                schema: "app",
                table: "SponsoredInternalDetails",
                newName: "SponsorLegalReference");

            migrationBuilder.DropIndex(
                name: "IX_SponsoredInternalDetails_CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.DropColumn(
                name: "CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsoredInternalDetails_CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentJobTitleId");

            migrationBuilder.RenameColumn(
                name: "ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "IX_RiderClientAssignments_EmployeeId_EffectiveFrom");

            migrationBuilder.RenameIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                newName: "IX_RiderClientAssignments_EmployeeId");

            migrationBuilder.RenameColumn(
                name: "ClosedByUserId",
                schema: "app",
                table: "HousingSupervisorPeriods",
                newName: "EndedByUserId");

            migrationBuilder.RenameColumn(
                name: "ClosedByUserId",
                schema: "app",
                table: "HousingResidencePeriods",
                newName: "EndedByUserId");

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                schema: "app",
                table: "SponsoredInternalDetails",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalityCountryCode",
                schema: "app",
                table: "SponsoredInternalDetails",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalityCountryCode",
                schema: "app",
                table: "OutsideRiderDetails",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryPhone",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedNameEn",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullNameEn",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiderProfiles_LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles",
                column: "LicenseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "ClientContractId",
                principalSchema: "app",
                principalTable: "ClientContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_Employees_EmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "EmployeeId",
                principalSchema: "app",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "PlatformRiderAccountId",
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId",
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderProfiles_EmployeeDocuments_LicenseDocumentId",
                schema: "app",
                table: "RiderProfiles",
                column: "LicenseDocumentId",
                principalSchema: "app",
                principalTable: "EmployeeDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SponsoredInternalDetails_JobTitles_CurrentJobTitleId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentJobTitleId",
                principalSchema: "app",
                principalTable: "JobTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
