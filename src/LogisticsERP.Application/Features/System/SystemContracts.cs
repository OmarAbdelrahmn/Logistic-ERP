using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.System;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);

public sealed record NotificationResponse(
    Guid Id,
    string EventType,
    string Severity,
    string TitleAr,
    string TitleEn,
    string BodyAr,
    string BodyEn,
    string? SourceEntityType,
    Guid? SourceEntityId,
    string? DeepLink,
    DateTimeOffset VisibleAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? ArchivedAtUtc,
    string RowVersion);

public sealed record CreateNotificationRequest(
    Guid RecipientUserId,
    string EventType,
    string Severity,
    string TitleAr,
    string TitleEn,
    string BodyAr,
    string BodyEn,
    string? SourceEntityType,
    Guid? SourceEntityId,
    string? DeepLink,
    string? ScopeSnapshotJson,
    string DeduplicationKey,
    DateTimeOffset? VisibleAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record NotificationStateRequest(string Action, string RowVersion);

public interface INotificationService
{
    Task<Result<PageResponse<NotificationResponse>>> GetMineAsync(bool unreadOnly, int pageSize, string? cursor, CancellationToken cancellationToken = default);
    Task<Result<int>> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<Result<NotificationResponse>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<Result<NotificationResponse>> ChangeStateAsync(Guid id, NotificationStateRequest request, CancellationToken cancellationToken = default);
}

public sealed record AuditEntryResponse(
    Guid EventId,
    long Sequence,
    Guid? ActorUserId,
    string ActorType,
    string Action,
    string Category,
    string EntityType,
    Guid? EntityId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string? Reason,
    string? BeforeJson,
    string? AfterJson,
    string Source,
    int SchemaVersion);

public sealed record AuditQuery(
    Guid? ActorUserId,
    string? EntityType,
    Guid? EntityId,
    string? Action,
    string? CorrelationId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int PageSize,
    long? BeforeSequence);

public interface IAuditQueryService
{
    Task<Result<PageResponse<AuditEntryResponse>>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
    Task<Result<AuditEntryResponse>> GetAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public sealed record SavedViewUpsertRequest(
    string ModuleKey,
    string Name,
    int SchemaVersion,
    string FiltersJson,
    string SortingJson,
    string ColumnsJson,
    string ColumnOrderJson,
    string Density,
    string? RowVersion);

public sealed record SavedViewResponse(
    Guid Id,
    string ModuleKey,
    string Name,
    int SchemaVersion,
    string FiltersJson,
    string SortingJson,
    string ColumnsJson,
    string ColumnOrderJson,
    string Density,
    string RowVersion);

public interface ISavedViewService
{
    Task<Result<IReadOnlyList<SavedViewResponse>>> GetMineAsync(string? moduleKey, CancellationToken cancellationToken = default);
    Task<Result<SavedViewResponse>> UpsertAsync(Guid? id, SavedViewUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default);
}

public sealed record CreateExportRequest(
    string ReportType,
    int ReportVersion,
    string FilterSnapshotJson,
    string Format,
    bool IncludesSensitiveValues,
    string? SensitiveExportReason);

public sealed record ExportJobResponse(
    Guid Id,
    string ReportType,
    int ReportVersion,
    string Format,
    bool IncludesSensitiveValues,
    string Status,
    int ProgressPercentage,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? ArtifactSizeBytes,
    DateTimeOffset? ArtifactExpiresAtUtc,
    string? ErrorCode,
    string RowVersion);

public sealed record ExportArtifactResponse(Stream Content, string ContentType, string DownloadFileName, long Length);

public interface IExportService
{
    Task<Result<IReadOnlyList<ExportJobResponse>>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<Result<ExportJobResponse>> CreateAsync(CreateExportRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExportJobResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ExportArtifactResponse>> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default);
}

public sealed record DatasetVersionResponse(string ModuleKey, long Version, DateTimeOffset LastChangedAtUtc, string RowVersion);

public interface IDatasetVersionService
{
    Task<Result<IReadOnlyList<DatasetVersionResponse>>> GetAsync(string? moduleKey, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string moduleKey, CancellationToken cancellationToken = default);
}

