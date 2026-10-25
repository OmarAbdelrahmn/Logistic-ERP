import type { AccountDetails, AccountsPage, AccountStatusUpdate, Approval, ApprovalRequestRow, ApprovalDetails, DecisionRequest,
  RiderPerformanceFilter, JahezRidersPerformanceReport,
  FeeRefundPreviewRequest, FeeRefundCreate, FeeRefundPreview, FeeRefund,
  FeeReceipt, FundBalance, MoneyRecord, Movement, MovementRow, MovementDateRange, Operation, OperationCreate, OperationUpdate, OperationPaymentMethodUpdate, OperationNetUpdate, OperationCommissionStartUpdate,
  Page, Preview, Problem, ProfitReport, Statistics, TransferredDebt, TransferredDebtRow, HandoverCreate, Handover,
  RiderTransferredDebtSummary, RiderDebtRepayment, RiderDebtRepaymentResponse,
  RiderDebtPartialRepayment, RiderDebtPartialRepaymentResponse, RiderDebtCommissionUpdate, RiderDebtCommissionUpdateResponse,
  CashboxHandover, CashboxHandoverCreate, CashboxHandoverConfirm, CashboxHandoverCancel,
  EarningsRequest, EarningsStatement, AdministrativeDebt, ImportBatch, ImportPreview, ImportCommit, ImportKind } from './jahez-v2-api-contracts';

export class JahezApiError extends Error {
  constructor(readonly status: number, readonly problem: Problem) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`);
  }
}

/** Supply the ERP's existing token provider. Retrying a mutation must reuse its original key. */
export function jahezClient(baseUrl: string, token: () => string) {
  const root = `${baseUrl.replace(/\/$/, '')}/api/jahez/v2`;
  async function send<T>(path: string, method = 'GET', body?: unknown, key?: string): Promise<T> {
    const response = await fetch(`${root}/${path}`, {
      method,
      headers: { Authorization: `Bearer ${token()}`, ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(key ? { 'Idempotency-Key': key } : {}) },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });
    const payload = await response.json();
    if (!response.ok) throw new JahezApiError(response.status, payload as Problem);
    return payload as T;
  }
  const page = (number: number, size: number) => `page=${number}&pageSize=${size}`;
  return {
    previewFeeRefund: (value: FeeRefundPreviewRequest) => send<FeeRefundPreview>('fees/refunds/preview', 'POST', value),
    refundFee: (value: FeeRefundCreate, key: string) => send<FeeRefund>('fees/refunds', 'POST', value, key),
    feeRefunds: (handoverId: string) => send<FeeRefund[]>(`handovers/${handoverId}/fee-refunds`),
    handover: (value: HandoverCreate, key: string) => send<Handover>('handovers', 'POST', value, key),
    recordEarnings: (value: EarningsRequest, key: string) => send<EarningsStatement>('earnings', 'POST', value, key),
    administrativeDebts: (number = 1, size = 50) => send<Page<AdministrativeDebt>>(`administrative-debts?${page(number, size)}`),
    imports: (number = 1, size = 50) => send<Page<ImportBatch>>(`imports?${page(number, size)}`),
    importPreview: (id: string) => send<ImportPreview>(`imports/${id}`),
    commitImport: (id: string, value: ImportCommit, key: string) => send<ImportPreview>(`imports/${id}/commit`, 'POST', value, key),
    upload: async (kind: ImportKind, files: File[], key: string, replacesBatchId?: string, correctionReason?: string) => {
      const form = new FormData(); form.append('kind', String(kind));
      files.forEach(file => form.append('files', file));
      if (replacesBatchId) form.append('replacesBatchId', replacesBatchId);
      if (correctionReason) form.append('correctionReason', correctionReason);
      const response = await fetch(`${root}/imports`, { method: 'POST',
        headers: { Authorization: `Bearer ${token()}`, 'Idempotency-Key': key }, body: form });
      const payload = await response.json();
      if (!response.ok) throw new JahezApiError(response.status, payload as Problem);
      return payload as ImportPreview;
    },
    importFile: async (id: string) => {
      const response = await fetch(`${root}/import-files/${id}`, { headers: { Authorization: `Bearer ${token()}` } });
      if (!response.ok) throw new JahezApiError(response.status, await response.json() as Problem);
      return response.blob();
    },
    accounts: (number = 1, size = 50) => send<AccountsPage>(`accounts?${page(number, size)}`),
    account: (id: string, number = 1, size = 50) => send<AccountDetails>(`accounts/${id}?${page(number, size)}`),
    statistics: () => send<Statistics>('statistics'),
    riderPerformance: (filter: RiderPerformanceFilter) => {
      const query = new URLSearchParams({ startDate: filter.startDate, endDate: filter.endDate });
      if (filter.riderId != null) query.set('riderId', filter.riderId);
      if (filter.accountId != null) query.set('accountId', filter.accountId);
      if (filter.page != null) query.set('page', String(filter.page));
      if (filter.pageSize != null) query.set('pageSize', String(filter.pageSize));
      return send<JahezRidersPerformanceReport>(`reports/riders?${query}`);
    },
    profitReport: (dates: MovementDateRange = {}) => {
      const query = new URLSearchParams();
      if (dates.fromDate != null) query.set('fromDate', dates.fromDate);
      if (dates.toDate != null) query.set('toDate', dates.toDate);
      return send<ProfitReport>(`reports/profit?${query}`);
    },
    updateStatus: (id: string, value: AccountStatusUpdate, key: string) => send<AccountsPage['items'][number]>(`accounts/${id}/status`, 'PUT', value, key),
    preview: (value: OperationCreate) => send<Preview>('operations/preview', 'POST', value),
    createOperation: (value: OperationCreate, key: string) => send<Operation>('operations', 'POST', value, key),
    updateOperation: (id: string, value: OperationUpdate, key: string) => send<Operation>(`operations/${id}`, 'PUT', value, key),
    updatePaymentMethod: (id: string, value: OperationPaymentMethodUpdate, key: string) => send<Operation>(`operations/${id}/payment-method`, 'PUT', value, key),
    updateNet: (id: string, value: OperationNetUpdate, key: string) => send<Operation>(`operations/${id}/net`, 'PUT', value, key),
    updateCommissionStart: (id: string, value: OperationCommissionStartUpdate, key: string) => send<Operation>(`operations/${id}/commission-start`, 'PUT', value, key),
    execute: (id: string, key: string) => send<Operation>(`operations/${id}/execute`, 'POST', undefined, key),
    operations: (number = 1, size = 50) => send<Page<Operation>>(`operations?${page(number, size)}`),
    requests: (number = 1, size = 50) => send<Page<ApprovalRequestRow>>(`requests?${page(number, size)}`),
    request: (id: string) => send<ApprovalDetails>(`requests/${id}`),
    createRequest: (value: import('./jahez-v2-api-contracts').RequestCreate, key: string) => send<Approval>('requests', 'POST', value, key),
    decide: (id: string, value: DecisionRequest, key: string) => send<Approval>(`requests/${id}/decision`, 'POST', value, key),
    recordFee: (value: FeeReceipt, key: string) => send<Movement>('fees/receipts', 'POST', value, key),
    transferredDebts: (number = 1, size = 50) => send<Page<TransferredDebtRow>>(`transferred-debts?${page(number, size)}`),
    ridersWithTransferredDebts: (number = 1, size = 50) => send<Page<RiderTransferredDebtSummary>>(`riders/transferred-debts?${page(number, size)}`),
    riderTransferredDebts: (riderId: string, number = 1, size = 50) => send<Page<TransferredDebtRow>>(`riders/${riderId}/transferred-debts?${page(number, size)}`),
    repayRiderTransferredDebts: (riderId: string, value: RiderDebtRepayment, key: string) => send<RiderDebtRepaymentResponse>(`riders/${riderId}/transferred-debts/repayment`, 'POST', value, key),
    partiallyRepayRiderTransferredDebts: (riderId: string, value: RiderDebtPartialRepayment, key: string) => send<RiderDebtPartialRepaymentResponse>(`riders/${riderId}/transferred-debts/partial-repayment`, 'POST', value, key),
    updateRiderDebtCommission: (riderId: string, value: RiderDebtCommissionUpdate, key: string) => send<RiderDebtCommissionUpdateResponse>(`riders/${riderId}/transferred-debts/commission`, 'PUT', value, key),
    repay: (id: string, value: MoneyRecord, key: string) => send<TransferredDebt>(`transferred-debts/${id}/repayment`, 'POST', value, key),
    movements: (number = 1, size = 50, dates: MovementDateRange = {}) => {
      const query = new URLSearchParams(page(number, size));
      if (dates.fromDate != null) query.set('fromDate', dates.fromDate);
      if (dates.toDate != null) query.set('toDate', dates.toDate);
      return send<Page<MovementRow>>(`money-movements?${query}`);
    },
    funds: () => send<FundBalance[]>('funds'),
    cashboxHandovers: (number = 1, size = 50) => send<Page<CashboxHandover>>(`cashbox/handovers?${page(number, size)}`),
    cashboxHandover: (id: string) => send<CashboxHandover>(`cashbox/handovers/${id}`),
    submitCashboxHandover: (value: CashboxHandoverCreate, key: string) => send<CashboxHandover>('cashbox/handovers', 'POST', value, key),
    confirmCashboxHandover: (id: string, value: CashboxHandoverConfirm, key: string) => send<CashboxHandover>(`cashbox/handovers/${id}/confirm`, 'POST', value, key),
    cancelCashboxHandover: (id: string, value: CashboxHandoverCancel, key: string) => send<CashboxHandover>(`cashbox/handovers/${id}/cancel`, 'POST', value, key),
  };
}
