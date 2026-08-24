using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SimplifyEmployeeRiderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This redesign intentionally starts the workforce module with clean data. Discover every
            // table that depends directly or indirectly on Employees, temporarily disable only those
            // tables' constraints, and clear them. Independent catalogs (Sponsors, cities, platforms,
            // vehicle catalogs, and similar reference data) are not targets and remain untouched.
            migrationBuilder.Sql(
                """
                DECLARE @Targets TABLE
                (
                    SchemaName sysname NOT NULL,
                    TableName sysname NOT NULL,
                    PRIMARY KEY (SchemaName, TableName)
                );

                INSERT INTO @Targets (SchemaName, TableName)
                VALUES (N'app', N'Employees');

                WHILE 1 = 1
                BEGIN
                    INSERT INTO @Targets (SchemaName, TableName)
                    SELECT DISTINCT childSchema.name, childTable.name
                    FROM sys.foreign_keys AS foreignKey
                    INNER JOIN sys.tables AS childTable
                        ON childTable.object_id = foreignKey.parent_object_id
                    INNER JOIN sys.schemas AS childSchema
                        ON childSchema.schema_id = childTable.schema_id
                    INNER JOIN sys.tables AS parentTable
                        ON parentTable.object_id = foreignKey.referenced_object_id
                    INNER JOIN sys.schemas AS parentSchema
                        ON parentSchema.schema_id = parentTable.schema_id
                    INNER JOIN @Targets AS target
                        ON target.SchemaName = parentSchema.name
                        AND target.TableName = parentTable.name
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM @Targets AS existing
                        WHERE existing.SchemaName = childSchema.name
                          AND existing.TableName = childTable.name
                    );

                    IF @@ROWCOUNT = 0 BREAK;
                END;

                DECLARE @DisableConstraints nvarchar(max);
                SELECT @DisableConstraints = STRING_AGG(CAST(
                    N'ALTER TABLE ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName)
                    + N' NOCHECK CONSTRAINT ALL;' AS nvarchar(max)), NCHAR(10))
                FROM @Targets;
                EXEC sys.sp_executesql @DisableConstraints;

                DECLARE @DeleteRows nvarchar(max);
                SELECT @DeleteRows = STRING_AGG(CAST(
                    N'DELETE FROM ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName) + N';' AS nvarchar(max)),
                    NCHAR(10))
                FROM @Targets;
                EXEC sys.sp_executesql @DeleteRows;

                DECLARE @EnableConstraints nvarchar(max);
                SELECT @EnableConstraints = STRING_AGG(CAST(
                    N'ALTER TABLE ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName)
                    + N' WITH CHECK CHECK CONSTRAINT ALL;' AS nvarchar(max)), NCHAR(10))
                FROM @Targets;
                EXEC sys.sp_executesql @EnableConstraints;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeStatusChangeRequests_EmployeeStatusPeriods_ResultingStatusPeriodId",
                schema: "app",
                table: "EmployeeStatusChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_ClientContracts_ClientContractId_ClientPlatformId",
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
                name: "FK_RiderProfiles_GlobalCities_PreferredCityId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderVehicleAssignments_Employees_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeJobTitlePeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeRelationshipPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeResidencyPermits",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeSponsorshipPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EmployeeStatusPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "OutsideRiderDetails",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SponsoredInternalDetails",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_RiderVehicleAssignments_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderVehicleAssignments_RiderProfileId_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderProfiles_PreferredCityId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RiderProfiles_Status",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderProfiles_DateRange",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments");

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
                name: "IX_PlatformRiderAccounts_ClientContractId_Status",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_NormalizedExternalAccountId",
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
                name: "IX_Employees_CurrentStatus",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeNumber",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NormalizedNameAr",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NormalizedNameEn",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_DocumentTypeId_NormalizedDocumentNumber",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "PreferredCityId",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "RiderEndDate",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "RiderStartDate",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropColumn(
                name: "BillingMode",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "LabelAr",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "LabelEn",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "NormalizedExternalAccountId",
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
                name: "EmployeeNumber",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NationalityCountryCode",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NormalizedNameAr",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IssuingAuthority",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IssuingCountryCode",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "NormalizedDocumentNumber",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "ResultingStatusPeriodId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                newName: "ResultingWorkHistoryId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeStatusChangeRequests_ResultingStatusPeriodId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                newName: "IX_EmployeeStatusChangeRequests_ResultingWorkHistoryId");

            migrationBuilder.RenameColumn(
                name: "NormalizedNameEn",
                schema: "app",
                table: "Employees",
                newName: "WorkingForMeAs");

            migrationBuilder.RenameColumn(
                name: "CurrentStatus",
                schema: "app",
                table: "Employees",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "CurrentRelationshipType",
                schema: "app",
                table: "Employees",
                newName: "MaritalStatus");

            migrationBuilder.AddColumn<int>(
                name: "TShirtSize",
                schema: "app",
                table: "RiderProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternateContactName",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternateContactPhone",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEndDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractStartDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "app",
                table: "Employees",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                schema: "app",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngagementType",
                schema: "app",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                schema: "app",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IqamaNo",
                schema: "app",
                table: "Employees",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmployee",
                schema: "app",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                schema: "app",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalWorkTypeId",
                schema: "app",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProbationEndDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidencyProfession",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryPhone",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                schema: "app",
                table: "Employees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TerminationDate",
                schema: "app",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeWorkHistory",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkHistory_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId",
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_ExternalAccountId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientPlatformId", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_Status",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "ClientPlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "OperatingCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "RegisteredEmployeeId", "ClientPlatformId" },
                unique: true,
                filter: "[RegisteredEmployeeId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullNameAr",
                schema: "app",
                table: "Employees",
                column: "FullNameAr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullNameEn",
                schema: "app",
                table: "Employees",
                column: "FullNameEn");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IqamaNo",
                schema: "app",
                table: "Employees",
                column: "IqamaNo",
                unique: true,
                filter: "[IqamaNo] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsEmployee_EngagementType_Status",
                schema: "app",
                table: "Employees",
                columns: new[] { "IsEmployee", "EngagementType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OperatingCityId",
                schema: "app",
                table: "Employees",
                column: "OperatingCityId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OperationalWorkTypeId",
                schema: "app",
                table: "Employees",
                column: "OperationalWorkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees",
                column: "ProfilePhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SponsorId",
                schema: "app",
                table: "Employees",
                column: "SponsorId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_ActiveInternalSponsor",
                schema: "app",
                table: "Employees",
                sql: "[Status] <> 3 OR [EngagementType] <> 1 OR [SponsorId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_ActiveIqama",
                schema: "app",
                table: "Employees",
                sql: "[Status] <> 3 OR [IqamaNo] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_ContractRange",
                schema: "app",
                table: "Employees",
                sql: "[ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_IqamaNo",
                schema: "app",
                table: "Employees",
                sql: "[IqamaNo] IS NULL OR (LEN([IqamaNo]) = 10 AND [IqamaNo] NOT LIKE '%[^0-9]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_OutsideIsRider",
                schema: "app",
                table: "Employees",
                sql: "[EngagementType] <> 2 OR [IsEmployee] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_DocumentTypeId_DocumentNumber",
                schema: "app",
                table: "EmployeeDocuments",
                columns: new[] { "DocumentTypeId", "DocumentNumber" },
                unique: true,
                filter: "[DocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkHistory_EmployeeId_EffectiveDate_ChangeType",
                schema: "app",
                table: "EmployeeWorkHistory",
                columns: new[] { "EmployeeId", "EffectiveDate", "ChangeType" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_EmployeeDocuments_ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees",
                column: "ProfilePhotoDocumentId",
                principalSchema: "app",
                principalTable: "EmployeeDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_OperatingCities_OperatingCityId",
                schema: "app",
                table: "Employees",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_OperationalWorkTypes_OperationalWorkTypeId",
                schema: "app",
                table: "Employees",
                column: "OperationalWorkTypeId",
                principalSchema: "app",
                principalTable: "OperationalWorkTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Sponsors_SponsorId",
                schema: "app",
                table: "Employees",
                column: "SponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeStatusChangeRequests_EmployeeWorkHistory_ResultingWorkHistoryId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "ResultingWorkHistoryId",
                principalSchema: "app",
                principalTable: "EmployeeWorkHistory",
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
                name: "FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "RiderProfileId",
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_EmployeeDocuments_ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_OperatingCities_OperatingCityId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_OperationalWorkTypes_OperationalWorkTypeId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Sponsors_SponsorId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeStatusChangeRequests_EmployeeWorkHistory_ResultingWorkHistoryId",
                schema: "app",
                table: "EmployeeStatusChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderClientAssignments_RiderProfiles_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeWorkHistory",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_ExternalAccountId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_ClientPlatformId_Status",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Employees_FullNameAr",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_FullNameEn",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_IqamaNo",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_IsEmployee_EngagementType_Status",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OperatingCityId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OperationalWorkTypeId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SponsorId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_ActiveInternalSponsor",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_ActiveIqama",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_ContractRange",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_IqamaNo",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_OutsideIsRider",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_DocumentTypeId_DocumentNumber",
                schema: "app",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "TShirtSize",
                schema: "app",
                table: "RiderProfiles");

            migrationBuilder.DropColumn(
                name: "AlternateContactName",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AlternateContactPhone",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EngagementType",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IqamaNo",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsEmployee",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Nationality",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OperationalWorkTypeId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoDocumentId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ResidencyProfession",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SecondaryPhone",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                schema: "app",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "ResultingWorkHistoryId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                newName: "ResultingStatusPeriodId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeStatusChangeRequests_ResultingWorkHistoryId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                newName: "IX_EmployeeStatusChangeRequests_ResultingStatusPeriodId");

            migrationBuilder.RenameColumn(
                name: "WorkingForMeAs",
                schema: "app",
                table: "Employees",
                newName: "NormalizedNameEn");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "app",
                table: "Employees",
                newName: "CurrentStatus");

            migrationBuilder.RenameColumn(
                name: "MaritalStatus",
                schema: "app",
                table: "Employees",
                newName: "CurrentRelationshipType");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredCityId",
                schema: "app",
                table: "RiderProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RiderEndDate",
                schema: "app",
                table: "RiderProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RiderStartDate",
                schema: "app",
                table: "RiderProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "app",
                table: "RiderProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "BillingMode",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "LabelAr",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedExternalAccountId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalityCountryCode",
                schema: "app",
                table: "Employees",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedNameAr",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IssuingAuthority",
                schema: "app",
                table: "EmployeeDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuingCountryCode",
                schema: "app",
                table: "EmployeeDocuments",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedDocumentNumber",
                schema: "app",
                table: "EmployeeDocuments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformRiderAccounts_Id_ClientContractId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "Id", "ClientContractId" });

            migrationBuilder.CreateTable(
                name: "EmployeeJobTitlePeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatingCityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalWorkTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_EmployeeJobTitlePeriods_OperatingCities_OperatingCityId",
                        column: x => x.OperatingCityId,
                        principalSchema: "app",
                        principalTable: "OperatingCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeJobTitlePeriods_OperationalWorkTypes_OperationalWorkTypeId",
                        column: x => x.OperationalWorkTypeId,
                        principalSchema: "app",
                        principalTable: "OperationalWorkTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRelationshipPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelationshipType = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
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
                name: "EmployeeResidencyPermits",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployeeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PermitNumberCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PermitNumberLastFour = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: false),
                    PermitNumberLookupHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    PreviousPermitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResidencyProfessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
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
                name: "EmployeeStatusPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
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
                    AlternateContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlternateContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngagementNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EngagementReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                name: "SponsoredInternalDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentSponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DependentsCount = table.Column<int>(type: "int", nullable: true),
                    EducationDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    EmergencyContactRelationship = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LegacySponsorReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaritalStatus = table.Column<int>(type: "int", nullable: true),
                    ProbationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Profession = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProfilePhotoDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SecondaryPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HomeAddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HomeAddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HomeAddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HomeAddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HomeAddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
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
                        name: "FK_SponsoredInternalDetails_Sponsors_CurrentSponsorId",
                        column: x => x.CurrentSponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_RiderProfileId_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                columns: new[] { "RiderProfileId", "EmployeeId" });

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderProfiles_DateRange",
                schema: "app",
                table: "RiderProfiles",
                sql: "[RiderEndDate] IS NULL OR [RiderStartDate] IS NULL OR [RiderEndDate] >= [RiderStartDate]");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "ActualEmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "ActualEmployeeId", "EffectiveFrom" });

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
                name: "IX_EmployeeDocuments_DocumentTypeId_NormalizedDocumentNumber",
                schema: "app",
                table: "EmployeeDocuments",
                columns: new[] { "DocumentTypeId", "NormalizedDocumentNumber" },
                unique: true,
                filter: "[NormalizedDocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3");

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
                name: "IX_EmployeeJobTitlePeriods_OperatingCityId_OperationalWorkTypeId_JobTitleId_EffectiveTo",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                columns: new[] { "OperatingCityId", "OperationalWorkTypeId", "JobTitleId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeJobTitlePeriods_OperationalWorkTypeId",
                schema: "app",
                table: "EmployeeJobTitlePeriods",
                column: "OperationalWorkTypeId");

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
                name: "IX_SponsoredInternalDetails_CurrentSponsorId",
                schema: "app",
                table: "SponsoredInternalDetails",
                column: "CurrentSponsorId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeStatusChangeRequests_EmployeeStatusPeriods_ResultingStatusPeriodId",
                schema: "app",
                table: "EmployeeStatusChangeRequests",
                column: "ResultingStatusPeriodId",
                principalSchema: "app",
                principalTable: "EmployeeStatusPeriods",
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
                name: "FK_RiderProfiles_GlobalCities_PreferredCityId",
                schema: "app",
                table: "RiderProfiles",
                column: "PreferredCityId",
                principalSchema: "platform",
                principalTable: "GlobalCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderVehicleAssignments_Employees_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EmployeeId",
                principalSchema: "app",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                columns: new[] { "RiderProfileId", "EmployeeId" },
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumns: new[] { "Id", "EmployeeId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
