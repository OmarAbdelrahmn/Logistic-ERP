-- Read-only. Run after the additive V3 migration. Current sponsor is NOT proof of historical ownership.
SET NOCOUNT ON;

SELECT m.Id AS MoneyMovementId, m.OccurredAtUtc, m.Amount, m.Fund, m.Direction, m.Purpose,
       m.OperationId, m.TransferredDebtId, m.HandoverId,
       a.Id AS PlatformAccountId, a.ExternalAccountId, a.SponsorId AS CurrentAccountSponsorId,
       s.EmployerIdentityNumber AS CurrentSponsorIdentifier,
       o.SponsorId AS ReviewedSponsorId, o.SponsorIdentifier AS ReviewedSponsorIdentifier, o.Evidence,
       (SELECT au.OccurredAtUtc, au.Action, au.ActorUserId, au.Reason, au.BeforeJson, au.AfterJson
        FROM audit.AuditEntries au
        WHERE au.EntityId IN(a.Id,m.Id,m.OperationId,m.TransferredDebtId)
        ORDER BY au.OccurredAtUtc FOR JSON PATH) AS AuditEvidenceJson
FROM jahez_v2.MoneyMovements m
LEFT JOIN jahez_v2.AccountSessions h ON h.Id=m.HandoverId
LEFT JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId
LEFT JOIN app.Sponsors s ON s.Id=a.SponsorId
LEFT JOIN jahez_v3.Ownership o ON o.Source=4 AND o.SourceId=m.Id
ORDER BY m.OccurredAtUtc,m.Id;

SELECT h.Id, h.BusinessDate, h.Status, h.CashboxAmount, h.BankAmount, h.PetroAppAmount,
       h.RequestedByUserId, h.AccountantUserId, h.ConfirmedAtUtc,
       (SELECT l.MoneyMovementId FROM jahez_v2.CashboxHandoverMovements l WHERE l.CashboxHandoverId=h.Id FOR JSON PATH) AS MembershipJson
FROM jahez_v2.CashboxHandovers h WHERE h.IsDeleted=0
ORDER BY h.BusinessDate,h.Id;

SELECT a.Code, a.NameAr, a.SponsorId, a.Location, a.Purpose,
       COALESCE(SUM(p.Amount),0) AS ReconstructedBalance
FROM jahez_v3.TreasurySubAccounts a
LEFT JOIN jahez_v3.Postings p ON p.SubAccountId=a.Id
WHERE a.IsPosting=1 AND a.IsDeleted=0
GROUP BY a.Code,a.NameAr,a.SponsorId,a.Location,a.Purpose
ORDER BY a.Code;
