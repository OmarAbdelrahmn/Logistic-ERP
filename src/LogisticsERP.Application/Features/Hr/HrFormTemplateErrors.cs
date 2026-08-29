using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class HrFormTemplateErrors
{
    public static readonly OperationError NotFound = new(
        "hr_form_templates.not_found",
        "The requested HR form template was not found.",
        ErrorType.NotFound);

    public static readonly OperationError VersionNotFound = new(
        "hr_form_templates.version_not_found",
        "The requested template version does not belong to this HR form template.",
        ErrorType.NotFound,
        "versionId");

    public static readonly OperationError DuplicateCode = new(
        "hr_form_templates.duplicate_code",
        "A non-archived HR form template already uses this code.",
        ErrorType.Conflict,
        "code");

    public static readonly OperationError InvalidMetadata = new(
        "hr_form_templates.invalid_metadata",
        "The template code, Arabic name, category, or description is invalid.",
        ErrorType.Validation);

    public static readonly OperationError InvalidDefinition = new(
        "hr_form_templates.invalid_definition",
        "The definition must be a schemaVersion 1 JSON object containing direction, page, sections.body, and a valid fields array. Its maximum encoded size is 512 KB.",
        ErrorType.Validation,
        "definition");

    public static readonly OperationError ConcurrencyConflict = new(
        "hr_form_templates.concurrency_conflict",
        "The template changed after it was loaded. Reload it and retry.",
        ErrorType.Conflict,
        "rowVersion");

    public static readonly OperationError ChangeNoteTooLong = new(
        "hr_form_templates.change_note_too_long",
        "The version change note cannot exceed 500 characters.",
        ErrorType.Validation,
        "changeNote");
}
