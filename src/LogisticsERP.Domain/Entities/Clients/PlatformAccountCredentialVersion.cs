using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class PlatformAccountCredentialVersion : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public byte[] Ciphertext { get; set; } = [];
    public byte[] Nonce { get; set; } = [];
    public byte[] AuthenticationTag { get; set; } = [];
    public int KeyVersion { get; set; }
    public DateTimeOffset RotatedAtUtc { get; set; }
    public Guid RotatedByUserId { get; set; }
    public string RotationReason { get; set; } = string.Empty;
    public Guid? SupersededVersionId { get; set; }
}
