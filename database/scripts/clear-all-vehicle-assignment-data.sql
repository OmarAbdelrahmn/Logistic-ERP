-- One-time cleanup of all rider-vehicle assignments and records that refer to them.
-- Run with sqlcmd -v Commit=0 to validate and roll back, or Commit=1 to apply.
-- Vehicle and rider master records are retained. Private file bytes live outside SQL Server.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @Commit bit = $(Commit);
DECLARE @Now datetimeoffset = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
DECLARE @Actor uniqueidentifier = '019c18d5-62e1-7000-d000-000000000003';
DECLARE @Deleted table (Entity nvarchar(100), RowsAffected int);

BEGIN TRY
    BEGIN TRANSACTION;

    SELECT Id INTO #Assignments
    FROM app.RiderVehicleAssignments WITH (TABLOCKX, HOLDLOCK);

    SELECT i.Id, i.VehicleId INTO #Issues
    FROM app.VehicleIssues i
    JOIN #Assignments a ON a.Id = i.RelatedAssignmentId;

    SELECT DISTINCT ac.Id, ac.VehicleId INTO #Accidents
    FROM app.VehicleAccidents ac
    LEFT JOIN #Assignments a ON a.Id = ac.RiderVehicleAssignmentId
    LEFT JOIN #Issues i ON i.Id = ac.VehicleIssueId
    WHERE a.Id IS NOT NULL OR i.Id IS NOT NULL;

    SELECT Id INTO #Sources FROM #Assignments
    UNION SELECT Id FROM #Issues
    UNION SELECT Id FROM #Accidents;

    SELECT DISTINCT v.Id INTO #ResetVehicles
    FROM app.Vehicles v
    WHERE v.CurrentAssignmentId IN (SELECT Id FROM #Assignments)
       OR EXISTS (SELECT 1 FROM app.VehicleOperationalStatusPeriods p
                  JOIN #Sources s ON s.Id = p.SourceEntityId
                  WHERE p.VehicleId = v.Id AND p.EffectiveToUtc IS NULL)
       OR (v.CurrentOperationalStatus IN (3, 4)
           AND (v.Id IN (SELECT VehicleId FROM #Issues)
                OR v.Id IN (SELECT VehicleId FROM #Accidents)));

    -- These tables have deeper business and financial dependencies. They are empty
    -- for the target assignments today; stop if that has changed.
    IF EXISTS (SELECT 1 FROM maintenance.MaterialUsages WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments))
       OR EXISTS (SELECT 1 FROM maintenance.RiderInventoryIssues WHERE RelatedAssignmentId IN (SELECT Id FROM #Assignments))
       OR EXISTS (SELECT 1 FROM maintenance.VehicleExpenses WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments))
       OR EXISTS (SELECT 1 FROM maintenance.WorkOrders
                  WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments)
                     OR VehicleIssueId IN (SELECT Id FROM #Issues))
        THROW 51000, 'Assignment-linked maintenance rows require a revised cleanup plan.', 1;

    IF EXISTS (SELECT 1 FROM #ResetVehicles rv
               JOIN app.VehicleOperationalStatusPeriods p ON p.VehicleId = rv.Id AND p.EffectiveToUtc IS NULL
               LEFT JOIN #Sources s ON s.Id = p.SourceEntityId
               WHERE s.Id IS NULL)
        THROW 51001, 'A vehicle being reset has an unrelated current status period.', 1;

    UPDATE app.VehicleAccidents SET CurrentReportVersionId = NULL WHERE Id IN (SELECT Id FROM #Accidents);

    DELETE FROM app.VehicleAccidentInstallments WHERE VehicleAccidentId IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidentInstallments', @@ROWCOUNT);
    DELETE FROM app.VehicleAccidentCases WHERE VehicleAccidentId IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidentCases', @@ROWCOUNT);
    DELETE FROM app.VehicleAccidentEvents WHERE VehicleAccidentId IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidentEvents', @@ROWCOUNT);
    DELETE FROM app.VehicleAccidentReportVersions WHERE VehicleAccidentId IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidentReportVersions', @@ROWCOUNT);
    DELETE FROM app.VehicleAccidentAttachments WHERE VehicleAccidentId IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidentAttachments', @@ROWCOUNT);

    DELETE FROM app.Notifications WHERE SourceEntityId IN (SELECT Id FROM #Sources);
    INSERT @Deleted VALUES ('Notifications', @@ROWCOUNT);
    DELETE FROM app.VehicleOdometerReadings WHERE SourceEntityId IN (SELECT Id FROM #Sources);
    INSERT @Deleted VALUES ('VehicleOdometerReadings', @@ROWCOUNT);
    DELETE FROM app.VehicleOperationalStatusPeriods WHERE SourceEntityId IN (SELECT Id FROM #Sources);
    INSERT @Deleted VALUES ('VehicleOperationalStatusPeriods', @@ROWCOUNT);

    DELETE FROM app.VehicleAccidents WHERE Id IN (SELECT Id FROM #Accidents);
    INSERT @Deleted VALUES ('VehicleAccidents', @@ROWCOUNT);
    DELETE FROM app.VehicleIssueEvents WHERE VehicleIssueId IN (SELECT Id FROM #Issues);
    INSERT @Deleted VALUES ('VehicleIssueEvents', @@ROWCOUNT);
    DELETE FROM app.VehicleIssueEvidenceFiles WHERE VehicleIssueId IN (SELECT Id FROM #Issues);
    INSERT @Deleted VALUES ('VehicleIssueEvidenceFiles', @@ROWCOUNT);
    DELETE FROM app.VehicleIssues WHERE Id IN (SELECT Id FROM #Issues);
    INSERT @Deleted VALUES ('VehicleIssues', @@ROWCOUNT);

    DELETE FROM app.RealRiders WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments);
    INSERT @Deleted VALUES ('RealRiders', @@ROWCOUNT);
    DELETE FROM app.RiderVehicleAssignmentPromissoryFiles WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments);
    INSERT @Deleted VALUES ('RiderVehicleAssignmentPromissoryFiles', @@ROWCOUNT);
    DELETE FROM app.RiderVehicleAssignmentEvents WHERE RiderVehicleAssignmentId IN (SELECT Id FROM #Assignments);
    INSERT @Deleted VALUES ('RiderVehicleAssignmentEvents', @@ROWCOUNT);

    UPDATE app.Vehicles
    SET CurrentAssignmentId = NULL, CurrentOperationalStatus = 1,
        UpdatedAtUtc = @Now, UpdatedByUserId = @Actor
    WHERE Id IN (SELECT Id FROM #ResetVehicles);
    INSERT @Deleted VALUES ('VehiclesResetToAvailable', @@ROWCOUNT);

    INSERT app.VehicleOperationalStatusPeriods
        (Id, VehicleId, Status, EffectiveFromUtc, Reason, SourceType,
         SourceEntityId, ChangedByUserId, CreatedAtUtc, CreatedByUserId, IsDeleted)
    SELECT NEWID(), Id, 1, @Now, N'Vehicle assignment data cleared by administrator.',
           5, Id, @Actor, @Now, @Actor, 0
    FROM #ResetVehicles;
    INSERT @Deleted VALUES ('AvailableStatusPeriodsCreated', @@ROWCOUNT);

    UPDATE app.RiderVehicleAssignments
    SET PreviousAssignmentId = NULL, CorrectionOfAssignmentId = NULL
    WHERE Id IN (SELECT Id FROM #Assignments);
    DELETE FROM app.RiderVehicleAssignments WHERE Id IN (SELECT Id FROM #Assignments);
    INSERT @Deleted VALUES ('RiderVehicleAssignments', @@ROWCOUNT);

    IF EXISTS (SELECT 1 FROM app.RiderVehicleAssignments)
       OR EXISTS (SELECT 1 FROM app.Vehicles WHERE CurrentAssignmentId IS NOT NULL OR CurrentOperationalStatus = 2)
        THROW 51002, 'Assignments or active assignment pointers remain.', 1;

    SELECT Entity, RowsAffected FROM @Deleted ORDER BY Entity;
    IF @Commit = 1
    BEGIN
        COMMIT TRANSACTION;
        SELECT 'COMMITTED' AS Result;
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 'DRY RUN ROLLED BACK' AS Result;
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
