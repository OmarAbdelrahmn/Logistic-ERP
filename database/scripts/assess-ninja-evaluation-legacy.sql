-- Read only. Run before migration/activation; no inferred values are written back.
-- Override @month to review another period. Dates are Riyadh business dates.
DECLARE @today date = CONVERT(date, DATEADD(hour, 3, SYSUTCDATETIME()));
DECLARE @month date = DATEFROMPARTS(YEAR(@today), MONTH(@today), 1);
DECLARE @next date = DATEADD(month, 1, @month);

SELECT a.Id AS AccountId, a.ExternalAccountId AS CaptainId, COUNT(*) AS AssignmentsMissingPricing
FROM app.RiderClientAssignments r
JOIN app.PlatformRiderAccounts a ON a.Id = r.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id = a.ClientPlatformId
WHERE p.Code = 'NINJA' AND r.Status IN (2, 3) AND r.EffectiveFrom < @next
  AND (r.EffectiveTo IS NULL OR r.EffectiveTo >= @month) AND r.NinjaRiderType IS NULL
GROUP BY a.Id, a.ExternalAccountId;

SELECT s.Id AS SourceId, s.PlatformRiderAccountId AS AccountId, s.ReportDate,
       s.TotalOrders AS SourceOrders, SUM(CONVERT(bigint, f.TotalOrders)) AS AllocatedOrders,
       s.TotalWorkingHours AS SourceHours, SUM(f.TotalWorkingHours) AS AllocatedHours
FROM ninja.NinjaDailyPerformanceSources s
LEFT JOIN ninja.NinjaDailyPerformance f ON f.SourceId = s.Id
WHERE s.ReportDate >= @month AND s.ReportDate < @next
GROUP BY s.Id, s.PlatformRiderAccountId, s.ReportDate, s.TotalOrders, s.TotalWorkingHours
HAVING COUNT(f.Id) = 0 OR SUM(CONVERT(bigint, f.TotalOrders)) <> s.TotalOrders
    OR SUM(f.TotalWorkingHours) <> s.TotalWorkingHours;

SELECT firstHolder.PlatformRiderAccountId AS AccountId, firstHolder.EffectiveTo AS HandoverDate,
       firstHolder.Id AS EarlierAssignmentId, nextHolder.Id AS NextAssignmentId,
       'NeedsExplicitPlannedWeights' AS EvaluationStatus
FROM app.RiderClientAssignments firstHolder
JOIN app.RiderClientAssignments nextHolder ON nextHolder.PlatformRiderAccountId = firstHolder.PlatformRiderAccountId
  AND nextHolder.EffectiveFrom = firstHolder.EffectiveTo AND nextHolder.Id <> firstHolder.Id
JOIN app.PlatformRiderAccounts a ON a.Id = firstHolder.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id = a.ClientPlatformId
WHERE p.Code = 'NINJA' AND firstHolder.Status IN (2, 3) AND nextHolder.Status IN (2, 3)
  AND firstHolder.EffectiveTo >= @month AND firstHolder.EffectiveTo < @next;

SELECT COUNT(*) AS LegacyImportsWithoutRetainedOriginalFileInTheOldModel
FROM ninja.NinjaDailyImports WHERE IsDeductions = 0;
-- Missing files/provenance/first-delivery dates must remain unknown. This script never treats missing days as zero.
