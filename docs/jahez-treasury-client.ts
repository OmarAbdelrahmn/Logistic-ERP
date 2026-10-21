import type * as T from './jahez-treasury-api-contracts';

export class TreasuryApiError extends Error {
  constructor(public readonly status: number, public readonly problem: unknown) { super(`Treasury API: ${status}`); }
}

export function createJahezTreasuryClient(apiBase: string, token: () => string) {
  const root = `${apiBase.replace(/\/$/, '')}/api/jahez/v3/treasury`;
  const query = (values: object) => {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(values)) if (value !== undefined && value !== null) params.set(key, String(value));
    return params.toString();
  };
  async function send<R>(path: string, method = 'GET', body?: unknown, key?: string): Promise<R> {
    const headers: Record<string, string> = { Authorization: `Bearer ${token()}` };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (key) headers['Idempotency-Key'] = key;
    const response = await fetch(`${root}/${path}`, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
    if (!response.ok) throw new TreasuryApiError(response.status, await response.json());
    return await response.json() as R;
  }
  return {
    accounts: () => send<T.TreasuryAccount[]>('accounts'),
    createAccount: (body: T.TreasuryAccountCreate, key: string) => send<T.TreasuryAccount>('accounts', 'POST', body, key),
    updateAccount: (id: string, body: T.TreasuryAccountUpdate, key: string) => send<T.TreasuryAccount>(`accounts/${id}`, 'PUT', body, key),
    balances: () => send<T.TreasuryBalance[]>('balances'),
    journal: (filter: T.TreasuryFilter) => send<T.TreasuryPage<T.TreasuryJournalRow>>(`journal?${query(filter)}`),
    transfer: (body: T.TreasuryTransferCreate, key: string) => send<string>('transfers', 'POST', body, key),
    cashEntry: (body: T.TreasuryCashCreate, key: string) => send<T.TreasuryPosting>('cash-entries', 'POST', body, key),
    reverse: (postingId: string, reference: string, reason: string, key: string) => send<string>('reversals', 'POST', { postingId, reference, reason }, key),
    readiness: () => send<T.TreasuryReadiness>('migration/readiness'),
    attributionReview: (page = 1, pageSize = 50) => send<T.TreasuryPage<T.TreasuryAttributionReview>>(`migration/attribution-review?${query({ page, pageSize })}`),
    attribute: (source: T.TreasurySource, sourceId: string, sponsorId: string, evidence: string, key: string) =>
      send<T.TreasuryOwnership>('migration/attributions', 'POST', { source, sourceId, sponsorId, evidence }, key),
    backfill: (reason: string, key: string) => send<number>('migration/backfill', 'POST', { reason }, key),
    verifyBalance: (subAccountId: string, balance: number, reference: string, reason: string, key: string) =>
      send<T.TreasuryVerification>('migration/balance-verifications', 'POST', { subAccountId, balance, reference, reason }, key),
    activate: (rowVersion: string, reason: string, key: string) => send<T.TreasuryState>('migration/activate', 'POST', { rowVersion, reason }, key),
    previewHandover: (businessDate: string) => send<T.TreasuryHandoverPreview>(`handovers/preview?${query({ businessDate })}`),
    handovers: (page = 1, pageSize = 50) => send<T.TreasuryPage<T.TreasuryHandover>>(`handovers?${query({ page, pageSize })}`),
    handover: (id: string) => send<T.TreasuryHandoverDetail>(`handovers/${id}`),
    submitHandover: (businessDate: string, reason: string, key: string) => send<T.TreasuryHandoverDetail>('handovers', 'POST', { businessDate, reason }, key),
    confirmHandover: (id: string, body: T.TreasuryHandoverConfirm, key: string) => send<T.TreasuryHandoverDetail>(`handovers/${id}/confirm`, 'POST', body, key),
    cancelHandover: (id: string, reason: string, rowVersion: string, key: string) => send<T.TreasuryHandoverDetail>(`handovers/${id}/cancel`, 'POST', { reason, rowVersion }, key),
    evidence: (filter: T.TreasuryFilter) => send<T.TreasuryPage<T.PetroEvidence>>(`petroapp/evidence?${query(filter)}`),
    addEvidence: (body: T.PetroEvidenceCreate, key: string) => send<T.PetroEvidence>('petroapp/evidence', 'POST', body, key),
    report: (type: T.TreasuryReportType, filter: T.TreasuryFilter, handoverId?: string) => send<T.TreasuryReport>(`reports/${type}?${query({ ...filter, handoverId })}`),
    exportReport: async (type: T.TreasuryReportType, format: 'xlsx' | 'pdf', filter: T.TreasuryFilter, handoverId?: string) => {
      const response = await fetch(`${root}/reports/${type}/export?${query({ ...filter, handoverId, format })}`, { headers: { Authorization: `Bearer ${token()}` } });
      if (!response.ok) throw new TreasuryApiError(response.status, await response.json());
      return { blob: await response.blob(), disposition: response.headers.get('Content-Disposition') };
    },
  };
}
