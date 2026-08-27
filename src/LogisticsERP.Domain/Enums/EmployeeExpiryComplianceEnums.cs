namespace LogisticsERP.Domain.Enums;

public enum EmployeeExpiryComplianceSourceType
{
    EmployeeDocument = 1,
    DriverLicense = 2,
    RiderCard = 3,
    HealthCard = 4,
    MedicalInsurance = 5
}

public enum EmployeeExpiryComplianceDueStatus
{
    Valid = 1,
    Upcoming = 2,
    DueToday = 3,
    Expired = 4,
    Missing = 5
}
