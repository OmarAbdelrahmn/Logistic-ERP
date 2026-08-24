export type Locale = "ar" | "en";
export type Theme = "light" | "dark" | "system";
export type Density = "compact" | "comfortable";

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
};

export type AuthResponse = AuthTokens & {
  tokenType: string;
  sessionId: string;
  user: {
    id: string;
    employeeId: string | null;
    userName: string;
    email: string | null;
    displayNameAr: string;
    displayNameEn: string;
    preferredLocale: Locale;
    requiresPasswordChange: boolean;
    roles: string[];
  };
};

export type UserProfile = {
  id: string;
  employeeId: string | null;
  userName: string;
  email: string | null;
  phoneNumber: string | null;
  displayNameAr: string;
  displayNameEn: string;
  status: string;
  preferredLocale: Locale;
  preferredTheme: Theme;
  preferredDensity: Density;
  requiresPasswordChange: boolean;
  lastLoginAtUtc: string | null;
  lastActivityAtUtc: string | null;
};

export type UserAuthorization = {
  roles: Array<{ code: string }>;
  effectivePermissionKeys: string[];
};

export type Employee = {
  id: string;
  iqamaNo: string | null;
  fullNameAr: string;
  fullNameEn: string | null;
  nationality: string | null;
  primaryPhone: string | null;
  isEmployee: boolean;
  engagementType: string;
  status: string;
  workingForMeAs: string | null;
  residencyProfession: string | null;
  riderProfileId: string | null;
  sponsorNameAr: string | null;
};

export type Rider = {
  id: string;
  employeeId: string;
  iqamaNo: string | null;
  fullNameAr: string;
  fullNameEn: string | null;
  engagementType: string;
  status: string;
  tShirtSize: string | null;
  operationalNotes: string | null;
};

export type Housing = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  cityAr: string;
  totalCapacity: number;
  currentResidents: number;
  availableCapacity: number;
  status: string;
};

export type PlatformAccount = {
  id: string;
  platformNameAr: string;
  registeredEmployeeNameAr: string | null;
  operatingCityAr: string;
  code: string;
  externalAccountId: string;
  status: string;
  endDate: string | null;
};

export type PlatformAssignment = {
  id: string;
  actualEmployeeNameAr: string;
  contractNameAr: string;
  externalAccountId: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  status: string;
};

export type ComplianceItem = {
  id: string;
  expiryDate?: string | null;
  endDate?: string | null;
  status: string;
  sponsorNameAr?: string | null;
  categoryAr?: string | null;
  employeeId: string;
};

export type LeaveRequest = {
  id: string;
  requestNumber: string;
  employeeNameAr: string;
  leaveTypeNameAr: string;
  startDate: string;
  endDate: string;
  status: string;
  hrStatus: string;
};

export type AbsenceCase = {
  id: string;
  caseNumber: string;
  employeeNameAr: string;
  absenceDate: string;
  removalDeadline: string;
  status: string;
};

export type StatusChangeRequest = {
  id: string;
  requestNumber: string;
  employeeNameAr: string;
  requestedStatus: string;
  effectiveFrom: string;
  status: string;
};

export type DashboardSnapshot = {
  employees: Employee[] | null;
  riders: Rider[] | null;
  housing: Housing[] | null;
  accounts: PlatformAccount[] | null;
  assignments: PlatformAssignment[] | null;
  driverLicenses: ComplianceItem[] | null;
  insurancePolicies: ComplianceItem[] | null;
  leaveRequests: LeaveRequest[] | null;
  absenceCases: AbsenceCase[] | null;
  statusChanges: StatusChangeRequest[] | null;
  unavailable: string[];
};

export type HrImportIssue = {
  rowNumber: number;
  iqamaNo: string | null;
  severity: string;
  message: string;
};

export type HrImportResponse = {
  validateOnly: boolean;
  worksheet: string;
  totalRows: number;
  validRows: number;
  createdEmployees: number;
  updatedEmployees: number;
  createdRiders: number;
  createdDriverLicenses: number;
  createdPlatformAccounts: number;
  createdPlatformAssignments: number;
  importedColumns: string[];
  ignoredColumns: string[];
  issues: HrImportIssue[];
};
