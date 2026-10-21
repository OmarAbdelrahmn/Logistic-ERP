import type { Audit, DateOnly, Id, Instant, Page, Purpose, CashboxHandover } from './jahez-v2-api-contracts';

export enum TreasuryLocation { Supervisor = 1, Central = 2, Bank = 3, PetroApp = 4 }
export enum TreasuryPurpose { Settlement = 1, AccountFee = 2, Combined = 3 }
export enum TreasuryDirections { InOnly = 1, OutOnly = 2, Dual = 3 }
export enum TreasuryEntryKind { Business = 1, Transfer = 2, Opening = 3, Adjustment = 4, Reversal = 5 }
export enum TreasurySource { Fee = 1, Operation = 2, Debt = 3, Movement = 4 }
export enum PetroEvidenceKind { Funding = 1, Statement = 2 }
export type TreasuryReportType = 'movements' | 'subaccounts' | 'trading' | 'petroapp' | 'handover';
export interface History { id: Id; createdAtUtc: Instant; createdByUserId: Id | null }
export interface TreasuryAccount extends Audit {
  code: string; nameAr: string; nameEn: string; parentId: Id | null; sponsorId: Id | null;
  location: TreasuryLocation; purpose: TreasuryPurpose; allowedDirections: TreasuryDirections;
  isPosting: boolean; isDefault: boolean; isActive: boolean;
}
export interface TreasuryAccountCreate {
  code: string; nameAr: string; nameEn: string; parentId: Id; sponsorId: Id; location: TreasuryLocation;
  purpose: TreasuryPurpose; allowedDirections: TreasuryDirections; isDefault?: boolean;
}
export interface TreasuryAccountUpdate { nameAr: string; nameEn: string; isActive: boolean; rowVersion: string }
export interface TreasuryPosting extends History {
  subAccountId: Id; sponsorId: Id; moneyMovementId: Id | null; cashboxHandoverId: Id | null;
  batchId: Id; revision: number; reversesPostingId: Id | null; kind: TreasuryEntryKind;
  amount: number; occurredAtUtc: Instant; actorUserId: Id; actorNameAr: string; reference: string; reason: string;
}
export interface TreasuryOwnership extends History {
  source: TreasurySource; sourceId: Id; platformAccountId: Id; sponsorId: Id;
  sponsorIdentifier: string; sponsorNameAr: string; evidence: string;
}
export interface TreasuryFilter {
  fromDate: DateOnly; toDate: DateOnly; subAccountId?: Id; sponsorId?: Id;
  purpose?: TreasuryPurpose; page?: number; pageSize?: number;
}
export interface TreasuryBalance {
  subAccountId: Id; code: string; nameAr: string; parentId: Id | null; sponsorId: Id | null;
  location: TreasuryLocation; isPosting: boolean; balance: number;
}
export interface TreasuryJournalRow {
  posting: TreasuryPosting; accountCode: string; accountNameAr: string; sponsorNameAr: string | null;
  platformAccountId: Id | null; operationId: Id | null; transferredDebtId: Id | null;
  businessPurpose: Purpose | null; isReconciled: boolean; driverId: Id | null; driverNameAr: string | null; iqamaNo: string | null;
}
export interface TreasuryTransferCreate { fromAccountId: Id; toAccountId: Id; amount: number; reference: string; reason: string; occurredAtUtc?: Instant | null }
export interface TreasuryCashCreate {
  subAccountId: Id; amount: number; kind: TreasuryEntryKind.Business | TreasuryEntryKind.Opening | TreasuryEntryKind.Adjustment;
  reference: string; reason: string; occurredAtUtc?: Instant | null;
}
export interface TreasuryHandoverLine extends History {
  handoverId: Id; subAccountId: Id; accountCode: string; accountNameAr: string; sponsorId: Id; sponsorNameAr: string; sponsorIdentifier: string; amount: number; movementCount: number;
}
export interface TreasuryHandover extends CashboxHandover { treasuryVersion: number; documentNumber: string | null; requestedByNameAr: string | null; accountantNameAr: string | null }
export interface TreasuryHandoverDetail { handover: TreasuryHandover; lines: TreasuryHandoverLine[] }
export interface TreasuryHandoverPreview {
  businessDate: DateOnly; lines: TreasuryHandoverLine[]; physicalCash: number;
  bankActivity: number; petroAppActivity: number; movementCount: number;
}
export interface TreasuryHandoverConfirm { cashCounts: { subAccountId: Id; amount: number }[]; reason: string; rowVersion: string }
export interface PetroEvidenceCreate {
  sponsorId: Id; kind: PetroEvidenceKind; businessDate: DateOnly; amount: number;
  allocatedCollections: number; reference: string; reason: string; replacesId?: Id | null;
}
export interface PetroEvidence extends History, Omit<PetroEvidenceCreate, 'replacesId'> { replacesId: Id | null }
export interface PetroReconciliation {
  sponsorId: Id; sponsorNameAr: string; collections: number; funding: number | null;
  allocatedCollections: number | null; unallocatedCollections: number | null; fundingDifference: number | null;
  openingStatement: number | null; closingStatement: number | null; importedConsumption: number | null;
  status: 'Matched' | 'Difference' | 'Unverified';
}
export interface TreasuryAccountPeriod {
  subAccountId: Id; code: string; nameAr: string; parentId: Id | null; sponsorId: Id | null; isPosting: boolean;
  opening: number; receipts: number; payments: number; transfersIn: number; transfersOut: number; closing: number;
}
export interface TreasuryReport {
  reportType: TreasuryReportType; filter: TreasuryFilter; accounts: TreasuryAccountPeriod[];
  movements: TreasuryJournalRow[]; fuel: PetroReconciliation[]; handover: TreasuryHandoverDetail | null; totalMovements: number;
}
export interface TreasuryReadiness {
  isActive: boolean; rowVersion: string; unresolvedMovements: number; unresolvedFees: number;
  unresolvedExecutedOperations: number; pendingLegacyHandovers: number; unverifiedAccounts: Id[];
}
export interface TreasuryState extends Audit { isActive: boolean; activatedAtUtc: Instant | null; nextHandoverNumber: number }
export interface TreasuryAttributionReview {
  source: TreasurySource; sourceId: Id; platformAccountId: Id; currentSponsorId: Id | null;
  requiresFinanceDecision: boolean; evidenceJson: string;
}
export interface TreasuryVerification extends History {
  subAccountId: Id; verifiedBalance: number; verifiedAtUtc: Instant; reference: string; reason: string;
}
export type TreasuryPage<T> = Page<T>;
