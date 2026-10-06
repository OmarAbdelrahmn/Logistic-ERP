using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class PlatformAccountErrors
{
    public static OperationError Required(string field, string label) =>
        Validation("platform.account_required_field", field, $"حقل {label} مطلوب.");

    public static OperationError Invalid(string field, string description) =>
        Validation("platform.account_invalid_field", field, description);

    public static OperationError ReferenceUnavailable(string field, string label) => new(
        "platform.account_reference_unavailable",
        $"{label} غير موجود أو غير متاح للاختيار. اختر قيمة صالحة.",
        ErrorType.NotFound,
        field);

    public static readonly OperationError UnsupportedPaymentModel = new(
        "platform.payment_model_not_supported",
        "نموذج الدفع المحدد غير مدعوم في المنصة المختارة. اختر أحد نماذج الدفع المفعلة للمنصة.",
        ErrorType.Conflict,
        "paymentModel");

    public static readonly OperationError DuplicateCode = new(
        "platform.account_code_duplicate", "رمز الحساب مستخدم بالفعل. أدخل رمزًا آخر.", ErrorType.Conflict, "code");

    public static readonly OperationError DuplicateExternalAccountId = new(
        "platform.account_external_id_duplicate", "رقم الحساب الخارجي مستخدم بالفعل في هذه المنصة.",
        ErrorType.Conflict, "externalAccountId");

    public static readonly OperationError DuplicateOwnerAccount = new(
        "platform.account_owner_duplicate",
        "يوجد حساب متاح أو مسند لنفس المالك والمنصة والمدينة والكفيل. يجب تعليق الحساب الحالي أو إحالته للتقاعد أو أرشفته قبل إنشاء حساب بديل.",
        ErrorType.Conflict, "ownerRiderProfileId");

    private static OperationError Validation(string code, string field, string description) => new(
        code, description, ErrorType.Validation, field,
        new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, string[]> { [field] = [description] }
        });
}
