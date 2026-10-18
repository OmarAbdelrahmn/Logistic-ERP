using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class ResetJahezToAccountsOnlyV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Explicitly authorized reset: retain only Jahez master accounts and their owners.
            // Credentials, assignments and all Jahez operational/financial data are erased.
            migrationBuilder.Sql("""
                DECLARE @disable nvarchar(max)=N'', @erase nvarchar(max)=N'';
                SELECT @disable += N'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N' NOCHECK CONSTRAINT ALL;'
                FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez');
                EXEC sys.sp_executesql @disable;
                SELECT @erase += N'DELETE FROM '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N';'
                FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez');
                EXEC sys.sp_executesql @erase;
                DELETE e FROM app.RiderAssignmentEvents e JOIN app.RiderClientAssignments r ON e.RiderClientAssignmentId=r.Id
                    JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
                DELETE r FROM app.PlatformAccountRegistrations r JOIN platform.ClientPlatforms p ON r.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
                DELETE v FROM app.PlatformAccountCredentialVersions v JOIN app.PlatformRiderAccounts a ON v.PlatformRiderAccountId=a.Id
                    JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
                DELETE r FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id
                    JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
                UPDATE a SET UserName=NULL,Status=CASE WHEN a.Status=2 THEN 1 ELSE a.Status END,
                    StatusReason=CASE WHEN a.Status=2 THEN NULL ELSE a.StatusReason END,UpdatedAtUtc=SYSUTCDATETIME()
                    FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
                DELETE FROM app.Notifications WHERE EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_JahezAccountFee_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezAccountFee");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezAccountFee_JahezApprovalRequest_ApprovalRequestId",
                schema: "jahez",
                table: "JahezAccountFee");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezApprovalDecision_JahezApprovalRequest_RequestId",
                schema: "jahez",
                table: "JahezApprovalDecision");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezApprovalRequest_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezApprovalRequest_JahezOperation_OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezApprovalRequest_PlatformRiderAccounts_TargetAccountId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezCommissionPolicyPeriod_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezCommissionPolicyPeriod");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezCommissionPolicyPeriod_JahezApprovalRequest_ApprovalRequestId",
                schema: "jahez",
                table: "JahezCommissionPolicyPeriod");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezDailyDispatch_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezDailyDispatch");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezDailyDispatch_JahezImportBatch_BatchId",
                schema: "jahez",
                table: "JahezDailyDispatch");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezDailyDispatch_JahezImportRow_ImportRowId",
                schema: "jahez",
                table: "JahezDailyDispatch");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezEarningsStatement_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezEarningsStatement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezEarningsStatement_JahezEarningsStatement_SupersedesId",
                schema: "jahez",
                table: "JahezEarningsStatement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezImportBatch_JahezImportBatch_ReplacesBatchId",
                schema: "jahez",
                table: "JahezImportBatch");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezImportFile_JahezImportBatch_BatchId",
                schema: "jahez",
                table: "JahezImportFile");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezImportRow_JahezImportFile_FileId",
                schema: "jahez",
                table: "JahezImportRow");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezLedgerEntry_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezLedgerEntry_JahezLedgerEntry_ReversesEntryId",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezMoneyMovement_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezMoneyMovement_JahezCashboxEntry_LegacyCashboxEntryId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezMoneyMovement_JahezOperation_OperationId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezMoneyMovement_JahezTransferredDebt_TransferredDebtId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezMoneyMovement_RiderProfiles_RiderProfileId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezOperation_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezOperation");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezOperation_JahezApprovalRequest_ApprovedRequestId",
                schema: "jahez",
                table: "JahezOperation");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezReminderState_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezReminderState");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezTransaction_JahezAccountHandover_HandoverId",
                schema: "jahez",
                table: "JahezTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezTransaction_JahezImportBatch_BatchId",
                schema: "jahez",
                table: "JahezTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezTransaction_JahezImportRow_ImportRowId",
                schema: "jahez",
                table: "JahezTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezTransferredDebt_JahezOperation_OperationId",
                schema: "jahez",
                table: "JahezTransferredDebt");

            migrationBuilder.DropForeignKey(
                name: "FK_JahezTransferredDebt_RiderProfiles_RiderProfileId",
                schema: "jahez",
                table: "JahezTransferredDebt");

            migrationBuilder.DropTable(
                name: "JahezCashboxEntry",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezCashboxHandover",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezRiderSettlement",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezAccountHandover",
                schema: "jahez");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezTransferredDebt",
                schema: "jahez",
                table: "JahezTransferredDebt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezTransaction",
                schema: "jahez",
                table: "JahezTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezReminderState",
                schema: "jahez",
                table: "JahezReminderState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezOperation",
                schema: "jahez",
                table: "JahezOperation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezMoneyMovement",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropIndex(
                name: "IX_JahezMoneyMovement_LegacyCashboxEntryId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezLedgerEntry",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezImportRow",
                schema: "jahez",
                table: "JahezImportRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezImportFile",
                schema: "jahez",
                table: "JahezImportFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezImportBatch",
                schema: "jahez",
                table: "JahezImportBatch");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezEarningsStatement",
                schema: "jahez",
                table: "JahezEarningsStatement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezDailyDispatch",
                schema: "jahez",
                table: "JahezDailyDispatch");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezCommissionPolicyPeriod",
                schema: "jahez",
                table: "JahezCommissionPolicyPeriod");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezCommandReceipt",
                schema: "jahez",
                table: "JahezCommandReceipt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezApprovalRequest",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezApprovalDecision",
                schema: "jahez",
                table: "JahezApprovalDecision");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JahezAccountFee",
                schema: "jahez",
                table: "JahezAccountFee");

            migrationBuilder.DropColumn(
                name: "LegacyCashboxEntryId",
                schema: "jahez",
                table: "JahezMoneyMovement");

            migrationBuilder.DropColumn(
                name: "ExternalResetReference",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "IsV2",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.EnsureSchema(
                name: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezTransferredDebt",
                schema: "jahez",
                newName: "TransferredDebts",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezTransaction",
                schema: "jahez",
                newName: "PlatformTransactions",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezReminderState",
                schema: "jahez",
                newName: "SettlementReminders",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezOperation",
                schema: "jahez",
                newName: "Operations",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezMoneyMovement",
                schema: "jahez",
                newName: "MoneyMovements",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezLedgerEntry",
                schema: "jahez",
                newName: "AccountingEntries",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezImportRow",
                schema: "jahez",
                newName: "ImportRows",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezImportFile",
                schema: "jahez",
                newName: "ImportFiles",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezImportBatch",
                schema: "jahez",
                newName: "ImportBatches",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezEarningsStatement",
                schema: "jahez",
                newName: "EarningsStatements",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezDailyDispatch",
                schema: "jahez",
                newName: "DailyOrders",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezCommissionPolicyPeriod",
                schema: "jahez",
                newName: "CommissionPolicies",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezCommandReceipt",
                schema: "jahez",
                newName: "CommandReceipts",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezApprovalRequest",
                schema: "jahez",
                newName: "Requests",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezApprovalDecision",
                schema: "jahez",
                newName: "RequestDecisions",
                newSchema: "jahez_v2");

            migrationBuilder.RenameTable(
                name: "JahezAccountFee",
                schema: "jahez",
                newName: "AccountFees",
                newSchema: "jahez_v2");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransferredDebt_RiderProfileId_PaidAtUtc",
                schema: "jahez_v2",
                table: "TransferredDebts",
                newName: "IX_TransferredDebts_RiderProfileId_PaidAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransferredDebt_OperationId",
                schema: "jahez_v2",
                table: "TransferredDebts",
                newName: "IX_TransferredDebts_OperationId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransferredDebt_IsDeleted",
                schema: "jahez_v2",
                table: "TransferredDebts",
                newName: "IX_TransferredDebts_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransaction_ImportRowId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                newName: "IX_PlatformTransactions_ImportRowId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransaction_HandoverId_OccurredAtUtc",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                newName: "IX_PlatformTransactions_HandoverId_OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezTransaction_BatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                newName: "IX_PlatformTransactions_BatchId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezReminderState_IsDeleted",
                schema: "jahez_v2",
                table: "SettlementReminders",
                newName: "IX_SettlementReminders_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezReminderState_HandoverId_AnchorAtUtc",
                schema: "jahez_v2",
                table: "SettlementReminders",
                newName: "IX_SettlementReminders_HandoverId_AnchorAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezOperation_IsDeleted",
                schema: "jahez_v2",
                table: "Operations",
                newName: "IX_Operations_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezOperation_HandoverId_CreatedAtUtc",
                schema: "jahez_v2",
                table: "Operations",
                newName: "IX_Operations_HandoverId_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezOperation_HandoverId",
                schema: "jahez_v2",
                table: "Operations",
                newName: "IX_Operations_HandoverId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezOperation_ApprovedRequestId",
                schema: "jahez_v2",
                table: "Operations",
                newName: "IX_Operations_ApprovedRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezMoneyMovement_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                newName: "IX_MoneyMovements_TransferredDebtId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezMoneyMovement_RiderProfileId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                newName: "IX_MoneyMovements_RiderProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezMoneyMovement_OperationId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                newName: "IX_MoneyMovements_OperationId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezMoneyMovement_HandoverId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                newName: "IX_MoneyMovements_HandoverId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezMoneyMovement_Fund_OccurredAtUtc",
                schema: "jahez_v2",
                table: "MoneyMovements",
                newName: "IX_MoneyMovements_Fund_OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezLedgerEntry_SourceId_Bucket_Kind",
                schema: "jahez_v2",
                table: "AccountingEntries",
                newName: "IX_AccountingEntries_SourceId_Bucket_Kind");

            migrationBuilder.RenameIndex(
                name: "IX_JahezLedgerEntry_ReversesEntryId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                newName: "IX_AccountingEntries_ReversesEntryId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezLedgerEntry_HandoverId_Bucket_OccurredAtUtc",
                schema: "jahez_v2",
                table: "AccountingEntries",
                newName: "IX_AccountingEntries_HandoverId_Bucket_OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezImportRow_FileId_RowNumber",
                schema: "jahez_v2",
                table: "ImportRows",
                newName: "IX_ImportRows_FileId_RowNumber");

            migrationBuilder.RenameIndex(
                name: "IX_JahezImportFile_BatchId_ContentHash",
                schema: "jahez_v2",
                table: "ImportFiles",
                newName: "IX_ImportFiles_BatchId_ContentHash");

            migrationBuilder.RenameIndex(
                name: "IX_JahezImportBatch_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches",
                newName: "IX_ImportBatches_ReplacesBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezImportBatch_Kind_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches",
                newName: "IX_ImportBatches_Kind_ContentHash");

            migrationBuilder.RenameIndex(
                name: "IX_JahezImportBatch_IsDeleted",
                schema: "jahez_v2",
                table: "ImportBatches",
                newName: "IX_ImportBatches_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezEarningsStatement_SupersedesId",
                schema: "jahez_v2",
                table: "EarningsStatements",
                newName: "IX_EarningsStatements_SupersedesId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezEarningsStatement_HandoverId_FromDate_ToDate",
                schema: "jahez_v2",
                table: "EarningsStatements",
                newName: "IX_EarningsStatements_HandoverId_FromDate_ToDate");

            migrationBuilder.RenameIndex(
                name: "IX_JahezDailyDispatch_ImportRowId_HandoverId",
                schema: "jahez_v2",
                table: "DailyOrders",
                newName: "IX_DailyOrders_ImportRowId_HandoverId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezDailyDispatch_HandoverId_Date",
                schema: "jahez_v2",
                table: "DailyOrders",
                newName: "IX_DailyOrders_HandoverId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_JahezDailyDispatch_BatchId",
                schema: "jahez_v2",
                table: "DailyOrders",
                newName: "IX_DailyOrders_BatchId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezCommissionPolicyPeriod_HandoverId_FromDate_ToDate",
                schema: "jahez_v2",
                table: "CommissionPolicies",
                newName: "IX_CommissionPolicies_HandoverId_FromDate_ToDate");

            migrationBuilder.RenameIndex(
                name: "IX_JahezCommissionPolicyPeriod_ApprovalRequestId",
                schema: "jahez_v2",
                table: "CommissionPolicies",
                newName: "IX_CommissionPolicies_ApprovalRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezCommandReceipt_ActorUserId_Operation_CommandKey",
                schema: "jahez_v2",
                table: "CommandReceipts",
                newName: "IX_CommandReceipts_ActorUserId_Operation_CommandKey");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalRequest_TargetAccountId",
                schema: "jahez_v2",
                table: "Requests",
                newName: "IX_Requests_TargetAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalRequest_Status_CreatedAtUtc",
                schema: "jahez_v2",
                table: "Requests",
                newName: "IX_Requests_Status_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalRequest_OperationId",
                schema: "jahez_v2",
                table: "Requests",
                newName: "IX_Requests_OperationId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalRequest_IsDeleted",
                schema: "jahez_v2",
                table: "Requests",
                newName: "IX_Requests_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalRequest_HandoverId",
                schema: "jahez_v2",
                table: "Requests",
                newName: "IX_Requests_HandoverId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezApprovalDecision_RequestId",
                schema: "jahez_v2",
                table: "RequestDecisions",
                newName: "IX_RequestDecisions_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezAccountFee_IsDeleted",
                schema: "jahez_v2",
                table: "AccountFees",
                newName: "IX_AccountFees_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_JahezAccountFee_HandoverId",
                schema: "jahez_v2",
                table: "AccountFees",
                newName: "IX_AccountFees_HandoverId");

            migrationBuilder.RenameIndex(
                name: "IX_JahezAccountFee_ApprovalRequestId",
                schema: "jahez_v2",
                table: "AccountFees",
                newName: "IX_AccountFees_ApprovalRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransferredDebts",
                schema: "jahez_v2",
                table: "TransferredDebts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlatformTransactions",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SettlementReminders",
                schema: "jahez_v2",
                table: "SettlementReminders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Operations",
                schema: "jahez_v2",
                table: "Operations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MoneyMovements",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountingEntries",
                schema: "jahez_v2",
                table: "AccountingEntries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportRows",
                schema: "jahez_v2",
                table: "ImportRows",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportFiles",
                schema: "jahez_v2",
                table: "ImportFiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportBatches",
                schema: "jahez_v2",
                table: "ImportBatches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EarningsStatements",
                schema: "jahez_v2",
                table: "EarningsStatements",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DailyOrders",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommissionPolicies",
                schema: "jahez_v2",
                table: "CommissionPolicies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommandReceipts",
                schema: "jahez_v2",
                table: "CommandReceipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Requests",
                schema: "jahez_v2",
                table: "Requests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestDecisions",
                schema: "jahez_v2",
                table: "RequestDecisions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountFees",
                schema: "jahez_v2",
                table: "AccountFees",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AccountSessions",
                schema: "jahez_v2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CommissionStartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CommissionPostedThrough = table.Column<DateOnly>(type: "date", nullable: true),
                    LastSettlementPaymentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSessions", x => x.Id);
                    table.CheckConstraint("CK_JahezHandover_Range", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_AccountSessions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountSessions_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountSessions_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_IsDeleted",
                schema: "jahez_v2",
                table: "AccountSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_PlatformRiderAccountId",
                schema: "jahez_v2",
                table: "AccountSessions",
                column: "PlatformRiderAccountId",
                unique: true,
                filter: "[EndedAtUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_RiderClientAssignmentId",
                schema: "jahez_v2",
                table: "AccountSessions",
                column: "RiderClientAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_RiderProfileId_StartedAtUtc",
                schema: "jahez_v2",
                table: "AccountSessions",
                columns: new[] { "RiderProfileId", "StartedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AccountFees_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "AccountFees",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountFees_Requests_ApprovalRequestId",
                schema: "jahez_v2",
                table: "AccountFees",
                column: "ApprovalRequestId",
                principalSchema: "jahez_v2",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingEntries_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingEntries_AccountingEntries_ReversesEntryId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                column: "ReversesEntryId",
                principalSchema: "jahez_v2",
                principalTable: "AccountingEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionPolicies_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "CommissionPolicies",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionPolicies_Requests_ApprovalRequestId",
                schema: "jahez_v2",
                table: "CommissionPolicies",
                column: "ApprovalRequestId",
                principalSchema: "jahez_v2",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOrders_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOrders_ImportBatches_BatchId",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "BatchId",
                principalSchema: "jahez_v2",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOrders_ImportRows_ImportRowId",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "ImportRowId",
                principalSchema: "jahez_v2",
                principalTable: "ImportRows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EarningsStatements_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "EarningsStatements",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EarningsStatements_EarningsStatements_SupersedesId",
                schema: "jahez_v2",
                table: "EarningsStatements",
                column: "SupersedesId",
                principalSchema: "jahez_v2",
                principalTable: "EarningsStatements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportBatches_ImportBatches_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches",
                column: "ReplacesBatchId",
                principalSchema: "jahez_v2",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportFiles_ImportBatches_BatchId",
                schema: "jahez_v2",
                table: "ImportFiles",
                column: "BatchId",
                principalSchema: "jahez_v2",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportRows_ImportFiles_FileId",
                schema: "jahez_v2",
                table: "ImportRows",
                column: "FileId",
                principalSchema: "jahez_v2",
                principalTable: "ImportFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_Operations_OperationId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "OperationId",
                principalSchema: "jahez_v2",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_RiderProfiles_RiderProfileId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "RiderProfileId",
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MoneyMovements_TransferredDebts_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "TransferredDebtId",
                principalSchema: "jahez_v2",
                principalTable: "TransferredDebts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "Operations",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_Requests_ApprovedRequestId",
                schema: "jahez_v2",
                table: "Operations",
                column: "ApprovedRequestId",
                principalSchema: "jahez_v2",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformTransactions_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformTransactions_ImportBatches_BatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "BatchId",
                principalSchema: "jahez_v2",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformTransactions_ImportRows_ImportRowId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "ImportRowId",
                principalSchema: "jahez_v2",
                principalTable: "ImportRows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDecisions_Requests_RequestId",
                schema: "jahez_v2",
                table: "RequestDecisions",
                column: "RequestId",
                principalSchema: "jahez_v2",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "Requests",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Operations_OperationId",
                schema: "jahez_v2",
                table: "Requests",
                column: "OperationId",
                principalSchema: "jahez_v2",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_PlatformRiderAccounts_TargetAccountId",
                schema: "jahez_v2",
                table: "Requests",
                column: "TargetAccountId",
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SettlementReminders_AccountSessions_HandoverId",
                schema: "jahez_v2",
                table: "SettlementReminders",
                column: "HandoverId",
                principalSchema: "jahez_v2",
                principalTable: "AccountSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferredDebts_Operations_OperationId",
                schema: "jahez_v2",
                table: "TransferredDebts",
                column: "OperationId",
                principalSchema: "jahez_v2",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferredDebts_RiderProfiles_RiderProfileId",
                schema: "jahez_v2",
                table: "TransferredDebts",
                column: "RiderProfileId",
                principalSchema: "app",
                principalTable: "RiderProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                DECLARE @validate nvarchar(max)=N'';
                SELECT @validate += N'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N' WITH CHECK CHECK CONSTRAINT ALL;'
                FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v2');
                EXEC sys.sp_executesql @validate;
                IF EXISTS(SELECT 1 FROM sys.objects WHERE schema_id=SCHEMA_ID(N'jahez'))
                    THROW 51000,'Old Jahez schema still contains objects.',1;
                IF SCHEMA_ID(N'jahez') IS NOT NULL EXEC(N'DROP SCHEMA [jahez]');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("The authorized Jahez account-only reset cannot restore deleted data. Use its secured backup for recovery.");
    }
}
