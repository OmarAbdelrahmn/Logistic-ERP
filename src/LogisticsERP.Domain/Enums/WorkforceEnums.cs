namespace LogisticsERP.Domain.Enums;

public enum EmployeeStatus { Draft = 1, Onboarding = 2, Active = 3, Suspended = 4, OnLeave = 5, Terminated = 6, Archived = 7 }
public enum EmployeeRelationshipType { SponsoredInternal = 1, OutsideRider = 2 }
public enum RiderStatus { Draft = 1, Active = 2, Suspended = 3, Ended = 4, Archived = 5 }
public enum Gender { Male = 1, Female = 2, Other = 3 }
public enum MaritalStatus { Unmarried = 1, Married = 2, Divorced = 3, Widowed = 4 }
public enum HousingStatus { Draft = 1, Active = 2, Inactive = 3, Closed = 4, Archived = 5 }
public enum DocumentStatus { Active = 1, Expired = 2, Superseded = 3, Archived = 4 }
public enum SponsorType { Establishment = 1, Company = 2, Individual = 3 }
public enum SponsorshipStatus { Pending = 1, Active = 2, TransferInProgress = 3, Ended = 4, Cancelled = 5 }
public enum ResidencyPermitStatus { PendingIssuance = 1, Active = 2, RenewalInProgress = 3, Expired = 4, Cancelled = 5 }
public enum DriverLicenseBookingStatus { NotApplicable = 1, NotBooked = 2, WaitingForAppointment = 3, Booked = 4, Cancelled = 5, Unknown = 6 }
public enum DriverLicenseIssuanceStatus { NotStarted = 1, InProgress = 2, Issued = 3, Rejected = 4, Cancelled = 5 }
public enum DriverLicenseStatus { Application = 1, Active = 2, Expired = 3, Suspended = 4, Revoked = 5, Rejected = 6, Superseded = 7, Cancelled = 8 }
public enum RiderCardType { Car = 1, Motorcycle = 2 }
public enum CardValidityCycle { Annual = 1, Custom = 2 }
public enum RiderCardStatus { Draft = 1, Active = 2, Expired = 3, Suspended = 4, Superseded = 5, Cancelled = 6 }
public enum RiderHealthCardStatus { Draft = 1, Active = 2, Expired = 3, Suspended = 4, Superseded = 5, Cancelled = 6 }
public enum PromissoryNoteStatus { Draft = 1, Active = 2, Settled = 3, Expired = 4, Cancelled = 5, Disputed = 6 }
public enum InsuranceCompanyStatus { Draft = 1, Active = 2, Suspended = 3, Inactive = 4, Archived = 5 }
public enum InsurancePlanStatus { Draft = 1, Active = 2, Inactive = 3, Archived = 4 }
public enum MedicalInsurancePolicyStatus { Pending = 1, Active = 2, Expired = 3, Cancelled = 4, Superseded = 5 }
