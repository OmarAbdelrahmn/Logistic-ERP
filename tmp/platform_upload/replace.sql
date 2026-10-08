SET XACT_ABORT ON;
BEGIN TRY
  BEGIN TRANSACTION;
  IF DB_NAME() <> N'db67927' THROW 51000, 'Unexpected database', 1;
  IF EXISTS (
      SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('app.PlatformRiderAccounts') AND name='SponsorId' AND is_nullable=0)
    ALTER TABLE app.PlatformRiderAccounts ALTER COLUMN SponsorId uniqueidentifier NULL;
  IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('app.PlatformRiderAccounts') AND name='SponsorId' AND is_nullable=0)
    THROW 51001, 'Outside-sponsorship migration has not been applied', 1;

  IF (SELECT COUNT(*) FROM #Accounts)<>1282 THROW 51002, 'Staged row count mismatch', 1;
  DECLARE @KeetaBefore int=(SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='KEETA');
  IF EXISTS(SELECT Platform,ExternalId FROM #Accounts GROUP BY Platform,ExternalId HAVING COUNT(*)>1)
    THROW 51003, 'Duplicate external account in file', 1;
  IF EXISTS(SELECT OwnerIqama FROM #Accounts WHERE LEN(OwnerIqama)<>10 OR OwnerIqama LIKE '%[^0-9]%')
    THROW 51004, 'Invalid owner iqama in file', 1;
  IF EXISTS(SELECT Platform FROM #Accounts WHERE Platform NOT IN ('JAHEZ','HUNGER','CHEFSZ'))
    THROW 51005, 'Unexpected platform in staged file', 1;
  IF (SELECT COUNT(*) FROM platform.ClientPlatforms WHERE Code IN ('JAHEZ','HUNGER','SHIFTZ') AND Status=1 AND IsDeleted=0)<>3
    THROW 51006, 'Required platform catalog missing', 1;
  IF EXISTS(SELECT 1 FROM #Accounts WHERE SourceStatus NOT IN (N'نشط (Active)',N'يعمل (نشط)',N'غير نشط (Inactive)',N'غير نشط (أرشيف)',N'موقوف (مشاكل نظامية)',N'محظور من التطبيق',N'معلق من المشرف'))
    THROW 51007, 'Unmapped status', 1;
  IF EXISTS(SELECT 1 FROM #Accounts WHERE City NOT IN (N'جدة',N'الرياض',N'بريدة'))
    THROW 51008, 'Unmapped city', 1;
  IF EXISTS(SELECT 1 FROM #Accounts WHERE Sponsor NOT IN (N'شركة اكسبرس جايت',N'شركة البوابا الموكبلا',N'مؤسسة البوابة التجارية',N'خارج الكفالة'))
    THROW 51009, 'Unmapped sponsor', 1;
  IF EXISTS(SELECT 1 FROM #Accounts WHERE Sponsor=N'خارج الكفالة' AND SourceStatus<>N'غير نشط (أرشيف)')
    THROW 51010, 'Outside sponsorship must be archived in this source', 1;
  IF EXISTS(SELECT IqamaNo FROM app.Employees WHERE IsDeleted=0 AND IqamaNo IN (SELECT OwnerIqama FROM #Accounts) GROUP BY IqamaNo HAVING COUNT(*)>1)
    THROW 51011, 'Ambiguous employee iqama', 1;
  IF EXISTS(SELECT DISTINCT a.OwnerIqama FROM #Accounts a WHERE NOT EXISTS(SELECT 1 FROM app.Employees e WHERE e.IqamaNo=a.OwnerIqama AND e.IsDeleted=0)
            AND NOT EXISTS(SELECT 1 FROM #OwnerNames n WHERE n.Iqama=a.OwnerIqama AND LEN(LTRIM(RTRIM(n.FullNameAr)))>0))
    THROW 51012, 'Missing owner name', 1;
  IF EXISTS(SELECT Iqama FROM #OwnerNames GROUP BY Iqama HAVING COUNT(*)>1)
    THROW 51013, 'Duplicate owner names', 1;
  IF EXISTS(SELECT 1 FROM #OwnerNames WHERE LEN(FullNameAr)>200)
    THROW 51014, 'Owner name exceeds employee limit', 1;

  SELECT a.Id INTO #OldAccounts FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
  WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ');
  IF (SELECT COUNT(*) FROM #OldAccounts)<>4 THROW 51015, 'Existing account count changed since backup', 1;
  IF EXISTS(SELECT Id FROM #OldAccounts EXCEPT SELECT Id FROM #ExpectedOldAccounts)
    OR EXISTS(SELECT Id FROM #ExpectedOldAccounts EXCEPT SELECT Id FROM #OldAccounts)
    THROW 51025, 'Existing account identities changed since backup', 1;
  SELECT a.Id INTO #OldAssignments FROM app.RiderClientAssignments a JOIN #OldAccounts o ON o.Id=a.PlatformRiderAccountId;
  SELECT h.Id INTO #OldHandovers FROM jahez.JahezAccountHandover h JOIN #OldAccounts o ON o.Id=h.PlatformRiderAccountId;
  SELECT r.Id INTO #OldApprovals FROM jahez.JahezApprovalRequest r
    WHERE r.HandoverId IN (SELECT Id FROM #OldHandovers) OR r.TargetAccountId IN (SELECT Id FROM #OldAccounts);
  IF (SELECT COUNT(*) FROM #OldAssignments)<>4 OR (SELECT COUNT(*) FROM #OldHandovers)<>4
    THROW 51016, 'Jahez linked history changed since backup', 1;

  DELETE d FROM jahez.JahezApprovalDecision d JOIN #OldApprovals o ON o.Id=d.RequestId;
  DELETE d FROM jahez.JahezAccountFee d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers) OR d.ApprovalRequestId IN (SELECT Id FROM #OldApprovals);
  DELETE d FROM jahez.JahezCashboxEntry d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezCommissionPolicyPeriod d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers) OR d.ApprovalRequestId IN (SELECT Id FROM #OldApprovals);
  DELETE d FROM jahez.JahezDailyDispatch d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezEarningsStatement d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezLedgerEntry d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezReminderState d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezRiderSettlement d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezTransaction d WHERE d.HandoverId IN (SELECT Id FROM #OldHandovers);
  DELETE d FROM jahez.JahezApprovalRequest d JOIN #OldApprovals o ON o.Id=d.Id;
  DELETE d FROM jahez.JahezAccountHandover d JOIN #OldHandovers o ON o.Id=d.Id;
  DELETE d FROM app.RiderAssignmentEvents d JOIN #OldAssignments o ON o.Id=d.RiderClientAssignmentId;
  DELETE d FROM app.RiderClientAssignments d JOIN #OldAssignments o ON o.Id=d.Id;
  DELETE d FROM app.PlatformAccountRegistrations d JOIN #OldAccounts o ON o.Id=d.PlatformRiderAccountId;
  DELETE d FROM app.PlatformAccountCredentialVersions d JOIN #OldAccounts o ON o.Id=d.PlatformRiderAccountId;
  DELETE d FROM app.PlatformRiderAccountTags d JOIN #OldAccounts o ON o.Id=d.PlatformRiderAccountId;
  DELETE d FROM app.VehiclePlatformAccountSwitches d JOIN #OldAccounts o ON o.Id=d.PlatformRiderAccountId;
  DELETE d FROM app.VehiclePlatformAccountAssignments d JOIN #OldAccounts o ON o.Id=d.PlatformRiderAccountId;
  DELETE a FROM app.PlatformRiderAccounts a JOIN #OldAccounts o ON o.Id=a.Id;
  IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ'))
    THROW 51017, 'Target accounts were not fully cleared', 1;

  IF NOT EXISTS(SELECT 1 FROM platform.GlobalCities WHERE Code='BURAIDAH' AND IsDeleted=0)
    INSERT platform.GlobalCities(Id,Code,NameAr,NameEn,RegionAr,RegionEn,CountryCode,DisplayOrder,Status,CreatedAtUtc,IsDeleted)
    VALUES(NEWID(),'BURAIDAH',N'بريدة','Buraidah',N'منطقة القصيم','Qassim Region','SA',3,1,SYSDATETIMEOFFSET(),0);
  IF NOT EXISTS(SELECT 1 FROM app.OperatingCities oc JOIN platform.GlobalCities gc ON gc.Id=oc.GlobalCityId WHERE gc.Code='BURAIDAH' AND oc.IsDeleted=0)
    INSERT app.OperatingCities(Id,GlobalCityId,EnabledFrom,Status,CreatedAtUtc,IsDeleted)
    SELECT NEWID(),Id,CONVERT(date,SYSDATETIMEOFFSET()),1,SYSDATETIMEOFFSET(),0 FROM platform.GlobalCities WHERE Code='BURAIDAH' AND IsDeleted=0;

  INSERT app.Employees(Id,IqamaNo,FullNameAr,Status,EngagementType,IsEmployee,CreatedAtUtc,IsDeleted,Notes)
  SELECT NEWID(),o.OwnerIqama,n.FullNameAr,1,2,0,SYSDATETIMEOFFSET(),0,
    N'Created as a registered platform-account owner from the 2026-10-07 directory. Employment and sponsorship are unverified.'
  FROM (SELECT DISTINCT OwnerIqama FROM #Accounts) o JOIN #OwnerNames n ON n.Iqama=o.OwnerIqama
  WHERE NOT EXISTS(SELECT 1 FROM app.Employees e WHERE e.IqamaNo=o.OwnerIqama AND e.IsDeleted=0);

  SELECT a.Platform,a.ExternalId,a.OwnerIqama,a.Vehicle,a.City,a.Sponsor,a.SourceStatus,a.SheetRow,
    p.Id AS PlatformId,e.Id AS OwnerId,oc.Id AS CityId,
    CASE a.Sponsor WHEN N'شركة اكسبرس جايت' THEN CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000042')
      WHEN N'شركة البوابا الموكبلا' THEN CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000041')
      WHEN N'مؤسسة البوابة التجارية' THEN CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000040') ELSE NULL END AS SponsorId,
    CASE WHEN a.SourceStatus IN (N'نشط (Active)',N'يعمل (نشط)') THEN 1
      WHEN a.SourceStatus IN (N'موقوف (مشاكل نظامية)',N'محظور من التطبيق',N'معلق من المشرف') THEN 3
      WHEN a.SourceStatus=N'غير نشط (Inactive)' THEN 4 ELSE 5 END AS ErpStatus
  INTO #Mapped
  FROM #Accounts a
  JOIN platform.ClientPlatforms p ON p.Code=CASE a.Platform WHEN 'CHEFSZ' THEN 'SHIFTZ' ELSE a.Platform END AND p.IsDeleted=0
  JOIN app.Employees e ON e.IqamaNo=a.OwnerIqama AND e.IsDeleted=0
  JOIN platform.GlobalCities gc ON gc.NameAr=a.City AND gc.IsDeleted=0
  JOIN app.OperatingCities oc ON oc.GlobalCityId=gc.Id AND oc.IsDeleted=0;
  IF (SELECT COUNT(*) FROM #Mapped)<>1282 THROW 51018, 'Not all accounts mapped', 1;
  IF EXISTS(SELECT 1 FROM #Mapped m WHERE m.SponsorId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM app.Sponsors s WHERE s.Id=m.SponsorId AND s.Status=1 AND s.IsDeleted=0))
    THROW 51019, 'Sponsor mapping not active', 1;
  IF EXISTS(SELECT PlatformId,ExternalId FROM #Mapped GROUP BY PlatformId,ExternalId HAVING COUNT(*)>1)
    THROW 51020, 'Mapped account identifiers duplicate', 1;

  INSERT app.PlatformRiderAccounts(Id,ClientPlatformId,RegisteredEmployeeId,OperatingCityId,SponsorId,Code,ExternalAccountId,
      PaymentModel,Status,StatusReason,OwnershipNotes,OperationalNotes,CreatedAtUtc,IsDeleted)
  SELECT NEWID(),PlatformId,OwnerId,CityId,SponsorId,
    CONCAT(CASE Platform WHEN 'JAHEZ' THEN 'JHZ-' WHEN 'HUNGER' THEN 'HUN-' ELSE 'SHF-' END,ExternalId),
    ExternalId,1,ErpStatus,SourceStatus,
    CONCAT(N'Registered owner iqama: ',OwnerIqama,N'; source sheet row: ',SheetRow),
    CONCAT(N'Original vehicle type: ',Vehicle),SYSDATETIMEOFFSET(),0
  FROM #Mapped;
  IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ'))<>1282
    THROW 51021, 'Final account count mismatch', 1;
  IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='JAHEZ')<>380
    OR (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='HUNGER')<>438
    OR (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ')<>464
    THROW 51022, 'Per-platform account count mismatch', 1;
  IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ') AND a.SponsorId IS NULL)<>6
    THROW 51023, 'Outside-sponsorship count mismatch', 1;
  IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ') AND a.RegisteredEmployeeId IS NULL)
    THROW 51024, 'Unlinked account owner', 1;
  IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='KEETA')<>@KeetaBefore
    THROW 51026, 'Keeta account count changed', 1;
  IF @DryRun=0 AND NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261007120737_AllowPlatformAccountOutsideSponsorship')
    INSERT migration.__ApplicationMigrationsHistory(MigrationId,ProductVersion)
    VALUES('20261007120737_AllowPlatformAccountOutsideSponsorship','10.0.11');
  SELECT p.Code,a.Status,COUNT(*) AS N FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
    WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ') GROUP BY p.Code,a.Status ORDER BY p.Code,a.Status;
  IF @DryRun=1 ROLLBACK TRANSACTION ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;
