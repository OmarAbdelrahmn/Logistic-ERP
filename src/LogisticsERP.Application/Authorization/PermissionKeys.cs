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
        public const string RolesCreate = "roles.create";
        public const string RolesUpdate = "roles.update";
        public const string RolesDelete = "roles.delete";
        public const string PermissionsRead = "permissions.read";
        public const string PermissionsCreate = "permissions.create";
        public const string PermissionsUpdate = "permissions.update";
        public const string PermissionsDelete = "permissions.delete";
        public const string AuditRead = "audit.read";
        public const string SupportAccessCreate = "support_access.create";
        public const string SupportAccessUpdate = "support_access.update";
        public const string SupportAccessDelete = "support_access.delete";
        public const string SupportAccessRead = "support_access.read";
    }

    public static class Catalog
    {
        public const string CompanyProfileRead = "company_profile.read";
        public const string CompanyProfileCreate = "company_profile.create";
        public const string CompanyProfileUpdate = "company_profile.update";
        public const string CompanyProfileDelete = "company_profile.delete";
        public const string OperatingCitiesRead = "operating_cities.read";
        public const string OperatingCitiesCreate = "operating_cities.create";
        public const string OperatingCitiesUpdate = "operating_cities.update";
        public const string OperatingCitiesDelete = "operating_cities.delete";
        public const string TagsRead = "tags.read";
        public const string TagsCreate = "tags.create";
        public const string TagsUpdate = "tags.update";
        public const string TagsDelete = "tags.delete";
    }

    public static class Workforce
    {
        public const string EmployeesRead = "employees.read";
        public const string EmployeesCreate = "employees.create";
        public const string EmployeesUpdate = "employees.update";
        public const string EmployeesArchive = "employees.archive";
        public const string EmployeesSensitiveRead = "employees.sensitive.read";
        public const string RidersRead = "riders.read";
        public const string RidersCreate = "riders.create";
        public const string RidersUpdate = "riders.update";
        public const string RidersDelete = "riders.delete";
        public const string ExternalRidersRead = "external_riders.read";
        public const string ExternalRidersCreate = "external_riders.create";
        public const string ExternalRidersUpdate = "external_riders.update";
        public const string ExternalRidersDelete = "external_riders.delete";
        public const string SponsorsRead = "sponsors.read";
        public const string SponsorsCreate = "sponsors.create";
        public const string SponsorsUpdate = "sponsors.update";
        public const string SponsorsDelete = "sponsors.delete";
    }

    public static class Compliance
    {
        public const string ResidencyRead = "residency.read";
        public const string ResidencyCreate = "residency.create";
        public const string ResidencyUpdate = "residency.update";
        public const string ResidencyDelete = "residency.delete";
        public const string LicensesRead = "licenses.read";
        public const string LicensesCreate = "licenses.create";
        public const string LicensesUpdate = "licenses.update";
        public const string LicensesDelete = "licenses.delete";
        public const string RiderCardsRead = "rider_cards.read";
        public const string RiderCardsCreate = "rider_cards.create";
        public const string RiderCardsUpdate = "rider_cards.update";
        public const string RiderCardsDelete = "rider_cards.delete";
        public const string HealthCardsRead = "health_cards.read";
        public const string HealthCardsCreate = "health_cards.create";
        public const string HealthCardsUpdate = "health_cards.update";
        public const string HealthCardsDelete = "health_cards.delete";
        public const string InsuranceRead = "insurance.read";
        public const string InsuranceCreate = "insurance.create";
        public const string InsuranceUpdate = "insurance.update";
        public const string InsuranceDelete = "insurance.delete";
        public const string PromissoryNotesRead = "promissory_notes.read";
        public const string PromissoryNotesCreate = "promissory_notes.create";
        public const string PromissoryNotesUpdate = "promissory_notes.update";
        public const string PromissoryNotesDelete = "promissory_notes.delete";
    }

    public static class Documents
    {
        public const string Read = "documents.read";
        public const string Upload = "documents.upload";
        public const string Download = "documents.download";
        public const string DownloadSensitive = "documents.download_sensitive";
        public const string CatalogRead = "documents.catalog.read";
        public const string CatalogCreate = "documents.catalog.create";
        public const string CatalogUpdate = "documents.catalog.update";
        public const string CatalogDelete = "documents.catalog.delete";
    }

    public static class Operations
    {
        public const string PlatformAccountsRead = "platform_accounts.read";
        public const string PlatformAccountsCreate = "platform_accounts.create";
        public const string PlatformAccountsUpdate = "platform_accounts.update";
        public const string PlatformAccountsDelete = "platform_accounts.delete";
        public const string PlatformCredentialsRead = "platform_credentials.read";
        public const string PlatformCredentialsRotate = "platform_credentials.rotate";
        public const string PlatformAssignmentsRead = "platform_assignments.read";
        public const string PlatformAssignmentsCreate = "platform_assignments.create";
        public const string PlatformAssignmentsUpdate = "platform_assignments.update";
        public const string PlatformAssignmentsDelete = "platform_assignments.delete";
        public const string HousingRead = "housing.read";
        public const string HousingCreate = "housing.create";
        public const string HousingUpdate = "housing.update";
        public const string HousingDelete = "housing.delete";
        public const string PhoneSimsRead = "phone_sims.read";
        public const string PhoneSimsCreate = "phone_sims.create";
        public const string PhoneSimsUpdate = "phone_sims.update";
        public const string PhoneSimsDelete = "phone_sims.delete";
    }

    public static class Reporting
    {
        public const string ReportsRead = "reports.read";
        public const string ExportsCreate = "exports.create";
        public const string NotificationsRead = "notifications.read";
        public const string NotificationsCreate = "notifications.create";
        public const string NotificationsUpdate = "notifications.update";
        public const string NotificationsDelete = "notifications.delete";
    }

    public static class Fleet
    {
        public const string VehiclesRead = "fleet.vehicles.read";
        public const string VehiclesCreate = "fleet.vehicles.create";
        public const string VehiclesUpdate = "fleet.vehicles.update";
        public const string VehiclesDelete = "fleet.vehicles.delete";
        public const string VehiclesArchive = "fleet.vehicles.archive";
        public const string VehiclesDecommission = "fleet.vehicles.decommission";
        public const string AssignmentsRead = "fleet.assignments.read";
        public const string AssignmentsCreate = "fleet.assignments.create";
        public const string AssignmentsUpdate = "fleet.assignments.update";
        public const string AssignmentsDelete = "fleet.assignments.delete";
        public const string AssignmentsCorrect = "fleet.assignments.correct";
        public const string IssuesRead = "fleet.issues.read";
        public const string IssuesCreate = "fleet.issues.create";
        public const string IssuesUpdate = "fleet.issues.update";
        public const string IssuesDelete = "fleet.issues.delete";
        public const string ComplianceRead = "fleet.compliance.read";
        public const string ComplianceCreate = "fleet.compliance.create";
        public const string ComplianceUpdate = "fleet.compliance.update";
        public const string ComplianceDelete = "fleet.compliance.delete";
        public const string FilesRead = "fleet.files.read";
        public const string FilesUpload = "fleet.files.upload";
        public const string FilesDownload = "fleet.files.download";
        public const string AccidentsRead = "fleet.accidents.read";
        public const string AccidentsReport = "fleet.accidents.report";
        public const string AccidentsFinalize = "fleet.accidents.finalize";
        public const string AccidentsDownload = "fleet.accidents.download";
        public const string CorrectionsRead = "fleet.corrections.read";
        public const string CorrectionsCreate = "fleet.corrections.create";
        public const string CorrectionsUpdate = "fleet.corrections.update";
        public const string CorrectionsDelete = "fleet.corrections.delete";
        public const string RegistrationTransitionsRead = "fleet.registration_transitions.read";
        public const string RegistrationTransitionsCreate = "fleet.registration_transitions.create";
        public const string RegistrationTransitionsUpdate = "fleet.registration_transitions.update";
        public const string RegistrationTransitionsDelete = "fleet.registration_transitions.delete";
        public const string DailyDistancesRead = "fleet.daily_distances.read";
        public const string DailyDistancesCreate = "fleet.daily_distances.create";
        public const string DailyDistancesUpdate = "fleet.daily_distances.update";
        public const string DailyDistancesDelete = "fleet.daily_distances.delete";
        public const string DailyDistancesImport = "fleet.daily_distances.import";
    }

    public static class Jahez
    {
        public const string Read = "jahez.read";
        public const string HandoversRead = "jahez.handovers.read";
        public const string HandoversCreate = "jahez.handovers.create";
        public const string HandoversUpdate = "jahez.handovers.update";
        public const string HandoversDelete = "jahez.handovers.delete";
        public const string CollectionsRead = "jahez.collections.read";
        public const string CollectionsCreate = "jahez.collections.create";
        public const string CollectionsUpdate = "jahez.collections.update";
        public const string CollectionsDelete = "jahez.collections.delete";
        public const string RequestsCreate = "jahez.requests.create";
        public const string RequestsApprove = "jahez.requests.approve";
        public const string ResetsApprove = "jahez.resets.approve";
        public const string EarningsRead = "jahez.earnings.read";
        public const string EarningsCreate = "jahez.earnings.create";
        public const string EarningsUpdate = "jahez.earnings.update";
        public const string EarningsDelete = "jahez.earnings.delete";
        public const string ImportsCreate = "jahez.imports.create";
        public const string ImportsUpdate = "jahez.imports.update";
        public const string ImportsDelete = "jahez.imports.delete";
        public const string ImportsRead = "jahez.imports.read";
        public const string AdjustmentsRead = "jahez.adjustments.read";
        public const string AdjustmentsCreate = "jahez.adjustments.create";
        public const string AdjustmentsUpdate = "jahez.adjustments.update";
        public const string AdjustmentsDelete = "jahez.adjustments.delete";
        public const string CashboxRead = "jahez.cashbox.read";
        public const string CashboxSubmit = "jahez.cashbox.submit";
        public const string CashboxConfirm = "jahez.cashbox.confirm";
        public const string CashboxApprove = "jahez.cashbox.approve";
    }

    public static class Fuel
    {
        public const string Read = "fuel.read";
        public const string Create = "fuel.create";
        public const string Update = "fuel.update";
        public const string Delete = "fuel.delete";
        public const string Import = "fuel.import";
    }

    public static class Maintenance
    {
        public const string LocationsRead = "maintenance.locations.read";
        public const string LocationsCreate = "maintenance.locations.create";
        public const string LocationsUpdate = "maintenance.locations.update";
        public const string LocationsDelete = "maintenance.locations.delete";
        public const string WorkOrdersRead = "maintenance.work_orders.read";
        public const string WorkOrdersCreate = "maintenance.work_orders.create";
        public const string WorkOrdersUpdate = "maintenance.work_orders.update";
        public const string WorkOrdersDelete = "maintenance.work_orders.delete";
        public const string OilRead = "maintenance.oil.read";
        public const string OilComplete = "maintenance.oil.complete";
        public const string ExternalJobsRead = "maintenance.external_jobs.read";
        public const string ExternalJobsCreate = "maintenance.external_jobs.create";
        public const string ExternalJobsUpdate = "maintenance.external_jobs.update";
        public const string ExternalJobsDelete = "maintenance.external_jobs.delete";
        public const string PartSalesRead = "maintenance.part_sales.read";
        public const string PartSalesCreate = "maintenance.part_sales.create";
        public const string PartSalesUpdate = "maintenance.part_sales.update";
        public const string PartSalesDelete = "maintenance.part_sales.delete";
        public const string CustomerLaborChargesRead = "maintenance.customer_labor_charges.read";
        public const string CustomerLaborChargesCreate = "maintenance.customer_labor_charges.create";
        public const string CustomerLaborChargesUpdate = "maintenance.customer_labor_charges.update";
        public const string CustomerLaborChargesDelete = "maintenance.customer_labor_charges.delete";
        public const string MechanicLaborPaymentsRead = "maintenance.mechanic_labor_payments.read";
        public const string MechanicLaborPaymentsCreate = "maintenance.mechanic_labor_payments.create";
        public const string MechanicLaborPaymentsUpdate = "maintenance.mechanic_labor_payments.update";
        public const string MechanicLaborPaymentsDelete = "maintenance.mechanic_labor_payments.delete";
        public const string ProfitReportsRead = "maintenance.profit_reports.read";
        public const string ProfitReportsExport = "maintenance.profit_reports.export";
    }

    public static class Inventory
    {
        public const string ItemsRead = "inventory.items.read";
        public const string ItemsCreate = "inventory.items.create";
        public const string ItemsUpdate = "inventory.items.update";
        public const string ItemsDelete = "inventory.items.delete";
        public const string StockRead = "inventory.stock.read";
        public const string StockMove = "inventory.stock.move";
        public const string StockAdjust = "inventory.stock.adjust";
        public const string CostLayersRead = "inventory.cost_layers.read";
        public const string ReceiptsCreate = "inventory.receipts.create";
        public const string ReceiptsUpdate = "inventory.receipts.update";
        public const string ReceiptsDelete = "inventory.receipts.delete";
        public const string ReceiptsRead = "inventory.receipts.read";
        public const string ReturnsRead = "inventory.returns.read";
        public const string ReturnsCreate = "inventory.returns.create";
        public const string ReturnsUpdate = "inventory.returns.update";
        public const string ReturnsDelete = "inventory.returns.delete";
        public const string SupplyRequestsSubmit = "inventory.supply_requests.submit";
        public const string SupplyRequestsRead = "inventory.supply_requests.read";
        public const string SupplyRequestsApprove = "inventory.supply_requests.approve";
    }

    public static class Workflows
    {
        public const string LeaveRequestsRead = "leave_requests.read";
        public const string LeaveRequestsCreate = "leave_requests.create";
        public const string LeaveRequestsUpdate = "leave_requests.update";
        public const string LeaveRequestsDelete = "leave_requests.delete";
        public const string LeaveRequestsApprove = "leave_requests.approve";
        public const string AbsenceCasesRead = "absence_cases.read";
        public const string AbsenceCasesCreate = "absence_cases.create";
        public const string AbsenceCasesUpdate = "absence_cases.update";
        public const string AbsenceCasesDelete = "absence_cases.delete";
        public const string EmployeeStatusChangesRead = "employee_status_changes.read";
        public const string EmployeeStatusChangesCreate = "employee_status_changes.create";
        public const string EmployeeStatusChangesUpdate = "employee_status_changes.update";
        public const string EmployeeStatusChangesDelete = "employee_status_changes.delete";
        public const string EmployeeStatusChangesApprove = "employee_status_changes.approve";
        public const string LegalCasesRead = "legal_cases.read";
        public const string LegalCasesCreate = "legal_cases.create";
        public const string LegalCasesUpdate = "legal_cases.update";
        public const string LegalCasesDelete = "legal_cases.delete";
        public const string LegalCaseFilesDownload = "legal_cases.files.download";
    }

    public static class HrForms
    {
        public const string TemplatesRead = "hr_forms.templates.read";
        public const string TemplatesCreate = "hr_forms.templates.create";
        public const string TemplatesUpdate = "hr_forms.templates.update";
        public const string TemplatesDelete = "hr_forms.templates.delete";
    }

    public static FrozenSet<string> All { get; } = new[]
    {
        Security.UsersRead,
        Security.UsersCreate,
        Security.UsersUpdate,
        Security.UsersArchive,
        Security.RolesRead,
        Security.RolesCreate,
        Security.RolesUpdate,
        Security.RolesDelete,
        Security.PermissionsRead,
        Security.PermissionsCreate,
        Security.PermissionsUpdate,
        Security.PermissionsDelete,
        Security.AuditRead,
        Security.SupportAccessCreate,
        Security.SupportAccessUpdate,
        Security.SupportAccessDelete,
        Security.SupportAccessRead,
        Catalog.CompanyProfileRead,
        Catalog.CompanyProfileCreate,
        Catalog.CompanyProfileUpdate,
        Catalog.CompanyProfileDelete,
        Catalog.OperatingCitiesRead,
        Catalog.OperatingCitiesCreate,
        Catalog.OperatingCitiesUpdate,
        Catalog.OperatingCitiesDelete,
        Catalog.TagsRead,
        Catalog.TagsCreate,
        Catalog.TagsUpdate,
        Catalog.TagsDelete,
        Workforce.EmployeesRead,
        Workforce.EmployeesCreate,
        Workforce.EmployeesUpdate,
        Workforce.EmployeesArchive,
        Workforce.EmployeesSensitiveRead,
        Workforce.RidersRead,
        Workforce.RidersCreate,
        Workforce.RidersUpdate,
        Workforce.RidersDelete,
        Workforce.ExternalRidersRead,
        Workforce.ExternalRidersCreate,
        Workforce.ExternalRidersUpdate,
        Workforce.ExternalRidersDelete,
        Workforce.SponsorsRead,
        Workforce.SponsorsCreate,
        Workforce.SponsorsUpdate,
        Workforce.SponsorsDelete,
        Compliance.ResidencyRead,
        Compliance.ResidencyCreate,
        Compliance.ResidencyUpdate,
        Compliance.ResidencyDelete,
        Compliance.LicensesRead,
        Compliance.LicensesCreate,
        Compliance.LicensesUpdate,
        Compliance.LicensesDelete,
        Compliance.RiderCardsRead,
        Compliance.RiderCardsCreate,
        Compliance.RiderCardsUpdate,
        Compliance.RiderCardsDelete,
        Compliance.HealthCardsRead,
        Compliance.HealthCardsCreate,
        Compliance.HealthCardsUpdate,
        Compliance.HealthCardsDelete,
        Compliance.InsuranceRead,
        Compliance.InsuranceCreate,
        Compliance.InsuranceUpdate,
        Compliance.InsuranceDelete,
        Compliance.PromissoryNotesRead,
        Compliance.PromissoryNotesCreate,
        Compliance.PromissoryNotesUpdate,
        Compliance.PromissoryNotesDelete,
        Documents.Read,
        Documents.Upload,
        Documents.Download,
        Documents.DownloadSensitive,
        Documents.CatalogRead,
        Documents.CatalogCreate,
        Documents.CatalogUpdate,
        Documents.CatalogDelete,
        Operations.PlatformAccountsRead,
        Operations.PlatformAccountsCreate,
        Operations.PlatformAccountsUpdate,
        Operations.PlatformAccountsDelete,
        Operations.PlatformCredentialsRead,
        Operations.PlatformCredentialsRotate,
        Operations.PlatformAssignmentsRead,
        Operations.PlatformAssignmentsCreate,
        Operations.PlatformAssignmentsUpdate,
        Operations.PlatformAssignmentsDelete,
        Operations.HousingRead,
        Operations.HousingCreate,
        Operations.HousingUpdate,
        Operations.HousingDelete,
        Operations.PhoneSimsRead,
        Operations.PhoneSimsCreate,
        Operations.PhoneSimsUpdate,
        Operations.PhoneSimsDelete,
        Reporting.ReportsRead,
        Reporting.ExportsCreate,
        Reporting.NotificationsRead,
        Reporting.NotificationsCreate,
        Reporting.NotificationsUpdate,
        Reporting.NotificationsDelete,
        Fleet.VehiclesRead,
        Fleet.VehiclesCreate,
        Fleet.VehiclesUpdate,
        Fleet.VehiclesDelete,
        Fleet.VehiclesArchive,
        Fleet.VehiclesDecommission,
        Fleet.AssignmentsRead,
        Fleet.AssignmentsCreate,
        Fleet.AssignmentsUpdate,
        Fleet.AssignmentsDelete,
        Fleet.AssignmentsCorrect,
        Fleet.IssuesRead,
        Fleet.IssuesCreate,
        Fleet.IssuesUpdate,
        Fleet.IssuesDelete,
        Fleet.ComplianceRead,
        Fleet.ComplianceCreate,
        Fleet.ComplianceUpdate,
        Fleet.ComplianceDelete,
        Fleet.FilesRead,
        Fleet.FilesUpload,
        Fleet.FilesDownload,
        Fleet.AccidentsRead,
        Fleet.AccidentsReport,
        Fleet.AccidentsFinalize,
        Fleet.AccidentsDownload,
        Fleet.CorrectionsRead,
        Fleet.CorrectionsCreate,
        Fleet.CorrectionsUpdate,
        Fleet.CorrectionsDelete,
        Fleet.RegistrationTransitionsRead,
        Fleet.RegistrationTransitionsCreate,
        Fleet.RegistrationTransitionsUpdate,
        Fleet.RegistrationTransitionsDelete,
        Fleet.DailyDistancesRead,
        Fleet.DailyDistancesCreate,
        Fleet.DailyDistancesUpdate,
        Fleet.DailyDistancesDelete,
        Fleet.DailyDistancesImport,
        Jahez.Read,
        Jahez.HandoversRead,
        Jahez.HandoversCreate,
        Jahez.HandoversUpdate,
        Jahez.HandoversDelete,
        Jahez.CollectionsRead,
        Jahez.CollectionsCreate,
        Jahez.CollectionsUpdate,
        Jahez.CollectionsDelete,
        Jahez.RequestsCreate,
        Jahez.RequestsApprove,
        Jahez.ResetsApprove,
        Jahez.EarningsRead,
        Jahez.EarningsCreate,
        Jahez.EarningsUpdate,
        Jahez.EarningsDelete,
        Jahez.ImportsCreate,
        Jahez.ImportsUpdate,
        Jahez.ImportsDelete,
        Jahez.ImportsRead,
        Jahez.AdjustmentsRead,
        Jahez.AdjustmentsCreate,
        Jahez.AdjustmentsUpdate,
        Jahez.AdjustmentsDelete,
        Jahez.CashboxRead,
        Jahez.CashboxSubmit,
        Jahez.CashboxConfirm,
        Jahez.CashboxApprove,
        Fuel.Read,
        Fuel.Create,
        Fuel.Update,
        Fuel.Delete,
        Fuel.Import,
        Maintenance.LocationsRead,
        Maintenance.LocationsCreate,
        Maintenance.LocationsUpdate,
        Maintenance.LocationsDelete,
        Maintenance.WorkOrdersRead,
        Maintenance.WorkOrdersCreate,
        Maintenance.WorkOrdersUpdate,
        Maintenance.WorkOrdersDelete,
        Maintenance.OilRead,
        Maintenance.OilComplete,
        Maintenance.ExternalJobsRead,
        Maintenance.ExternalJobsCreate,
        Maintenance.ExternalJobsUpdate,
        Maintenance.ExternalJobsDelete,
        Maintenance.PartSalesRead,
        Maintenance.PartSalesCreate,
        Maintenance.PartSalesUpdate,
        Maintenance.PartSalesDelete,
        Maintenance.CustomerLaborChargesRead,
        Maintenance.CustomerLaborChargesCreate,
        Maintenance.CustomerLaborChargesUpdate,
        Maintenance.CustomerLaborChargesDelete,
        Maintenance.MechanicLaborPaymentsRead,
        Maintenance.MechanicLaborPaymentsCreate,
        Maintenance.MechanicLaborPaymentsUpdate,
        Maintenance.MechanicLaborPaymentsDelete,
        Maintenance.ProfitReportsRead,
        Maintenance.ProfitReportsExport,
        Inventory.ItemsRead,
        Inventory.ItemsCreate,
        Inventory.ItemsUpdate,
        Inventory.ItemsDelete,
        Inventory.StockRead,
        Inventory.StockMove,
        Inventory.StockAdjust,
        Inventory.CostLayersRead,
        Inventory.ReceiptsCreate,
        Inventory.ReceiptsUpdate,
        Inventory.ReceiptsDelete,
        Inventory.ReceiptsRead,
        Inventory.ReturnsRead,
        Inventory.ReturnsCreate,
        Inventory.ReturnsUpdate,
        Inventory.ReturnsDelete,
        Inventory.SupplyRequestsSubmit,
        Inventory.SupplyRequestsRead,
        Inventory.SupplyRequestsApprove,
        Workflows.LeaveRequestsRead,
        Workflows.LeaveRequestsCreate,
        Workflows.LeaveRequestsUpdate,
        Workflows.LeaveRequestsDelete,
        Workflows.LeaveRequestsApprove,
        Workflows.AbsenceCasesRead,
        Workflows.AbsenceCasesCreate,
        Workflows.AbsenceCasesUpdate,
        Workflows.AbsenceCasesDelete,
        Workflows.EmployeeStatusChangesRead,
        Workflows.EmployeeStatusChangesCreate,
        Workflows.EmployeeStatusChangesUpdate,
        Workflows.EmployeeStatusChangesDelete,
        Workflows.EmployeeStatusChangesApprove,
        Workflows.LegalCasesRead,
        Workflows.LegalCasesCreate,
        Workflows.LegalCasesUpdate,
        Workflows.LegalCasesDelete,
        Workflows.LegalCaseFilesDownload,
        HrForms.TemplatesRead,
        HrForms.TemplatesCreate,
        HrForms.TemplatesUpdate,
        HrForms.TemplatesDelete
    }.ToFrozenSet(StringComparer.Ordinal);
}
