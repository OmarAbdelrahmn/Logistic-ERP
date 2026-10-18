import type { AccountDetails, AccountsPage, AccountStatusUpdate, Approval, ApprovalRequest, DecisionRequest,
  FeeReceipt, FundBalance, MoneyRecord, Movement, MovementRow, MovementDateRange, Operation, OperationCreate, OperationUpdate, OperationPaymentMethodUpdate,
  Page, Preview, Problem, Statistics, TransferredDebt, HandoverCreate, Handover,
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
    updateStatus: (id: string, value: AccountStatusUpdate, key: string) => send<AccountsPage['items'][number]>(`accounts/${id}/status`, 'PUT', value, key),
    preview: (value: OperationCreate) => send<Preview>('operations/preview', 'POST', value),
    createOperation: (value: OperationCreate, key: string) => send<Operation>('operations', 'POST', value, key),
    updateOperation: (id: string, value: OperationUpdate, key: string) => send<Operation>(`operations/${id}`, 'PUT', value, key),
    updatePaymentMethod: (id: string, value: OperationPaymentMethodUpdate, key: string) => send<Operation>(`operations/${id}/payment-method`, 'PUT', value, key),
    execute: (id: string, key: string) => send<Operation>(`operations/${id}/execute`, 'POST', undefined, key),
    operations: (number = 1, size = 50) => send<Page<Operation>>(`operations?${page(number, size)}`),
    requests: (number = 1, size = 50) => send<Page<ApprovalRequest>>(`requests?${page(number, size)}`),
    request: (id: string) => send<Approval>(`requests/${id}`),
    createRequest: (value: import('./jahez-v2-api-contracts').RequestCreate, key: string) => send<Approval>('requests', 'POST', value, key),
    decide: (id: string, value: DecisionRequest, key: string) => send<Approval>(`requests/${id}/decision`, 'POST', value, key),
    recordFee: (value: FeeReceipt, key: string) => send<Movement>('fees/receipts', 'POST', value, key),
    transferredDebts: (number = 1, size = 50) => send<Page<TransferredDebt>>(`transferred-debts?${page(number, size)}`),
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
