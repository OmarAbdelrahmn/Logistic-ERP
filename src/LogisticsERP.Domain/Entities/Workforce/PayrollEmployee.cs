using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class PayrollEmployee : AuditableEntity
{
    public int Number { get; set; }
    public Guid SponsorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly JoiningDate { get; set; }
    public string PersonalIban { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string Status { get; set; } = string.Empty;
}
