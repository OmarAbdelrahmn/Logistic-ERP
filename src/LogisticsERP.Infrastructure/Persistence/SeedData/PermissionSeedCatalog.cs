using LogisticsERP.Application.Authorization;

namespace LogisticsERP.Infrastructure.Persistence.SeedData;

internal sealed record PermissionSeed(
    Guid Id,
    string Key,
    string Category,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    bool RequiresHousingScope,
    bool RequiresClientScope,
    bool IsSensitive,
    bool IsHighTrust,
    string? GrantabilityRule,
    int DisplayOrder);

internal static class PermissionSeedCatalog
{
    public static IReadOnlyList<PermissionSeed> All { get; } =
    [
        Create(1, PermissionKeys.Security.UsersRead, "Security", "عرض المستخدمين", "Read users", "عرض حسابات المستخدمين وحالتها.", "View user accounts and their status.", sensitive: true),
        Create(2, PermissionKeys.Security.UsersCreate, "Security", "إنشاء المستخدمين", "Create users", "إنشاء حسابات مستخدمين جديدة.", "Create new user accounts.", sensitive: true, highTrust: true),
        Create(3, PermissionKeys.Security.UsersUpdate, "Security", "تعديل المستخدمين", "Update users", "تعديل حالة وبيانات حسابات المستخدمين.", "Update user account details and status.", sensitive: true, highTrust: true),
        Create(4, PermissionKeys.Security.UsersArchive, "Security", "أرشفة المستخدمين", "Archive users", "أرشفة حساب مستخدم وإبطال جلساته دون حذف بياناته.", "Archive a user and revoke sessions without deleting records.", sensitive: true, highTrust: true),
        Create(5, PermissionKeys.Security.RolesRead, "Security", "عرض الأدوار", "Read roles", "عرض الأدوار وقوالب الصلاحيات.", "View roles and permission templates."),
        Create(6, PermissionKeys.Security.RolesManage, "Security", "إدارة الأدوار", "Manage roles", "إدارة الأدوار غير المحمية وتعيينها للمستخدمين.", "Manage non-protected roles and user role assignments.", highTrust: true),
        Create(7, PermissionKeys.Security.PermissionsRead, "Security", "عرض الصلاحيات", "Read permissions", "عرض كتالوج الصلاحيات والمنح والمنع.", "View the permission catalog, grants, and denies."),
        Create(8, PermissionKeys.Security.PermissionsManage, "Security", "إدارة الصلاحيات", "Manage permissions", "إدارة منح ومنع الصلاحيات ونطاقاتها.", "Manage permission grants, denies, and scopes.", highTrust: true),
        Create(9, PermissionKeys.Security.AuditRead, "Security", "عرض سجل التدقيق", "Read audit log", "عرض سجل التدقيق الأمني والتشغيلي.", "View security and operational audit records.", sensitive: true, highTrust: true),
        Create(10, PermissionKeys.Security.SupportAccessManage, "Security", "إدارة وصول الدعم", "Manage support access", "إدارة وصول الدعم المؤقت وحالات الطوارئ.", "Manage temporary and break-glass support access.", sensitive: true, highTrust: true),

        Create(11, PermissionKeys.Catalog.OperatingCitiesRead, "Catalog", "عرض المدن التشغيلية", "Read operating cities", "عرض المدن والفروع التشغيلية.", "View operating cities and branches."),
        Create(12, PermissionKeys.Catalog.OperatingCitiesManage, "Catalog", "إدارة المدن التشغيلية", "Manage operating cities", "إضافة وتعديل وتعطيل المدن التشغيلية.", "Add, update, and disable operating cities."),

        Create(13, PermissionKeys.Workforce.EmployeesRead, "Workforce", "عرض الموظفين", "Read employees", "عرض بيانات الموظفين غير الحساسة.", "View non-sensitive employee data."),
        Create(14, PermissionKeys.Workforce.EmployeesCreate, "Workforce", "إنشاء الموظفين", "Create employees", "إنشاء سجلات موظفين جديدة.", "Create new employee records."),
        Create(15, PermissionKeys.Workforce.EmployeesUpdate, "Workforce", "تعديل الموظفين", "Update employees", "تعديل بيانات الموظفين التشغيلية.", "Update operational employee data."),
        Create(16, PermissionKeys.Workforce.EmployeesArchive, "Workforce", "أرشفة الموظفين", "Archive employees", "أرشفة الموظفين دون حذف تاريخهم.", "Archive employees without deleting their history.", highTrust: true),
        Create(17, PermissionKeys.Workforce.EmployeesSensitiveRead, "Workforce", "عرض بيانات الموظفين الحساسة", "Read sensitive employee data", "عرض الهوية والإقامة والبيانات الشخصية المقيدة.", "View restricted identity, residency, and personal data.", sensitive: true, highTrust: true),
        Create(18, PermissionKeys.Workforce.RidersRead, "Workforce", "عرض المناديب", "Read riders", "عرض ملفات المناديب وبياناتهم التشغيلية.", "View rider profiles and operational data."),
        Create(19, PermissionKeys.Workforce.RidersManage, "Workforce", "إدارة المناديب", "Manage riders", "إنشاء وتعديل حالات وملفات المناديب.", "Create and update rider profiles and status."),
        Create(20, PermissionKeys.Workforce.SponsorsRead, "Workforce", "عرض الكفلاء", "Read sponsors", "عرض جهات الكفالة وبيانات السجل.", "View sponsors and registry information.", sensitive: true),
        Create(21, PermissionKeys.Workforce.SponsorsManage, "Workforce", "إدارة الكفلاء", "Manage sponsors", "إدارة جهات الكفالة وفترات كفالة الموظفين.", "Manage sponsors and employee sponsorship periods.", sensitive: true),

        Create(22, PermissionKeys.Compliance.ResidencyRead, "Compliance", "عرض الإقامات", "Read residency permits", "عرض بيانات الإقامة المقيدة.", "View restricted residency permit data.", sensitive: true),
        Create(23, PermissionKeys.Compliance.ResidencyManage, "Compliance", "إدارة الإقامات", "Manage residency permits", "إضافة وتجديد وتحديث حالات الإقامة.", "Add, renew, and update residency permit status.", sensitive: true),
        Create(24, PermissionKeys.Compliance.LicensesRead, "Compliance", "عرض الرخص", "Read driver licenses", "عرض رخص القيادة وإصداراتها.", "View driver licenses and their versions.", sensitive: true),
        Create(25, PermissionKeys.Compliance.LicensesManage, "Compliance", "إدارة الرخص", "Manage driver licenses", "إدارة إصدار وتجديد وحالة رخص القيادة.", "Manage driver-license issuance, renewal, and status.", sensitive: true),
        Create(26, PermissionKeys.Compliance.RiderCardsRead, "Compliance", "عرض بطاقات السائق", "Read rider cards", "عرض بطاقات السائق وتجديداتها.", "View rider cards and renewals.", sensitive: true),
        Create(27, PermissionKeys.Compliance.RiderCardsManage, "Compliance", "إدارة بطاقات السائق", "Manage rider cards", "إدارة إصدار وتجديد بطاقات السائق.", "Manage rider-card issuance and renewal.", sensitive: true),
        Create(28, PermissionKeys.Compliance.HealthCardsRead, "Compliance", "عرض البطاقات الصحية", "Read health cards", "عرض البطاقات الصحية وتجديداتها.", "View health cards and renewals.", sensitive: true),
        Create(29, PermissionKeys.Compliance.HealthCardsManage, "Compliance", "إدارة البطاقات الصحية", "Manage health cards", "إدارة إصدار وتجديد البطاقات الصحية.", "Manage health-card issuance and renewal.", sensitive: true),
        Create(30, PermissionKeys.Compliance.InsuranceRead, "Compliance", "عرض التأمين الطبي", "Read medical insurance", "عرض وثائق ومستويات التأمين الطبي.", "View medical-insurance policies and plan levels.", sensitive: true),
        Create(31, PermissionKeys.Compliance.InsuranceManage, "Compliance", "إدارة التأمين الطبي", "Manage medical insurance", "إدارة وثائق وتجديدات ومستويات التأمين الطبي.", "Manage medical-insurance policies, renewals, and levels.", sensitive: true),
        Create(32, PermissionKeys.Compliance.PromissoryNotesRead, "Compliance", "عرض سندات الأمر", "Read promissory notes", "عرض بيانات سندات الأمر المالية.", "View financial promissory-note data.", sensitive: true, highTrust: true),
        Create(33, PermissionKeys.Compliance.PromissoryNotesManage, "Compliance", "إدارة سندات الأمر", "Manage promissory notes", "إدارة حالات ونسخ سندات الأمر دون حذف.", "Manage promissory-note status and versions without deletion.", sensitive: true, highTrust: true),

        Create(34, PermissionKeys.Documents.Read, "Documents", "عرض بيانات الوثائق", "Read document metadata", "عرض بيانات الوثائق ونسخها دون تنزيل المحتوى.", "View document metadata and versions without downloading content.", sensitive: true),
        Create(35, PermissionKeys.Documents.Upload, "Documents", "رفع الوثائق", "Upload documents", "رفع نسخة وثيقة جديدة وفق سياسة الملفات.", "Upload a new document version under the file policy.", sensitive: true),
        Create(36, PermissionKeys.Documents.Download, "Documents", "تنزيل الوثائق", "Download documents", "تنزيل محتوى الوثائق غير المصنفة عالية الحساسية.", "Download document content not classified as highly sensitive.", sensitive: true),
        Create(37, PermissionKeys.Documents.DownloadSensitive, "Documents", "تنزيل الوثائق الحساسة", "Download sensitive documents", "تنزيل محتوى وثائق الهوية والمالية عالية الحساسية.", "Download highly sensitive identity and financial documents.", sensitive: true, highTrust: true),

        Create(38, PermissionKeys.Operations.PlatformAccountsRead, "Operations", "عرض حسابات المنصات", "Read platform accounts", "عرض حسابات منصات العملاء ضمن النطاق المسموح.", "View client-platform accounts within the allowed scope.", clientScope: true),
        Create(39, PermissionKeys.Operations.PlatformAccountsManage, "Operations", "إدارة حسابات المنصات", "Manage platform accounts", "إدارة التسجيل والحالة والملكية الرسمية لحسابات المنصات ضمن النطاق.", "Manage registration, status, and official ownership of platform accounts within scope.", clientScope: true),
        Create(40, PermissionKeys.Operations.PlatformAssignmentsRead, "Operations", "عرض تكليفات المنصات", "Read platform assignments", "عرض تاريخ الاستخدام الفعلي لحسابات المنصات ضمن النطاق.", "View actual platform-account usage history within scope.", clientScope: true),
        Create(41, PermissionKeys.Operations.PlatformAssignmentsManage, "Operations", "إدارة تكليفات المنصات", "Manage platform assignments", "إدارة تكليفات الاستخدام الفعلي مع حفظ التاريخ ضمن النطاق.", "Manage actual-use assignments while preserving history within scope.", clientScope: true),
        Create(42, PermissionKeys.Operations.HousingRead, "Operations", "عرض السكن", "Read housing", "عرض السكن وفترات الإقامة ضمن النطاق المسموح.", "View housing and residence periods within the allowed scope.", housingScope: true),
        Create(43, PermissionKeys.Operations.HousingManage, "Operations", "إدارة السكن", "Manage housing", "إدارة السكن والمشرفين وفترات الإقامة ضمن النطاق.", "Manage housing, supervisors, and residence periods within scope.", housingScope: true),

        Create(44, PermissionKeys.Reporting.ReportsRead, "Reporting", "عرض التقارير", "Read reports", "عرض التقارير التشغيلية المصرح بها.", "View authorized operational reports."),
        Create(45, PermissionKeys.Reporting.ExportsCreate, "Reporting", "إنشاء التصديرات", "Create exports", "إنشاء ملفات تصدير من البيانات المصرح بها فقط.", "Create export files from authorized data only.", sensitive: true),
        Create(46, PermissionKeys.Reporting.NotificationsRead, "Reporting", "عرض الإشعارات", "Read notifications", "عرض الإشعارات التشغيلية.", "View operational notifications."),
        Create(47, PermissionKeys.Reporting.NotificationsManage, "Reporting", "إدارة الإشعارات", "Manage notifications", "إدارة حالة ومحتوى الإشعارات التشغيلية.", "Manage operational notification status and content."),

        Create(48, PermissionKeys.Workflows.LeaveRequestsRead, "Workflows", "عرض طلبات الإجازة", "Read leave requests", "عرض طلبات الإجازة وتاريخها.", "View leave requests and history.", sensitive: true),
        Create(49, PermissionKeys.Workflows.LeaveRequestsManage, "Workflows", "إدارة طلبات الإجازة", "Manage leave requests", "إنشاء وتعديل طلبات الإجازة وفق حالتها.", "Create and update leave requests according to their state.", sensitive: true),
        Create(50, PermissionKeys.Workflows.LeaveRequestsApprove, "Workflows", "اعتماد طلبات الإجازة", "Approve leave requests", "الموافقة أو الرفض الموثق لطلبات الإجازة.", "Record approval or rejection decisions for leave requests.", sensitive: true, highTrust: true),
        Create(51, PermissionKeys.Workflows.AbsenceCasesRead, "Workflows", "عرض حالات الغياب", "Read absence cases", "عرض حالات الغياب والهروب وسجل أحداثها.", "View absence and escaped-employee cases and their events.", sensitive: true),
        Create(52, PermissionKeys.Workflows.AbsenceCasesManage, "Workflows", "إدارة حالات الغياب", "Manage absence cases", "إدارة حالات الغياب والهروب مع حفظ سجل الأحداث.", "Manage absence and escaped-employee cases while preserving event history.", sensitive: true),
        Create(53, PermissionKeys.Workflows.EmployeeStatusChangesRead, "Workflows", "عرض طلبات تغيير الحالة", "Read employee status changes", "عرض طلبات تغيير حالة الموظف.", "View employee status-change requests.", sensitive: true),
        Create(54, PermissionKeys.Workflows.EmployeeStatusChangesManage, "Workflows", "إدارة طلبات تغيير الحالة", "Manage employee status changes", "إنشاء وتحديث طلبات تغيير حالة الموظف.", "Create and update employee status-change requests.", sensitive: true),
        Create(55, PermissionKeys.Workflows.EmployeeStatusChangesApprove, "Workflows", "اعتماد تغيير حالة الموظف", "Approve employee status changes", "اعتماد تغيير حالة الموظف مع حفظ الأثر التاريخي.", "Approve employee status changes while preserving history.", sensitive: true, highTrust: true)
    ];

    private static PermissionSeed Create(
        int displayOrder,
        string key,
        string category,
        string nameAr,
        string nameEn,
        string descriptionAr,
        string descriptionEn,
        bool housingScope = false,
        bool clientScope = false,
        bool sensitive = false,
        bool highTrust = false) => new(
            CreateId(displayOrder),
            key,
            category,
            nameAr,
            nameEn,
            descriptionAr,
            descriptionEn,
            housingScope,
            clientScope,
            sensitive,
            highTrust,
            highTrust ? "HIGH_TRUST_ONLY" : sensitive ? "SENSITIVE_DATA" : null,
            displayOrder);

    private static Guid CreateId(int displayOrder) =>
        Guid.Parse($"019c18d5-62e1-7000-a000-{displayOrder:D12}");
}
