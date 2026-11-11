namespace LogisticsERP.Api.Controllers;

public sealed class HungerEvidenceForm
{
    public Guid PlatformId { get; init; }
    public Guid AccountId { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public DateOnly ObservedOn { get; init; }
    public IFormFile? File { get; init; }
}
