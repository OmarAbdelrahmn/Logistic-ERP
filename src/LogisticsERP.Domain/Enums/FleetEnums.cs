namespace LogisticsERP.Domain.Enums;

public enum VehicleCatalogStatus { Active = 1, Disabled = 2, Archived = 3 }
public enum VehicleType { Motorcycle = 1, Car = 2, Van = 3, Truck = 4, Other = 5 }
public enum VehicleFuelType { Petrol = 1, Diesel = 2, Electric = 3, Hybrid = 4, Other = 5 }
public enum VehicleTransmissionType { Manual = 1, Automatic = 2, Other = 3 }
public enum VehicleOwnershipType { Owned = 1, Leased = 2, ThirdParty = 3 }
public enum VehicleRegistrationType
{
    Private = 1,
    PrivateTransport = 2,
    SmallBus = 3,
    Taxi = 4,
    PublicTransport = 5,
    PublicBus = 6,
    Motorcycle = 7,
    PublicWorks = 8
}
public enum VehicleOperationalStatus
{
    Available = 1,
    Assigned = 2,
    ProblemHold = 3,
    AccidentHold = 4,
    Stolen = 5,
    OutOfService = 6,
    Decommissioned = 7
}
public enum VehicleStatusSourceType { Vehicle = 1, Assignment = 2, Issue = 3, Accident = 4, Administrative = 5 }
public enum VehicleOdometerSourceType { Manual = 1, AssignmentTake = 2, AssignmentReturn = 3, Accident = 4, Correction = 5 }
public enum RiderVehicleAssignmentStatus { Active = 1, Completed = 2, Cancelled = 3, Corrected = 4 }
public enum RiderVehicleAssignmentEventType
{
    Taken = 1,
    Returned = 2,
    SwitchedOut = 3,
    SwitchedIn = 4,
    PermissionRenewed = 5,
    Cancelled = 6,
    Corrected = 7
}
public enum VehicleCondition { Unknown = 1, Good = 2, Fair = 3, Damaged = 4, Unsafe = 5 }
public enum ComplianceRecordStatus { Active = 1, Superseded = 2, Cancelled = 3 }
public enum VehicleInspectionResult { Passed = 1, Conditional = 2, Failed = 3 }
public enum VehicleComplianceDueStatus { Valid = 1, Upcoming = 2, DueToday = 3, Expired = 4, Missing = 5 }
public enum VehicleFileKind
{
    Istimara = 1,
    OperationCard = 2,
    FrontImage = 3,
    RearImage = 4,
    LeftImage = 5,
    RightImage = 6,
    Legacy = 99
}
public enum VehicleIssueCategory { Problem = 1, Accident = 2, Theft = 3, Damage = 4, Administrative = 5 }
public enum VehicleIssueSeverity { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum VehicleIssueStatus { Open = 1, UnderReview = 2, Resolved = 3, Closed = 4, Rejected = 5 }
public enum VehicleIssueEventType { Reported = 1, ReviewStarted = 2, Resolved = 3, Closed = 4, Rejected = 5, Corrected = 6 }
public enum VehicleAccidentStatus { Reported = 1, Finalized = 2, Closed = 3 }
public enum VehicleAccidentSeverity { Minor = 1, Moderate = 2, Serious = 3, Critical = 4 }
public enum VehicleAccidentEventType { Reported = 1, EvidenceAdded = 2, Finalized = 3, Corrected = 4, Closed = 5 }
public enum VehicleAccidentEvidenceType { Image = 1, UploadedReport = 2, Other = 3 }
