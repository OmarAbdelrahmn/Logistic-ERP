using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Tags;

public sealed class EmployeeTag : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid TagId { get; set; }
}

public sealed class HousingTag : AuditableEntity
{
    public Guid HousingId { get; set; }
    public Guid TagId { get; set; }
}

public sealed class ClientContractTag : AuditableEntity
{
    public Guid ClientContractId { get; set; }
    public Guid TagId { get; set; }
}

public sealed class PlatformRiderAccountTag : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid TagId { get; set; }
}
