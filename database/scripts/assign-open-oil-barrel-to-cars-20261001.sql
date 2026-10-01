-- User-requested hosted data assignment. Only the inspected open barrel is targeted.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

IF DB_NAME() <> N'db67927'
    THROW 51030, 'This assignment targets hosted database db67927 only.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @barrelId uniqueidentifier = '01A0E5C1-696A-7C7D-8D59-E9412E748D48';
    DECLARE @remainingLiters decimal(18, 3);
    DECLARE @allowedVehicleType int;

    IF (SELECT COUNT(*) FROM maintenance.OilBarrels WITH (UPDLOCK, HOLDLOCK)
        WHERE Status = 2 AND IsDeleted = 0) <> 1
        THROW 51031, 'The inspected single open barrel state has changed.', 1;

    SELECT @remainingLiters = RemainingLiters, @allowedVehicleType = AllowedVehicleType
    FROM maintenance.OilBarrels WITH (UPDLOCK, HOLDLOCK)
    WHERE Id = @barrelId AND Status = 2 AND IsDeleted = 0;

    IF @remainingLiters IS NULL OR (@allowedVehicleType IS NOT NULL AND @allowedVehicleType <> 2)
        THROW 51032, 'The target barrel is missing, closed, or assigned to another type.', 1;

    IF @allowedVehicleType IS NULL
    BEGIN
        UPDATE maintenance.OilBarrels
        SET AllowedVehicleType = 2, UpdatedAtUtc = SYSUTCDATETIME()
        WHERE Id = @barrelId AND Status = 2 AND IsDeleted = 0 AND AllowedVehicleType IS NULL;

        IF @@ROWCOUNT <> 1
            THROW 51033, 'Expected exactly one barrel assignment.', 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM maintenance.OilBarrels
        WHERE Id = @barrelId AND Status = 2 AND IsDeleted = 0
            AND AllowedVehicleType = 2 AND RemainingLiters = @remainingLiters)
        THROW 51034, 'Barrel assignment verification failed.', 1;

    COMMIT TRANSACTION;

    SELECT Id, BarrelNumber, AllowedVehicleType, Status, RemainingLiters, UpdatedAtUtc
    FROM maintenance.OilBarrels WHERE Id = @barrelId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
