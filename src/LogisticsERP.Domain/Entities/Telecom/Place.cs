using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Telecom;

public sealed class Place : Entity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<PhoneSimCard> PhoneSims { get; set; } = [];
}
