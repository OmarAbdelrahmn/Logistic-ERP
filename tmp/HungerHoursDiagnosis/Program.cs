using System.Reflection;
using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Infrastructure.Persistence;

var path = args[0];
using var source = new XLWorkbook(path);
var sheet = source.Worksheet(1);
var header = sheet.Row(1).CellsUsed().Single(c => c.GetString() == "Actual Working Hours").Address.ColumnNumber;
var parser = typeof(ApplicationDbContext).Assembly.GetType("LogisticsERP.Infrastructure.Hunger.HungerQualityService")!
    .GetMethod("ParseOperations", BindingFlags.NonPublic | BindingFlags.Static)!;
var envelope = parser.Invoke(null, [File.ReadAllBytes(path)])!;
var json = JsonSerializer.SerializeToElement(envelope, envelope.GetType());
var records = json.GetProperty("Inputs").EnumerateArray().ToArray();
var mismatches = records.Where(input => input.GetProperty("ActualWorkingHours").GetDecimal()
    != decimal.Round((decimal)sheet.Cell(input.GetProperty("RowNumber").GetInt32(), header).GetDouble(), 8)).ToArray();
var hours = records.Select(input => input.GetProperty("ActualWorkingHours").GetDecimal()).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new { SourceRows=sheet.RowsUsed().Count()-1, ParsedRows=records.Length,
    RejectedRows=json.GetProperty("Errors").GetArrayLength(), HoursMismatches=mismatches.Length,
    TotalHours=hours.Sum(), Minimum=hours.Min(), Maximum=hours.Max(), FirstHours=hours.Take(6),
    CellTypes=sheet.RowsUsed().Skip(1).GroupBy(r=>r.Cell(header).DataType.ToString()).ToDictionary(g=>g.Key,g=>g.Count()) }));
