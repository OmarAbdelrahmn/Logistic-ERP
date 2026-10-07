SET NOCOUNT ON;
CREATE TABLE #ReturnOdometer (StartOdometer bigint NOT NULL,EndOdometer bigint NULL,
    CHECK ([StartOdometer]>=0 AND ([EndOdometer] IS NULL OR [EndOdometer]>=0)));
INSERT #ReturnOdometer VALUES (10000,0),(10000,5000),(10000,12000),(10000,17000),(10000,NULL);
DECLARE @rejected int=0;
BEGIN TRY
    INSERT #ReturnOdometer VALUES (10000,-1);
    THROW 51590,'Negative return reading was incorrectly accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()<>547 THROW;
    SET @rejected+=1;
END CATCH;
BEGIN TRY
    INSERT #ReturnOdometer VALUES (-1,5000);
    THROW 51591,'Negative start reading was incorrectly accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()<>547 THROW;
    SET @rejected+=1;
END CATCH;
SELECT COUNT(*) AS AcceptedReadings,@rejected AS RejectedNegativeReadings FROM #ReturnOdometer;
DROP TABLE #ReturnOdometer;
SELECT COUNT(*) AS ExistingNegativeAssignmentReadings FROM app.RiderVehicleAssignments WHERE StartOdometer<0 OR EndOdometer<0;
