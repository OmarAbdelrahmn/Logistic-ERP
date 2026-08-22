namespace LogisticsERP.Infrastructure.Fleet;

public sealed class PdfGenerationOptions
{
    public const string SectionName = "PdfGeneration";
    public string QuestPdfLicense { get; set; } = string.Empty;
    public string ArabicFontFamily { get; set; } = "Arial";
    public string? ArabicFontPath { get; set; }
}
