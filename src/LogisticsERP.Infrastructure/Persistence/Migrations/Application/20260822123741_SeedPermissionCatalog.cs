using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SeedPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000001"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حسابات المستخدمين وحالتها.", "View user accounts and their status.", 1, "SENSITIVE_DATA", false, false, false, true, "users.read", "عرض المستخدمين", "Read users", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000002"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء حسابات مستخدمين جديدة.", "Create new user accounts.", 2, "HIGH_TRUST_ONLY", false, false, true, true, "users.create", "إنشاء المستخدمين", "Create users", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000003"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل حالة وبيانات حسابات المستخدمين.", "Update user account details and status.", 3, "HIGH_TRUST_ONLY", false, false, true, true, "users.update", "تعديل المستخدمين", "Update users", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000004"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "أرشفة حساب مستخدم وإبطال جلساته دون حذف بياناته.", "Archive a user and revoke sessions without deleting records.", 4, "HIGH_TRUST_ONLY", false, false, true, true, "users.archive", "أرشفة المستخدمين", "Archive users", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000005"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض الأدوار وقوالب الصلاحيات.", "View roles and permission templates.", 5, null, false, false, false, false, "roles.read", "عرض الأدوار", "Read roles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000006"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة الأدوار غير المحمية وتعيينها للمستخدمين.", "Manage non-protected roles and user role assignments.", 6, "HIGH_TRUST_ONLY", false, false, true, false, "roles.manage", "إدارة الأدوار", "Manage roles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000007"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض كتالوج الصلاحيات والمنح والمنع.", "View the permission catalog, grants, and denies.", 7, null, false, false, false, false, "permissions.read", "عرض الصلاحيات", "Read permissions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000008"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة منح ومنع الصلاحيات ونطاقاتها.", "Manage permission grants, denies, and scopes.", 8, "HIGH_TRUST_ONLY", false, false, true, false, "permissions.manage", "إدارة الصلاحيات", "Manage permissions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000009"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض سجل التدقيق الأمني والتشغيلي.", "View security and operational audit records.", 9, "HIGH_TRUST_ONLY", false, false, true, true, "audit.read", "عرض سجل التدقيق", "Read audit log", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000010"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة وصول الدعم المؤقت وحالات الطوارئ.", "Manage temporary and break-glass support access.", 10, "HIGH_TRUST_ONLY", false, false, true, true, "support_access.manage", "إدارة وصول الدعم", "Manage support access", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000011"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض المدن والفروع التشغيلية.", "View operating cities and branches.", 11, null, false, false, false, false, "operating_cities.read", "عرض المدن التشغيلية", "Read operating cities", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000012"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إضافة وتعديل وتعطيل المدن التشغيلية.", "Add, update, and disable operating cities.", 12, null, false, false, false, false, "operating_cities.manage", "إدارة المدن التشغيلية", "Manage operating cities", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000013"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات الموظفين غير الحساسة.", "View non-sensitive employee data.", 13, null, false, false, false, false, "employees.read", "عرض الموظفين", "Read employees", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000014"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء سجلات موظفين جديدة.", "Create new employee records.", 14, null, false, false, false, false, "employees.create", "إنشاء الموظفين", "Create employees", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000015"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بيانات الموظفين التشغيلية.", "Update operational employee data.", 15, null, false, false, false, false, "employees.update", "تعديل الموظفين", "Update employees", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000016"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "أرشفة الموظفين دون حذف تاريخهم.", "Archive employees without deleting their history.", 16, "HIGH_TRUST_ONLY", false, false, true, false, "employees.archive", "أرشفة الموظفين", "Archive employees", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000017"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض الهوية والإقامة والبيانات الشخصية المقيدة.", "View restricted identity, residency, and personal data.", 17, "HIGH_TRUST_ONLY", false, false, true, true, "employees.sensitive.read", "عرض بيانات الموظفين الحساسة", "Read sensitive employee data", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000018"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض ملفات المناديب وبياناتهم التشغيلية.", "View rider profiles and operational data.", 18, null, false, false, false, false, "riders.read", "عرض المناديب", "Read riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000019"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء وتعديل حالات وملفات المناديب.", "Create and update rider profiles and status.", 19, null, false, false, false, false, "riders.manage", "إدارة المناديب", "Manage riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000020"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض جهات الكفالة وبيانات السجل.", "View sponsors and registry information.", 20, "SENSITIVE_DATA", false, false, false, true, "sponsors.read", "عرض الكفلاء", "Read sponsors", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000021"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة جهات الكفالة وفترات كفالة الموظفين.", "Manage sponsors and employee sponsorship periods.", 21, "SENSITIVE_DATA", false, false, false, true, "sponsors.manage", "إدارة الكفلاء", "Manage sponsors", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000022"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات الإقامة المقيدة.", "View restricted residency permit data.", 22, "SENSITIVE_DATA", false, false, false, true, "residency.read", "عرض الإقامات", "Read residency permits", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000023"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إضافة وتجديد وتحديث حالات الإقامة.", "Add, renew, and update residency permit status.", 23, "SENSITIVE_DATA", false, false, false, true, "residency.manage", "إدارة الإقامات", "Manage residency permits", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000024"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض رخص القيادة وإصداراتها.", "View driver licenses and their versions.", 24, "SENSITIVE_DATA", false, false, false, true, "licenses.read", "عرض الرخص", "Read driver licenses", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000025"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة إصدار وتجديد وحالة رخص القيادة.", "Manage driver-license issuance, renewal, and status.", 25, "SENSITIVE_DATA", false, false, false, true, "licenses.manage", "إدارة الرخص", "Manage driver licenses", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000026"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بطاقات السائق وتجديداتها.", "View rider cards and renewals.", 26, "SENSITIVE_DATA", false, false, false, true, "rider_cards.read", "عرض بطاقات السائق", "Read rider cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000027"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة إصدار وتجديد بطاقات السائق.", "Manage rider-card issuance and renewal.", 27, "SENSITIVE_DATA", false, false, false, true, "rider_cards.manage", "إدارة بطاقات السائق", "Manage rider cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000028"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض البطاقات الصحية وتجديداتها.", "View health cards and renewals.", 28, "SENSITIVE_DATA", false, false, false, true, "health_cards.read", "عرض البطاقات الصحية", "Read health cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000029"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة إصدار وتجديد البطاقات الصحية.", "Manage health-card issuance and renewal.", 29, "SENSITIVE_DATA", false, false, false, true, "health_cards.manage", "إدارة البطاقات الصحية", "Manage health cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000030"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض وثائق ومستويات التأمين الطبي.", "View medical-insurance policies and plan levels.", 30, "SENSITIVE_DATA", false, false, false, true, "insurance.read", "عرض التأمين الطبي", "Read medical insurance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000031"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة وثائق وتجديدات ومستويات التأمين الطبي.", "Manage medical-insurance policies, renewals, and levels.", 31, "SENSITIVE_DATA", false, false, false, true, "insurance.manage", "إدارة التأمين الطبي", "Manage medical insurance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000032"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات سندات الأمر المالية.", "View financial promissory-note data.", 32, "HIGH_TRUST_ONLY", false, false, true, true, "promissory_notes.read", "عرض سندات الأمر", "Read promissory notes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000033"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة حالات ونسخ سندات الأمر دون حذف.", "Manage promissory-note status and versions without deletion.", 33, "HIGH_TRUST_ONLY", false, false, true, true, "promissory_notes.manage", "إدارة سندات الأمر", "Manage promissory notes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000034"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات الوثائق ونسخها دون تنزيل المحتوى.", "View document metadata and versions without downloading content.", 34, "SENSITIVE_DATA", false, false, false, true, "documents.read", "عرض بيانات الوثائق", "Read document metadata", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000035"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "رفع نسخة وثيقة جديدة وفق سياسة الملفات.", "Upload a new document version under the file policy.", 35, "SENSITIVE_DATA", false, false, false, true, "documents.upload", "رفع الوثائق", "Upload documents", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000036"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنزيل محتوى الوثائق غير المصنفة عالية الحساسية.", "Download document content not classified as highly sensitive.", 36, "SENSITIVE_DATA", false, false, false, true, "documents.download", "تنزيل الوثائق", "Download documents", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000037"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنزيل محتوى وثائق الهوية والمالية عالية الحساسية.", "Download highly sensitive identity and financial documents.", 37, "HIGH_TRUST_ONLY", false, false, true, true, "documents.download_sensitive", "تنزيل الوثائق الحساسة", "Download sensitive documents", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000038"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حسابات منصات العملاء ضمن النطاق المسموح.", "View client-platform accounts within the allowed scope.", 38, null, false, false, false, false, "platform_accounts.read", "عرض حسابات المنصات", "Read platform accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000039"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة التسجيل والحالة والملكية الرسمية لحسابات المنصات ضمن النطاق.", "Manage registration, status, and official ownership of platform accounts within scope.", 39, null, false, false, false, false, "platform_accounts.manage", "إدارة حسابات المنصات", "Manage platform accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000040"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض تاريخ الاستخدام الفعلي لحسابات المنصات ضمن النطاق.", "View actual platform-account usage history within scope.", 40, null, false, false, false, false, "platform_assignments.read", "عرض تكليفات المنصات", "Read platform assignments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000041"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة تكليفات الاستخدام الفعلي مع حفظ التاريخ ضمن النطاق.", "Manage actual-use assignments while preserving history within scope.", 41, null, false, false, false, false, "platform_assignments.manage", "إدارة تكليفات المنصات", "Manage platform assignments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000042"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض السكن وفترات الإقامة ضمن النطاق المسموح.", "View housing and residence periods within the allowed scope.", 42, null, false, false, false, false, "housing.read", "عرض السكن", "Read housing", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000043"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة السكن والمشرفين وفترات الإقامة ضمن النطاق.", "Manage housing, supervisors, and residence periods within scope.", 43, null, false, false, false, false, "housing.manage", "إدارة السكن", "Manage housing", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000044"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض التقارير التشغيلية المصرح بها.", "View authorized operational reports.", 44, null, false, false, false, false, "reports.read", "عرض التقارير", "Read reports", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000045"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء ملفات تصدير من البيانات المصرح بها فقط.", "Create export files from authorized data only.", 45, "SENSITIVE_DATA", false, false, false, true, "exports.create", "إنشاء التصديرات", "Create exports", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000046"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض الإشعارات التشغيلية.", "View operational notifications.", 46, null, false, false, false, false, "notifications.read", "عرض الإشعارات", "Read notifications", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000047"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة حالة ومحتوى الإشعارات التشغيلية.", "Manage operational notification status and content.", 47, null, false, false, false, false, "notifications.manage", "إدارة الإشعارات", "Manage notifications", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000048"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض طلبات الإجازة وتاريخها.", "View leave requests and history.", 48, "SENSITIVE_DATA", false, false, false, true, "leave_requests.read", "عرض طلبات الإجازة", "Read leave requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000049"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء وتعديل طلبات الإجازة وفق حالتها.", "Create and update leave requests according to their state.", 49, "SENSITIVE_DATA", false, false, false, true, "leave_requests.manage", "إدارة طلبات الإجازة", "Manage leave requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000050"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "الموافقة أو الرفض الموثق لطلبات الإجازة.", "Record approval or rejection decisions for leave requests.", 50, "HIGH_TRUST_ONLY", false, false, true, true, "leave_requests.approve", "اعتماد طلبات الإجازة", "Approve leave requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000051"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حالات الغياب والهروب وسجل أحداثها.", "View absence and escaped-employee cases and their events.", 51, "SENSITIVE_DATA", false, false, false, true, "absence_cases.read", "عرض حالات الغياب", "Read absence cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000052"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة حالات الغياب والهروب مع حفظ سجل الأحداث.", "Manage absence and escaped-employee cases while preserving event history.", 52, "SENSITIVE_DATA", false, false, false, true, "absence_cases.manage", "إدارة حالات الغياب", "Manage absence cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000053"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض طلبات تغيير حالة الموظف.", "View employee status-change requests.", 53, "SENSITIVE_DATA", false, false, false, true, "employee_status_changes.read", "عرض طلبات تغيير الحالة", "Read employee status changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000054"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء وتحديث طلبات تغيير حالة الموظف.", "Create and update employee status-change requests.", 54, "SENSITIVE_DATA", false, false, false, true, "employee_status_changes.manage", "إدارة طلبات تغيير الحالة", "Manage employee status changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000055"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد تغيير حالة الموظف مع حفظ الأثر التاريخي.", "Approve employee status changes while preserving history.", 55, "HIGH_TRUST_ONLY", false, false, true, true, "employee_status_changes.approve", "اعتماد تغيير حالة الموظف", "Approve employee status changes", null, false, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000010"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000012"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000018"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000023"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000026"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000027"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000028"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000029"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000032"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000036"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000037"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000038"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000039"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000042"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000046"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000047"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000048"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000049"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000050"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000051"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000052"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000053"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000054"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000055"));
        }
    }
}
