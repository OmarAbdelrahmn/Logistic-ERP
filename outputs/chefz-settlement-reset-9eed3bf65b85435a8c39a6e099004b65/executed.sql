SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Platform uniqueidentifier='01a02a0d-845d-7535-ac25-1e1f0c8e5808',@Now datetimeoffset=SYSUTCDATETIME(),
 @Settlements int=(SELECT COUNT(*) FROM chefz.WalletSettlements),
 @Imports int=(SELECT COUNT(*) FROM chefz.OrderSettlementImports);
IF @Settlements<>241 OR @Imports<>3 THROW 51000,'Unexpected settlement/import counts.',1;
IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName='Omar' AND IsDeleted=0)
 THROW 51000,'Audit actor not found.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code='SHIFTZ' AND IsDeleted=0)
 THROW 51000,'Expected Chefz platform not found.',1;
IF EXISTS(SELECT 1 FROM chefz.WalletSettlements s JOIN app.PlatformRiderAccounts a ON a.Id=s.PlatformRiderAccountId WHERE a.ClientPlatformId<>@Platform)
 THROW 51000,'Unexpected non-Chefz settlement reference.',1;
DECLARE @Before nvarchar(max)=(SELECT @Settlements SettlementCount,@Imports OrderImportCount,
 (SELECT SUM(GrossEarnings) FROM chefz.WalletSettlements) NetEarnings,
 (SELECT SUM(CompanyAmount) FROM chefz.WalletSettlements) CompanyAmount,
 (SELECT SUM(RiderAmount) FROM chefz.WalletSettlements) RiderAmount,
 JSON_QUERY((SELECT Id FROM chefz.WalletSettlements ORDER BY Id FOR JSON PATH)) SettlementIds,
 JSON_QUERY((SELECT Id,FileName,ContentHash,SourceRows FROM chefz.OrderSettlementImports ORDER BY Id FOR JSON PATH)) OrderImports
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
DELETE FROM chefz.WalletSettlements;
IF @@ROWCOUNT<>241 THROW 51000,'Unexpected settlement deletion count.',1;
DELETE FROM chefz.OrderSettlementImports;
IF @@ROWCOUNT<>3 THROW 51000,'Unexpected import deletion count.',1;
IF EXISTS(SELECT 1 FROM chefz.WalletSettlements) OR EXISTS(SELECT 1 FROM chefz.OrderSettlementImports)
 THROW 51000,'Settlement history was not completely cleared.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,
 OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','ChefzSettlementHistoryReset','chefz','ChefzSettlementHistory',@Platform,
 @Now,@Correlation,N'User requested clearing all old Chefz settlement data after confirming VAT-inclusive delivery fees. All settlement and order-import rows backed up before deletion; accounts, assignments and performance preserved.',
 @Before,N'{"SettlementCount":0,"OrderImportCount":0}','CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='chefz';
IF @@ROWCOUNT<>1 THROW 51000,'Chefz cache-version update failed.',1;
SELECT @Settlements DeletedSettlements,@Imports DeletedOrderImports,@Correlation CorrelationId;
