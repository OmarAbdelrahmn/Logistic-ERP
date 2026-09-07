using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddAccidentClaimWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                schema: "app",
                table: "VehicleAccidentAttachments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "app",
                table: "VehicleAccidentAttachments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                schema: "app",
                table: "VehicleAccidentAttachments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                schema: "app",
                table: "VehicleAccidentAttachments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransportedAtUtc",
                schema: "app",
                table: "VehicleAccidentAttachments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleAccidentCases",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAccidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    RiderFaultPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OtherPartiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NajmAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DamageAssessment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EstimatedRepairCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DamagePromissoryNoteAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpeningFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OpeningFeeAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpeningFeePaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedClaimType = table.Column<int>(type: "int", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ClaimSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClaimSubmissionAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IqamaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicenseVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistrationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettlementAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AssessmentReceiptAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InsuranceSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InsuranceDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InsuranceRespondedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InsuranceRejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentReceiptAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SupplierTransferDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TransferReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TransferReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RepairLocation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RepairContact = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RepairStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepairCompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReinspectionLocation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReinspectionAppointmentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalLossConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VehicleCollectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IncidentEndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastActionAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RefundStatus = table.Column<int>(type: "int", nullable: false),
                    RefundReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RefundRequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RefundReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_VehicleAccidentCases", x => x.Id);
                    table.CheckConstraint("CK_AccidentCase_Fault", "[RiderFaultPercentage] IS NULL OR [RiderFaultPercentage] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_AccidentCase_OpeningFee", "[OpeningFeeAmount] IS NULL OR [OpeningFeeAmount] = 2500");
                    table.CheckConstraint("CK_AccidentCase_Parties", "ISJSON([OtherPartiesJson]) = 1");
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_EmployeeDocumentVersions_IqamaVersionId",
                        column: x => x.IqamaVersionId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_EmployeeDocumentVersions_LicenseVersionId",
                        column: x => x.LicenseVersionId,
                        principalSchema: "app",
                        principalTable: "EmployeeDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_AssessmentReceiptAttachmentId",
                        column: x => x.AssessmentReceiptAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_ClaimSubmissionAttachmentId",
                        column: x => x.ClaimSubmissionAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_DamagePromissoryNoteAttachmentId",
                        column: x => x.DamagePromissoryNoteAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_NajmAttachmentId",
                        column: x => x.NajmAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_OpeningFeeAttachmentId",
                        column: x => x.OpeningFeeAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidentAttachments_PaymentReceiptAttachmentId",
                        column: x => x.PaymentReceiptAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAccidents_VehicleAccidentId",
                        column: x => x.VehicleAccidentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleAttachmentVersions_RegistrationVersionId",
                        column: x => x.RegistrationVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAttachmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentCases_VehicleSuppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "app",
                        principalTable: "VehicleSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAccidentInstallments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAccidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundEligibleAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceiptAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_VehicleAccidentInstallments", x => x.Id);
                    table.CheckConstraint("CK_AccidentInstallment_Amount", "[Amount] > 0 AND [RefundEligibleAmount] > 0 AND [RefundEligibleAmount] <= [Amount]");
                    table.CheckConstraint("CK_AccidentInstallment_Dates", "[PeriodTo] >= [PeriodFrom]");
                    table.ForeignKey(
                        name: "FK_VehicleAccidentInstallments_VehicleAccidentAttachments_ReceiptAttachmentId",
                        column: x => x.ReceiptAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidentInstallments_VehicleAccidents_VehicleAccidentId",
                        column: x => x.VehicleAccidentId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_AssessmentReceiptAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "AssessmentReceiptAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_ClaimSubmissionAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "ClaimSubmissionAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_DamagePromissoryNoteAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "DamagePromissoryNoteAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_IqamaVersionId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "IqamaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_IsDeleted",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_LicenseVersionId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "LicenseVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_NajmAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "NajmAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_OpeningFeeAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "OpeningFeeAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_PaymentReceiptAttachmentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "PaymentReceiptAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_RegistrationVersionId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "RegistrationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_Stage_InsuranceDueAtUtc",
                schema: "app",
                table: "VehicleAccidentCases",
                columns: new[] { "Stage", "InsuranceDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_Stage_SupplierTransferDueAtUtc",
                schema: "app",
                table: "VehicleAccidentCases",
                columns: new[] { "Stage", "SupplierTransferDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_SupplierId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentCases_VehicleAccidentId",
                schema: "app",
                table: "VehicleAccidentCases",
                column: "VehicleAccidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentInstallments_IsDeleted",
                schema: "app",
                table: "VehicleAccidentInstallments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentInstallments_ReceiptAttachmentId",
                schema: "app",
                table: "VehicleAccidentInstallments",
                column: "ReceiptAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentInstallments_VehicleAccidentId_ReceiptAttachmentId",
                schema: "app",
                table: "VehicleAccidentInstallments",
                columns: new[] { "VehicleAccidentId", "ReceiptAttachmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Existing reports retain their lifecycle; open reports begin with a Najm assessment.
            migrationBuilder.Sql("""
                INSERT INTO [app].[VehicleAccidentCases]
                    ([Id], [VehicleAccidentId], [Stage], [OtherPartiesJson], [RefundStatus], [IncidentEndedAtUtc], [CreatedAtUtc], [IsDeleted])
                SELECT NEWID(), [Id], CASE WHEN [Status] = 3 THEN 18 ELSE 1 END, N'[]', 1,
                    CASE WHEN [Status] = 3 THEN [ClosedAtUtc] ELSE NULL END, SYSUTCDATETIME(), 0
                FROM [app].[VehicleAccidents] WHERE [IsDeleted] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleAccidentCases",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAccidentInstallments",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "Amount",
                schema: "app",
                table: "VehicleAccidentAttachments");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "app",
                table: "VehicleAccidentAttachments");

            migrationBuilder.DropColumn(
                name: "FromLocation",
                schema: "app",
                table: "VehicleAccidentAttachments");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                schema: "app",
                table: "VehicleAccidentAttachments");

            migrationBuilder.DropColumn(
                name: "TransportedAtUtc",
                schema: "app",
                table: "VehicleAccidentAttachments");
        }
    }
}
