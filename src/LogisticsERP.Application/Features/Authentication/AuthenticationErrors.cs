using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Authentication;

public static class AuthenticationErrors
{
    public static readonly OperationError InvalidRequest = new(
        "Authentication.InvalidRequest",
        "طلب المصادقة غير صالح.",
        ErrorType.Validation);

    public static readonly OperationError InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        "اسم المستخدم أو كلمة المرور غير صحيحة.",
        ErrorType.Unauthorized);

    public static readonly OperationError AccountLocked = new(
        "Authentication.AccountLocked",
        "الحساب مقفل مؤقتًا. حاول مرة أخرى لاحقًا.",
        ErrorType.Forbidden);

    public static readonly OperationError AccountUnavailable = new(
        "Authentication.AccountUnavailable",
        "الحساب غير متاح لتسجيل الدخول.",
        ErrorType.Forbidden);

    public static readonly OperationError AccountNotConfirmed = new(
        "Authentication.AccountNotConfirmed",
        "يجب تأكيد الحساب قبل تسجيل الدخول.",
        ErrorType.Forbidden);

    public static readonly OperationError InvalidRefreshToken = new(
        "Authentication.InvalidRefreshToken",
        "رمز التحديث غير صالح أو منتهي الصلاحية.",
        ErrorType.Unauthorized);

    public static readonly OperationError CurrentUserUnavailable = new(
        "Authentication.CurrentUserUnavailable",
        "المستخدم المصادق عليه حاليًا غير متاح.",
        ErrorType.Unauthorized);

    public static readonly OperationError InvalidCurrentPassword = new(
        "Authentication.InvalidCurrentPassword",
        "كلمة المرور الحالية غير صحيحة.",
        ErrorType.Validation);

    public static readonly OperationError PasswordRejected = new(
        "Authentication.PasswordRejected",
        "كلمة المرور الجديدة لا تستوفي سياسة كلمات المرور.",
        ErrorType.Validation);

    public static readonly OperationError SessionNotFound = new(
        "Authentication.SessionNotFound",
        "لم يتم العثور على الجلسة.",
        ErrorType.NotFound);

    public static readonly OperationError ConcurrentRefresh = new(
        "Authentication.ConcurrentRefresh",
        "تم استخدام رمز التحديث مسبقًا. سجّل الدخول مرة أخرى.",
        ErrorType.Unauthorized);

    public static readonly OperationError ConcurrentLogin = new(
        "Authentication.ConcurrentLogin",
        "اكتمل تسجيل دخول آخر في الوقت نفسه. أعد تسجيل الدخول لجعل هذا الجهاز الجلسة النشطة.",
        ErrorType.Conflict);
}
