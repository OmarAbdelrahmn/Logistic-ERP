using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Maintenance;

public static class MaintenanceErrors
{
    public static readonly OperationError Forbidden = new("maintenance.forbidden", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Forbidden);
    public static readonly OperationError CurrentUserUnavailable = new("maintenance.current_user_unavailable", "تعذر تحديد المستخدم الحالي.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidRequest = new("maintenance.invalid_request", "بيانات الطلب غير صالحة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new("maintenance.not_found", "لم يتم العثور على السجل المطلوب.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new("maintenance.duplicate", "يوجد سجل آخر بالقيمة نفسها.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("maintenance.concurrency_conflict", "تم تعديل السجل من مستخدم آخر؛ أعد تحميل البيانات.", ErrorType.Conflict);
    public static readonly OperationError InvalidLocation = new("maintenance.invalid_location", "موقع الصيانة أو المخزون غير صالح لهذه العملية.", ErrorType.Validation, "maintenanceLocationId");
    public static readonly OperationError InvalidSubject = new("maintenance.invalid_subject", "يجب اختيار مركبة شركة أو مرجع مركبة خارجية، وليس كليهما.", ErrorType.Validation);
    public static readonly OperationError InvalidState = new("maintenance.invalid_state", "حالة أمر الصيانة لا تسمح بهذه العملية.", ErrorType.Conflict);
    public static readonly OperationError InsufficientStock = new("maintenance.insufficient_stock", "رصيد المخزون غير كافٍ لإتمام العملية بطريقة FIFO.", ErrorType.Conflict);
    public static readonly OperationError InvalidInventoryItem = new("maintenance.invalid_inventory_item", "صنف المخزون أو وحدته لا يناسب العملية.", ErrorType.Validation, "inventoryItemId");
    public static readonly OperationError InvalidOilQuantity = new("maintenance.invalid_oil_quantity", "كمية الزيت غير مضبوطة لهذا النوع من المركبات.", ErrorType.Validation, "configuredOilQuantityLiters");
    public static readonly OperationError InvalidOdometer = new("maintenance.invalid_odometer", "قراءة العداد أقل من القراءة الحالية للمركبة.", ErrorType.Validation, "odometerAtChange");
    public static readonly OperationError InvalidOilFilter = new("maintenance.invalid_oil_filter", "بيانات فلتر الزيت لا تطابق حالة تغيير الفلتر.", ErrorType.Validation, "oilFilterInventoryItemId");
    public static readonly OperationError InvalidBillFile = new("maintenance.invalid_bill_file", "يجب رفع ملف فاتورة PDF أو صورة صالح لا يتجاوز 10 ميجابايت.", ErrorType.Validation, "billFile");
    public static readonly OperationError FileMissing = new("maintenance.bill_file_missing", "ملف الفاتورة غير موجود.", ErrorType.NotFound);
    public static readonly OperationError AlreadyReversed = new("maintenance.already_reversed", "تم عكس هذه العملية مسبقًا.", ErrorType.Conflict);
    public static readonly OperationError InvalidOilBarrel = new("maintenance.invalid_oil_barrel", "برميل الزيت غير صالح لهذه العملية.", ErrorType.Validation, "oilBarrelId");
    public static readonly OperationError OilLossAllowanceExceeded = new("maintenance.oil_loss_allowance_exceeded", "الفقد المسجل يتجاوز نسبة 2% المسموحة للبرميل.", ErrorType.Validation, "quantityLiters");
    public static readonly OperationError OilTransferRequiresWholeBarrels = new("maintenance.oil_transfer_requires_whole_barrels", "نقل الزيت يجب أن يشمل براميل كاملة دون تقسيم محتوى البرميل.", ErrorType.Validation, "quantity");
    public static readonly OperationError OilBarrelNotNextFifo = new("maintenance.oil_barrel_not_next_fifo", "البرميل المختار ليس من أقدم طبقة تكلفة متاحة وفق FIFO.", ErrorType.Conflict, "oilBarrelId");
    public static readonly OperationError OpenOilBarrelRequired = new("maintenance.open_oil_barrel_required", "يجب فتح برميل زيت أولاً، أو اختيار البرميل التالي إذا كانت العملية ستستنفد البرميل المفتوح.", ErrorType.Conflict, "nextOilBarrelId");
}
