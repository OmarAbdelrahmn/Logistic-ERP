/**
 * Jahez HTTP JSON contracts, checked against the repository on 2026-10-11.
 * Documentation / integration reference; this file is not an application client.
 * Amounts are JSON numbers in SAR. Preserve imported six-place precision.
 * Requests with `?` properties may omit those properties; null is explicit null.
 * Response fields are present in the DTO; nullable display data can be null.
 */
export type UUID = string;
export type DateOnly = string; // YYYY-MM-DD; do not convert through browser UTC.
export type DateTimeOffset = string; // ISO 8601 instant with timezone offset.

export enum JahezDebtStatus {
  Active = 1,
  Closed = 2,
  TransferredToRider = 3,
  Frozen = 4,
  AccountCleared = 5,
}
export enum JahezDebtPaymentStatus { Outstanding = 1, Paid = 2 }
export enum JahezApprovalKind {
  FeeException = 1, FreeSwitch = 2, PercentageCommission = 3, ResetAccount = 4,
}
export enum JahezApprovalStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
export enum JahezLedgerBucket { AccountFees = 1, PlatformDebt = 2, Commission = 3 }
export enum JahezLedgerKind {
  Charge = 1, Payment = 2, Adjustment = 3, Transfer = 4, OpeningBalance = 5, StatusChange = 6,
}
export enum JahezImportKind { Transactions = 1, DailyDispatches = 2 }
export enum JahezCashboxSection { AccountFees = 1, Settlements = 2 }
export enum JahezCashboxHandoverStatus { Pending = 1, AccountantConfirmed = 2, Approved = 3, Rejected = 4 }

export interface JahezPage<T> { items: T[]; page: number; pageSize: number }
export interface JahezPaginationQuery { page?: number; pageSize?: number }
export interface JahezHandoversQuery extends JahezPaginationQuery { accountId?: UUID; riderId?: UUID }
export interface JahezBalanceQuery { through: DateOnly }
export interface JahezDebtsQuery extends JahezPaginationQuery { riderId?: UUID; overdueOnly?: boolean }
export interface JahezLedgerQuery extends JahezPaginationQuery { riderId?: UUID; handoverId?: UUID }
export interface JahezRequestsQuery extends JahezPaginationQuery { status?: JahezApprovalStatus }
export interface JahezDispatchesQuery extends JahezPaginationQuery { from: DateOnly; to: DateOnly; riderId?: UUID }
export interface JahezCashboxEntriesQuery extends JahezPaginationQuery { cashboxHandoverId?: UUID }
export interface JahezAccountDisplay { id: UUID; code: string; externalAccountId: string }
export interface JahezNamedResponse {
  account: JahezAccountDisplay | null;
  ownerRiderProfileId: UUID | null;
  ownerEmployeeId: UUID | null;
  ownerRiderNameAr: string | null;
  ownerRiderNameEn: string | null;
  actualRiderProfileId: UUID | null;
  actualEmployeeId: UUID | null;
  actualRiderNameAr: string | null;
  actualRiderNameEn: string | null;
  createdByUserNameAr: string | null;
  createdByUserNameEn: string | null;
  updatedByUserNameAr: string | null;
  updatedByUserNameEn: string | null;
  deletedByUserNameAr: string | null;
  deletedByUserNameEn: string | null;
  requestedByUserNameAr: string | null;
  requestedByUserNameEn: string | null;
  collectedByUserNameAr: string | null;
  collectedByUserNameEn: string | null;
  actorUserNameAr: string | null;
  actorUserNameEn: string | null;
  uploadedByUserNameAr: string | null;
  uploadedByUserNameEn: string | null;
  accountantUserNameAr: string | null;
  accountantUserNameEn: string | null;
  approvedByUserNameAr: string | null;
  approvedByUserNameEn: string | null;
  targetAccount: JahezAccountDisplay | null;
}
export interface JahezHistoryMetadata {
  id: UUID;
  createdAtUtc: DateTimeOffset;
  createdByUserId: UUID | null;
}
export interface JahezAuditableMetadata extends JahezHistoryMetadata {
  updatedAtUtc: DateTimeOffset | null;
  updatedByUserId: UUID | null;
  rowVersion: string; // Base64 JSON encoding of byte[]. These Jahez POSTs do not accept it.
  isDeleted: boolean;
  deletedAtUtc: DateTimeOffset | null;
  deletedByUserId: UUID | null;
  deletionReason: string | null;
}

export interface JahezHandoverRequest {
  accountId: UUID;
  riderProfileId: UUID;
  effectiveAtUtc: DateTimeOffset;
  reason: string;
  initialFeePayment: number;
  feeApprovalRequestId?: UUID | null; // Must be null/omitted; non-null is rejected.
}
export interface JahezLegacyAdoptionRequest {
  assignmentId: UUID;
  financialStartOn: DateOnly;
  openingDebt: number;
  openingFees: number;
  openingCommission: number;
  reason: string;
}
export interface JahezCloseRequest { effectiveAtUtc: DateTimeOffset; reason: string }
export interface JahezDebtFreezeRequest { effectiveAtUtc: DateTimeOffset; reason: string }
export interface JahezDebtTransferRequest {
  effectiveAtUtc: DateTimeOffset; reason: string; externalClearanceReference: string;
}
export interface JahezAccountClearanceRequest { reason: string; externalClearanceReference: string }
export interface JahezDebtPaidRequest { reason: string }
export interface JahezApprovalCreateRequest {
  handoverId: UUID;
  kind: JahezApprovalKind;
  reason: string;
  targetAccountId?: UUID | null;
  waiverAmount?: number;
  fromDate?: DateOnly | null;
  toDate?: DateOnly | null;
  effectiveAtUtc?: DateTimeOffset | null;
  externalResetReference?: string | null;
}
export interface JahezDecisionRequest { approve: boolean; reason: string }
export interface JahezCancelRequest { reason: string }
export interface JahezEarningsRequest {
  handoverId: UUID;
  fromDate: DateOnly;
  toDate: DateOnly;
  totalDeliveryPrice: number;
  totalPenalties: number;
  totalCashAmount: number;
  totalDriverDebit: number;
  totalServiceDeduction: number;
  totalDriverCredit: number;
  totalBonuses: number;
  totalTips: number;
  totalFreeOrders: number; // Monetary amount, not an order count.
  reason: string;
  supersedesId?: UUID | null;
}
export interface JahezPaymentRequest {
  handoverId: UUID;
  throughDate: DateOnly;
  feePayment: number;
  debtPayment: number;
  commissionPayment: number;
  countsAsSettlement: boolean;
  reason: string;
}
export interface JahezLedgerAdjustmentRequest {
  handoverId: UUID;
  bucket: JahezLedgerBucket;
  amount: number;
  reason: string;
  reversesEntryId?: UUID | null;
}
export interface JahezCashboxCreateRequest { businessDate: DateOnly; reason: string }
export interface JahezAccountantConfirmRequest { feeAmount: number; settlementAmount: number; reason: string }
export interface JahezDispatchAllocation { rowId: UUID; handoverId: UUID; count: number; reason: string }
export interface JahezImportCommitRequest { allocations?: JahezDispatchAllocation[] | null }
// POST /imports uses FormData, not a JSON request. Append kind and repeated files.
export interface JahezImportMultipartInput {
  kind: JahezImportKind;
  files: File[];
  replacesBatchId?: UUID | null;
  correctionReason?: string | null;
}

export interface JahezHandoverResponse extends JahezNamedResponse {
  id: UUID;
  accountId: UUID;
  externalAccountId: string;
  riderProfileId: UUID;
  assignmentId: UUID;
  startedAtUtc: DateTimeOffset;
  endedAtUtc: DateTimeOffset | null;
  commissionStartsOn: DateOnly;
  commissionPostedThrough: DateOnly | null;
  lastSettlementPaymentAtUtc: DateTimeOffset | null;
  isLegacy: boolean;
  debtTransferred: boolean;
  debtStatus: JahezDebtStatus;
  frozenAtUtc: DateTimeOffset | null;
  accountClearedAtUtc: DateTimeOffset | null;
  debtPaidAtUtc: DateTimeOffset | null;
  externalClearanceReference: string | null;
}
export interface JahezBalanceResponse extends JahezNamedResponse {
  handoverId: UUID;
  accountId: UUID;
  externalAccountId: string;
  riderProfileId: UUID;
  throughDate: DateOnly;
  fees: number;
  platformDebt: number;
  postedCommission: number;
  unpostedCommission: number;
  totalReceivable: number;
  commissionComplete: boolean;
  problems: string[];
  reminderAnchorAtUtc: DateTimeOffset;
  daysSinceSettlementPayment: number;
  isOverdue: boolean;
  debtTransferred: boolean;
  latestTransactionAtUtc: DateTimeOffset | null;
  debtStatus: JahezDebtStatus;
  paymentStatus: JahezDebtPaymentStatus;
  frozenAtUtc: DateTimeOffset | null;
  accountClearedAtUtc: DateTimeOffset | null;
  debtPaidAtUtc: DateTimeOffset | null;
  outstandingAmount: number;
  accountReceivable: number;
  isAccountFrozen: boolean;
  isAccountReady: boolean;
}
export interface JahezRiderDebtSummary extends JahezNamedResponse {
  riderProfileId: UUID;
  totalReceivable: number;
  outstandingAmount: number;
  fees: number;
  platformDebt: number;
  commission: number;
  frozenAccountCount: number;
  paymentStatus: JahezDebtPaymentStatus;
}
export interface JahezAccountFeeResponse extends JahezNamedResponse, JahezAuditableMetadata {
  handoverId: UUID; amount: number; waivedAmount: number; approvalRequestId: UUID | null;
}
export interface JahezApprovalRequestResponse extends JahezNamedResponse, JahezAuditableMetadata {
  handoverId: UUID;
  kind: JahezApprovalKind;
  status: JahezApprovalStatus;
  requestedByUserId: UUID;
  targetAccountId: UUID | null;
  waiverAmount: number;
  fromDate: DateOnly | null;
  toDate: DateOnly | null;
  effectiveAtUtc: DateTimeOffset | null;
  reason: string;
  externalResetReference: string | null;
}
export interface JahezApprovalDecisionResponse extends JahezNamedResponse, JahezHistoryMetadata {
  requestId: UUID; actorUserId: UUID; status: JahezApprovalStatus; decidedAtUtc: DateTimeOffset; reason: string;
}
export interface JahezApprovalDetailsResponse {
  request: JahezApprovalRequestResponse; decisions: JahezApprovalDecisionResponse[];
}
export interface JahezCommissionPolicyPeriodResponse extends JahezNamedResponse, JahezHistoryMetadata {
  handoverId: UUID; approvalRequestId: UUID; fromDate: DateOnly; toDate: DateOnly; rate: number;
}
export interface JahezEarningsStatementResponse extends JahezNamedResponse, JahezHistoryMetadata {
  handoverId: UUID;
  fromDate: DateOnly;
  toDate: DateOnly;
  totalDeliveryPrice: number;
  totalPenalties: number;
  totalCashAmount: number;
  totalDriverDebit: number;
  totalServiceDeduction: number;
  totalDriverCredit: number;
  totalBonuses: number;
  totalTips: number;
  totalFreeOrders: number;
  supersedesId: UUID | null;
  reason: string;
}
export interface JahezRiderSettlementResponse extends JahezNamedResponse, JahezHistoryMetadata {
  handoverId: UUID; throughDate: DateOnly; recordedAtUtc: DateTimeOffset; collectedByUserId: UUID;
  feePayment: number; debtPayment: number; commissionPayment: number; countsAsSettlement: boolean; reason: string;
}
export interface JahezLedgerEntryResponse extends JahezNamedResponse, JahezHistoryMetadata {
  handoverId: UUID; bucket: JahezLedgerBucket; kind: JahezLedgerKind; amount: number; sourceId: UUID;
  reversesEntryId: UUID | null; occurredAtUtc: DateTimeOffset; reason: string;
  fromDate: DateOnly | null; throughDate: DateOnly | null; calculationJson: string | null;
}
export interface JahezImportBatchResponse extends JahezNamedResponse, JahezAuditableMetadata {
  kind: JahezImportKind; contentHash: string; uploadedByUserId: UUID;
  committedAtUtc: DateTimeOffset | null; replacesBatchId: UUID | null; correctionReason: string | null;
}
export interface JahezImportIssue {
  rowId: UUID; fileName: string; rowNumber: number; code: string; description: string;
}
export interface JahezImportRowPreview extends JahezNamedResponse {
  rowId: UUID; fileName: string; rowNumber: number; driverId: string; occurredAtUtc: DateTimeOffset;
  accountId: UUID | null; handoverId: UUID | null; riderProfileId: UUID | null;
  netAmount: number; dispatches: number | null;
}
export interface JahezImportAccountSummary extends JahezNamedResponse {
  accountId: UUID | null; driverId: string; fromDate: DateOnly; toDate: DateOnly; validRowCount: number;
  netAmount: number; platformDebtChange: number; dispatches: number | null; hasIssues: boolean;
}
export interface JahezImportFileMetadata { id: UUID; fileName: string }
export interface JahezImportPreview {
  batchId: UUID; kind: JahezImportKind; committed: boolean; rows: JahezImportRowPreview[];
  issues: JahezImportIssue[]; files: JahezImportFileMetadata[] | null; accounts: JahezImportAccountSummary[] | null;
}
// GET /import-files/{id} returns a binary Blob, not JahezImportFileResponse JSON.
export interface JahezDispatchReportRow extends JahezNamedResponse {
  accountId: UUID; externalAccountId: string; riderProfileId: UUID; handoverId: UUID; date: DateOnly; count: number;
}
export interface JahezCashboxBalance {
  fees: number; settlements: number; reservedFees: number; reservedSettlements: number;
  availableFees: number; availableSettlements: number;
}
export interface JahezCashboxEntryResponse extends JahezNamedResponse, JahezAuditableMetadata {
  settlementId: UUID; handoverId: UUID; section: JahezCashboxSection; amount: number;
  collectedByUserId: UUID; receivedAtUtc: DateTimeOffset; cashboxHandoverId: UUID | null;
}
export interface JahezCashboxHandoverResponse extends JahezNamedResponse, JahezAuditableMetadata {
  businessDate: DateOnly; status: JahezCashboxHandoverStatus; feeAmount: number; settlementAmount: number;
  accountantFeeAmount: number | null; accountantSettlementAmount: number | null; requestedByUserId: UUID;
  accountantUserId: UUID | null; approvedByUserId: UUID | null; confirmedAtUtc: DateTimeOffset | null;
  decidedAtUtc: DateTimeOffset | null; reason: string; confirmationReason: string | null; decisionReason: string | null;
}
export interface JahezProblemDetails {
  type: string; title: string; status: number; detail: string; instance: string;
  errorCode: string; correlationId: string; field?: string; [extension: string]: unknown;
}

// Shared account API uses string enum names, unlike numeric Jahez enums.
export type PlatformAccountStatus = "Available" | "Assigned" | "Suspended" | "Retired" | "Archived";
export interface SimplePlatformAccountUpsertRequest {
  platformId: UUID; operatingCityId: UUID; sponsorId: UUID; ownerRiderProfileId: UUID;
  code: string; externalAccountId: string; userName: string | null;
  paymentModel: "PayPerOrder" | "Salary"; status: PlatformAccountStatus;
  statusReason: string | null; acquisitionDate: DateOnly | null; startDate: DateOnly | null;
  endDate: DateOnly | null; notes: string | null; archiveReason: string | null; rowVersion: string | null;
}
export interface SimplePlatformAssignmentResponse {
  id: UUID; accountId: UUID; paymentModel: string; actualRiderProfileId: UUID; actualEmployeeId: UUID;
  actualRiderNameAr: string; actualRiderNameEn: string | null; effectiveFrom: DateOnly;
  effectiveTo: DateOnly | null; status: string; startReason: string | null; endReason: string | null;
  wasBackdated: boolean; backdatedReason: string | null; assignedByUserId: UUID; endedByUserId: UUID | null; rowVersion: string;
}
export interface SimplePlatformAccountResponse {
  id: UUID; platformId: UUID; platformCode: string; platformNameAr: string; platformNameEn: string;
  operatingCityId: UUID; operatingCityNameAr: string; operatingCityNameEn: string;
  sponsorId: UUID | null; sponsorNameAr: string | null; sponsorNameEn: string | null;
  ownerRiderProfileId: UUID | null; ownerEmployeeId: UUID | null; ownerRiderNameAr: string | null; ownerRiderNameEn: string | null;
  code: string; externalAccountId: string; userName: string | null; paymentModel: string;
  status: PlatformAccountStatus; statusReason: string | null; acquisitionDate: DateOnly | null;
  startDate: DateOnly | null; endDate: DateOnly | null; notes: string | null;
  currentAssignment: SimplePlatformAssignmentResponse | null; rowVersion: string;
}
