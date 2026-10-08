SET XACT_ABORT ON;
BEGIN TRY
  BEGIN TRANSACTION;
  IF DB_NAME() <> N'db67927' THROW 51200, 'Unexpected database', 1;
  IF (SELECT COUNT(*) FROM #Followup) <> 259 THROW 51201, 'Source row count changed', 1;
  IF (SELECT COUNT(*) FROM #Followup WHERE PaymentModel=2) <> 257
     OR (SELECT COUNT(*) FROM #Followup WHERE PaymentModel=1) <> 2
    THROW 51202, 'Settlement mode mapping changed', 1;
  IF (SELECT COUNT(*) FROM #Followup WHERE VehicleId IS NOT NULL AND AlreadyAssigned=0) <> 35
     OR (SELECT COUNT(*) FROM #Followup WHERE VehicleId IS NULL) <> 3
     OR (SELECT COUNT(*) FROM #Followup WHERE PlateConflict=1 AND AlreadyAssigned=0) <> 12
    THROW 51203, 'Vehicle matching changed', 1;
  IF EXISTS(SELECT ExternalId FROM #Followup GROUP BY ExternalId HAVING COUNT(*)>1)
    THROW 51204, 'Duplicate source account ID', 1;
  DECLARE @Platform uniqueidentifier =
    (SELECT Id FROM platform.ClientPlatforms WHERE Code='KEETA' AND Status=1 AND IsDeleted=0);
  IF @Platform IS NULL THROW 51205, 'Keeta platform unavailable', 1;
  IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@Platform)<>259
    THROW 51206, 'Live account count changed', 1;
  IF (SELECT COUNT(*) FROM app.VehiclePlatformAccountAssignments x
      JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
      WHERE a.ClientPlatformId=@Platform AND x.EndedAtUtc IS NULL AND x.IsDeleted=0)<>221
    THROW 51207, 'Live assignment count changed', 1;
  IF EXISTS(SELECT 1 FROM #Followup i LEFT JOIN app.PlatformRiderAccounts a
      ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=i.ExternalId AND a.IsDeleted=0
      LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId AND e.IsDeleted=0
      WHERE a.Id IS NULL OR e.IqamaNo<>i.OwnerIqama)
    THROW 51208, 'Live account owner match changed', 1;
  IF EXISTS(SELECT 1 FROM #Followup i JOIN app.PlatformRiderAccounts a
      ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=i.ExternalId
      LEFT JOIN app.VehiclePlatformAccountAssignments x
      ON x.PlatformRiderAccountId=a.Id AND x.EndedAtUtc IS NULL AND x.IsDeleted=0
      WHERE (i.AlreadyAssigned=1 AND (x.Id IS NULL OR x.VehicleId<>i.VehicleId))
         OR (i.AlreadyAssigned=0 AND x.Id IS NOT NULL))
    THROW 51209, 'Live assignment match changed', 1;
  IF EXISTS(SELECT 1 FROM #Followup i WHERE i.VehicleId IS NOT NULL AND NOT EXISTS
      (SELECT 1 FROM app.Vehicles v WHERE v.Id=i.VehicleId AND v.IsDeleted=0
       AND v.SerialNumber=i.VehicleSerial AND v.PlateNumberAr=i.ExpectedVehiclePlate))
    THROW 51210, 'Vehicle identity changed since matching', 1;
  IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id='019c18d5-62e1-7000-c000-000000000001'
      AND UserName='Omar' AND IsDeleted=0 AND IsDevelopmentOnly=0)
    THROW 51211, 'Import actor unavailable', 1;

  DECLARE @Now datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
  DECLARE @Actor uniqueidentifier = '019c18d5-62e1-7000-c000-000000000001';
  UPDATE a SET a.PaymentModel=i.PaymentModel, a.UpdatedAtUtc=@Now, a.UpdatedByUserId=@Actor
  FROM app.PlatformRiderAccounts a JOIN #Followup i ON i.ExternalId=a.ExternalAccountId
  WHERE a.ClientPlatformId=@Platform AND a.PaymentModel<>i.PaymentModel;
  DECLARE @PaymentUpdates int = @@ROWCOUNT;
  IF @PaymentUpdates<>257 THROW 51212, 'Payment update count mismatch', 1;

  UPDATE a SET
      a.OperationalNotes=CONCAT(N'Source vehicle type: ',i.VehicleType,N'; serial: ',i.VehicleSerial,
          N'; plate: ',i.SourcePlate,N'; settlement mode: ',i.SettlementMode),
      a.UpdatedAtUtc=@Now, a.UpdatedByUserId=@Actor
  FROM app.PlatformRiderAccounts a JOIN #Followup i ON i.ExternalId=a.ExternalAccountId
  WHERE a.ClientPlatformId=@Platform AND i.VehicleId IS NOT NULL AND i.AlreadyAssigned=0;
  IF @@ROWCOUNT<>35 THROW 51213, 'Account note update count mismatch', 1;

  INSERT app.VehiclePlatformAccountAssignments
      (Id,VehicleId,PlatformRiderAccountId,AssignedAtUtc,AssignmentReason,
       ApprovalStatus,ApprovedAtUtc,ApprovedByUserId,Status,
       CreatedAtUtc,CreatedByUserId,IsDeleted)
  SELECT NEWID(),i.VehicleId,a.Id,@Now,
      CONCAT(N'Keeta workbook follow-up, source row ',i.SheetRow,
          CASE WHEN i.PlateConflict=1 THEN CONCAT(N'; source plate: ',i.SourcePlate,
              N'; ERP plate: ',i.ExpectedVehiclePlate) ELSE N'' END),
      1,@Now,@Actor,1,@Now,@Actor,0
  FROM #Followup i JOIN app.PlatformRiderAccounts a
      ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=i.ExternalId
  WHERE i.VehicleId IS NOT NULL AND i.AlreadyAssigned=0;
  IF @@ROWCOUNT<>35 THROW 51214, 'New assignment count mismatch', 1;

  SELECT 'payment_model_salary' AS Metric,COUNT(*) AS N FROM app.PlatformRiderAccounts
      WHERE ClientPlatformId=@Platform AND PaymentModel=2
  UNION ALL SELECT 'payment_model_pay_per_order',COUNT(*) FROM app.PlatformRiderAccounts
      WHERE ClientPlatformId=@Platform AND PaymentModel=1
  UNION ALL SELECT 'active_assignments',COUNT(*) FROM app.VehiclePlatformAccountAssignments x
      JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
      WHERE a.ClientPlatformId=@Platform AND x.EndedAtUtc IS NULL AND x.IsDeleted=0
  UNION ALL SELECT 'unassigned_accounts',COUNT(*) FROM app.PlatformRiderAccounts a
      WHERE a.ClientPlatformId=@Platform AND NOT EXISTS
      (SELECT 1 FROM app.VehiclePlatformAccountAssignments x
       WHERE x.PlatformRiderAccountId=a.Id AND x.EndedAtUtc IS NULL AND x.IsDeleted=0);
  IF @DryRun=1 ROLLBACK TRANSACTION ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
