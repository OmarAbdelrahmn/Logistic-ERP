namespace LogisticsERP.Domain.Enums;

public enum ClientContractStatus { Draft = 1, Active = 2, Suspended = 3, Ended = 4, Archived = 5 }
public enum PlatformRiderAccountStatus { Available = 1, Assigned = 2, Suspended = 3, Retired = 4, Archived = 5 }
public enum RiderAssignmentStatus { Planned = 1, Active = 2, Ended = 3, Cancelled = 4 }
public enum PlatformRegistrationType { Sponsored = 1, Freelancer = 2 }
public enum PlatformBillingMode { Unknown = 1, Slab = 2, PerOrder = 3 }
public enum PlatformAccountRegistrationStatus { NotRequired = 1, Requested = 2, ActivationInProgress = 3, Activated = 4, Suspended = 5, Rejected = 6, MissingData = 7, Cancelled = 8 }
