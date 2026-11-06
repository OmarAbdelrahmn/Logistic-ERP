using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Hunger;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerQualityServiceTests
{
    [Fact]
    public async Task HungerCaptainClassificationIsSourceDatedAndIndependentOfNoShowAndQualitySegment()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        byte[] WithClassification(string classification)
        {
            using var book = new XLWorkbook(new MemoryStream(OperationsFile("8", noShow: 3)));
            var sheet = book.Worksheet(1); sheet.Cell(1, 10).Value = "CaptainClassification"; sheet.Cell(2, 10).Value = classification;
            using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
        }
        var preview = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", WithClassification("HS Gold"), new(2026, 11, 1), ct));
        Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct));
        var row = Assert.Single(Value(await f.Service.OperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, null, 1, 50, ct)).Items);
        Assert.Equal("HS Gold", row.CaptainClassification); Assert.Equal(3, row.NoShowCount); Assert.Equal(2, row.RejectedOrders); Assert.Equal(1, row.CancelledOrders);
        var export = Value(await f.Service.ExportOperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, ct));
        using var book = new XLWorkbook(new MemoryStream(export.Content)); Assert.Equal("HS Gold", book.Worksheet(1).Cell(2, 16).GetString());
        var conflict = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", WithClassification("HS Silver"), new(2026, 11, 1), ct));
        Assert.False(conflict.CanConfirm);
        Assert.Equal("HS Gold", (await f.Db.Set<HungerDailyOperation>().SingleAsync(ct)).CaptainClassification);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
    }
}
