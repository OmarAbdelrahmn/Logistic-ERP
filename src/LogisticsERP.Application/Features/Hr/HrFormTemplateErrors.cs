using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class HrFormTemplateErrors
{
    public static readonly OperationError NotFound = new(
        "hr_form_templates.not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound);

    public static readonly OperationError VersionNotFound = new(
        "hr_form_templates.version_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "versionId");

    public static readonly OperationError DuplicateCode = new(
        "hr_form_templates.duplicate_code",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "code");

    public static readonly OperationError InvalidMetadata = new(
        "hr_form_templates.invalid_metadata",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError InvalidDefinition = new(
        "hr_form_templates.invalid_definition",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "definition");

    public static readonly OperationError ConcurrencyConflict = new(
        "hr_form_templates.concurrency_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "rowVersion");

    public static readonly OperationError ChangeNoteTooLong = new(
        "hr_form_templates.change_note_too_long",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "changeNote");
}
