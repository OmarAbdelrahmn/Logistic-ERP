using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hunger;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerQualityRulesTests
{
    [Fact]
    public void CumulativeSnapshotsProduceDailyDifferences()
    {
        var first = HungerQualityRules.Split(new(CompletedOrders: 20), new(), null);
        var second = HungerQualityRules.Split(new(CompletedOrders: 45), new(CompletedOrders: 20), null);
        var third = HungerQualityRules.Split(new(CompletedOrders: 72), new(CompletedOrders: 45), null);
        Assert.Null(first.Error); Assert.Null(second.Error); Assert.Null(third.Error);
        Assert.Equal(20, first.Daily.CompletedOrders); Assert.Equal(25, second.Daily.CompletedOrders);
        Assert.Equal(27, third.Daily.CompletedOrders);
        Assert.Equal(72, (first.Daily + second.Daily + third.Daily).CompletedOrders);
    }
    [Fact]
    public void LateSnapshotSplitsNextIntervalWithoutChangingCumulative()
    {
        var result = HungerQualityRules.Split(new(CompletedOrders: 45), new(CompletedOrders: 20), new(CompletedOrders: 72));
        Assert.Null(result.Error); Assert.Equal(25, result.Daily.CompletedOrders); Assert.Equal(27, result.NextDaily!.CompletedOrders);
        Assert.Equal(72, (new HungerQualityCounters(CompletedOrders: 20) + result.Daily + result.NextDaily).CompletedOrders);
    }
    [Theory]
    [InlineData(19, 20, 72)]
    [InlineData(73, 20, 72)]
    public void InconsistentSnapshotIsRejected(int uploaded, int prior, int next)
    {
        var result = HungerQualityRules.Split(new(CompletedOrders: uploaded), new(CompletedOrders: prior), new(CompletedOrders: next));
        Assert.NotNull(result.Error);
    }
    [Fact]
    public void SuccessfulAppealAllowsSignedFailedOrderAdjustments()
    {
        var result = HungerQualityRules.Split(new(FailedOrdersByRider: 2), new(FailedOrdersByRider: 5), new(FailedOrdersByRider: 1));
        Assert.Null(result.Error); Assert.Equal(-3, result.Daily.FailedOrdersByRider); Assert.Equal(-1, result.NextDaily!.FailedOrdersByRider);
        Assert.NotNull(HungerQualityRules.Split(new(FailedOrdersByRider: -1), new(), null).Error);
    }
    [Theory]
    [InlineData("A", 1)]
    [InlineData(" b ", 2)]
    [InlineData("C", 3)]
    [InlineData("Z", 26)]
    [InlineData("Unknown", null)]
    public void RankPreservesAlphabeticMeaning(string segment, int? expected) => Assert.Equal(expected, HungerQualityRules.Rank(segment));
    [Fact]
    public void InvalidRowDoesNotDiscardValidRowsAndRatesAreNormalized()
    {
        using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet("Quality");
        for (var i = 0; i < HungerQualityParser.Headers.Length; i++) sheet.Cell(1, i + 1).Value = HungerQualityParser.Headers[i];
        for (var row = 2; row <= 3; row++)
        {
            sheet.Cell(row, 1).Value = row.ToString(CultureInfo.InvariantCulture);
            for (var column = 2; column <= 7; column++) sheet.Cell(row, column).Value = 20;
            sheet.Cell(row, 8).Value = "B";
            for (var column = 9; column <= 11; column++) sheet.Cell(row, column).Value = "95%";
        }
        sheet.Cell(3, 3).Value = -1;
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        var result = HungerQualityParser.Parse("test.xlsx", stream.ToArray());
        Assert.Null(result.FileError); Assert.Single(result.Inputs); Assert.Single(result.Errors);
        Assert.Equal(0.95m, result.Inputs[0].FinalDeliveryQualityScore);
    }
}
