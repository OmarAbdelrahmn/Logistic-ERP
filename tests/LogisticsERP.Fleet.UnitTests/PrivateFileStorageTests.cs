using System.Security.Cryptography;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Infrastructure.Files;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PrivateFileStorageTests : IDisposable
{
    private static readonly byte[] PdfBytes = "%PDF-1.7\n1 0 obj\n%%EOF"u8.ToArray();
    private readonly string contentRoot = Path.Combine(Path.GetTempPath(), "LogisticsERP.Fleet.UnitTests", Guid.CreateVersion7().ToString("N"));
    private readonly PrivateFileStorage storage;

    public PrivateFileStorageTests()
    {
        Directory.CreateDirectory(contentRoot);
        storage = new PrivateFileStorage(new TestHostEnvironment(contentRoot));
    }

    [Fact]
    public async Task StoreAndOpenReadPreservePrivatePdfAndChecksum()
    {
        await using var upload = new MemoryStream(PdfBytes);

        var stored = await storage.StoreAsync(
            "vehicles/019c18d562e17000a000000000000001",
            new PrivateFileUpload(upload, "registration.pdf", "application/pdf", PdfBytes.LongLength),
            10 * 1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(stored.IsSuccess);
        Assert.StartsWith("wwwroot/private/vehicles/", stored.Value!.StoragePath, StringComparison.Ordinal);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(PdfBytes)), stored.Value.Sha256Checksum);
        Assert.EndsWith(".pdf", stored.Value.StoredFileName, StringComparison.OrdinalIgnoreCase);

        var opened = await storage.OpenReadAsync(stored.Value.StoragePath, stored.Value.ContentType, stored.Value.OriginalFileName, stored.Value.Length, TestContext.Current.CancellationToken);
        Assert.True(opened.IsSuccess);
        await using var content = opened.Value!.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, TestContext.Current.CancellationToken);
        Assert.Equal(PdfBytes, copy.ToArray());
    }

    [Fact]
    public async Task StoreRejectsDeclaredTypeThatDoesNotMatchMagicBytes()
    {
        var bytes = "not a pdf"u8.ToArray();
        await using var upload = new MemoryStream(bytes);

        var result = await storage.StoreAsync(
            "vehicles/test",
            new PrivateFileUpload(upload, "evidence.pdf", "application/pdf", bytes.LongLength),
            10 * 1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(Directory.EnumerateFiles(contentRoot, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("evidence.png", "application/pdf")]
    [InlineData("../evidence.pdf", "application/pdf")]
    [InlineData("evidence.exe", "application/pdf")]
    public async Task StoreRejectsInvalidOrUnsafeOriginalFileNames(string fileName, string contentType)
    {
        await using var upload = new MemoryStream(PdfBytes);

        var result = await storage.StoreAsync(
            "vehicles/test",
            new PrivateFileUpload(upload, fileName, contentType, PdfBytes.LongLength),
            10 * 1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StoreRejectsDirectoryTraversal()
    {
        await using var upload = new MemoryStream(PdfBytes);

        var result = await storage.StoreAsync(
            "../outside",
            new PrivateFileUpload(upload, "evidence.pdf", "application/pdf", PdfBytes.LongLength),
            10 * 1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.False(Directory.Exists(Path.Combine(contentRoot, "wwwroot", "outside")));
    }

    [Fact]
    public async Task StoreRejectsOversizedDeclarationBeforeWriting()
    {
        await using var upload = new MemoryStream(PdfBytes);

        var result = await storage.StoreAsync(
            "vehicles/test",
            new PrivateFileUpload(upload, "evidence.pdf", "application/pdf", 10 * 1024 * 1024 + 1L),
            10 * 1024 * 1024,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.False(Directory.Exists(Path.Combine(contentRoot, "wwwroot")));
    }

    public void Dispose()
    {
        if (Directory.Exists(contentRoot)) Directory.Delete(contentRoot, recursive: true);
        GC.SuppressFinalize(this);
    }
}
