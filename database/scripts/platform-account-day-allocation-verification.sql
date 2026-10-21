-- Read-only post-migration verification. Every reconciliation result set should be empty.
SET NOCOUNT ON;

SELECT 'NinjaPerformance' Dataset, s.Id SourceId, s.PlatformRiderAccountId, s.ReportDate,
       s.TotalOrders, SUM(p.TotalOrders) AllocatedOrders, s.TotalWorkingHours, SUM(p.TotalWorkingHours) AllocatedHours
FROM ninja.NinjaDailyPerformanceSources s LEFT JOIN ninja.NinjaDailyPerformance p ON p.SourceId = s.Id
GROUP BY s.Id,s.PlatformRiderAccountId,s.ReportDate,s.TotalOrders,s.TotalWorkingHours
HAVING COUNT(p.Id) = 0 OR SUM(p.TotalOrders) <> s.TotalOrders OR SUM(p.TotalWorkingHours) <> s.TotalWorkingHours;

SELECT 'NinjaDeductions' Dataset, s.Id SourceId, s.PlatformRiderAccountId, s.ReportDate,
       s.TotalCollectedAmountAtDelivery, SUM(p.TotalCollectedAmountAtDelivery) AllocatedCollected,
       s.TotalDeductions, SUM(p.TotalDeductions) AllocatedDeductions
FROM ninja.NinjaDailyDeductionSources s LEFT JOIN ninja.NinjaDailyDeductions p ON p.SourceId = s.Id
GROUP BY s.Id,s.PlatformRiderAccountId,s.ReportDate,s.TotalCollectedAmountAtDelivery,s.TotalDeductions
HAVING COUNT(p.Id) = 0 OR SUM(p.TotalCollectedAmountAtDelivery) <> s.TotalCollectedAmountAtDelivery OR SUM(p.TotalDeductions) <> s.TotalDeductions;

SELECT 'ChefzPerformance' Dataset, s.Id SourceId, s.PlatformRiderAccountId,s.ReportDate
FROM chefz.DailyPerformanceSources s LEFT JOIN chefz.DailyPerformance p ON p.SourceId = s.Id
GROUP BY s.Id,s.PlatformRiderAccountId,s.ReportDate,s.AcceptedOffers,s.TotalValidOffers,s.DeclinedOffers,s.MissedOffers
HAVING COUNT(p.Id) = 0 OR SUM(p.AcceptedOffers) <> s.AcceptedOffers OR SUM(p.TotalValidOffers) <> s.TotalValidOffers
    OR SUM(p.DeclinedOffers) <> s.DeclinedOffers OR SUM(p.MissedOffers) <> s.MissedOffers;

-- Exact source and payable rider identity must agree for every allocated observation.
SELECT 'NinjaPerformanceIdentity' Dataset,p.Id FROM ninja.NinjaDailyPerformance p
JOIN ninja.NinjaDailyPerformanceSources s ON s.Id=p.SourceId JOIN app.RiderClientAssignments a ON a.Id=p.RiderClientAssignmentId
WHERE p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId
   OR p.PlatformRiderAccountId<>a.PlatformRiderAccountId OR p.RiderProfileId<>a.RiderProfileId;
SELECT 'NinjaDeductionIdentity' Dataset,p.Id FROM ninja.NinjaDailyDeductions p
JOIN ninja.NinjaDailyDeductionSources s ON s.Id=p.SourceId JOIN app.RiderClientAssignments a ON a.Id=p.RiderClientAssignmentId
WHERE p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId
   OR p.PlatformRiderAccountId<>a.PlatformRiderAccountId OR p.RiderProfileId<>a.RiderProfileId;
SELECT 'ChefzPerformanceIdentity' Dataset,p.Id FROM chefz.DailyPerformance p
JOIN chefz.DailyPerformanceSources s ON s.Id=p.SourceId JOIN app.RiderClientAssignments a ON a.Id=p.RiderClientAssignmentId
WHERE p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId
   OR p.PlatformRiderAccountId<>a.PlatformRiderAccountId OR p.RiderProfileId<>a.RiderProfileId;

-- This is a review list, not permission to guess a legacy handover sequence.
SELECT 'UnresolvedLegacyOrder' Issue,PlatformRiderAccountId,BusinessDate,COUNT(*) Participants
FROM app.PlatformAccountDayUsages WHERE IsDeleted=0
GROUP BY PlatformRiderAccountId,BusinessDate HAVING COUNT(*)>1 AND COUNT([Sequence])<>COUNT(*);

SELECT 'ChefzFullTimeOverlap' Issue,a.PlatformRiderAccountId,a.Id FirstAssignment,b.Id SecondAssignment
FROM app.RiderClientAssignments a JOIN app.RiderClientAssignments b ON b.PlatformRiderAccountId=a.PlatformRiderAccountId AND b.Id>a.Id
JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.PlatformRiderAccountId AND d.AccountType=2 AND d.IsDeleted=0
WHERE a.IsDeleted=0 AND b.IsDeleted=0 AND a.Status<>4 AND b.Status<>4
  AND a.EffectiveFrom<=COALESCE(b.EffectiveTo,'99991231') AND b.EffectiveFrom<=COALESCE(a.EffectiveTo,'99991231');

-- Historical imports intentionally have no invented row evidence. Verify only imports with retained rows.
SELECT 'ChefzOrderMoney' Issue,s.Id SettlementId
FROM chefz.WalletSettlements s JOIN chefz.ImportedOrderRows r
  ON r.ImportId=s.ImportId AND r.RiderClientAssignmentId=s.RiderClientAssignmentId AND r.BusinessDate BETWEEN s.PeriodFrom AND s.PeriodTo
GROUP BY s.Id,s.SourceRows,s.DeliveryFees,s.Tips,s.Compensation,s.Bonuses,s.Deductions
HAVING COUNT(*)<>s.SourceRows OR SUM(r.DeliveryFees)<>s.DeliveryFees OR SUM(r.Tips)<>s.Tips
  OR SUM(r.Compensation)<>s.Compensation OR SUM(r.Bonuses)<>s.Bonuses OR SUM(r.Deductions)<>s.Deductions;

SELECT 'ChefzOrderRowCount' Issue,i.Id ImportId,i.SourceRows,COUNT(r.Id) RetainedRows
FROM chefz.OrderSettlementImports i JOIN chefz.ImportedOrderRows r ON r.ImportId=i.Id
GROUP BY i.Id,i.SourceRows HAVING COUNT(r.Id)<>i.SourceRows;
