using System.Collections.Frozen;

namespace LogisticsERP.Application.Authorization;

public static class PermissionKeys
{
    public static class Security
    {
        public const string UsersRead = "users.read";
        public const string UsersCreate = "users.create";
        public const string UsersUpdate = "users.update";
        public const string UsersArchive = "users.archive";
        public const string RolesRead = "roles.read";
        public const string RolesManage = "roles.manage";
        public const string PermissionsRead = "permissions.read";
        public const string PermissionsManage = "permissions.manage";
        public const string AuditRead = "audit.read";
        public const string SupportAccessManage = "support_access.manage";
    }

    public static class Catalog
    {
        public const string CompanyProfileRead = "company_profile.read";
        public const string CompanyProfileManage = "company_profile.manage";
        public const string OperatingCitiesRead = "operating_cities.read";
        public const string OperatingCitiesManage = "operating_cities.manage";
        public const string TagsRead = "tags.read";
        public const string TagsManage = "tags.manage";
    }

    public static class Workforce
    {
        public const string EmployeesRead = "employees.read";
        public const string EmployeesCreate = "employees.create";
        public const string EmployeesUpdate = "employees.update";
        public const string EmployeesArchive = "employees.archive";
        public const string EmployeesSensitiveRead = "employees.sensitive.read";
        public const string RidersRead = "riders.read";
        public const string RidersManage = "riders.manage";
        public const string ExternalRidersRead = "external_riders.read";
        public const string ExternalRidersCreate = "external_riders.create";
        public const string ExternalRidersUpdate = "external_riders.update";
        public const string ExternalRidersDelete = "external_riders.delete";
        public const string SponsorsRead = "sponsors.read";
        public const string SponsorsManage = "sponsors.manage";
    }

    public static class Compliance
    {
        public const string ResidencyRead = "residency.read";
        public const string ResidencyManage = "residency.manage";
        public const string LicensesRead = "licenses.read";
        public const string LicensesManage = "licenses.manage";
        public const string RiderCardsRead = "rider_cards.read";
        public const string RiderCardsManage = "rider_cards.manage";
        public const string HealthCardsRead = "health_cards.read";
        public const string HealthCardsManage = "health_cards.manage";
        public const string InsuranceRead = "insurance.read";
        public const string InsuranceManage = "insurance.manage";
        public const string PromissoryNotesRead = "promissory_notes.read";
        public const string PromissoryNotesManage = "promissory_notes.manage";
    }

    public static class Documents
    {
        public const string Read = "documents.read";
        public const string Upload = "documents.upload";
        public const string Download = "documents.download";
        public const string DownloadSensitive = "documents.download_sensitive";
        public const string CatalogManage = "documents.catalog.manage";
    }

    public static class Operations
    {
        public const string PlatformAccountsRead = "platform_accounts.read";
        public const string PlatformAccountsManage = "platform_accounts.manage";
        public const string PlatformCredentialsRead = "platform_credentials.read";
        public const string PlatformCredentialsRotate = "platform_credentials.rotate";
        public const string PlatformAssignmentsRead = "platform_assignments.read";
        public const string PlatformAssignmentsManage = "platform_assignments.manage";
        public const string HousingRead = "housing.read";
        public const string HousingManage = "housing.manage";
        public const string PhoneSimsRead = "phone_sims.read";
        public const string PhoneSimsManage = "phone_sims.manage";
    }

    public static class Reporting
    {
        public const string ReportsRead = "reports.read";
        public const string ExportsCreate = "exports.create";
        public const string NotificationsRead = "notifications.read";
        public const string NotificationsManage = "notifications.manage";
    }

    public static class Fleet
    {
        public const string VehiclesRead = "fleet.vehicles.read";
        public const string VehiclesManage = "fleet.vehicles.manage";
        public const string VehiclesArchive = "fleet.vehicles.archive";
        public const string VehiclesDecommission = "fleet.vehicles.decommission";
        public const string AssignmentsRead = "fleet.assignments.read";
        public const string AssignmentsManage = "fleet.assignments.manage";
        public const string AssignmentsCorrect = "fleet.assignments.correct";
        public const string IssuesRead = "fleet.issues.read";
        public const string IssuesManage = "fleet.issues.manage";
        public const string ComplianceRead = "fleet.compliance.read";
        public const string ComplianceManage = "fleet.compliance.manage";
        public const string FilesRead = "fleet.files.read";
        public const string FilesUpload = "fleet.files.upload";
        public const string FilesDownload = "fleet.files.download";
        public const string AccidentsRead = "fleet.accidents.read";
        public const string AccidentsReport = "fleet.accidents.report";
        public const string AccidentsFinalize = "fleet.accidents.finalize";
        public const string AccidentsDownload = "fleet.accidents.download";
        public const string CorrectionsManage = "fleet.corrections.manage";
        public const string RegistrationTransitionsManage = "fleet.registration_transitions.manage";
        public const string DailyDistancesRead = "fleet.daily_distances.read";
        public const string DailyDistancesManage = "fleet.daily_distances.manage";
        public const string DailyDistancesImport = "fleet.daily_distances.import";
    }

    public static class Jahez
    {
        public const string Read = "jahez.read";
        public const string HandoversManage = "jahez.handovers.manage";
        public const string CollectionsManage = "jahez.collections.manage";
        public const string RequestsCreate = "jahez.requests.create";
        public const string RequestsApprove = "jahez.requests.approve";
        public const string ResetsApprove = "jahez.resets.approve";
        public const string EarningsManage = "jahez.earnings.manage";
        public const string ImportsManage = "jahez.imports.manage";
        public const string AdjustmentsManage = "jahez.adjustments.manage";
        public const string CashboxRead = "jahez.cashbox.read";
        public const string CashboxSubmit = "jahez.cashbox.submit";
        public const string CashboxConfirm = "jahez.cashbox.confirm";
        public const string CashboxApprove = "jahez.cashbox.approve";
    }

    public static class Fuel
    {
        public const string Read = "fuel.read";
        public const string Manage = "fuel.manage";
        public const string Import = "fuel.import";
    }

    public static class Maintenance
    {
        public const string LocationsRead = "maintenance.locations.read";
        public const string LocationsManage = "maintenance.locations.manage";
        public const string WorkOrdersRead = "maintenance.work_orders.read";
        public const string WorkOrdersCreate = "maintenance.work_orders.create";
        public const string WorkOrdersUpdate = "maintenance.work_orders.update";
        public const string WorkOrdersDelete = "maintenance.work_orders.delete";
        public const string OilRead = "maintenance.oil.read";
        public const string OilComplete = "maintenance.oil.complete";
        public const string ExternalJobsRead = "maintenance.external_jobs.read";
        public const string ExternalJobsManage = "maintenance.external_jobs.manage";
        public const string PartSalesManage = "maintenance.part_sales.manage";
        public const string CustomerLaborChargesManage = "maintenance.customer_labor_charges.manage";
        public const string MechanicLaborPaymentsManage = "maintenance.mechanic_labor_payments.manage";
        public const string ProfitReportsRead = "maintenance.profit_reports.read";
        public const string ProfitReportsExport = "maintenance.profit_reports.export";
    }

    public static class Inventory
    {
        public const string ItemsRead = "inventory.items.read";
        public const string ItemsManage = "inventory.items.manage";
        public const string StockRead = "inventory.stock.read";
        public const string StockMove = "inventory.stock.move";
        public const string StockAdjust = "inventory.stock.adjust";
        public const string CostLayersRead = "inventory.cost_layers.read";
        public const string ReceiptsManage = "inventory.receipts.manage";
        public const string ReturnsManage = "inventory.returns.manage";
        public const string SupplyRequestsSubmit = "inventory.supply_requests.submit";
        public const string SupplyRequestsRead = "inventory.supply_requests.read";
        public const string SupplyRequestsApprove = "inventory.supply_requests.approve";
    }

    public static class Workflows
    {
        public const string LeaveRequestsRead = "leave_requests.read";
        public const string LeaveRequestsManage = "leave_requests.manage";
        public const string LeaveRequestsApprove = "leave_requests.approve";
        public const string AbsenceCasesRead = "absence_cases.read";
        public const string AbsenceCasesManage = "absence_cases.manage";
        public const string EmployeeStatusChangesRead = "employee_status_changes.read";
        public const string EmployeeStatusChangesManage = "employee_status_changes.manage";
        public const string EmployeeStatusChangesApprove = "employee_status_changes.approve";
        public const string LegalCasesRead = "legal_cases.read";
        public const string LegalCasesManage = "legal_cases.manage";
        public const string LegalCaseFilesDownload = "legal_cases.files.download";
    }

    public static class HrForms
    {
        public const string TemplatesRead = "hr_forms.templates.read";
        public const string TemplatesManage = "hr_forms.templates.manage";
    }

    public static FrozenSet<string> All { get; } = new[]
    {
        Security.UsersRead,
        Security.UsersCreate,
        Security.UsersUpdate,
        Security.UsersArchive,
        Security.RolesRead,
        Security.RolesManage,
        Security.PermissionsRead,
        Security.PermissionsManage,
        Security.AuditRead,
        Security.SupportAccessManage,
        Catalog.CompanyProfileRead,
        Catalog.CompanyProfileManage,
        Catalog.OperatingCitiesRead,
        Catalog.OperatingCitiesManage,
        Catalog.TagsRead,
        Catalog.TagsManage,
        Workforce.EmployeesRead,
        Workforce.EmployeesCreate,
        Workforce.EmployeesUpdate,
        Workforce.EmployeesArchive,
        Workforce.EmployeesSensitiveRead,
        Workforce.RidersRead,
        Workforce.RidersManage,
        Workforce.ExternalRidersRead,
        Workforce.ExternalRidersCreate,
        Workforce.ExternalRidersUpdate,
        Workforce.ExternalRidersDelete,
        Workforce.SponsorsRead,
        Workforce.SponsorsManage,
        Compliance.ResidencyRead,
        Compliance.ResidencyManage,
        Compliance.LicensesRead,
        Compliance.LicensesManage,
        Compliance.RiderCardsRead,
        Compliance.RiderCardsManage,
        Compliance.HealthCardsRead,
        Compliance.HealthCardsManage,
        Compliance.InsuranceRead,
        Compliance.InsuranceManage,
        Compliance.PromissoryNotesRead,
        Compliance.PromissoryNotesManage,
        Documents.Read,
        Documents.Upload,
        Documents.Download,
        Documents.DownloadSensitive,
        Documents.CatalogManage,
        Operations.PlatformAccountsRead,
        Operations.PlatformAccountsManage,
        Operations.PlatformCredentialsRead,
        Operations.PlatformCredentialsRotate,
        Operations.PlatformAssignmentsRead,
        Operations.PlatformAssignmentsManage,
        Operations.HousingRead,
        Operations.HousingManage,
        Operations.PhoneSimsRead,
        Operations.PhoneSimsManage,
        Reporting.ReportsRead,
        Reporting.ExportsCreate,
        Reporting.NotificationsRead,
        Reporting.NotificationsManage,
        Fleet.VehiclesRead,
        Fleet.VehiclesManage,
        Fleet.VehiclesArchive,
        Fleet.VehiclesDecommission,
        Fleet.AssignmentsRead,
        Fleet.AssignmentsManage,
        Fleet.AssignmentsCorrect,
        Fleet.IssuesRead,
        Fleet.IssuesManage,
        Fleet.ComplianceRead,
        Fleet.ComplianceManage,
        Fleet.FilesRead,
        Fleet.FilesUpload,
        Fleet.FilesDownload,
        Fleet.AccidentsRead,
        Fleet.AccidentsReport,
        Fleet.AccidentsFinalize,
        Fleet.AccidentsDownload,
        Fleet.CorrectionsManage,
        Fleet.RegistrationTransitionsManage,
        Fleet.DailyDistancesRead,
        Fleet.DailyDistancesManage,
        Fleet.DailyDistancesImport,
        Jahez.Read,
        Jahez.HandoversManage,
        Jahez.CollectionsManage,
        Jahez.RequestsCreate,
        Jahez.RequestsApprove,
        Jahez.ResetsApprove,
        Jahez.EarningsManage,
        Jahez.ImportsManage,
        Jahez.AdjustmentsManage,
        Jahez.CashboxRead,
        Jahez.CashboxSubmit,
        Jahez.CashboxConfirm,
        Jahez.CashboxApprove,
        Fuel.Read,
        Fuel.Manage,
        Fuel.Import,
        Maintenance.LocationsRead,
        Maintenance.LocationsManage,
        Maintenance.WorkOrdersRead,
        Maintenance.WorkOrdersCreate,
        Maintenance.WorkOrdersUpdate,
        Maintenance.WorkOrdersDelete,
        Maintenance.OilRead,
        Maintenance.OilComplete,
        Maintenance.ExternalJobsRead,
        Maintenance.ExternalJobsManage,
        Maintenance.PartSalesManage,
        Maintenance.CustomerLaborChargesManage,
        Maintenance.MechanicLaborPaymentsManage,
        Maintenance.ProfitReportsRead,
        Maintenance.ProfitReportsExport,
        Inventory.ItemsRead,
        Inventory.ItemsManage,
        Inventory.StockRead,
        Inventory.StockMove,
        Inventory.StockAdjust,
        Inventory.CostLayersRead,
        Inventory.ReceiptsManage,
        Inventory.ReturnsManage,
        Inventory.SupplyRequestsSubmit,
        Inventory.SupplyRequestsRead,
        Inventory.SupplyRequestsApprove,
        Workflows.LeaveRequestsRead,
        Workflows.LeaveRequestsManage,
        Workflows.LeaveRequestsApprove,
        Workflows.AbsenceCasesRead,
        Workflows.AbsenceCasesManage,
        Workflows.EmployeeStatusChangesRead,
        Workflows.EmployeeStatusChangesManage,
        Workflows.EmployeeStatusChangesApprove,
        Workflows.LegalCasesRead,
        Workflows.LegalCasesManage,
        Workflows.LegalCaseFilesDownload,
        HrForms.TemplatesRead,
        HrForms.TemplatesManage
    }.ToFrozenSet(StringComparer.Ordinal);
}
