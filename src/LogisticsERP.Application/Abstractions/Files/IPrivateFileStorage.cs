using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Abstractions.Files;

public sealed record PrivateFileUpload(Stream Content, string OriginalFileName, string ContentType, long Length);
public sealed record StoredPrivateFile(string StoragePath, string StoredFileName, long Length, string Sha256Checksum, string ContentType, string OriginalFileName);
public sealed record PrivateFileDownload(Stream Content, string ContentType, string DownloadFileName, long Length);

public interface IPrivateFileStorage
{
    Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default);
    void DeleteBestEffort(string storagePath);
}

public static class PrivateFileErrors
{
    public static readonly OperationError InvalidFile = new("files.invalid_file", "الملف فارغ أو كبير جدًا أو غير مدعوم أو لا يتطابق مع نوعه المعلن.", ErrorType.Validation);
    public static readonly OperationError FileMissing = new("files.file_missing", "الملف المحفوظ غير موجود.", ErrorType.NotFound);
}
