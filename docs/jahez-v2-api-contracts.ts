// Jahez V2. JSON enum values are numeric; dates are ISO strings; rowversions are Base64.
export type Id = string;
export type DateOnly = string;
export type Instant = string;
export interface ProfitReport { fromDate: DateOnly; toDate: DateOnly;
  accountOpeningFeesTotal: number; settlementCommissionTotal: number; totalProfit: number }
export interface RiderPerformanceFilter { startDate: DateOnly; endDate: DateOnly; riderId?: Id;
  accountId?: Id; page?: number; pageSize?: number }
export interface JahezPerformanceSummary { periodDays: number; daysWithData: number; daysWithOrders: number;
  zeroOrderDays: number; missingDays: number; orders: number; averageOrdersPerDayWithData: number | null;
  maxOrdersPerDay: number | null }
export interface JahezPerformanceDay { date: DateOnly; orders: number | null }
export interface JahezAccountPerformanceReport { accountId: Id; externalAccountId: string;
  summary: JahezPerformanceSummary; daily: JahezPerformanceDay[] }
export interface JahezRiderPerformanceReport { riderProfileId: Id; riderName: string; riderIqamaNo: string | null;
  selectedSummary: JahezPerformanceSummary; daily: JahezPerformanceDay[]; accounts: JahezAccountPerformanceReport[] }
export interface JahezRidersPerformanceReport extends Page<JahezRiderPerformanceReport> { startDate: DateOnly;
  endDate: DateOnly; riderId: Id | null; accountId: Id | null; totalCount: number }
export enum OperationMode { Settle = 1, SettleAndWithdraw = 2, Withdraw = 3 }
export enum OperationStatus { Ready = 1, UnderReview = 2, Executed = 3 }
export enum PaymentStatus { None = 1, Paid = 2, Transferred = 3, Deferred = 4 }
export enum Direction { None = 0, Receipt = 1, Disbursement = 2 }
export enum Fund { Cashbox = 1, PetroApp = 2, Bank = 3 }
export enum Purpose { Settlement = 1, Fuel = 2, AccountFee = 3, TransferredDebtRepayment = 4, AccountFeeRefund = 5 }
export enum ApprovalKind { FeeException = 1, AccountSwitch = 2, PercentageCommission = 3, SettlementTransfer = 4, Adjustment = 5 }
export enum ApprovalStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
export enum AdjustmentTarget { Settlement = 1, AdministrativeDebt = 2 }
export interface Page<T> { items: T[]; page: number; pageSize: number }
export interface Audit { id: Id; createdAtUtc: Instant; createdByUserId: Id | null; updatedAtUtc: Instant | null;
  updatedByUserId: Id | null; rowVersion: string; isDeleted: boolean; deletedAtUtc: Instant | null;
  deletedByUserId: Id | null; deletionReason: string | null }
export interface Person { employeeId: Id; riderProfileId: Id | null; nameAr: string; nameEn: string | null;
  iqamaNo: string | null; mobile: string | null; sponsorId: Id | null }
export interface Account { accountId: Id; code: string; externalAccountId: string; userName: string | null;
  vehicleType: 'Motorcycle' | 'Car' | 'Van' | 'Truck' | 'Other' | null;
  operatingCityId: Id; cityNameAr: string; sponsorId: Id | null; sponsorNameAr: string | null;
  status: 'Available' | 'Assigned' | 'Suspended' | 'Retired' | 'Archived'; statusReason: string | null;
  isAssigned: boolean; hasPassword: boolean; owner: Person | null; actualRider: Person | null;
  currentAssignmentId: Id | null; currentHandoverId: Id | null; acquisitionDate: DateOnly | null;
  startDate: DateOnly | null; endDate: DateOnly | null; ownershipNotes: string | null; operationalNotes: string | null;
  yesterdayOrders: number | null; lastOrderDate: DateOnly | null; lastOrderCount: number | null; rowVersion: string }
export interface AccountsPage extends Page<Account> { totalCount: number }
export interface SponsorStatistics { sponsorId: Id; nameAr: string; accounts: number; assignedAccounts: number;
  actualRiders: number; sponsoredRiders: number; sponsoredRidersUsingOtherSponsorsAccounts: number }
export interface Statistics { yesterday: DateOnly; totalAccounts: number; activeAccounts: number;
  unassignedAccounts: number; blockedAccounts: number; missingPasswordAccounts: number; yesterdayOrders: number | null;
  accountsWithYesterdayData: number; sponsors: SponsorStatistics[] }
export interface PlatformAccountDayUsageInput { ordersCount?: number | null; workingHours?: number | null;
  collectedAmount?: number | null; deductionsAmount?: number | null; totalValidOffers?: number | null;
  declinedOffers?: number | null; missedOffers?: number | null }
export interface FeeRefundPreviewRequest { handoverId: Id; waiveCommission: boolean }
export interface FeeRefundCreate extends FeeRefundPreviewRequest { reference: string; reason: string;
  dayUsage?: PlatformAccountDayUsageInput | null }
export interface FeeRefundPreview { handoverId: Id; expiresAtUtc: Instant; grossFeeRefund: number;
  cancelledUnpaidFee: number; calculatedCommission: number; waiveCommission: boolean;
  deductedCommission: number; refundAmount: number; calculationJson: string }
export interface FeeRefund { id: Id; handoverId: Id; accountFeeId: Id; moneyMovementId: Id;
  grossFeeRefund: number; cancelledUnpaidFee: number; waiveCommission: boolean; calculatedCommission: number;
  deductedCommission: number; refundAmount: number; calculationJson: string; actorUserId: Id;
  occurredAtUtc: Instant; reference: string; reason: string; createdAtUtc: Instant; createdByUserId: Id | null }
export interface OperationCreate { handoverId: Id; mode: OperationMode; throughDate: DateOnly; effectiveAtUtc: Instant;
  net: number; reason: string; fund?: Fund | null; purpose?: Purpose; transferToRider?: boolean;
  paymentReference?: string | null; dayUsage?: PlatformAccountDayUsageInput | null }
export interface Preview { net: number; commission: number; remaining: number; direction: Direction;
  sponsorId: Id | null; sponsorIdentifier: string | null; sponsorNameAr: string | null;
  allowedFunds: Fund[]; withdrawsAccount: boolean; commissionComplete: boolean; problems: string[]; calculationJson: string }
export interface Operation extends Audit { expectedSponsorId: Id | null; handoverId: Id; mode: OperationMode; status: OperationStatus;
  paymentStatus: PaymentStatus; throughDate: DateOnly; commissionStartDate: DateOnly | null; effectiveAtUtc: Instant; net: number; commission: number;
  calculatedCommission: number; remaining: number; calculationJson: string; dayUsageJson: string | null; fund: Fund | null; purpose: Purpose;
  transferToRider: boolean; paymentReference: string | null; reason: string; approvedRequestId: Id | null;
  executedAtUtc: Instant | null; executedByUserId: Id | null }
export interface OperationUpdate { values: OperationCreate; rowVersion: string }
export interface OperationPaymentMethodUpdate { fund: Fund.Cashbox | Fund.Bank; paymentReference: string;
  reason: string; rowVersion: string }
export interface OperationNetUpdate { net: number; reason: string; rowVersion: string;
  fund?: Fund | null; paymentReference?: string | null }
export interface OperationCommissionStartUpdate { startDate: DateOnly; reason: string; rowVersion: string;
  fund?: Fund | null; paymentReference?: string | null }
export interface RequestCreate { handoverId: Id; kind: ApprovalKind; reason: string; operationId?: Id | null;
  targetAccountId?: Id | null; waiverAmount?: number; fromDate?: DateOnly | null; toDate?: DateOnly | null;
  effectiveAtUtc?: Instant | null; adjustmentTarget?: AdjustmentTarget | null; proposedNet?: number | null;
  proposedCommission?: number | null; debtAdjustment?: number | null }
export interface ApprovalRequest extends Audit { handoverId: Id; kind: ApprovalKind; status: ApprovalStatus;
  requestedByUserId: Id; operationId: Id | null; targetAccountId: Id | null; waiverAmount: number;
  fromDate: DateOnly | null; toDate: DateOnly | null; effectiveAtUtc: Instant | null; reason: string;
  adjustmentTarget: AdjustmentTarget | null; proposedNet: number | null; proposedCommission: number | null;
  debtAdjustment: number | null }
export interface DecisionRequest { approve: boolean; reason: string; approvedNet?: number | null;
  approvedCommission?: number | null; approvedDebtAdjustment?: number | null; useOriginalValues?: boolean }
export interface Decision { id: Id; requestId: Id; actorUserId: Id; status: ApprovalStatus; decidedAtUtc: Instant;
  reason: string; decisionJson: string | null; createdAtUtc: Instant; createdByUserId: Id | null }
export interface SettlementAmounts { dashboardDebt: number; commission: number; total: number }
export interface Approval { request: ApprovalRequest; decisions: Decision[]; amounts: SettlementAmounts | null }
// Read endpoints enrich the original request handover; write responses keep Approval.
export interface ApprovalRequestRow extends ApprovalRequest { userRider: Person | null;
  assignment: ApprovalAssignment | null; account: ApprovalAccount | null;
  targetAccount: ApprovalAccount | null; operation: Operation | null; amounts: SettlementAmounts | null }
export interface ApprovalAssignment { assignmentId: Id; handoverId: Id; accountId: Id; riderProfileId: Id;
  startedAtUtc: Instant; endedAtUtc: Instant | null; effectiveFrom: DateOnly | null; effectiveTo: DateOnly | null;
  status: string | null; startReason: string | null; endReason: string | null; commissionStartsOn: DateOnly;
  commissionPostedThrough: DateOnly | null; lastSettlementPaymentAtUtc: Instant | null }
export interface ApprovalAccount { accountId: Id; code: string; externalAccountId: string; userName: string | null;
  status: string; statusReason: string | null; operatingCityId: Id; cityNameAr: string | null;
  sponsorId: Id | null; sponsorNameAr: string | null }
export interface ApprovalDetails { request: ApprovalRequestRow; decisions: Decision[] }
export interface MoneyRecord { fund: Fund; reference: string; reason: string; purpose?: Purpose }
export interface FeeReceipt { handoverId: Id; amount: number; fund: Fund; reference: string; reason: string }
export interface TransferredDebt extends Audit { operationId: Id; riderProfileId: Id; amount: number;
  paidAmount: number; paidAtUtc: Instant | null; paidByUserId: Id | null }
export interface RiderTransferredDebtSummary { riderProfileId: Id; userRider: Person | null;
  unpaidDebtCount: number; outstandingAmount: number; amounts: SettlementAmounts | null;
  paidAmount: number; totalCommission: number | null }
export interface RiderDebtPartialRepayment { amount: number; expectedAmounts: SettlementAmounts;
  fund: Fund; reference: string; reason: string }
export interface RiderDebtPaymentAllocation { debtId: Id; movementId: Id; dashboardDebtPaid: number;
  commissionPaid: number; remainingAmount: number; isPaid: boolean }
export interface RiderDebtPartialRepaymentResponse { riderProfileId: Id; paidAmount: number;
  dashboardDebtPaid: number; commissionPaid: number; remainingAmounts: SettlementAmounts;
  allocations: RiderDebtPaymentAllocation[] }
export interface RiderDebtCommissionUpdate { commission: number; expectedCommission: number;
  expectedAmounts: SettlementAmounts; reason: string }
export interface RiderDebtCommissionUpdateResponse { riderProfileId: Id; previousCommission: number;
  commission: number; remainingAmounts: SettlementAmounts; adjustedDebtIds: Id[] }
export interface RiderDebtRepayment { expectedAmount: number; fund: Fund; reference: string; reason: string }
export interface RiderDebtRepaymentResponse { riderProfileId: Id; paidAmount: number; paidDebtIds: Id[]; remainingAmount: number }
// GET transferred-debts follows the debt's source operation, never the account's current assignment.
export interface TransferredDebtRow extends TransferredDebt { userRider: Person | null;
  assignment: TransferredDebtAssignment | null; sourceOperation: TransferredDebtSourceOperation | null;
  outstandingAmount: number; amounts: SettlementAmounts | null }
export interface TransferredDebtAssignment { assignmentId: Id; handoverId: Id; accountId: Id;
  externalAccountId: string | null; riderProfileId: Id; startedAtUtc: Instant; endedAtUtc: Instant | null;
  effectiveFrom: DateOnly | null; effectiveTo: DateOnly | null; status: string | null;
  startReason: string | null; endReason: string | null }
export interface TransferredDebtSourceOperation { id: Id; mode: OperationMode; throughDate: DateOnly;
  effectiveAtUtc: Instant; net: number; commission: number; remaining: number; reason: string;
  approvedRequestId: Id | null; executedAtUtc: Instant | null }
export interface Movement { id: Id; handoverId: Id; riderProfileId: Id; operationId: Id | null;
  transferredDebtId: Id | null; fund: Fund; direction: Direction; purpose: Purpose;
  amount: number; actorUserId: Id; occurredAtUtc: Instant; reference: string; reason: string;
  createdAtUtc: Instant; createdByUserId: Id | null; dashboardDebtPaid: number | null; commissionPaid: number | null }
// GET money-movements: the recorded movement rider and the linked account's registered owner.
export interface MovementRow extends Movement { sponsorId: Id | null; sponsorIdentifier: string | null; treasurySubAccountId: Id | null; userRider: Person | null; ownerRider: Person | null;
  paymentReference: string | null;
  sponsorNameAr: string | null; cashboxHandoverId: Id | null; isHandedToAccountant: boolean;
  accountId: Id | null; platformAccountId: string | null; externalAccountId: string | null; net: number | null;
  commission: number | null; calculatedCommission: number | null;
  // Inclusive saved company-commission accrual range, only for purpose = Settlement.
  startDate: DateOnly | null; endDate: DateOnly | null; operationRowVersion: string | null }
// Dates use YYYY-MM-DD, inclusive in Riyadh time. Defaults: current month's first day through today.
// Each omitted bound defaults independently; fromDate > toDate returns HTTP 400.
export interface MovementDateRange { fromDate?: DateOnly | null; toDate?: DateOnly | null }
export interface FundBalance { fund: Fund; receipts: number; disbursements: number; balance: number }
export enum CashboxHandoverStatus { Pending = 1, Confirmed = 2, Cancelled = 3 }
export interface CashboxHandover extends Audit { businessDate: DateOnly; status: CashboxHandoverStatus;
  requestedByUserId: Id; cashboxAmount: number; petroAppAmount: number; bankAmount: number;
  movementCount: number; reason: string; accountantUserId: Id | null; confirmedAtUtc: Instant | null;
  confirmationReason: string | null; cancelledAtUtc: Instant | null; cancellationReason: string | null }
export interface CashboxHandoverCreate { businessDate: DateOnly; reason: string }
export interface CashboxHandoverConfirm { cashboxAmount: number; petroAppAmount: number; bankAmount: number;
  reason: string; rowVersion: string }
export interface CashboxHandoverCancel { reason: string; rowVersion: string }
export interface AccountStatusUpdate { status: Account['status']; reason: string; rowVersion: string }
export interface AccountFee extends Audit { handoverId: Id; amount: number; waivedAmount: number; approvalRequestId: Id | null }
export interface HandoverCreate { accountId: Id; riderProfileId: Id; effectiveAtUtc: Instant; reason: string;
  initialFeePayment?: number; paymentReference?: string | null }
export interface Handover { id: Id; accountId: Id; externalAccountId: string; riderProfileId: Id; assignmentId: Id;
  startedAtUtc: Instant; endedAtUtc: Instant | null; commissionStartsOn: DateOnly; commissionPostedThrough: DateOnly | null;
  lastSettlementPaymentAtUtc: Instant | null }
export interface EarningsRequest { handoverId: Id; fromDate: DateOnly; toDate: DateOnly; totalDeliveryPrice: number;
  totalPenalties: number; totalCashAmount: number; totalDriverDebit: number; totalServiceDeduction: number;
  totalDriverCredit: number; totalBonuses: number; totalTips: number; totalFreeOrders: number; reason: string; supersedesId?: Id | null }
export interface EarningsStatement extends EarningsRequest { id: Id; createdAtUtc: Instant; createdByUserId: Id | null }
export interface AdministrativeDebt { handoverId: Id; accountId: Id; externalAccountId: string; riderProfileId: Id;
  riderNameAr: string; platformDebt: number }
export enum ImportKind { Transactions = 1, DailyOrders = 2 }
export interface ImportBatch extends Audit { kind: ImportKind; contentHash: string; uploadedByUserId: Id;
  committedAtUtc: Instant | null; replacesBatchId: Id | null; correctionReason: string | null }
export interface ImportIssue { rowId: Id; fileName: string; rowNumber: number; code: string; description: string }
export interface ImportRow { rowId: Id; fileName: string; rowNumber: number; driverId: string; occurredAtUtc: Instant;
  accountId: Id | null; handoverId: Id | null; riderProfileId: Id | null; netAmount: number; dispatches: number | null }
export interface ImportAccountSummary { accountId: Id | null; driverId: string; fromDate: DateOnly; toDate: DateOnly;
  validRowCount: number; netAmount: number; platformDebtChange: number; dispatches: number | null; hasIssues: boolean }
export interface ImportPreview { batchId: Id; kind: ImportKind; committed: boolean; rows: ImportRow[]; issues: ImportIssue[];
  files: { id: Id; fileName: string }[] | null; accounts: ImportAccountSummary[] | null }
export interface ImportCommit { allocations?: { rowId: Id; handoverId: Id; count: number; reason: string }[] | null }
export interface OrderRow { accountId: Id; externalAccountId: string; riderProfileId: Id; handoverId: Id; date: DateOnly; count: number }
export interface LedgerEntry { id: Id; handoverId: Id; bucket: number; kind: number; amount: number; sourceId: Id;
  reversesEntryId: Id | null; occurredAtUtc: Instant; reason: string; fromDate: DateOnly | null; throughDate: DateOnly | null;
  calculationJson: string | null; createdAtUtc: Instant; createdByUserId: Id | null }
export interface AccountDetails { account: Account; handovers: Page<Handover>; operations: Page<Operation>;
  requests: Page<ApprovalRequest>; fees: Page<AccountFee>; orders: Page<OrderRow>; ledger: Page<LedgerEntry> }
export interface Problem { type?: string; title?: string; status: number; detail?: string; errorCode?: string; correlationId?: string }
// All mutations require Idempotency-Key. No new account filter parameters exist.
export const routes = {
  accounts: '/api/jahez/v2/accounts', statistics: '/api/jahez/v2/statistics', operations: '/api/jahez/v2/operations',
  preview: '/api/jahez/v2/operations/preview', requests: '/api/jahez/v2/requests',
  transferredDebts: '/api/jahez/v2/transferred-debts', feeReceipts: '/api/jahez/v2/fees/receipts',
  ridersWithTransferredDebts: '/api/jahez/v2/riders/transferred-debts',
  moneyMovements: '/api/jahez/v2/money-movements', funds: '/api/jahez/v2/funds',
  cashboxHandovers: '/api/jahez/v2/cashbox/handovers',
  handovers: '/api/jahez/v2/handovers', earnings: '/api/jahez/v2/earnings', imports: '/api/jahez/v2/imports', administrativeDebts: '/api/jahez/v2/administrative-debts'
} as const;
