import type {
  AbsenceCase,
  AuthResponse,
  AuthTokens,
  ComplianceItem,
  DashboardSnapshot,
  Employee,
  Housing,
  HrImportResponse,
  LeaveRequest,
  PlatformAccount,
  PlatformAssignment,
  Rider,
  StatusChangeRequest,
  UserAuthorization,
  UserProfile,
} from "./types";
import type { ControllerEndpoint } from "./controller-catalog";

// Production defaults to the hosted API. Local development opts into localhost
// through `.env.local`, so a missing build variable can never ship localhost.
export const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://gate.premiumasp.net").replace(/\/$/, "");

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

async function request<T>(path: string, init: RequestInit = {}, accessToken?: string): Promise<T> {
  const isFormData = typeof FormData !== "undefined" && init.body instanceof FormData;
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
    throw new ApiError(problem?.detail ?? problem?.title ?? "The request could not be completed.", response.status);
  }

  return response.status === 204 ? (undefined as T) : response.json() as Promise<T>;
}

/** Executes a declared controller endpoint. Route values are resolved by the UI
 * from the endpoint template, preventing arbitrary API paths from being sent. */
export async function invokeControllerEndpoint(
  accessToken: string,
  endpoint: ControllerEndpoint,
  path: string,
  body?: unknown,
  file?: File | null,
) {
  const multipart = endpoint.content === "file";
  const requestBody = multipart
    ? createMultipartBody(body, file)
    : endpoint.method === "GET" ? undefined : JSON.stringify(body ?? {});

  if (endpoint.content === "download") {
    const response = await fetch(`${apiBaseUrl}${path}`, { headers: { Authorization: `Bearer ${accessToken}` } });
    if (!response.ok) throw await responseError(response);
    const blob = await response.blob();
    const disposition = response.headers.get("content-disposition") ?? "";
    const name = /filename\*?=(?:UTF-8''|\")?([^;\"]+)/i.exec(disposition)?.[1] ?? "document";
    return { download: { blob, name: decodeURIComponent(name.replace(/\"/g, "")) } };
  }

  return request<unknown>(path, { method: endpoint.method, body: requestBody }, accessToken);
}

function createMultipartBody(body: unknown, file?: File | null) {
  const form = new FormData();
  if (body && typeof body === "object" && !Array.isArray(body)) {
    for (const [key, value] of Object.entries(body)) {
      if (value !== null && value !== undefined && value !== "") form.append(key, String(value));
    }
  }
  if (file) form.append("file", file);
  return form;
}

async function responseError(response: Response) {
  const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
  return new ApiError(problem?.detail ?? problem?.title ?? "The request could not be completed.", response.status);
}

export async function login(loginName: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ login: loginName, password, deviceLabel: "ERP web" }),
  });
}

export async function refresh(refreshToken: string): Promise<AuthResponse> {
  return request<AuthResponse>("/api/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}

export function getProfile(accessToken: string) {
  return request<UserProfile>("/api/user-profile/me", {}, accessToken);
}

export function getAuthorization(accessToken: string) {
  return request<UserAuthorization>("/api/user-profile/me/authorization", {}, accessToken);
}

export function updatePreferences(
  accessToken: string,
  preferences: Partial<Pick<UserProfile, "preferredLocale" | "preferredTheme" | "preferredDensity">>,
) {
  return request<UserProfile>("/api/user-profile/me/preferences", {
    method: "PATCH",
    body: JSON.stringify(preferences),
  }, accessToken);
}

export function changePassword(
  accessToken: string,
  currentPassword: string,
  newPassword: string,
) {
  return request<AuthResponse>("/api/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
  }, accessToken);
}

async function uploadHrWorkbook(accessToken: string, workbook: File, validateOnly: boolean) {
  const formData = new FormData();
  formData.append("file", workbook);
  return request<HrImportResponse>(
    validateOnly ? "/api/import/employees-riders/validate" : "/api/import/employees-riders",
    { method: "POST", body: formData },
    accessToken,
  );
}

export function validateHrWorkbook(accessToken: string, workbook: File) {
  return uploadHrWorkbook(accessToken, workbook, true);
}

export function importHrWorkbook(accessToken: string, workbook: File) {
  return uploadHrWorkbook(accessToken, workbook, false);
}

type DashboardRequest<T> = { permission: string; path: string; key: keyof DashboardSnapshot };

async function loadDashboardResource<T>(
  requestDefinition: DashboardRequest<T>,
  accessToken: string,
  permissions: Set<string>,
): Promise<{ key: keyof DashboardSnapshot; data: T[] | null; unavailable: string | null }> {
  if (!permissions.has(requestDefinition.permission)) {
    return { key: requestDefinition.key, data: null, unavailable: requestDefinition.key };
  }

  try {
    return {
      key: requestDefinition.key,
      data: await request<T[]>(requestDefinition.path, {}, accessToken),
      unavailable: null,
    };
  } catch {
    return { key: requestDefinition.key, data: null, unavailable: requestDefinition.key };
  }
}

export async function getManagerDashboard(
  accessToken: string,
  effectivePermissions: string[],
): Promise<DashboardSnapshot> {
  const permissions = new Set(effectivePermissions);
  const resources = await Promise.all([
    loadDashboardResource<Employee>({ key: "employees", permission: "employees.read", path: "/api/employees" }, accessToken, permissions),
    loadDashboardResource<Rider>({ key: "riders", permission: "riders.read", path: "/api/riders" }, accessToken, permissions),
    loadDashboardResource<Housing>({ key: "housing", permission: "housing.read", path: "/api/housing" }, accessToken, permissions),
    loadDashboardResource<PlatformAccount>({ key: "accounts", permission: "platform_accounts.read", path: "/api/platform-operations/accounts" }, accessToken, permissions),
    loadDashboardResource<PlatformAssignment>({ key: "assignments", permission: "platform_assignments.read", path: "/api/platform-operations/assignments?currentOnly=true" }, accessToken, permissions),
    loadDashboardResource<ComplianceItem>({ key: "driverLicenses", permission: "licenses.read", path: "/api/compliance/driver-licenses" }, accessToken, permissions),
    loadDashboardResource<ComplianceItem>({ key: "insurancePolicies", permission: "insurance.read", path: "/api/insurance/policies" }, accessToken, permissions),
    loadDashboardResource<LeaveRequest>({ key: "leaveRequests", permission: "leave_requests.read", path: "/api/hr-workflows/leave-requests" }, accessToken, permissions),
    loadDashboardResource<AbsenceCase>({ key: "absenceCases", permission: "absence_cases.read", path: "/api/hr-workflows/absence-cases" }, accessToken, permissions),
    loadDashboardResource<StatusChangeRequest>({ key: "statusChanges", permission: "employee_status_changes.read", path: "/api/hr-workflows/employee-status-change-requests" }, accessToken, permissions),
  ]);

  const snapshot: DashboardSnapshot = {
    employees: null, riders: null, housing: null, accounts: null, assignments: null,
    driverLicenses: null, insurancePolicies: null,
    leaveRequests: null, absenceCases: null, statusChanges: null, unavailable: [],
  };

  for (const resource of resources) {
    if (resource.unavailable) snapshot.unavailable.push(resource.unavailable);
    else Object.assign(snapshot, { [resource.key]: resource.data });
  }

  return snapshot;
}

export function tokensFrom(response: AuthResponse): AuthTokens {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
  };
}
