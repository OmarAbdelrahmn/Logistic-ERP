"use client";

import { FormEvent, ReactNode, useCallback, useEffect, useMemo, useState } from "react";
import { ApiError, changePassword, getAuthorization, getManagerDashboard, getProfile, importHrWorkbook, login, refresh, tokensFrom, updatePreferences, validateHrWorkbook } from "../lib/api";
import { ControllerWorkspace } from "./controller-workspace";
import { copy, type Translation } from "../lib/locales";
import type { AuthResponse, AuthTokens, DashboardSnapshot, Density, HrImportResponse, Locale, Theme, UserProfile } from "../lib/types";

const sessionKey = "logistics-erp.web.session";
type Session = { tokens: AuthTokens; profile: UserProfile; roles: string[]; permissions: string[] };

function isManager(roles: string[]) {
  return roles.some((role) => role === "MANAGER" || role === "SYSTEM_ADMIN");
}

function readSession(): AuthTokens | null {
  try {
    const value = window.sessionStorage.getItem(sessionKey);
    return value ? JSON.parse(value) as AuthTokens : null;
  } catch {
    return null;
  }
}

function writeSession(tokens: AuthTokens) {
  window.sessionStorage.setItem(sessionKey, JSON.stringify(tokens));
}

function initials(profile: UserProfile) {
  const name = profile.displayNameEn || profile.displayNameAr || profile.userName;
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join("").toUpperCase();
}

function displayName(profile: UserProfile, locale: Locale) {
  return locale === "ar" ? profile.displayNameAr || profile.displayNameEn : profile.displayNameEn || profile.displayNameAr;
}

export function ManagerPortal() {
  const [session, setSession] = useState<Session | null>(null);
  const [status, setStatus] = useState<"checking" | "signed-out" | "ready" | "password-change" | "forbidden">("checking");
  const [error, setError] = useState<string | null>(null);

  const establishSession = useCallback(async (response: AuthResponse) => {
    const tokens = tokensFrom(response);
    writeSession(tokens);
    const [profile, authorization] = await Promise.all([
      getProfile(tokens.accessToken),
      getAuthorization(tokens.accessToken),
    ]);
    const roles = authorization.roles.map((role) => role.code);
    setSession({ tokens, profile, roles, permissions: authorization.effectivePermissionKeys });
    setStatus(profile.requiresPasswordChange ? "password-change" : isManager(roles) ? "ready" : "forbidden");
  }, []);

  useEffect(() => {
    const restoreSession = async () => {
      const tokens = readSession();
      if (!tokens) {
        setStatus("signed-out");
        return;
      }

      try {
        const [profile, authorization] = await Promise.all([
          getProfile(tokens.accessToken),
          getAuthorization(tokens.accessToken),
        ]);
        const roles = authorization.roles.map((role) => role.code);
        setSession({ tokens, profile, roles, permissions: authorization.effectivePermissionKeys });
        setStatus(profile.requiresPasswordChange ? "password-change" : isManager(roles) ? "ready" : "forbidden");
      } catch (requestError) {
        if (!(requestError instanceof ApiError) || requestError.status !== 401) {
          setError(requestError instanceof Error ? requestError.message : "Unable to reach the API.");
          setStatus("signed-out");
          return;
        }

        try {
          await establishSession(await refresh(tokens.refreshToken));
        } catch {
          window.sessionStorage.removeItem(sessionKey);
          setError(copy.ar.invalidSession);
          setStatus("signed-out");
        }
      }
    };

    void restoreSession();
  }, [establishSession]);

  const updateProfile = useCallback(async (preferences: Partial<Pick<UserProfile, "preferredLocale" | "preferredTheme" | "preferredDensity">>) => {
    if (!session) return;
    try {
      const profile = await updatePreferences(session.tokens.accessToken, preferences);
      setSession((current) => current ? { ...current, profile } : current);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unable to save preferences.");
    }
  }, [session]);

  if (status === "checking") return <LoadingScreen />;
  if (status === "signed-out") return <LoginScreen error={error} onAuthenticated={establishSession} />;
  if (!session) return <LoadingScreen />;
  if (status === "password-change") return <PasswordChangeScreen session={session} onAuthenticated={establishSession} />;
  if (status === "forbidden") return <ForbiddenScreen profile={session.profile} onSignOut={() => { window.sessionStorage.removeItem(sessionKey); setSession(null); setStatus("signed-out"); }} />;

  return <Dashboard session={session} onUpdateProfile={updateProfile} onSignOut={() => { window.sessionStorage.removeItem(sessionKey); setSession(null); setStatus("signed-out"); }} />;
}

function LoginScreen({ error, onAuthenticated }: { error: string | null; onAuthenticated: (response: AuthResponse) => Promise<void> }) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState(error);
  const t = copy.ar;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setFormError(null);
    setIsSubmitting(true);
    try {
      await onAuthenticated(await login(String(form.get("login") ?? ""), String(form.get("password") ?? "")));
    } catch (requestError) {
      setFormError(requestError instanceof Error ? requestError.message : "تعذر تسجيل الدخول.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return <AuthShell title={t.signIn} description={t.signInDescription}>
    <form className="auth-form" onSubmit={submit}>
      <label><span>{t.userName}</span><input name="login" autoComplete="username" required /></label>
      <label><span>{t.password}</span><input name="password" type="password" autoComplete="current-password" required /></label>
      {formError && <p className="form-error" role="alert">{formError}</p>}
      <button className="primary-button" disabled={isSubmitting} type="submit">{isSubmitting ? t.signingIn : t.continue}</button>
    </form>
  </AuthShell>;
}

function PasswordChangeScreen({ session, onAuthenticated }: { session: Session; onAuthenticated: (response: AuthResponse) => Promise<void> }) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const t = copy[session.profile.preferredLocale];

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError(null);
    setIsSubmitting(true);
    try {
      await onAuthenticated(await changePassword(
        session.tokens.accessToken,
        String(form.get("currentPassword") ?? ""),
        String(form.get("newPassword") ?? ""),
      ));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Unable to change password.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return <AuthShell title={t.passwordChange} description={t.passwordChangeDescription} locale={session.profile.preferredLocale}>
    <form className="auth-form" onSubmit={submit}>
      <label><span>{t.currentPassword}</span><input name="currentPassword" type="password" autoComplete="current-password" required /></label>
      <label><span>{t.newPassword}</span><input name="newPassword" type="password" autoComplete="new-password" minLength={12} required /></label>
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="primary-button" disabled={isSubmitting} type="submit">{t.saveAndContinue}</button>
    </form>
  </AuthShell>;
}

function AuthShell({ title, description, children, locale = "ar" }: { title: string; description: string; children: ReactNode; locale?: Locale }) {
  return <main className="auth-page" dir={locale === "ar" ? "rtl" : "ltr"}>
    <section className="auth-panel" aria-labelledby="auth-title">
      <Brand />
      <div className="auth-copy"><p className="eyebrow">{copy[locale].company}</p><h1 id="auth-title">{title}</h1><p>{description}</p></div>
      {children}
    </section>
    <aside className="auth-aside" aria-hidden="true"><div className="route-line route-line-one" /><div className="route-line route-line-two" /><span>ERP</span></aside>
  </main>;
}

function ForbiddenScreen({ profile, onSignOut }: { profile: UserProfile; onSignOut: () => void }) {
  const locale = profile.preferredLocale;
  return <AuthShell title={copy[locale].noAccess} description={copy[locale].dashboardDescription} locale={locale}>
    <button className="secondary-button" onClick={onSignOut}>{copy[locale].signOut}</button>
  </AuthShell>;
}

function LoadingScreen() {
  return <main className="loading-screen"><span className="loading-mark" aria-hidden="true" /><span>Loading workspace…</span></main>;
}

function Dashboard({ session, onUpdateProfile, onSignOut }: { session: Session; onUpdateProfile: (preferences: Partial<Pick<UserProfile, "preferredLocale" | "preferredTheme" | "preferredDensity">>) => Promise<void>; onSignOut: () => void }) {
  const { profile } = session;
  const locale = profile.preferredLocale;
  const t = copy[locale];
  const dir = locale === "ar" ? "rtl" : "ltr";
  const [dashboard, setDashboard] = useState<DashboardSnapshot | null>(null);
  const [dashboardError, setDashboardError] = useState<string | null>(null);
  const [isLoadingDashboard, setIsLoadingDashboard] = useState(true);
  const [activeView, setActiveView] = useState("overview");

  const refreshDashboard = useCallback(async () => {
    setIsLoadingDashboard(true);
    setDashboardError(null);
    try {
      setDashboard(await getManagerDashboard(session.tokens.accessToken, session.permissions));
    } catch {
      setDashboardError(locale === "ar" ? "تعذر تحميل بيانات لوحة التحكم. حاول مجددًا." : "The dashboard data could not be loaded. Try again.");
    } finally {
      setIsLoadingDashboard(false);
    }
  }, [locale, session.permissions, session.tokens.accessToken]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = dir;
    document.documentElement.dataset.theme = profile.preferredTheme;
    document.documentElement.dataset.density = profile.preferredDensity;
  }, [dir, locale, profile.preferredDensity, profile.preferredTheme]);

  useEffect(() => {
    const task = window.setTimeout(() => { void refreshDashboard(); }, 0);
    return () => window.clearTimeout(task);
  }, [refreshDashboard]);

  const nav = useMemo(() => [
    ["overview", t.overview, "grid"], ["api", t.apiWorkspace, "server"], ["import", t.importWorkbook, "upload"], ["workforce", t.workforce, "people"], ["riders", t.riders, "route"], ["compliance", t.compliance, "shield"], ["operations", t.operations, "briefcase"], ["housing", t.housing, "home"], ["client-platforms", t.clientPlatforms, "layers"], ["reports", t.reports, "chart"],
  ], [t]);
  const activeLabel = nav.find(([id]) => id === activeView)?.[1] ?? t.dashboard;

  const activeRiders = dashboard?.riders?.filter((rider) => isActiveStatus(rider.status)).length ?? null;
  const availableHousing = dashboard?.housing?.reduce((total, housing) => total + housing.availableCapacity, 0) ?? null;
  const totalHousing = dashboard?.housing?.reduce((total, housing) => total + housing.totalCapacity, 0) ?? null;
  const metricValues = [dashboard?.employees?.length ?? null, activeRiders, dashboard?.assignments?.length ?? null, availableHousing];
  const metricIcons = ["people", "route", "layers", "home"];
  const attention = buildAttention(dashboard, t, locale);

  return <div className="app-shell" dir={dir} data-theme={profile.preferredTheme} data-density={profile.preferredDensity}>
    <a className="skip-link" href="#main-content">Skip to main content</a>
    <aside className="sidebar" aria-label={t.workspace}>
      <Brand compact />
      <nav className="primary-nav" aria-label={t.workspace}>
        <p className="nav-label">{t.workspace}</p>
        {nav.map(([id, label, icon]) => <button className={`nav-link ${id === activeView ? "is-active" : ""}`} type="button" onClick={() => setActiveView(id)} key={id} aria-current={id === activeView ? "page" : undefined}><Icon name={icon} /><span>{label}</span></button>)}
      </nav>
      <div className="sidebar-bottom">
        <p className="nav-label">{t.preferences}</p>
        <PreferenceSelect label={t.language} value={profile.preferredLocale} onChange={(value) => void onUpdateProfile({ preferredLocale: value as Locale })} options={[["ar", t.arabic], ["en", t.english]]} />
        <PreferenceSelect label={t.theme} value={profile.preferredTheme} onChange={(value) => void onUpdateProfile({ preferredTheme: value as Theme })} options={[["light", t.light], ["dark", t.dark], ["system", t.system]]} />
        <PreferenceSelect label={t.density} value={profile.preferredDensity} onChange={(value) => void onUpdateProfile({ preferredDensity: value as Density })} options={[["compact", t.compact], ["comfortable", t.comfortable]]} />
      </div>
    </aside>
    <div className="workspace">
      <header className="topbar">
        <div><p className="eyebrow">{t.workspace}</p><strong>{t.dashboard}</strong></div>
        <div className="account"><span className="status-dot" aria-hidden="true" /><span className="account-status">{t.active}</span><span className="avatar" aria-label={displayName(profile, locale)}>{initials(profile)}</span><div className="account-copy"><strong>{displayName(profile, locale)}</strong><span>{t.role}</span></div><button className="sign-out" onClick={onSignOut}>{t.signOut}</button></div>
      </header>
      <main id="main-content" className="dashboard-content">
        <section className="page-heading" id="overview"><div><p className="eyebrow">{t.workspace}</p><h1>{activeView === "overview" ? `${t.welcome} ${displayName(profile, locale)}` : activeLabel}</h1><p>{activeView === "overview" ? t.dashboardDescription : activeView === "api" ? t.apiWorkspaceDescription : `${activeLabel} · ${t.liveData}`}</p></div><div className="dashboard-actions">{activeView === "overview" && <div className="connection-note"><Icon name="spark" /><span>{t.dataNote}</span></div>}{activeView !== "api" && <button className="refresh-button" onClick={() => void refreshDashboard()} disabled={isLoadingDashboard}><Icon name="refresh" />{t.refresh}</button>}</div></section>
        {activeView !== "overview" ? activeView === "api" ? <ControllerWorkspace accessToken={session.tokens.accessToken} permissions={session.permissions} locale={locale} t={t} /> : activeView === "import" ? <ImportView accessToken={session.tokens.accessToken} locale={locale} t={t} onImported={refreshDashboard} /> : <ResourceView view={activeView} snapshot={dashboard} locale={locale} t={t} isLoading={isLoadingDashboard} /> : <>
        {dashboardError && <p className="dashboard-error" role="alert">{dashboardError}</p>}
        <section className="metric-grid" aria-label={t.dashboard}>
          {t.metrics.map((metric, index) => <article className="metric-card" key={metric}><span className="metric-icon"><Icon name={metricIcons[index]} /></span><p>{metric}</p><strong>{formatMetric(metricValues[index], locale)}</strong><small>{metricValues[index] === null ? t.unavailable : t.liveData}</small></article>)}
        </section>
        {isLoadingDashboard && !dashboard ? <p className="dashboard-state" aria-live="polite">{t.loadingData}</p> : <>
        <section className="dashboard-grid" id="housing">
          <article className="content-card housing-card"><div className="card-heading"><div><p className="eyebrow">{t.housing}</p><h2>{t.housingStatus}</h2></div><span className="data-total">{totalHousing === null ? t.unavailable : `${formatMetric(availableHousing, locale)} ${t.available}`}</span></div>
            {dashboard?.housing?.length ? <div className="housing-list">{dashboard.housing.slice(0, 5).map((housing) => <div className="housing-row" key={housing.id}><div><strong>{locale === "ar" ? housing.nameAr : housing.nameEn || housing.nameAr}</strong><span>{housing.cityAr}</span></div><div className="housing-meter"><span style={{ width: `${Math.min(100, (housing.currentResidents / Math.max(housing.totalCapacity, 1)) * 100)}%` }} /><small>{housing.currentResidents} / {housing.totalCapacity}</small></div></div>)}</div> : <EmptyData text={t.unavailable} icon="home" />}
          </article>
          <article className="content-card attention-card" id="compliance"><div className="card-heading"><div><p className="eyebrow">{t.operations}</p><h2>{t.attention}</h2></div><span className="attention-count">{attention.length}</span></div>
            {attention.length ? <ul className="attention-list">{attention.map((item) => <li key={item.id}><span className={`attention-icon ${item.level}`}><Icon name={item.level === "danger" ? "shield" : "clock"} /></span><div><strong>{item.title}</strong><span>{item.detail}</span></div></li>)}</ul> : <EmptyData text={t.noAttention} icon="check" />}
          </article>
        </section>
        <section className="dashboard-grid dashboard-grid-secondary">
          <article className="content-card roster-card" id="riders"><div className="card-heading"><div><p className="eyebrow">{t.riders}</p><h2>{t.roster}</h2></div><span className="data-total">{activeRiders === null ? t.unavailable : `${formatMetric(activeRiders, locale)} ${t.active}`}</span></div>
            {dashboard?.riders?.length ? <div className="table-wrap"><table><thead><tr><th>{t.employee}</th><th>{t.city}</th><th>{t.status}</th></tr></thead><tbody>{dashboard.riders.slice(0, 6).map((rider) => <tr key={rider.id}><td><strong>{locale === "ar" ? rider.fullNameAr : rider.fullNameEn || rider.fullNameAr}</strong><span>{rider.employeeNumber}</span></td><td>{rider.preferredCityAr ?? "—"}</td><td><span className={`status-chip ${isActiveStatus(rider.status) ? "is-good" : ""}`}>{rider.status}</span></td></tr>)}</tbody></table></div> : <EmptyData text={t.noRiders} icon="route" />}
          </article>
          <article className="content-card assignments-card" id="client-platforms"><div className="card-heading"><div><p className="eyebrow">{t.clientPlatforms}</p><h2>{t.assignmentsStatus}</h2></div><span className="data-total">{dashboard?.assignments === null ? t.unavailable : formatMetric(dashboard?.assignments.length ?? null, locale)}</span></div>
            {dashboard?.assignments?.length ? <ul className="assignment-list">{dashboard.assignments.slice(0, 5).map((assignment) => <li key={assignment.id}><span className="assignment-avatar">{assignment.actualEmployeeNameAr.slice(0, 1)}</span><div><strong>{assignment.actualEmployeeNameAr}</strong><span>{assignment.contractNameAr} · {assignment.externalAccountId}</span></div><span className="status-chip is-good">{assignment.status}</span></li>)}</ul> : <EmptyData text={t.unavailable} icon="layers" />}
          </article>
        </section></>}</>}
      </main>
    </div>
  </div>;
}

function EmptyData({ text, icon }: { text: string; icon: string }) {
  return <div className="empty-state"><Icon name={icon} /><p>{text}</p></div>;
}

function isActiveStatus(status: string) {
  return ["active", "operational", "approved", "enabled"].includes(status.trim().toLowerCase());
}

function formatMetric(value: number | null, locale: Locale) {
  return value === null ? "—" : new Intl.NumberFormat(locale === "ar" ? "ar-SA" : "en-US").format(value);
}

function buildAttention(snapshot: DashboardSnapshot | null, t: Translation, locale: Locale) {
  if (!snapshot) return [] as Array<{ id: string; title: string; detail: string; level: "warning" | "danger" }>;
  const today = new Date();
  const threshold = new Date(today);
  threshold.setDate(today.getDate() + 30);
  const items: Array<{ id: string; title: string; detail: string; level: "warning" | "danger" }> = [];
  for (const item of [...(snapshot.residencyPermits ?? []), ...(snapshot.driverLicenses ?? []), ...(snapshot.insurancePolicies ?? [])]) {
    const expiry = item.expiryDate ?? item.endDate;
    if (expiry && new Date(expiry) <= threshold) items.push({ id: item.id, title: t.upcomingExpiry, detail: new Intl.DateTimeFormat(locale === "ar" ? "ar-SA" : "en-GB", { dateStyle: "medium" }).format(new Date(expiry)), level: new Date(expiry) < today ? "danger" : "warning" });
  }
  for (const housing of snapshot.housing ?? []) {
    if (housing.availableCapacity <= 0) items.push({ id: housing.id, title: t.housingFull, detail: locale === "ar" ? housing.nameAr : housing.nameEn || housing.nameAr, level: "warning" });
  }
  for (const leave of snapshot.leaveRequests ?? []) {
    if (!["completed", "cancelled", "rejected"].includes(leave.status.toLowerCase())) items.push({ id: leave.id, title: t.leavePending, detail: leave.employeeNameAr, level: "warning" });
  }
  for (const absence of snapshot.absenceCases ?? []) {
    if (!["closed", "resolved"].includes(absence.status.toLowerCase())) items.push({ id: absence.id, title: t.absenceOpen, detail: absence.employeeNameAr, level: "danger" });
  }
  for (const change of snapshot.statusChanges ?? []) {
    if (!["resolved", "rejected", "cancelled"].includes(change.status.toLowerCase())) items.push({ id: change.id, title: t.statusChangePending, detail: change.employeeNameAr, level: "warning" });
  }
  return items.slice(0, 6);
}

type ResourceRow = { id: string; primary: string; secondary?: string; cells: string[]; status: string };
type ResourceTable = { columns: string[]; rows: ResourceRow[]; permitted: boolean };

function ImportView({ accessToken, locale, t, onImported }: { accessToken: string; locale: Locale; t: Translation; onImported: () => Promise<void> }) {
  const [workbook, setWorkbook] = useState<File | null>(null);
  const [result, setResult] = useState<HrImportResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isWorking, setIsWorking] = useState(false);
  const hasErrors = result?.issues.some((issue) => issue.severity.toLowerCase() === "error") ?? true;

  async function validate() {
    if (!workbook) return setError(t.fileRequired);
    if (workbook.size > 20 * 1024 * 1024) return setError(t.fileLimit);
    setIsWorking(true); setError(null); setResult(null);
    try {
      setResult(await validateHrWorkbook(accessToken, workbook));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : t.fileRequired);
    } finally { setIsWorking(false); }
  }

  async function commit() {
    if (!workbook || !result || hasErrors || !window.confirm(t.confirmImport)) return;
    setIsWorking(true); setError(null);
    try {
      const imported = await importHrWorkbook(accessToken, workbook);
      setResult(imported);
      await onImported();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : t.fileRequired);
    } finally { setIsWorking(false); }
  }

  return <section className="import-view">
    <article className="content-card import-card"><div className="card-heading"><div><p className="eyebrow">XLSX</p><h2>{t.importWorkbook}</h2></div><Icon name="upload" /></div>
      <p className="import-copy">{locale === "ar" ? "ارفع ملف Excel واحدًا، ثم افحص النتائج قبل تنفيذ أي تغيير في السجلات." : "Upload one Excel workbook, review its validation results, then apply the import."}</p>
      <label className="file-picker"><span>{t.selectWorkbook}</span><input type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" onChange={(event) => { setWorkbook(event.target.files?.[0] ?? null); setResult(null); setError(null); }} /><small>{workbook ? `${workbook.name} · ${Math.ceil(workbook.size / 1024)} KB` : ".xlsx · 20 MB max"}</small></label>
      <div className="import-actions"><button className="primary-button" type="button" onClick={() => void validate()} disabled={isWorking}>{isWorking ? t.importing : t.validateWorkbook}</button>{result?.validateOnly && <button className="secondary-button" type="button" onClick={() => void commit()} disabled={isWorking || hasErrors}>{t.commitImport}</button>}</div>
      {error && <p className="form-error" role="alert">{error}</p>}
    </article>
    {result && <ImportResult result={result} locale={locale} t={t} />}
  </section>;
}

function ImportResult({ result, locale, t }: { result: HrImportResponse; locale: Locale; t: Translation }) {
  const summary = [[t.totalRows, result.totalRows], [t.validRows, result.validRows], [t.createdEmployees, result.createdEmployees], [t.updatedEmployees, result.updatedEmployees], [t.createdRiders, result.createdRiders], [t.createdResidencies, result.createdResidencyPermits], [t.createdLicenses, result.createdDriverLicenses], [t.createdAccounts, result.createdPlatformAccounts], [t.createdAssignments, result.createdPlatformAssignments]];
  return <article className="content-card import-result"><div className="card-heading"><div><p className="eyebrow">{result.validateOnly ? t.validationResults : t.importSuccessful}</p><h2>{result.worksheet}</h2></div><span className={`status-chip ${result.issues.some((issue) => issue.severity.toLowerCase() === "error") ? "" : "is-good"}`}>{result.issues.length} {t.issues}</span></div>
    <dl className="import-summary">{summary.map(([label, value]) => <div key={String(label)}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>
    <div className="import-columns"><div><strong>{t.supportedColumns}</strong><p>{result.importedColumns.join(" · ") || "—"}</p></div><div><strong>{t.ignoredColumns}</strong><p>{result.ignoredColumns.join(" · ") || "—"}</p></div></div>
    {result.issues.length > 0 && <div className="table-wrap"><table className="resource-table import-issues"><thead><tr><th>#</th><th>{t.employee}</th><th>{t.status}</th><th>{t.issues}</th></tr></thead><tbody>{result.issues.map((issue) => <tr key={`${issue.rowNumber}-${issue.message}`}><td>{issue.rowNumber}</td><td>{issue.employeeNumber ?? "—"}</td><td><span className={`status-chip ${issue.severity.toLowerCase() === "error" ? "is-danger" : ""}`}>{issue.severity}</span></td><td>{issue.message}</td></tr>)}</tbody></table></div>}
  </article>;
}

function ResourceView({ view, snapshot, locale, t, isLoading }: { view: string; snapshot: DashboardSnapshot | null; locale: Locale; t: Translation; isLoading: boolean }) {
  if (isLoading && !snapshot) return <p className="dashboard-state" aria-live="polite">{t.loadingData}</p>;
  const table = resourceTable(view, snapshot, locale, t);
  if (!table.permitted) return <section className="content-card resource-view"><EmptyData text={t.accessRequired} icon="shield" /></section>;
  return <section className="content-card resource-view" aria-label={t.records}>
    <div className="card-heading"><div><p className="eyebrow">{t.liveData}</p><h2>{table.rows.length} {t.records}</h2></div></div>
    {table.rows.length ? <div className="table-wrap"><table className="resource-table"><thead><tr><th>{t.employee}</th>{table.columns.map((column) => <th key={column}>{column}</th>)}</tr></thead><tbody>{table.rows.map((row) => <tr key={row.id}><td><strong>{row.primary}</strong>{row.secondary && <span>{row.secondary}</span>}</td>{row.cells.map((cell, index) => <td key={`${row.id}-${index}`}>{cell}</td>)}<td><span className={`status-chip ${isActiveStatus(row.status) ? "is-good" : ""}`}>{row.status}</span></td></tr>)}</tbody></table></div> : <EmptyData text={t.noData} icon="grid" />}
  </section>;
}

function resourceTable(view: string, snapshot: DashboardSnapshot | null, locale: Locale, t: Translation): ResourceTable {
  if (!snapshot) return { columns: [], rows: [], permitted: false };
  if (view === "workforce") return {
    permitted: snapshot.employees !== null, columns: [t.city, t.type, t.status],
    rows: (snapshot.employees ?? []).map((employee) => ({ id: employee.id, primary: locale === "ar" ? employee.fullNameAr : employee.fullNameEn || employee.fullNameAr, secondary: employee.employeeNumber, cells: [employee.operatingCityAr ?? "—", employee.jobTitleAr ?? employee.relationshipType ?? "—"], status: employee.status })),
  };
  if (view === "riders") return {
    permitted: snapshot.riders !== null, columns: [t.city, t.type, t.status],
    rows: (snapshot.riders ?? []).map((rider) => ({ id: rider.id, primary: locale === "ar" ? rider.fullNameAr : rider.fullNameEn || rider.fullNameAr, secondary: rider.employeeNumber, cells: [rider.preferredCityAr ?? "—", rider.isOutsideRider ? t.outsideRider : t.rider], status: rider.status })),
  };
  if (view === "housing") return {
    permitted: snapshot.housing !== null, columns: [t.city, t.capacity, t.status],
    rows: (snapshot.housing ?? []).map((housing) => ({ id: housing.id, primary: locale === "ar" ? housing.nameAr : housing.nameEn || housing.nameAr, secondary: housing.code, cells: [housing.cityAr, `${housing.currentResidents} / ${housing.totalCapacity}`], status: housing.status })),
  };
  if (view === "operations" || view === "client-platforms") return {
    permitted: snapshot.accounts !== null, columns: [t.contract, t.city, t.status],
    rows: (snapshot.accounts ?? []).map((account) => ({ id: account.id, primary: account.labelAr || account.externalAccountId, secondary: account.platformNameAr, cells: [account.contractNameAr, account.operatingCityAr], status: account.status })),
  };
  if (view === "compliance") {
    const items = [...(snapshot.residencyPermits ?? []), ...(snapshot.driverLicenses ?? []), ...(snapshot.insurancePolicies ?? [])];
    return {
      permitted: snapshot.residencyPermits !== null || snapshot.driverLicenses !== null || snapshot.insurancePolicies !== null, columns: [t.type, t.expiry, t.status],
      rows: items.map((item) => ({ id: item.id, primary: item.sponsorNameAr || t.compliance, secondary: item.categoryAr ?? undefined, cells: [item.categoryAr || "—", formatDate(item.expiryDate ?? item.endDate, locale)], status: item.status })),
    };
  }
  const workflowRows: ResourceRow[] = [
    ...(snapshot.leaveRequests ?? []).map((item) => ({ id: item.id, primary: item.employeeNameAr, secondary: item.requestNumber, cells: [item.leaveTypeNameAr, formatDate(item.startDate, locale)], status: item.status })),
    ...(snapshot.absenceCases ?? []).map((item) => ({ id: item.id, primary: item.employeeNameAr, secondary: item.caseNumber, cells: [t.absenceOpen, formatDate(item.removalDeadline, locale)], status: item.status })),
    ...(snapshot.statusChanges ?? []).map((item) => ({ id: item.id, primary: item.employeeNameAr, secondary: item.requestNumber, cells: [item.requestedStatus, formatDate(item.effectiveFrom, locale)], status: item.status })),
  ];
  return { permitted: snapshot.leaveRequests !== null || snapshot.absenceCases !== null || snapshot.statusChanges !== null, columns: [t.type, t.effectiveDate, t.status], rows: workflowRows };
}

function formatDate(value: string | null | undefined, locale: Locale) {
  if (!value) return "—";
  return new Intl.DateTimeFormat(locale === "ar" ? "ar-SA" : "en-GB", { dateStyle: "medium" }).format(new Date(value));
}

function PreferenceSelect({ label, value, options, onChange }: { label: string; value: string; options: Array<[string, string]>; onChange: (value: string) => void }) {
  return <label className="preference"><span>{label}</span><select value={value} onChange={(event) => onChange(event.target.value)}>{options.map(([optionValue, optionLabel]) => <option key={optionValue} value={optionValue}>{optionLabel}</option>)}</select></label>;
}

function Brand({ compact = false }: { compact?: boolean }) {
  return <div className="brand"><span className="brand-mark"><span /><span /><span /></span>{!compact && <span className="brand-name">Al Bawaba<br />Logistics</span>}</div>;
}

function Icon({ name }: { name: string }) {
  const paths: Record<string, ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>,
    people: <><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></>,
    route: <><circle cx="6" cy="19" r="2" /><circle cx="18" cy="5" r="2" /><path d="M6 17V9a4 4 0 0 1 4-4h6M18 7v8a4 4 0 0 1-4 4H8" /></>,
    shield: <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />,
    briefcase: <><rect x="3" y="7" width="18" height="13" rx="2" /><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18" /></>,
    home: <><path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1Z" /><path d="M9 21v-6h6v6" /></>,
    layers: <><path d="m12 2 9 5-9 5-9-5 9-5Z" /><path d="m3 12 9 5 9-5M3 17l9 5 9-5" /></>,
    chart: <><path d="M3 3v18h18" /><path d="m7 16 4-5 3 2 5-7" /></>,
    spark: <path d="m12 2-1.7 6.3L4 10l6.3 1.7L12 18l1.7-6.3L20 10l-6.3-1.7L12 2Z" />,
    upload: <><path d="M12 16V3" /><path d="m7 8 5-5 5 5" /><path d="M5 21h14a2 2 0 0 0 2-2v-4M3 15v4a2 2 0 0 0 2 2" /></>,
    server: <><rect x="3" y="3" width="18" height="7" rx="2" /><rect x="3" y="14" width="18" height="7" rx="2" /><path d="M7 6.5h.01M7 17.5h.01M11 6.5h6M11 17.5h6" /></>,
    refresh: <><path d="M20 11a8 8 0 0 0-15-3l-2 2" /><path d="M5 3v5h5" /><path d="M4 13a8 8 0 0 0 15 3l2-2" /><path d="M19 21v-5h-5" /></>,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    check: <path d="m5 12 4 4L19 6" />,
  };
  return <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name] ?? paths.grid}</svg>;
}
