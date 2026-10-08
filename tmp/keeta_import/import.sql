SET XACT_ABORT ON;
BEGIN TRY
  BEGIN TRANSACTION;
  IF DB_NAME() <> N'db67927' THROW 51100, 'Unexpected database', 1;
  IF (SELECT COUNT(*) FROM #KeetaImport) <> 259 THROW 51101, 'Unexpected source row count', 1;
  IF EXISTS (SELECT ExternalId FROM #KeetaImport GROUP BY ExternalId HAVING COUNT(*) > 1)
    THROW 51102, 'Duplicate source account ID', 1;
  IF EXISTS (SELECT OwnerIqama FROM #KeetaImport GROUP BY OwnerIqama HAVING COUNT(*) > 1)
    THROW 51103, 'Duplicate source iqama', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport WHERE LEN(OwnerIqama) <> 10 OR OwnerIqama LIKE '%[^0-9]%')
    THROW 51104, 'Invalid iqama', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport WHERE LEN(ExternalId) > 26 OR LEN(OwnerNameEn) > 150)
    THROW 51105, 'Account field too long', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport i WHERE
      (SELECT COUNT(*) FROM app.Employees e WHERE e.IqamaNo = i.OwnerIqama AND e.IsDeleted = 0) <> 1)
    THROW 51106, 'Owner employee match missing or ambiguous', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport i WHERE NOT EXISTS
      (SELECT 1 FROM app.OperatingCities c WHERE c.Id = i.OperatingCityId AND c.IsDeleted = 0))
    THROW 51107, 'Operating city missing', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport i WHERE NOT EXISTS
      (SELECT 1 FROM app.Sponsors s WHERE s.Id = i.SponsorId AND s.IsDeleted = 0 AND s.Status = 1))
    THROW 51108, 'Sponsor missing or inactive', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport i WHERE i.VehicleId IS NOT NULL AND NOT EXISTS
      (SELECT 1 FROM app.Vehicles v WHERE v.Id = i.VehicleId AND v.IsDeleted = 0
       AND v.SerialNumber = i.VehicleSerial AND v.PlateNumberAr = i.ExpectedVehiclePlate))
    THROW 51109, 'Vehicle identity changed since matching', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport WHERE VehicleId IS NOT NULL AND ReviewStatus <> N'مقبول')
    THROW 51110, 'Unaccepted account has vehicle assignment', 1;
  IF EXISTS (SELECT 1 FROM #KeetaImport WHERE VehicleId IS NOT NULL AND AssignmentHoldReason <> N'')
    THROW 51111, 'Held row has vehicle assignment', 1;
  IF (SELECT COUNT(*) FROM platform.ClientPlatforms WHERE Code = 'KEETA' AND Status = 1 AND IsDeleted = 0) <> 1
    THROW 51112, 'Keeta platform missing', 1;
  IF NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Id = '019c18d5-62e1-7000-c000-000000000001'
      AND UserName = 'Omar' AND IsDeleted = 0 AND IsDevelopmentOnly = 0)
    THROW 51113, 'Import actor unavailable', 1;
  IF EXISTS (SELECT 1 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p
      ON p.Id = a.ClientPlatformId WHERE p.Code = 'KEETA')
    THROW 51114, 'Keeta accounts already exist; import requires reconciliation', 1;

  DECLARE @Now datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
  DECLARE @Actor uniqueidentifier = '019c18d5-62e1-7000-c000-000000000001';
  DECLARE @Platform uniqueidentifier = (SELECT Id FROM platform.ClientPlatforms WHERE Code = 'KEETA');

  INSERT app.PlatformRiderAccounts
      (Id, ClientPlatformId, RegisteredEmployeeId, OperatingCityId, SponsorId,
       Code, ExternalAccountId, UserName, PaymentModel, Status, StatusReason,
       OwnershipNotes, OperationalNotes, CreatedAtUtc, CreatedByUserId, IsDeleted)
  SELECT NEWID(), @Platform, e.Id, i.OperatingCityId, i.SponsorId,
      CONCAT('KEETA-', i.ExternalId), i.ExternalId, i.OwnerNameEn, 1,
      CASE WHEN i.ReviewStatus = N'مقبول' THEN 1 ELSE 3 END,
      CONCAT(N'Keeta review: ', i.ReviewStatus, N'; delivery status: ', i.ServiceStatus),
      CONCAT(N'Registered owner iqama: ', i.OwnerIqama, N'; source sheet row: ', i.SheetRow),
      CONCAT(N'Source vehicle type: ', i.VehicleType, N'; serial: ', i.VehicleSerial,
             N'; plate: ', i.VehiclePlate, N'; settlement mode: ', i.SettlementMode,
             CASE WHEN i.AssignmentHoldReason = N'' THEN N''
                  ELSE CONCAT(N'; vehicle assignment held: ', i.AssignmentHoldReason) END),
      @Now, @Actor, 0
  FROM #KeetaImport i JOIN app.Employees e ON e.IqamaNo = i.OwnerIqama AND e.IsDeleted = 0;
  IF @@ROWCOUNT <> 259 THROW 51115, 'Account insert count mismatch', 1;

  INSERT app.VehiclePlatformAccountAssignments
      (Id, VehicleId, PlatformRiderAccountId, AssignedAtUtc, AssignmentReason,
       ApprovalStatus, ApprovedAtUtc, ApprovedByUserId, Status,
       CreatedAtUtc, CreatedByUserId, IsDeleted)
  SELECT NEWID(), i.VehicleId, a.Id, @Now,
      CONCAT(N'Keeta workbook import, source row ', i.SheetRow),
      1, @Now, @Actor, 1, @Now, @Actor, 0
  FROM #KeetaImport i JOIN app.PlatformRiderAccounts a
      ON a.ClientPlatformId = @Platform AND a.ExternalAccountId = i.ExternalId
  JOIN app.Vehicles v ON v.Id = i.VehicleId
  WHERE i.VehicleId IS NOT NULL
    AND (@IncludeUnavailableVehicles = 1 OR v.CurrentOperationalStatus IN (1, 2));

  DECLARE @AssignmentCount int = @@ROWCOUNT;
  IF @AssignmentCount <> (SELECT COUNT(*) FROM #KeetaImport i JOIN app.Vehicles v ON v.Id=i.VehicleId
      WHERE @IncludeUnavailableVehicles = 1 OR v.CurrentOperationalStatus IN (1, 2))
    THROW 51116, 'Assignment insert count mismatch', 1;
  SELECT 'accounts' AS Metric, COUNT(*) AS N FROM app.PlatformRiderAccounts WHERE ClientPlatformId = @Platform
  UNION ALL SELECT 'assignments', @AssignmentCount
  UNION ALL SELECT 'unassigned', 259-@AssignmentCount;
  SELECT i.AssignmentHoldReason, COUNT(*) AS N FROM #KeetaImport i
  WHERE i.AssignmentHoldReason <> N'' GROUP BY i.AssignmentHoldReason;
  IF @DryRun = 1 ROLLBACK TRANSACTION ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
