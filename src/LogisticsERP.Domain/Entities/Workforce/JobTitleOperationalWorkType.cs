using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class JobTitleOperationalWorkType : AuditableEntity
{
    public Guid JobTitleId { get; set; }
    public Guid OperationalWorkTypeId { get; set; }
}
