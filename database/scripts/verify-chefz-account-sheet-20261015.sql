DECLARE @Platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * INTO #s FROM OPENJSON(@PlanJson,'$.accounts') WITH(Id uniqueidentifier '$.id',CityId uniqueidentifier '$.cityId',SponsorId uniqueidentifier '$.sponsorId',AccountType int '$.accountType',VehicleType nvarchar(20) '$.vehicleType',Status int '$.desiredStatus',DashboardStatus nvarchar(100) '$.dashboardStatus');
SELECT * INTO #i FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH(Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',UserIqama varchar(10) '$.userIqama',FromDate date '$.fromDate',ToDate date '$.toDate',Status int '$.status',Slot int '$.slot');
SELECT * INTO #c FROM OPENJSON(@PlanJson,'$.closeAssignments') WITH(Id uniqueidentifier '$.id',ToDate date '$.endDate',Status int '$.status');
SELECT 'Account fields' CheckName,COUNT(*) Mismatches FROM #s s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.Id LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id
 WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.Status<>s.Status OR a.OperatingCityId<>s.CityId OR a.SponsorId<>s.SponsorId OR d.AccountType<>s.AccountType OR d.VehicleType<>s.VehicleType OR d.DashboardStatus<>s.DashboardStatus
UNION ALL SELECT 'Assignments',COUNT(*) FROM #i s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id LEFT JOIN app.RiderProfiles r ON r.Id=a.RiderProfileId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.PlatformRiderAccountId<>s.AccountId OR a.RiderProfileId<>s.RiderId OR e.IqamaNo<>s.UserIqama OR a.EffectiveFrom<>s.FromDate OR ISNULL(a.EffectiveTo,'9999-12-31')<>ISNULL(s.ToDate,'9999-12-31') OR a.Status<>s.Status OR a.RiderAccountSlot<>s.Slot
UNION ALL SELECT 'Ended/cancelled previous assignments',COUNT(*) FROM #c s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.Status<>s.Status OR a.EffectiveTo<>s.ToDate
UNION ALL SELECT 'Original assignment restored',COUNT(*) FROM OPENJSON(@PlanJson,'$.reopenAssignments') WITH(Id uniqueidentifier '$.id',RiderId uniqueidentifier '$.riderId',FromDate date '$.fromDate',Slot int '$.slot') s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id WHERE a.Id IS NULL OR a.RiderProfileId<>s.RiderId OR a.Status<>2 OR a.EffectiveTo IS NOT NULL OR a.EffectiveFrom<>s.FromDate OR a.RiderAccountSlot<>s.Slot
UNION ALL SELECT 'Assignment events',COUNT(*) FROM #i s WHERE NOT EXISTS(SELECT 1 FROM app.RiderAssignmentEvents e WHERE e.RiderClientAssignmentId=s.Id AND e.ToStatus=2) OR (s.Status=3 AND NOT EXISTS(SELECT 1 FROM app.RiderAssignmentEvents e WHERE e.RiderClientAssignmentId=s.Id AND e.ToStatus=3))
UNION ALL SELECT 'Password versions',COUNT(*) FROM OPENJSON(@PlanJson,'$.credentials') WITH(Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',Ciphertext nvarchar(max) '$.ciphertext',Nonce nvarchar(100) '$.nonce',Tag nvarchar(100) '$.tag') c
 LEFT JOIN app.PlatformAccountCredentialVersions v ON v.Id=c.Id WHERE v.Id IS NULL OR v.PlatformRiderAccountId<>c.AccountId OR v.KeyVersion<>1
 OR v.Ciphertext<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Ciphertext"))','varbinary(max)')
 OR v.Nonce<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Nonce"))','varbinary(12)')
 OR v.AuthenticationTag<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Tag"))','varbinary(16)')
UNION ALL SELECT 'Account history overlap',COUNT(*) FROM #i s JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId=s.AccountId AND a.Id<>s.Id WHERE a.IsDeleted=0 AND a.Status IN(2,3) AND a.EffectiveFrom<=ISNULL(s.ToDate,'9999-12-31') AND ISNULL(a.EffectiveTo,'9999-12-31')>=s.FromDate
UNION ALL SELECT 'Active rider limit',COUNT(*) FROM(SELECT a.RiderProfileId FROM app.RiderClientAssignments a WHERE a.EffectiveTo IS NULL AND a.IsDeleted=0 AND a.RiderProfileId IN(SELECT RiderId FROM #i WHERE Status=2) GROUP BY a.RiderProfileId HAVING COUNT(*)>2)t
UNION ALL SELECT 'Chefz account count',ABS(COUNT(*)-465) FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@Platform AND IsDeleted=0
UNION ALL SELECT 'Performance count',ABS(COUNT(*)-149) FROM chefz.DailyPerformance
UNION ALL SELECT 'Settlement count',COUNT(*) FROM chefz.WalletSettlements
UNION ALL SELECT 'Audit record',ABS(COUNT(*)-1) FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId AND Action='ChefzAccountSheetImport';
SELECT a.RiderProfileId,COUNT(*) AccountCount FROM app.RiderClientAssignments a JOIN app.PlatformRiderAccounts p ON p.Id=a.PlatformRiderAccountId WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL AND p.ClientPlatformId=@Platform GROUP BY a.RiderProfileId HAVING COUNT(*)=2;
SELECT v.* FROM app.PlatformAccountCredentialVersions v JOIN OPENJSON(@PlanJson,'$.credentials') WITH(Id uniqueidentifier '$.id') s ON s.Id=v.Id;
DROP TABLE #s;DROP TABLE #i;DROP TABLE #c;
