using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fleet;

public static class FleetErrors
{
    public static readonly OperationError InvalidRequest = new("fleet.invalid_request", "يحتوي طلب الأسطول على بيانات غير صالحة أو غير مكتملة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new("fleet.not_found", "لم يتم العثور على سجل الأسطول المطلوب.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new("fleet.duplicate", "يوجد بالفعل سجل مركبة أو كتالوج بالقيمة الفريدة نفسها.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new("fleet.conflict", "تتعارض العملية مع الحالة الحالية للمركبة أو السائق.", ErrorType.Conflict);
    public static readonly OperationError VehicleUnavailable = new("fleet.vehicle_unavailable", "المركبة غير متاحة للتخصيص.", ErrorType.Conflict);
    public static readonly OperationError RiderUnavailable = new("fleet.rider_unavailable", "السائق غير نشط أو لديه مركبة نشطة بالفعل.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("fleet.concurrency_conflict", "تغير السجل بعد تحميله. أعد تحميله وحاول مرة أخرى.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyConflict = new("fleet.idempotency_conflict", "تم استخدام مفتاح عدم التكرار مسبقًا لطلب مختلف.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyRequired = new("fleet.idempotency_required", "مطلوب وجود ترويسة Idempotency-Key.", ErrorType.Validation);
    public static readonly OperationError Forbidden = new("fleet.forbidden", "لا يمكن للمستخدم الحالي الوصول إلى سجل الأسطول هذا.", ErrorType.Forbidden);
    public static readonly OperationError CurrentUserUnavailable = new("fleet.current_user_unavailable", "تعذر تحديد هوية المستخدم المصادق عليه.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new("fleet.invalid_file", "الملف فارغ أو كبير جدًا أو غير مدعوم أو لا يتطابق مع نوعه المعلن.", ErrorType.Validation);
    public static readonly OperationError FileLimit = new("fleet.file_limit", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError FileMissing = new("fleet.file_missing", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError InvalidState = new("fleet.invalid_state", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError OdometerDecreased = new("fleet.odometer_decreased", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError AccidentAssignmentMismatch = new("fleet.accident_assignment_mismatch", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError KeetaPlatformUnavailable = new("fleet.keeta_platform_unavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError LeaseVehicleSponsorMismatch = new("fleet.lease_vehicle_sponsor_mismatch", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError LeasePeriodConflict = new("fleet.lease_period_conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError InvalidGpsFile = new("fleet.daily_distance.invalid_gps_file", "ملف GPS غير صالح أو لا يحتوي على أعمدة المركبة وطول الطريق وفترة التقرير المطلوبة.", ErrorType.Validation, "file");
    public static readonly OperationError GpsFramesetMissingSheet = new("fleet.daily_distance.gps_frameset_missing_sheet", "ملف Excel المحدد هو صفحة ربط ولا يحتوي على بيانات المركبات داخله. احفظ التقرير بصيغة XLSX، أو ارفع ملف sheet001.htm من المجلد المرافق، أو ارفع ملف ZIP يحتوي على ملف XLS والمجلد المرافق.", ErrorType.Validation, "file");
    public static readonly OperationError GpsDateMismatch = new("fleet.daily_distance.gps_date_mismatch", "تاريخ ملف GPS لا يطابق التاريخ المتوقع.", ErrorType.Validation, "expectedWorkDate");
    public static readonly OperationError DuplicateGpsImport = new("fleet.daily_distance.duplicate_gps_import", "تم استيراد ملف GPS نفسه لهذا اليوم مسبقًا.", ErrorType.Conflict, "file");
    public static readonly OperationError InvalidManualOdometer = new("fleet.daily_distance.invalid_manual_odometer", "قراءة العداد اليدوية يجب ألا تقل عن قراءة الأساس أو القراءة اليدوية السابقة.", ErrorType.Validation, "odometerReading");
    public static readonly OperationError ManualBaselineRequired = new("fleet.daily_distance.manual_baseline_required", "يلزم إدخال قراءة عداد أساس لحساب مسافة أول يوم يدوي لهذه المركبة.", ErrorType.Validation, "baselineOdometerReading");
    public static readonly OperationError ReturnConditionReportRequired = new("fleet.return_condition_report_required", "عند إرجاع مركبة أو استبدالها بحالة غير جيدة، يجب تحديد تصنيف المشكلة والخطورة وإدخال الوصف والمسؤولية والتكلفة التقديرية وإرفاق ملف إثبات واحد أو ملفين.", ErrorType.Validation, "conditionReport");
    public static readonly OperationError ReturnConditionReportNotAllowed = new("fleet.return_condition_report_not_allowed", "لا يمكن إضافة تقرير مشكلة أو ملفات إثبات عندما تكون حالة المركبة جيدة.", ErrorType.Validation, "conditionReport");
}
