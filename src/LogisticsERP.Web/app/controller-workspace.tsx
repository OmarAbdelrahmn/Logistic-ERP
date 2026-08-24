"use client";

import { FormEvent, useState } from "react";
import { invokeControllerEndpoint } from "../lib/api";
import { controllerAreas, resolvePath, routeParameters, type ControllerEndpoint } from "../lib/controller-catalog";
import type { Locale } from "../lib/types";
import type { Translation } from "../lib/locales";

type ApiResponse = { value?: unknown; message?: string; error?: string };

export function ControllerWorkspace({ accessToken, permissions, locale, t }: { accessToken: string; permissions: string[]; locale: Locale; t: Translation }) {
  const [controllerId, setControllerId] = useState(controllerAreas[0].id);
  const controller = controllerAreas.find((area) => area.id === controllerId) ?? controllerAreas[0];
  const [endpointId, setEndpointId] = useState(controller.endpoints[0].id);
  const endpoint = controller.endpoints.find((item) => item.id === endpointId) ?? controller.endpoints[0];
  const [routeValues, setRouteValues] = useState<Record<string, string>>({});
  const [query, setQuery] = useState("");
  const [body, setBody] = useState("{}");
  const [file, setFile] = useState<File | null>(null);
  const [response, setResponse] = useState<ApiResponse | null>(null);
  const [isRunning, setIsRunning] = useState(false);
  const permitted = !endpoint.permission || permissions.includes(endpoint.permission);
  const parameters = routeParameters(endpoint.path);

  function selectController(value: string) {
    const next = controllerAreas.find((area) => area.id === value) ?? controllerAreas[0];
    setControllerId(next.id); setEndpointId(next.endpoints[0].id); resetRequest();
  }

  function selectEndpoint(value: string) {
    setEndpointId(value); resetRequest();
  }

  function resetRequest() {
    setRouteValues({}); setQuery(""); setBody("{}"); setFile(null); setResponse(null);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!permitted) return;
    if (parameters.some((parameter) => !routeValues[parameter]?.trim())) {
      setResponse({ error: t.parameterRequired }); return;
    }
    if (endpoint.content === "file" && !file) {
      setResponse({ error: t.fileRequired }); return;
    }
    let parsedBody: unknown = {};
    try {
      parsedBody = body.trim() ? JSON.parse(body) : {};
    } catch {
      setResponse({ error: t.invalidJson }); return;
    }
    setIsRunning(true); setResponse(null);
    try {
      const path = `${resolvePath(endpoint.path, routeValues)}${query.trim() ? `${query.trim().startsWith("?") ? "" : "?"}${query.trim()}` : ""}`;
      const value = await invokeControllerEndpoint(accessToken, endpoint, path, parsedBody, file);
      if (isDownload(value)) {
        const url = URL.createObjectURL(value.download.blob);
        const link = document.createElement("a"); link.href = url; link.download = value.download.name; link.click(); URL.revokeObjectURL(url);
        setResponse({ message: t.downloadComplete });
      } else {
        setResponse({ value, message: endpoint.method === "GET" ? undefined : t.actionComplete });
      }
    } catch (error) {
      setResponse({ error: error instanceof Error ? error.message : t.noData });
    } finally { setIsRunning(false); }
  }

  return <section className="controller-workspace" aria-label={t.apiWorkspace}>
    <div className="controller-layout">
      <aside className="controller-list" aria-label={t.apiController}>
        {controllerAreas.map((area) => <button key={area.id} type="button" className={area.id === controller.id ? "is-selected" : ""} onClick={() => selectController(area.id)}><span>{area.label[locale]}</span><small>{area.endpoints.length}</small></button>)}
      </aside>
      <form className="controller-card content-card" onSubmit={(event) => void submit(event)}>
        <div className="card-heading"><div><p className="eyebrow">{t.apiWorkspace}</p><h2>{controller.label[locale]}</h2></div><span className={`api-method ${endpoint.method.toLowerCase()}`}>{endpoint.method}</span></div>
        <p className="controller-description">{t.apiWorkspaceDescription}</p>
        <div className="controller-fields">
          <label><span>{t.apiEndpoint}</span><select value={endpoint.id} onChange={(event) => selectEndpoint(event.target.value)}>{controller.endpoints.map((item) => <option key={item.id} value={item.id}>{item.method} · {item.label[locale]}</option>)}</select></label>
          <div className="endpoint-path"><strong>{endpoint.label[locale]}</strong><code>{endpoint.path}</code></div>
          <p className={`endpoint-access ${permitted ? "is-permitted" : ""}`}><span>{t.endpointAccess}:</span> {endpoint.permission ?? "Authenticated user"}</p>
          {parameters.length > 0 && <fieldset><legend>{t.routeParameters}</legend><div className="route-parameters">{parameters.map((parameter) => <label key={parameter}><span>{parameter}</span><input value={routeValues[parameter] ?? ""} onChange={(event) => setRouteValues((current) => ({ ...current, [parameter]: event.target.value }))} required placeholder="GUID or value" /></label>)}</div></fieldset>}
          {endpoint.method === "GET" && <label><span>{t.optionalQuery}</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="employeeId=…&currentOnly=true" /></label>}
          {endpoint.method !== "GET" && <label><span>{t.requestBody}</span><textarea value={body} onChange={(event) => setBody(event.target.value)} spellCheck={false} rows={8} /></label>}
          {endpoint.content === "file" && <label className="controller-file"><span>{t.selectFile}</span><input type="file" onChange={(event) => setFile(event.target.files?.[0] ?? null)} required /><small>{t.fileUploadHint}</small></label>}
        </div>
        <button className="primary-button controller-submit" disabled={!permitted || isRunning} type="submit">{isRunning ? t.runningAction : endpoint.method === "GET" ? t.loadEndpoint : t.runAction}</button>
        {!permitted && <p className="form-error" role="alert">{t.accessRequired}</p>}
      </form>
    </div>
    {response && <article className={`controller-response content-card ${response.error ? "is-error" : ""}`} aria-live="polite"><div className="card-heading"><div><p className="eyebrow">{t.response}</p><h2>{response.error ?? response.message ?? endpoint.label[locale]}</h2></div></div>{response.value !== undefined && (endpoint.id === "employees" && isEmployeeList(response.value) ? <EmployeesTable employees={response.value} locale={locale} t={t} /> : <pre>{JSON.stringify(response.value, null, 2)}</pre>)}</article>}
  </section>;
}

type EmployeeRecord = {
  id: string; iqamaNo: string | null; fullNameAr: string; fullNameEn: string | null; primaryPhone: string | null;
  nationality: string | null; isEmployee: boolean; engagementType: string; status: string; riderProfileId: string | null;
  workingForMeAs: string | null; residencyProfession: string | null; sponsorNameAr: string | null;
};

function isEmployeeList(value: unknown): value is EmployeeRecord[] {
  return Array.isArray(value) && value.every((item) => Boolean(item && typeof item === "object" && "iqamaNo" in item && "fullNameAr" in item));
}

function EmployeesTable({ employees, locale, t }: { employees: EmployeeRecord[]; locale: Locale; t: Translation }) {
  return <div className="table-wrap employee-response-table"><table className="resource-table"><thead><tr><th>{t.employee}</th><th>{t.jobAndWork}</th><th>{t.relationshipAndSponsor}</th><th>{t.riderProfile}</th><th>{t.contactAndNationality}</th><th>{t.status}</th></tr></thead><tbody>{employees.map((employee) => <tr key={employee.id}><td><strong>{locale === "ar" ? employee.fullNameAr : employee.fullNameEn || employee.fullNameAr}</strong><span>{t.iqamaNo}: {employee.iqamaNo ?? "—"}</span></td><td><strong>{employee.workingForMeAs ?? "—"}</strong><span>{employee.residencyProfession ?? "—"}</span></td><td><strong>{employee.engagementType}</strong><span>{employee.sponsorNameAr ?? "—"}</span></td><td><strong>{employee.isEmployee ? (locale === "ar" ? "إداري" : "Administrative") : t.rider}</strong><span>{employee.riderProfileId ?? "—"}</span></td><td><strong dir="ltr">{employee.primaryPhone ?? "—"}</strong><span>{employee.nationality ?? "—"}</span></td><td><span className={`status-chip ${isActive(employee.status) ? "is-good" : ""}`}>{employee.status}</span></td></tr>)}</tbody></table></div>;
}

function isActive(status: string) {
  return status.trim().toLowerCase() === "active";
}

function isDownload(value: unknown): value is { download: { blob: Blob; name: string } } {
  return Boolean(value && typeof value === "object" && "download" in value);
}
