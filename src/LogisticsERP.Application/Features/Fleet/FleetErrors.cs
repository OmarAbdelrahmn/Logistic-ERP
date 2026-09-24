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
    public static readonly OperationError RiderAlreadyHasVehicle = new("fleet.rider_already_has_vehicle", "لا يمكن تخصيص مركبة أخرى لأن السائق لديه مركبة نشطة بالفعل.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("fleet.concurrency_conflict", "تغير السجل بعد تحميله. أعد تحميله وحاول مرة أخرى.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyConflict = new("fleet.idempotency_conflict", "تم استخدام مفتاح عدم التكرار مسبقًا لطلب مختلف.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyRequired = new("fleet.idempotency_required", "مطلوب وجود ترويسة Idempotency-Key.", ErrorType.Validation);
    public static readonly OperationError Forbidden = new("fleet.forbidden", "لا يمكن للمستخدم الحالي الوصول إلى سجل الأسطول هذا.", ErrorType.Forbidden);
    public static readonly OperationError CurrentUserUnavailable = new("fleet.current_user_unavailable", "تعذر تحديد هوية المستخدم المصادق عليه.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new("fleet.invalid_file", "الملف فارغ أو كبير جدًا أو غير مدعوم أو لا يتطابق مع نوعه المعلن.", ErrorType.Validation);
    public static readonly OperationError TransitionPlateNumberArInvalid = new("fleet.registration_transition.plate_number_ar_invalid", "أدخل رقم اللوحة العربية الجديدة بما لا يزيد عن 32 حرفًا.", ErrorType.Validation, "plateNumberAr");
    public static readonly OperationError TransitionPlateNumberEnInvalid = new("fleet.registration_transition.plate_number_en_invalid", "أدخل رقم اللوحة الإنجليزية الجديدة بما لا يزيد عن 32 حرفًا.", ErrorType.Validation, "plateNumberEn");
    public static readonly OperationError TransitionPlateLettersArInvalid = new("fleet.registration_transition.plate_letters_ar_invalid", "حروف اللوحة العربية الجديدة يجب ألا تتجاوز 8 أحرف.", ErrorType.Validation, "plateLettersAr");
    public static readonly OperationError TransitionPlateLettersEnInvalid = new("fleet.registration_transition.plate_letters_en_invalid", "حروف اللوحة الإنجليزية الجديدة يجب ألا تتجاوز 8 أحرف.", ErrorType.Validation, "plateLettersEn");
    public static readonly OperationError TransitionPlateDigitsInvalid = new("fleet.registration_transition.plate_digits_invalid", "أرقام اللوحة الجديدة يجب ألا تتجاوز 8 أحرف.", ErrorType.Validation, "plateDigits");
    public static readonly OperationError TransitionReasonInvalid = new("fleet.registration_transition.reason_invalid", "أدخل سبب التحويل بما لا يزيد عن 1000 حرف.", ErrorType.Validation, "reason");
    public static readonly OperationError TransitionEffectiveDateRequired = new("fleet.registration_transition.effective_date_required", "أدخل تاريخ ووقت سريان التحويل.", ErrorType.Validation, "effectiveAtUtc");
    public static readonly OperationError TransitionRowVersionRequired = new("fleet.registration_transition.row_version_required", "رمز نسخة المركبة مطلوب. أعد تحميل بيانات المركبة ثم حاول مرة أخرى.", ErrorType.Validation, "rowVersion");
    public static readonly OperationError TransitionIstimaraInvalid = new("fleet.registration_transition.istimara_invalid", "أرفق صورة أو PDF صالحًا للاستمارة، بحجم لا يتجاوز 10 ميجابايت.", ErrorType.Validation, "istimara");
    public static readonly OperationError TransitionOperationCardInvalid = new("fleet.registration_transition.operation_card_invalid", "أرفق صورة أو PDF صالحًا لكرت التشغيل، بحجم لا يتجاوز 10 ميجابايت.", ErrorType.Validation, "operationCard");
    public static readonly OperationError TransitionDuplicatePlateNumberAr = new("fleet.registration_transition.plate_number_ar_duplicate", "رقم اللوحة العربية الجديدة مستخدم لمركبة أخرى.", ErrorType.Conflict, "plateNumberAr");
    public static readonly OperationError TransitionDuplicatePlateNumberEn = new("fleet.registration_transition.plate_number_en_duplicate", "رقم اللوحة الإنجليزية الجديدة مستخدم لمركبة أخرى.", ErrorType.Conflict, "plateNumberEn");
    public static readonly OperationError TransitionRegistrationTypeMissing = new("fleet.registration_transition.registration_type_missing", "نوع تسجيل المركبة الحالي غير محدد. صحح بيانات المركبة قبل التحويل.", ErrorType.Conflict, "registrationType");
    public static readonly OperationError FileLimit = new("fleet.file_limit", "الحد الأقصى لسندات الأمر النشطة للمندوب هو 3 ملفات.", ErrorType.Conflict);
    public static readonly OperationError PromissoryFilesRequired = new("fleet.promissory_files_required", "أرفق سند أمر واحدًا على الأقل.", ErrorType.Validation, "promissoryFiles");
    public static readonly OperationError AssignmentNotActive = new("fleet.assignment_not_active", "العهدة لم تعد نشطة لهذه المركبة. حدّث الصفحة ثم حاول مجددًا.", ErrorType.Conflict);
    public static readonly OperationError FileMissing = new("fleet.file_missing", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError InvalidState = new("fleet.invalid_state", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError VehicleIdentityCorrectionRequired = new("fleet.vehicle_identity_correction_required", "لا يمكن تعديل الرقم التسلسلي أو رقم الهيكل أو بيانات اللوحة أو نوع التسجيل من نموذج تعديل المركبة. استخدم تصحيح هوية المركبة لهذا التغيير.", ErrorType.Conflict);
    public static readonly OperationError OdometerDecreased = new("fleet.odometer_decreased", "قراءة العداد المدخلة أقل من القراءة المسجلة للمركبة. أدخل قراءة مساوية أو أعلى.", ErrorType.Conflict);
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
