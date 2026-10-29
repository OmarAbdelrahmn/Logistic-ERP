using LogisticsERP.Domain.Common;
namespace LogisticsERP.Domain.Entities.Clients;
public sealed class PlatformAccountDayContribution : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid RepresentativeAssignmentId { get; set; }
    public string AssignmentIdsJson { get; set; } = "[]";
    public string? FiguresJson { get; set; }
    public string Reason { get; set; } = string.Empty;
}
