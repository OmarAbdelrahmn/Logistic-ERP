using System.Security.Cryptography;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using Microsoft.Extensions.Hosting;

namespace LogisticsERP.Infrastructure.Files;

internal sealed class PrivateFileStorage(IHostEnvironment hostEnvironment) : IPrivateFileStorage
{
    private static readonly Dictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = ".pdf",
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png"
        };

    private static readonly Dictionary<string, HashSet<string>> AllowedOriginalExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = new(StringComparer.OrdinalIgnoreCase) { ".pdf" },
            ["image/jpeg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
            ["image/png"] = new(StringComparer.OrdinalIgnoreCase) { ".png" }
        };

    private readonly string storageRoot = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "private"));

    public async Task<Result<StoredPrivateFile>> StoreAsync(
        string relativeDirectory,
        PrivateFileUpload file,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        var originalFileName = Path.GetFileName(file.OriginalFileName);
        if (maximumBytes <= 0
            || file.Length is <= 0
            || file.Length > maximumBytes
            || string.IsNullOrWhiteSpace(file.OriginalFileName)
            || !string.Equals(originalFileName, file.OriginalFileName, StringComparison.Ordinal)
            || !Extensions.TryGetValue(file.ContentType, out var extension)
            || !AllowedOriginalExtensions[file.ContentType].Contains(Path.GetExtension(originalFileName)))
        {
            return Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile);
        }

        var safeRelative = relativeDirectory.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(safeRelative))
        {
            return Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile);
        }

        Directory.CreateDirectory(storageRoot);
        var directory = Path.GetFullPath(Path.Combine(storageRoot, safeRelative));
        if (!IsWithinRoot(directory))
        {
            return Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile);
        }

        Directory.CreateDirectory(directory);
        var storedName = $"{Guid.CreateVersion7():N}{extension}";
        var fullPath = Path.Combine(directory, storedName);
        var completed = false;
        try
        {
            await using var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[64 * 1024];
            var header = new byte[8];
            var headerLength = 0;
            long total = 0;
            while (true)
            {
                var read = await file.Content.ReadAsync(buffer, cancellationToken);
                if (read == 0) break;
                if (headerLength < header.Length)
                {
                    var copy = Math.Min(read, header.Length - headerLength);
                    buffer.AsSpan(0, copy).CopyTo(header.AsSpan(headerLength));
                    headerLength += copy;
                }
                total += read;
                if (total > maximumBytes)
                {
                    return Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile);
                }
                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (total == 0 || total != file.Length || !HeaderMatches(file.ContentType, header.AsSpan(0, headerLength)))
            {
                return Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile);
            }

            await destination.FlushAsync(cancellationToken);
            var relativePath = Path.GetRelativePath(hostEnvironment.ContentRootPath, fullPath).Replace('\\', '/');
            completed = true;
            return Result.Success(new StoredPrivateFile(
                relativePath,
                storedName,
                total,
                Convert.ToHexString(hash.GetHashAndReset()),
                file.ContentType.ToLowerInvariant(),
                originalFileName));
        }
        finally
        {
            if (!completed && File.Exists(fullPath)) DeleteFile(fullPath);
        }
    }

    public Task<Result<PrivateFileDownload>> OpenReadAsync(
        string storagePath,
        string contentType,
        string downloadFileName,
        long length,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(storagePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return Task.FromResult(Result.Failure<PrivateFileDownload>(PrivateFileErrors.FileMissing));
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(Result.Success(new PrivateFileDownload(stream, contentType, Path.GetFileName(downloadFileName), length)));
    }

    public void DeleteBestEffort(string storagePath)
    {
        var path = Resolve(storagePath);
        if (path is not null) DeleteFile(path);
    }

    private string? Resolve(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || Path.IsPathRooted(storagePath)) return null;
        var fullPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        return IsWithinRoot(fullPath) ? fullPath : null;
    }

    private bool IsWithinRoot(string path) => path.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static bool HeaderMatches(string contentType, ReadOnlySpan<byte> header) => contentType.ToLowerInvariant() switch
    {
        "application/pdf" => header.Length >= 4 && header[..4].SequenceEqual("%PDF"u8),
        "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        "image/png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        _ => false
    };

    private static void DeleteFile(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
