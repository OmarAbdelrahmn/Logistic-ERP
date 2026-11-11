using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerFinanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionalWalletImageLinkSurvivesConfirmation(bool attachImage)
    {
        var root = Path.Combine(Path.GetTempPath(), "HungerWalletImages", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new PrivateFileStorage(new TestHostEnvironment(root));
            await using var f = await Fixture.Create(files: storage);
            var ct = TestContext.Current.CancellationToken;
            f.Db.RemoveRange(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
            await f.Db.SaveChangesAsync(ct);
            var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
            await using var stream = new MemoryStream(bytes);
            var image = attachImage ? new PrivateFileUpload(stream, "wallet.png", "image/png", bytes.Length) : null;
            var review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, Delivery(f), image, ct));
            if (attachImage)
            {
                Assert.StartsWith($"/private/hunger/wallets/{f.Account.Id:D}/", review.WalletEvidence);
                var savedPath = Path.Combine(root, "wwwroot", review.WalletEvidence!.TrimStart('/'));
                Assert.Equal(bytes, await File.ReadAllBytesAsync(savedPath, ct));
            }
            else Assert.Null(review.WalletEvidence);
            var confirmed = Value(await f.Service.ConfirmWalletAsync(review.ReviewId, ct));
            var entry = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == confirmed.EventId, ct);
            Assert.Equal(review.WalletEvidence ?? string.Empty, entry.EvidenceReference);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WalletImageRejectsSpoofedImageWithoutSavingFileOrReview()
    {
        var root = Path.Combine(Path.GetTempPath(), "HungerWalletImages", Guid.NewGuid().ToString("N"));
        try
        {
            await using var f = await Fixture.Create(files: new PrivateFileStorage(new TestHostEnvironment(root)));
            var ct = TestContext.Current.CancellationToken;
            await using var stream = new MemoryStream("not an image"u8.ToArray());
            var result = await f.Service.ReviewWalletWithImageAsync(f.Account.Id, Delivery(f),
                new(stream, "wallet.png", "image/png", stream.Length), ct);
            Assert.True(result.IsFailure);
            Assert.Equal("files.invalid_file", result.Error.Code);
            Assert.Empty(await f.Db.Set<HungerWalletReview>().ToArrayAsync(ct));
            Assert.Empty(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
