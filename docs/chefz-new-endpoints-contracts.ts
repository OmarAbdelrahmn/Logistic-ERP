/** Implemented Chefz workbook-update contracts. Dates are Riyadh calendar dates (YYYY-MM-DD). */
export type Guid = string;
export type DateOnly = string;
export type RowVersion = string;
export type ChefzRating = 'A' | 'B' | 'C' | 'D' | 'CHANGE';
export type ChefzDailyRatingValue = 'A' | 'B' | 'C' | 'D' | 'متابعه شديدة';
export type ChefzDashboardStatus = 'active' | 'Block By CHEFZ' | 'The supervisor suspended';
export type ChefzAccountStatus = 'Available' | 'Assigned' | 'Suspended' | 'Retired' | 'Archived';
export interface ChefzAccountStatusRequest {
  status: 'Active' | 'Suspended' | 'Retired' | 'Archived';
  /** The accountRowVersion from ChefzAccountResponse; not its details rowVersion. */
  rowVersion: RowVersion;
  reason?: string | null;
}
export interface ChefzPage<T> { items: T[]; page: number; pageSize: number }

export interface ChefzSettingsRequest {
  overallAOrders: number;
  overallBOrders: number;
  overallCOrders: number;
  dailyAOrders: number;
  dailyBOrders: number;
  dailyCOrders: number;
  aAcceptanceRate: number;
  bAcceptanceRate: number;
  cAcceptanceRate: number;
  changeAfterNoOrderDays: number;
  followUpOnZeroOrdersWithMissedOffers: boolean;
  rowVersion: RowVersion | null;
}
export interface ChefzSettingsResponse { settings: ChefzSettingsRequest; rowVersion: RowVersion }

export interface ChefzStatisticsRequest {
  sponsorshipCount: number;
  working: number;
  notWorking: number;
  blocked: number;
  permanentlyBlocked: number;
  updateBlocked: number;
  uploaded: number;
  privateLicense: number;
  pendingUpload: number;
  noLicense: number;
  managementSuspended: number;
  motorcycle: number;
  workingOutsideSponsorship: number;
  workingWithoutPerformance: number | null;
  rowVersion?: RowVersion | null;
}
export interface ChefzStatisticsResponse {
  id: Guid;
  sponsorId: Guid;
  sponsorNameAr: string;
  values: ChefzStatisticsRequest;
  rowVersion: RowVersion;
}

export interface ChefzRiderReportQuery {
  accountId?: Guid;
  riderId?: Guid;
  year: number;
  month: number;
  half: 1 | 2;
  halves: number;
}
export interface ChefzAllRidersReportQuery {
  year: number;
  month: number;
  half: 1 | 2;
  /** Defaults to 1; valid range 1–120. */
  halves?: number;
}
export interface ChefzRatingCounts {
  a: number;
  b: number;
  c: number;
  d: number;
  change: number;
  unrated: number;
}
export interface ChefzAccountPerformanceSummary {
  accountId: Guid;
  externalAccountId: string;
  summary: ChefzReportSummary;
}
export interface ChefzRiderPerformanceSummary {
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  selectedSummary: ChefzReportSummary;
  halves: ChefzHalfReport[];
  accounts: ChefzAccountPerformanceSummary[];
}
export interface ChefzRidersPerformanceReport {
  from: DateOnly;
  to: DateOnly;
  requestedHalves: number;
  totalRiders: number;
  ratingCounts: ChefzRatingCounts;
  riders: ChefzRiderPerformanceSummary[];
}
export interface ChefzHalfReport {
  from: DateOnly;
  to: DateOnly;
  daysWithData: number;
  orders: number;
  acceptanceRate: number | null;
  rating: ChefzRating | null;
  message: string | null;
}
export interface ChefzReportSummary {
  halvesWithData: number;
  daysWithData: number;
  orders: number;
  averageOrdersPerHalf: number | null;
  acceptanceRate: number | null;
  rating: ChefzRating | null;
}
export interface ChefzDailyRating {
  date: DateOnly;
  orders: number;
  validOffers: number;
  missedOffers: number;
  acceptanceRate: number | null;
  rating: ChefzDailyRatingValue;
}
export interface ChefzRiderReport {
  /** Set only when the request includes riderId, including periods without performance. */
  riderName: string | null;
  riderIqamaNo: string | null;
  accountId: Guid | null;
  riderId: Guid | null;
  requestedHalves: number;
  halves: ChefzHalfReport[];
  selectedSummary: ChefzReportSummary;
  allTimeSummary: ChefzReportSummary;
  daily: ChefzDailyRating[];
  messages: string[];
  accounts: ChefzAccountPerformanceReport[];
}
export interface ChefzPerformanceResponse {
  id: Guid;
  accountId: Guid;
  externalAccountId: string;
  assignmentId: Guid;
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  reportDate: DateOnly;
  reportIdNumber: string;
  totalValidOffers: number;
  acceptedOffers: number;
  declinedOffers: number;
  missedOffers: number;
  acceptanceRate: number;
}
export interface ChefzPerformanceRider {
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  accounts: ChefzPerformanceResponse[];
}
export interface ChefzImportResponse {
  importId: Guid | null;
  reportDate: DateOnly;
  importedRows: number;
  skippedRows: number;
  issues: { rowNumber: number; idNumber: string | null; message: string }[];
  riders: ChefzPerformanceRider[];
}
export interface ChefzAccountPerformanceReport {
  accountId: Guid;
  externalAccountId: string;
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  /** Summary for this account/rider in the requested period. */
  summary: ChefzReportSummary;
  daily: ChefzPerformanceResponse[];
}

export interface ChefzManagementReport {
  sponsorId: Guid;
  sponsorNameAr: string;
  application: string;
  reportDate: DateOnly;
  performanceDate: DateOnly;
  cycleFrom: DateOnly;
  cycleOrders: number;
  accountsBelowThreeOrdersInCycle: number;
  ridersWithOrdersYesterday: number;
  /** Riders with 1–3 accepted orders, combined across their accounts within this sponsor; excludes zero/missing data. */
  ridersWithThreeOrFewerOrdersYesterday: number;
  ridersWithMoreThanThreeOrdersYesterday: number;
}
export interface ChefzAccountReminder {
  accountId: Guid;
  ownerEmployeeId: Guid;
  ownerName: string;
  ownerStatus: string;
  condition: 'OutsideSponsorship' | 'FinalExit';
  message: string;
}

export interface ChefzAccountResponse {
  /** Account list: current assigned worker's ERP iqama; null when unavailable. */
  riderIqamaNo: string | null;
  accountId: Guid;
  externalAccountId: string;
  reportIdNumber: string;
  accountType: 'Freelancer' | 'FullTime' | 'Unconfigured';
  commissionRate: number;
  dailyFee: number;
  hasPassword: boolean;
  rowVersion: RowVersion;
  sponsorId: Guid | null;
  operatingCityId: Guid | null;
  vehicleType: 'Car' | 'Motorcycle' | 'Unknown';
  dashboardStatus: ChefzDashboardStatus | 'Available' | 'Assigned' | 'Suspended' | 'Retired' | 'Archived';
  status: ChefzAccountStatus;
  statusReason: string | null;
  accountRowVersion: RowVersion;
  isOperationallyBlocked: boolean;
  operationalBlockReason: string | null;
}
export interface ChefzAccountCounts {
  total: number;
  /** Accounts with an open rider link, including those in a manual lifecycle status. */
  taken: number;
  notTaken: number;
  available: number;
  assigned: number;
  suspended: number;
  retired: number;
  archived: number;
}
export interface ChefzSponsorAccountCounts {
  sponsorId: Guid | null;
  sponsorNameAr: string;
  sponsorNameEn: string | null;
  counts: ChefzAccountCounts;
}
export interface ChefzAccountsResponse extends ChefzPage<ChefzAccountResponse> {
  totalCount: number;
  summary: ChefzAccountCounts;
  sponsors: ChefzSponsorAccountCounts[];
}
export interface ChefzAccountDetailsRequest {
  accountType: 'Freelancer' | 'FullTime';
  reportIdNumber?: string | null;
  rowVersion?: RowVersion | null;
  vehicleType?: 'Car' | 'Motorcycle' | null;
  dashboardStatus?: ChefzDashboardStatus | null;
}
export interface ChefzProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  correlationId?: string;
  [key: string]: unknown;
}

export const CHEFZ_BASE_PATH = '/api/chefz';
/** API-relative paths: pass to the application's authenticated client. */
export const chefzPaths = {
  orderSettlementPreview: `${CHEFZ_BASE_PATH}/settlements/orders/preview`,
  orderSettlementImport: `${CHEFZ_BASE_PATH}/settlements/orders/import`,
  orderSettlementImportById: (importId: Guid) => `${CHEFZ_BASE_PATH}/settlements/orders/imports/${encodeURIComponent(importId)}`,
  settings: `${CHEFZ_BASE_PATH}/settings/performance`,
  statistics: (page = 1, pageSize = 50) => `${CHEFZ_BASE_PATH}/sponsor-statistics?${new URLSearchParams({ page: String(page), pageSize: String(pageSize) })}`,
  statisticsBySponsor: (sponsorId: Guid) => `${CHEFZ_BASE_PATH}/sponsor-statistics/${encodeURIComponent(sponsorId)}`,
  deleteStatistics: (sponsorId: Guid, rowVersion: RowVersion) => `${CHEFZ_BASE_PATH}/sponsor-statistics/${encodeURIComponent(sponsorId)}?${new URLSearchParams({ rowVersion })}`,
  riderReport: (query: ChefzRiderReportQuery) => {
    const params = new URLSearchParams({ year: String(query.year), month: String(query.month), half: String(query.half), halves: String(query.halves) });
    if (query.accountId) params.set('accountId', query.accountId);
    if (query.riderId) params.set('riderId', query.riderId);
    return `${CHEFZ_BASE_PATH}/reports/riders?${params}`;
  },
  allRidersReport: (query: ChefzAllRidersReportQuery) => {
    const params = new URLSearchParams({ year: String(query.year), month: String(query.month), half: String(query.half) });
    if (query.halves !== undefined) params.set('halves', String(query.halves));
    return `${CHEFZ_BASE_PATH}/reports/riders/all?${params}`;
  },
  managementReport: (reportDate: DateOnly, sponsorId?: Guid) => {
    const params = new URLSearchParams({ reportDate });
    if (sponsorId) params.set('sponsorId', sponsorId);
    return `${CHEFZ_BASE_PATH}/reports/management?${params}`;
  },
  reminders: (page = 1, pageSize = 50) => `${CHEFZ_BASE_PATH}/reminders?${new URLSearchParams({ page: String(page), pageSize: String(pageSize) })}`,
  accounts: (page = 1, pageSize = 50) => `${CHEFZ_BASE_PATH}/accounts?${new URLSearchParams({ page: String(page), pageSize: String(pageSize) })}`,
  account: (accountId: Guid) => `${CHEFZ_BASE_PATH}/accounts/${encodeURIComponent(accountId)}`,
  accountStatus: (accountId: Guid) => `${CHEFZ_BASE_PATH}/accounts/${encodeURIComponent(accountId)}/status`,
  performanceImport: `${CHEFZ_BASE_PATH}/performance/import`,
  performance: (from: DateOnly, to: DateOnly, page = 1, pageSize = 50, accountId?: Guid, riderId?: Guid) => {
    const params = new URLSearchParams({ from, to, page: String(page), pageSize: String(pageSize) });
    if (accountId) params.set('accountId', accountId);
    if (riderId) params.set('riderId', riderId);
    return `${CHEFZ_BASE_PATH}/performance?${params}`;
  },
  walletPreview: `${CHEFZ_BASE_PATH}/settlements/preview`,
  walletSave: `${CHEFZ_BASE_PATH}/settlements`,
};

/** Upload input: FormData containing only `file`. Cycles derive from each order Date. */
export interface ChefzImportIssue { rowNumber: number; idNumber: string | null; message: string }
export interface ChefzOrderSettlementResponse {
  id: Guid;
  accountId: Guid;
  driverId: string;
  assignmentId: Guid;
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  accountType: 'Freelancer' | 'FullTime';
  periodFrom: DateOnly;
  periodTo: DateOnly;
  chargedFrom: DateOnly;
  chargedTo: DateOnly;
  assignedDays: number;
  sourceRows: number;
  deliveryFees: number;
  vatAmount: number;
  deliveryFeesAfterVat: number;
  tips: number;
  compensation: number;
  bonuses: number;
  deductions: number;
  netEarnings: number;
  companyAmount: number;
  riderAmount: number;
}
export interface ChefzRiderCycleSettlement {
  riderProfileId: Guid;
  riderName: string;
  /** Actual worker employee iqama; null when missing. Never the account owner/report ID. */
  riderIqamaNo: string | null;
  periodFrom: DateOnly;
  periodTo: DateOnly;
  sourceRows: number;
  deliveryFees: number;
  vatAmount: number;
  deliveryFeesAfterVat: number;
  tips: number;
  compensation: number;
  bonuses: number;
  deductions: number;
  netEarnings: number;
  companyAmount: number;
  riderAmount: number;
  accounts: ChefzOrderSettlementResponse[];
}
export interface ChefzOrderSettlementImportResponse {
  importId: Guid | null;
  isPreview: boolean;
  alreadyImported: boolean;
  canSave: boolean;
  sourceRows: number;
  riders: ChefzRiderCycleSettlement[];
  netEarnings: number;
  companyAmount: number;
  riderAmount: number;
  issues: ChefzImportIssue[];
}

/** Existing GET /settlements items, extended to identify order imports. */
export interface ChefzSettlementResponse {
  riderName: string;
  riderIqamaNo: string | null;
  id: Guid;
  accountId: Guid;
  /** Current account sponsor; null when no visible sponsor is linked. */
  sponsorNameAr: string | null;
  sponsorNameEn: string | null;
  assignmentId: Guid;
  riderProfileId: Guid;
  accountType: 'Freelancer' | 'FullTime';
  periodFrom: DateOnly;
  periodTo: DateOnly;
  chargedFrom: DateOnly;
  chargedTo: DateOnly;
  assignedDays: number;
  startWalletAmount: number;
  endWalletAmount: number;
  /** Orders: net after VAT/additions/deductions. Wallet: end minus start. */
  grossEarnings: number;
  commissionRate: number;
  dailyFee: number;
  companyAmount: number;
  riderAmount: number;
  source: 'Wallet' | 'Orders';
  importId: Guid | null;
  sourceRows: number;
  deliveryFees: number;
  vatAmount: number;
  tips: number;
  compensation: number;
  bonuses: number;
  deductions: number;
}
