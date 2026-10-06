using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SplitManagementPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000006"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الأدوار ضمن النطاق المصرح به.", "Create roles within the authorized scope.", "roles.create", "إنشاء الأدوار", "Create roles" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000008"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الصلاحيات ضمن النطاق المصرح به.", "Create permissions within the authorized scope.", "permissions.create", "إنشاء الصلاحيات", "Create permissions" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000010"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء وصول الدعم ضمن النطاق المصرح به.", "Create support access within the authorized scope.", "support_access.create", "إنشاء وصول الدعم", "Create support access" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000012"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء المدن التشغيلية ضمن النطاق المصرح به.", "Create operating cities within the authorized scope.", "operating_cities.create", "إنشاء المدن التشغيلية", "Create operating cities" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000019"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء المناديب ضمن النطاق المصرح به.", "Create riders within the authorized scope.", "riders.create", "إنشاء المناديب", "Create riders" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000021"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الكفلاء ضمن النطاق المصرح به.", "Create sponsors within the authorized scope.", "sponsors.create", "إنشاء الكفلاء", "Create sponsors" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000023"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الإقامات ضمن النطاق المصرح به.", "Create residency permits within the authorized scope.", "residency.create", "إنشاء الإقامات", "Create residency permits" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000025"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الرخص ضمن النطاق المصرح به.", "Create driver licenses within the authorized scope.", "licenses.create", "إنشاء الرخص", "Create driver licenses" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000027"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء بطاقات السائق ضمن النطاق المصرح به.", "Create rider cards within the authorized scope.", "rider_cards.create", "إنشاء بطاقات السائق", "Create rider cards" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000029"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء البطاقات الصحية ضمن النطاق المصرح به.", "Create health cards within the authorized scope.", "health_cards.create", "إنشاء البطاقات الصحية", "Create health cards" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000031"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء التأمين الطبي ضمن النطاق المصرح به.", "Create medical insurance within the authorized scope.", "insurance.create", "إنشاء التأمين الطبي", "Create medical insurance" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000033"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء سندات الأمر ضمن النطاق المصرح به.", "Create promissory notes within the authorized scope.", "promissory_notes.create", "إنشاء سندات الأمر", "Create promissory notes" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000039"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء حسابات المنصات ضمن النطاق المصرح به.", "Create platform accounts within the authorized scope.", "platform_accounts.create", "إنشاء حسابات المنصات", "Create platform accounts" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000041"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تكليفات المنصات ضمن النطاق المصرح به.", "Create platform assignments within the authorized scope.", "platform_assignments.create", "إنشاء تكليفات المنصات", "Create platform assignments" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000043"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء السكن ضمن النطاق المصرح به.", "Create housing within the authorized scope.", "housing.create", "إنشاء السكن", "Create housing" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000047"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الإشعارات ضمن النطاق المصرح به.", "Create notifications within the authorized scope.", "notifications.create", "إنشاء الإشعارات", "Create notifications" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000049"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء طلبات الإجازة ضمن النطاق المصرح به.", "Create leave requests within the authorized scope.", "leave_requests.create", "إنشاء طلبات الإجازة", "Create leave requests" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000052"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء حالات الغياب ضمن النطاق المصرح به.", "Create absence cases within the authorized scope.", "absence_cases.create", "إنشاء حالات الغياب", "Create absence cases" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000054"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء طلبات تغيير الحالة ضمن النطاق المصرح به.", "Create employee status changes within the authorized scope.", "employee_status_changes.create", "إنشاء طلبات تغيير الحالة", "Create employee status changes" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000057"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء المركبات ضمن النطاق المصرح به.", "Create vehicles within the authorized scope.", "fleet.vehicles.create", "إنشاء المركبات", "Create vehicles" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000061"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء عهد المركبات ضمن النطاق المصرح به.", "Create vehicle assignments within the authorized scope.", "fleet.assignments.create", "إنشاء عهد المركبات", "Create vehicle assignments" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000064"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء بلاغات المركبات ضمن النطاق المصرح به.", "Create vehicle issues within the authorized scope.", "fleet.issues.create", "إنشاء بلاغات المركبات", "Create vehicle issues" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000066"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء التزام المركبات ضمن النطاق المصرح به.", "Create vehicle compliance within the authorized scope.", "fleet.compliance.create", "إنشاء التزام المركبات", "Create vehicle compliance" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000074"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تصحيح بيانات الأسطول ضمن النطاق المصرح به.", "Create fleet corrections within the authorized scope.", "fleet.corrections.create", "إنشاء تصحيح بيانات الأسطول", "Create fleet corrections" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000076"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء ملف الشركة ضمن النطاق المصرح به.", "Create company profile within the authorized scope.", "company_profile.create", "إنشاء ملف الشركة", "Create company profile" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000078"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء الوسوم ضمن النطاق المصرح به.", "Create tags within the authorized scope.", "tags.create", "إنشاء الوسوم", "Create tags" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000079"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء كتالوج الوثائق ضمن النطاق المصرح به.", "Create document catalog within the authorized scope.", "documents.catalog.create", "إنشاء كتالوج الوثائق", "Create document catalog" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000082"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تحويل تسجيل المركبة ضمن النطاق المصرح به.", "Create vehicle registration transitions within the authorized scope.", "fleet.registration_transitions.create", "إنشاء تحويل تسجيل المركبة", "Create vehicle registration transitions" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000084"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.", "Create HR form templates within the authorized scope.", "hr_forms.templates.create", "إنشاء قوالب نماذج الموارد البشرية", "Create HR form templates" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000086"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء شرائح الاتصال ضمن النطاق المصرح به.", "Create phone SIMs within the authorized scope.", "phone_sims.create", "إنشاء شرائح الاتصال", "Create phone SIMs" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000088"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء المسافات اليومية للمركبات ضمن النطاق المصرح به.", "Create vehicle daily distances within the authorized scope.", "fleet.daily_distances.create", "إنشاء المسافات اليومية للمركبات", "Create vehicle daily distances" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000091"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء بطاقات الوقود ضمن النطاق المصرح به.", "Create fuel cards within the authorized scope.", "fuel.create", "إنشاء بطاقات الوقود", "Create fuel cards" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000094"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء مواقع الصيانة ضمن النطاق المصرح به.", "Create maintenance locations within the authorized scope.", "maintenance.locations.create", "إنشاء مواقع الصيانة", "Create maintenance locations" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000100"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء صيانة المركبات الخارجية ضمن النطاق المصرح به.", "Create external maintenance jobs within the authorized scope.", "maintenance.external_jobs.create", "إنشاء صيانة المركبات الخارجية", "Create external maintenance jobs" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000101"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء بيع قطع الغيار ضمن النطاق المصرح به.", "Create spare-part sales within the authorized scope.", "maintenance.part_sales.create", "إنشاء بيع قطع الغيار", "Create spare-part sales" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000102"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء مصنعية العميل ضمن النطاق المصرح به.", "Create customer labor charges within the authorized scope.", "maintenance.customer_labor_charges.create", "إنشاء مصنعية العميل", "Create customer labor charges" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000103"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء أجرة الميكانيكي ضمن النطاق المصرح به.", "Create mechanic labor payments within the authorized scope.", "maintenance.mechanic_labor_payments.create", "إنشاء أجرة الميكانيكي", "Create mechanic labor payments" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000107"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء أصناف المخزون ضمن النطاق المصرح به.", "Create inventory items within the authorized scope.", "inventory.items.create", "إنشاء أصناف المخزون", "Create inventory items" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000112"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء فواتير الشراء ضمن النطاق المصرح به.", "Create purchase receipts within the authorized scope.", "inventory.receipts.create", "إنشاء فواتير الشراء", "Create purchase receipts" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000113"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء مرتجعات المورد ضمن النطاق المصرح به.", "Create supplier returns within the authorized scope.", "inventory.returns.create", "إنشاء مرتجعات المورد", "Create supplier returns" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000118"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء القضايا القانونية ضمن النطاق المصرح به.", "Create legal cases within the authorized scope.", "legal_cases.create", "إنشاء القضايا القانونية", "Create legal cases" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000131"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.", "Create Jahez handovers within the authorized scope.", "jahez.handovers.create", "إنشاء تسليم وإغلاق حسابات جاهز", "Create Jahez handovers" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000132"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تحصيل مبالغ جاهز ضمن النطاق المصرح به.", "Create Collect Jahez payments within the authorized scope.", "jahez.collections.create", "إنشاء تحصيل مبالغ جاهز", "Create Collect Jahez payments" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000136"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تسجيل أرباح جاهز ضمن النطاق المصرح به.", "Create Jahez earnings within the authorized scope.", "jahez.earnings.create", "إنشاء تسجيل أرباح جاهز", "Create Jahez earnings" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000137"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء استيراد تقارير جاهز ضمن النطاق المصرح به.", "Create Import Jahez reports within the authorized scope.", "jahez.imports.create", "إنشاء استيراد تقارير جاهز", "Create Import Jahez reports" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000138"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء تسويات وأرصدة جاهز ضمن النطاق المصرح به.", "Create Adjust Jahez balances within the authorized scope.", "jahez.adjustments.create", "إنشاء تسويات وأرصدة جاهز", "Create Adjust Jahez balances" });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000200"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الأدوار ضمن النطاق المصرح به.", "Update roles within the authorized scope.", 200, "HIGH_TRUST_ONLY", false, false, true, false, "roles.update", "تعديل الأدوار", "Edit roles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000201"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الأدوار ضمن النطاق المصرح به.", "Delete roles within the authorized scope.", 201, "HIGH_TRUST_ONLY", false, false, true, false, "roles.delete", "حذف الأدوار", "Delete roles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000202"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الصلاحيات ضمن النطاق المصرح به.", "Update permissions within the authorized scope.", 202, "HIGH_TRUST_ONLY", false, false, true, false, "permissions.update", "تعديل الصلاحيات", "Edit permissions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000203"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الصلاحيات ضمن النطاق المصرح به.", "Delete permissions within the authorized scope.", 203, "HIGH_TRUST_ONLY", false, false, true, false, "permissions.delete", "حذف الصلاحيات", "Delete permissions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000204"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل وصول الدعم ضمن النطاق المصرح به.", "Update support access within the authorized scope.", 204, "HIGH_TRUST_ONLY", false, false, true, true, "support_access.update", "تعديل وصول الدعم", "Edit support access", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000205"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف وصول الدعم ضمن النطاق المصرح به.", "Delete support access within the authorized scope.", 205, "HIGH_TRUST_ONLY", false, false, true, true, "support_access.delete", "حذف وصول الدعم", "Delete support access", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000206"), "Security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض وصول الدعم ضمن النطاق المصرح به.", "Read support access within the authorized scope.", 206, "HIGH_TRUST_ONLY", false, false, true, true, "support_access.read", "عرض وصول الدعم", "Read support access", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000207"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل ملف الشركة ضمن النطاق المصرح به.", "Update company profile within the authorized scope.", 207, "HIGH_TRUST_ONLY", false, false, true, true, "company_profile.update", "تعديل ملف الشركة", "Edit company profile", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000208"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف ملف الشركة ضمن النطاق المصرح به.", "Delete company profile within the authorized scope.", 208, "HIGH_TRUST_ONLY", false, false, true, true, "company_profile.delete", "حذف ملف الشركة", "Delete company profile", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000209"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل المدن التشغيلية ضمن النطاق المصرح به.", "Update operating cities within the authorized scope.", 209, null, false, false, false, false, "operating_cities.update", "تعديل المدن التشغيلية", "Edit operating cities", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000210"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف المدن التشغيلية ضمن النطاق المصرح به.", "Delete operating cities within the authorized scope.", 210, null, false, false, false, false, "operating_cities.delete", "حذف المدن التشغيلية", "Delete operating cities", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000211"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الوسوم ضمن النطاق المصرح به.", "Update tags within the authorized scope.", 211, null, false, false, false, false, "tags.update", "تعديل الوسوم", "Edit tags", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000212"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الوسوم ضمن النطاق المصرح به.", "Delete tags within the authorized scope.", 212, null, false, false, false, false, "tags.delete", "حذف الوسوم", "Delete tags", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000213"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل المناديب ضمن النطاق المصرح به.", "Update riders within the authorized scope.", 213, null, false, false, false, false, "riders.update", "تعديل المناديب", "Edit riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000214"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف المناديب ضمن النطاق المصرح به.", "Delete riders within the authorized scope.", 214, null, false, false, false, false, "riders.delete", "حذف المناديب", "Delete riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000215"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الكفلاء ضمن النطاق المصرح به.", "Update sponsors within the authorized scope.", 215, "SENSITIVE_DATA", false, false, false, true, "sponsors.update", "تعديل الكفلاء", "Edit sponsors", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000216"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الكفلاء ضمن النطاق المصرح به.", "Delete sponsors within the authorized scope.", 216, "SENSITIVE_DATA", false, false, false, true, "sponsors.delete", "حذف الكفلاء", "Delete sponsors", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000217"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الإقامات ضمن النطاق المصرح به.", "Update residency permits within the authorized scope.", 217, "SENSITIVE_DATA", false, false, false, true, "residency.update", "تعديل الإقامات", "Edit residency permits", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000218"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الإقامات ضمن النطاق المصرح به.", "Delete residency permits within the authorized scope.", 218, "SENSITIVE_DATA", false, false, false, true, "residency.delete", "حذف الإقامات", "Delete residency permits", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000219"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الرخص ضمن النطاق المصرح به.", "Update driver licenses within the authorized scope.", 219, "SENSITIVE_DATA", false, false, false, true, "licenses.update", "تعديل الرخص", "Edit driver licenses", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000220"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الرخص ضمن النطاق المصرح به.", "Delete driver licenses within the authorized scope.", 220, "SENSITIVE_DATA", false, false, false, true, "licenses.delete", "حذف الرخص", "Delete driver licenses", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000221"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بطاقات السائق ضمن النطاق المصرح به.", "Update rider cards within the authorized scope.", 221, "SENSITIVE_DATA", false, false, false, true, "rider_cards.update", "تعديل بطاقات السائق", "Edit rider cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000222"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف بطاقات السائق ضمن النطاق المصرح به.", "Delete rider cards within the authorized scope.", 222, "SENSITIVE_DATA", false, false, false, true, "rider_cards.delete", "حذف بطاقات السائق", "Delete rider cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000223"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل البطاقات الصحية ضمن النطاق المصرح به.", "Update health cards within the authorized scope.", 223, "SENSITIVE_DATA", false, false, false, true, "health_cards.update", "تعديل البطاقات الصحية", "Edit health cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000224"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف البطاقات الصحية ضمن النطاق المصرح به.", "Delete health cards within the authorized scope.", 224, "SENSITIVE_DATA", false, false, false, true, "health_cards.delete", "حذف البطاقات الصحية", "Delete health cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000225"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل التأمين الطبي ضمن النطاق المصرح به.", "Update medical insurance within the authorized scope.", 225, "SENSITIVE_DATA", false, false, false, true, "insurance.update", "تعديل التأمين الطبي", "Edit medical insurance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000226"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف التأمين الطبي ضمن النطاق المصرح به.", "Delete medical insurance within the authorized scope.", 226, "SENSITIVE_DATA", false, false, false, true, "insurance.delete", "حذف التأمين الطبي", "Delete medical insurance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000227"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل سندات الأمر ضمن النطاق المصرح به.", "Update promissory notes within the authorized scope.", 227, "HIGH_TRUST_ONLY", false, false, true, true, "promissory_notes.update", "تعديل سندات الأمر", "Edit promissory notes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000228"), "Compliance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف سندات الأمر ضمن النطاق المصرح به.", "Delete promissory notes within the authorized scope.", 228, "HIGH_TRUST_ONLY", false, false, true, true, "promissory_notes.delete", "حذف سندات الأمر", "Delete promissory notes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000229"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل كتالوج الوثائق ضمن النطاق المصرح به.", "Update document catalog within the authorized scope.", 229, "SENSITIVE_DATA", false, false, false, true, "documents.catalog.update", "تعديل كتالوج الوثائق", "Edit document catalog", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000230"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف كتالوج الوثائق ضمن النطاق المصرح به.", "Delete document catalog within the authorized scope.", 230, "SENSITIVE_DATA", false, false, false, true, "documents.catalog.delete", "حذف كتالوج الوثائق", "Delete document catalog", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000231"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل حسابات المنصات ضمن النطاق المصرح به.", "Update platform accounts within the authorized scope.", 231, null, false, false, false, false, "platform_accounts.update", "تعديل حسابات المنصات", "Edit platform accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000232"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف حسابات المنصات ضمن النطاق المصرح به.", "Delete platform accounts within the authorized scope.", 232, null, false, false, false, false, "platform_accounts.delete", "حذف حسابات المنصات", "Delete platform accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000233"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تكليفات المنصات ضمن النطاق المصرح به.", "Update platform assignments within the authorized scope.", 233, null, false, false, false, false, "platform_assignments.update", "تعديل تكليفات المنصات", "Edit platform assignments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000234"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تكليفات المنصات ضمن النطاق المصرح به.", "Delete platform assignments within the authorized scope.", 234, null, false, false, false, false, "platform_assignments.delete", "حذف تكليفات المنصات", "Delete platform assignments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000235"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل السكن ضمن النطاق المصرح به.", "Update housing within the authorized scope.", 235, null, false, false, false, false, "housing.update", "تعديل السكن", "Edit housing", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000236"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف السكن ضمن النطاق المصرح به.", "Delete housing within the authorized scope.", 236, null, false, false, false, false, "housing.delete", "حذف السكن", "Delete housing", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000237"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل شرائح الاتصال ضمن النطاق المصرح به.", "Update phone SIMs within the authorized scope.", 237, "SENSITIVE_DATA", false, false, false, true, "phone_sims.update", "تعديل شرائح الاتصال", "Edit phone SIMs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000238"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف شرائح الاتصال ضمن النطاق المصرح به.", "Delete phone SIMs within the authorized scope.", 238, "SENSITIVE_DATA", false, false, false, true, "phone_sims.delete", "حذف شرائح الاتصال", "Delete phone SIMs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000239"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل الإشعارات ضمن النطاق المصرح به.", "Update notifications within the authorized scope.", 239, null, false, false, false, false, "notifications.update", "تعديل الإشعارات", "Edit notifications", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000240"), "Reporting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف الإشعارات ضمن النطاق المصرح به.", "Delete notifications within the authorized scope.", 240, null, false, false, false, false, "notifications.delete", "حذف الإشعارات", "Delete notifications", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000241"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل المركبات ضمن النطاق المصرح به.", "Update vehicles within the authorized scope.", 241, null, false, false, false, false, "fleet.vehicles.update", "تعديل المركبات", "Edit vehicles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000242"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف المركبات ضمن النطاق المصرح به.", "Delete vehicles within the authorized scope.", 242, null, false, false, false, false, "fleet.vehicles.delete", "حذف المركبات", "Delete vehicles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000243"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل عهد المركبات ضمن النطاق المصرح به.", "Update vehicle assignments within the authorized scope.", 243, null, false, false, false, false, "fleet.assignments.update", "تعديل عهد المركبات", "Edit vehicle assignments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000244"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف عهد المركبات ضمن النطاق المصرح به.", "Delete vehicle assignments within the authorized scope.", 244, null, false, false, false, false, "fleet.assignments.delete", "حذف عهد المركبات", "Delete vehicle assignments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000245"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بلاغات المركبات ضمن النطاق المصرح به.", "Update vehicle issues within the authorized scope.", 245, null, false, false, false, false, "fleet.issues.update", "تعديل بلاغات المركبات", "Edit vehicle issues", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000246"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف بلاغات المركبات ضمن النطاق المصرح به.", "Delete vehicle issues within the authorized scope.", 246, null, false, false, false, false, "fleet.issues.delete", "حذف بلاغات المركبات", "Delete vehicle issues", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000247"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل التزام المركبات ضمن النطاق المصرح به.", "Update vehicle compliance within the authorized scope.", 247, null, false, false, false, false, "fleet.compliance.update", "تعديل التزام المركبات", "Edit vehicle compliance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000248"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف التزام المركبات ضمن النطاق المصرح به.", "Delete vehicle compliance within the authorized scope.", 248, null, false, false, false, false, "fleet.compliance.delete", "حذف التزام المركبات", "Delete vehicle compliance", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000249"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تصحيح بيانات الأسطول ضمن النطاق المصرح به.", "Update fleet corrections within the authorized scope.", 249, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.corrections.update", "تعديل تصحيح بيانات الأسطول", "Edit fleet corrections", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000250"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تصحيح بيانات الأسطول ضمن النطاق المصرح به.", "Delete fleet corrections within the authorized scope.", 250, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.corrections.delete", "حذف تصحيح بيانات الأسطول", "Delete fleet corrections", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000251"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تحويل تسجيل المركبة ضمن النطاق المصرح به.", "Update vehicle registration transitions within the authorized scope.", 251, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.registration_transitions.update", "تعديل تحويل تسجيل المركبة", "Edit vehicle registration transitions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000252"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تحويل تسجيل المركبة ضمن النطاق المصرح به.", "Delete vehicle registration transitions within the authorized scope.", 252, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.registration_transitions.delete", "حذف تحويل تسجيل المركبة", "Delete vehicle registration transitions", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000253"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل المسافات اليومية للمركبات ضمن النطاق المصرح به.", "Update vehicle daily distances within the authorized scope.", 253, "SENSITIVE_DATA", false, false, false, true, "fleet.daily_distances.update", "تعديل المسافات اليومية للمركبات", "Edit vehicle daily distances", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000254"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف المسافات اليومية للمركبات ضمن النطاق المصرح به.", "Delete vehicle daily distances within the authorized scope.", 254, "SENSITIVE_DATA", false, false, false, true, "fleet.daily_distances.delete", "حذف المسافات اليومية للمركبات", "Delete vehicle daily distances", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000255"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.", "Update Jahez handovers within the authorized scope.", 255, "SENSITIVE_DATA", false, false, false, true, "jahez.handovers.update", "تعديل تسليم وإغلاق حسابات جاهز", "Edit Jahez handovers", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000256"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.", "Delete Jahez handovers within the authorized scope.", 256, "SENSITIVE_DATA", false, false, false, true, "jahez.handovers.delete", "حذف تسليم وإغلاق حسابات جاهز", "Delete Jahez handovers", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000257"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تحصيل مبالغ جاهز ضمن النطاق المصرح به.", "Update Collect Jahez payments within the authorized scope.", 257, "SENSITIVE_DATA", false, false, false, true, "jahez.collections.update", "تعديل تحصيل مبالغ جاهز", "Edit Collect Jahez payments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000258"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تحصيل مبالغ جاهز ضمن النطاق المصرح به.", "Delete Collect Jahez payments within the authorized scope.", 258, "SENSITIVE_DATA", false, false, false, true, "jahez.collections.delete", "حذف تحصيل مبالغ جاهز", "Delete Collect Jahez payments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000259"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تسجيل أرباح جاهز ضمن النطاق المصرح به.", "Update Jahez earnings within the authorized scope.", 259, "SENSITIVE_DATA", false, false, false, true, "jahez.earnings.update", "تعديل تسجيل أرباح جاهز", "Edit Jahez earnings", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000260"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تسجيل أرباح جاهز ضمن النطاق المصرح به.", "Delete Jahez earnings within the authorized scope.", 260, "SENSITIVE_DATA", false, false, false, true, "jahez.earnings.delete", "حذف تسجيل أرباح جاهز", "Delete Jahez earnings", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000261"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل استيراد تقارير جاهز ضمن النطاق المصرح به.", "Update Import Jahez reports within the authorized scope.", 261, "SENSITIVE_DATA", false, false, false, true, "jahez.imports.update", "تعديل استيراد تقارير جاهز", "Edit Import Jahez reports", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000262"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف استيراد تقارير جاهز ضمن النطاق المصرح به.", "Delete Import Jahez reports within the authorized scope.", 262, "SENSITIVE_DATA", false, false, false, true, "jahez.imports.delete", "حذف استيراد تقارير جاهز", "Delete Import Jahez reports", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000263"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض استيراد تقارير جاهز ضمن النطاق المصرح به.", "Read Import Jahez reports within the authorized scope.", 263, "SENSITIVE_DATA", false, false, false, true, "jahez.imports.read", "عرض استيراد تقارير جاهز", "Read Import Jahez reports", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000264"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل تسويات وأرصدة جاهز ضمن النطاق المصرح به.", "Update Adjust Jahez balances within the authorized scope.", 264, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.adjustments.update", "تعديل تسويات وأرصدة جاهز", "Edit Adjust Jahez balances", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000265"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف تسويات وأرصدة جاهز ضمن النطاق المصرح به.", "Delete Adjust Jahez balances within the authorized scope.", 265, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.adjustments.delete", "حذف تسويات وأرصدة جاهز", "Delete Adjust Jahez balances", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000266"), "Fuel", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بطاقات الوقود ضمن النطاق المصرح به.", "Update fuel cards within the authorized scope.", 266, "SENSITIVE_DATA", false, false, false, true, "fuel.update", "تعديل بطاقات الوقود", "Edit fuel cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000267"), "Fuel", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف بطاقات الوقود ضمن النطاق المصرح به.", "Delete fuel cards within the authorized scope.", 267, "SENSITIVE_DATA", false, false, false, true, "fuel.delete", "حذف بطاقات الوقود", "Delete fuel cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000268"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل مواقع الصيانة ضمن النطاق المصرح به.", "Update maintenance locations within the authorized scope.", 268, "HIGH_TRUST_ONLY", false, false, true, false, "maintenance.locations.update", "تعديل مواقع الصيانة", "Edit maintenance locations", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000269"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف مواقع الصيانة ضمن النطاق المصرح به.", "Delete maintenance locations within the authorized scope.", 269, "HIGH_TRUST_ONLY", false, false, true, false, "maintenance.locations.delete", "حذف مواقع الصيانة", "Delete maintenance locations", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000270"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل صيانة المركبات الخارجية ضمن النطاق المصرح به.", "Update external maintenance jobs within the authorized scope.", 270, "SENSITIVE_DATA", false, false, false, true, "maintenance.external_jobs.update", "تعديل صيانة المركبات الخارجية", "Edit external maintenance jobs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000271"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف صيانة المركبات الخارجية ضمن النطاق المصرح به.", "Delete external maintenance jobs within the authorized scope.", 271, "SENSITIVE_DATA", false, false, false, true, "maintenance.external_jobs.delete", "حذف صيانة المركبات الخارجية", "Delete external maintenance jobs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000272"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بيع قطع الغيار ضمن النطاق المصرح به.", "Update spare-part sales within the authorized scope.", 272, "SENSITIVE_DATA", false, false, false, true, "maintenance.part_sales.update", "تعديل بيع قطع الغيار", "Edit spare-part sales", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000273"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف بيع قطع الغيار ضمن النطاق المصرح به.", "Delete spare-part sales within the authorized scope.", 273, "SENSITIVE_DATA", false, false, false, true, "maintenance.part_sales.delete", "حذف بيع قطع الغيار", "Delete spare-part sales", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000274"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل مصنعية العميل ضمن النطاق المصرح به.", "Update customer labor charges within the authorized scope.", 274, "SENSITIVE_DATA", false, false, false, true, "maintenance.customer_labor_charges.update", "تعديل مصنعية العميل", "Edit customer labor charges", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000275"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف مصنعية العميل ضمن النطاق المصرح به.", "Delete customer labor charges within the authorized scope.", 275, "SENSITIVE_DATA", false, false, false, true, "maintenance.customer_labor_charges.delete", "حذف مصنعية العميل", "Delete customer labor charges", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000276"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل أجرة الميكانيكي ضمن النطاق المصرح به.", "Update mechanic labor payments within the authorized scope.", 276, "SENSITIVE_DATA", false, false, false, true, "maintenance.mechanic_labor_payments.update", "تعديل أجرة الميكانيكي", "Edit mechanic labor payments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000277"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف أجرة الميكانيكي ضمن النطاق المصرح به.", "Delete mechanic labor payments within the authorized scope.", 277, "SENSITIVE_DATA", false, false, false, true, "maintenance.mechanic_labor_payments.delete", "حذف أجرة الميكانيكي", "Delete mechanic labor payments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000278"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل أصناف المخزون ضمن النطاق المصرح به.", "Update inventory items within the authorized scope.", 278, "SENSITIVE_DATA", false, false, false, true, "inventory.items.update", "تعديل أصناف المخزون", "Edit inventory items", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000279"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف أصناف المخزون ضمن النطاق المصرح به.", "Delete inventory items within the authorized scope.", 279, "SENSITIVE_DATA", false, false, false, true, "inventory.items.delete", "حذف أصناف المخزون", "Delete inventory items", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000280"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل فواتير الشراء ضمن النطاق المصرح به.", "Update purchase receipts within the authorized scope.", 280, "SENSITIVE_DATA", false, false, false, true, "inventory.receipts.update", "تعديل فواتير الشراء", "Edit purchase receipts", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000281"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف فواتير الشراء ضمن النطاق المصرح به.", "Delete purchase receipts within the authorized scope.", 281, "SENSITIVE_DATA", false, false, false, true, "inventory.receipts.delete", "حذف فواتير الشراء", "Delete purchase receipts", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000282"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض فواتير الشراء ضمن النطاق المصرح به.", "Read purchase receipts within the authorized scope.", 282, "SENSITIVE_DATA", false, false, false, true, "inventory.receipts.read", "عرض فواتير الشراء", "Read purchase receipts", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000283"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل مرتجعات المورد ضمن النطاق المصرح به.", "Update supplier returns within the authorized scope.", 283, "SENSITIVE_DATA", false, false, false, true, "inventory.returns.update", "تعديل مرتجعات المورد", "Edit supplier returns", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000284"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف مرتجعات المورد ضمن النطاق المصرح به.", "Delete supplier returns within the authorized scope.", 284, "SENSITIVE_DATA", false, false, false, true, "inventory.returns.delete", "حذف مرتجعات المورد", "Delete supplier returns", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000285"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل طلبات الإجازة ضمن النطاق المصرح به.", "Update leave requests within the authorized scope.", 285, "SENSITIVE_DATA", false, false, false, true, "leave_requests.update", "تعديل طلبات الإجازة", "Edit leave requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000286"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف طلبات الإجازة ضمن النطاق المصرح به.", "Delete leave requests within the authorized scope.", 286, "SENSITIVE_DATA", false, false, false, true, "leave_requests.delete", "حذف طلبات الإجازة", "Delete leave requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000287"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل حالات الغياب ضمن النطاق المصرح به.", "Update absence cases within the authorized scope.", 287, "SENSITIVE_DATA", false, false, false, true, "absence_cases.update", "تعديل حالات الغياب", "Edit absence cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000288"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف حالات الغياب ضمن النطاق المصرح به.", "Delete absence cases within the authorized scope.", 288, "SENSITIVE_DATA", false, false, false, true, "absence_cases.delete", "حذف حالات الغياب", "Delete absence cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000289"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل طلبات تغيير الحالة ضمن النطاق المصرح به.", "Update employee status changes within the authorized scope.", 289, "SENSITIVE_DATA", false, false, false, true, "employee_status_changes.update", "تعديل طلبات تغيير الحالة", "Edit employee status changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000290"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف طلبات تغيير الحالة ضمن النطاق المصرح به.", "Delete employee status changes within the authorized scope.", 290, "SENSITIVE_DATA", false, false, false, true, "employee_status_changes.delete", "حذف طلبات تغيير الحالة", "Delete employee status changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000291"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل القضايا القانونية ضمن النطاق المصرح به.", "Update legal cases within the authorized scope.", 291, "SENSITIVE_DATA", false, false, false, true, "legal_cases.update", "تعديل القضايا القانونية", "Edit legal cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000292"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف القضايا القانونية ضمن النطاق المصرح به.", "Delete legal cases within the authorized scope.", 292, "SENSITIVE_DATA", false, false, false, true, "legal_cases.delete", "حذف القضايا القانونية", "Delete legal cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000293"), "HrForms", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.", "Update HR form templates within the authorized scope.", 293, "HIGH_TRUST_ONLY", false, false, true, true, "hr_forms.templates.update", "تعديل قوالب نماذج الموارد البشرية", "Edit HR form templates", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000294"), "HrForms", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حذف قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.", "Delete HR form templates within the authorized scope.", 294, "HIGH_TRUST_ONLY", false, false, true, true, "hr_forms.templates.delete", "حذف قوالب نماذج الموارد البشرية", "Delete HR form templates", null, false, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Independent action permissions cannot safely be collapsed. Restore the pre-migration authorization backup instead.");
    }
}
