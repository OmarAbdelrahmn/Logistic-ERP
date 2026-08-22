namespace LogisticsERP.Domain.Enums;

public enum EmployeeStatus { Draft = 1, Onboarding = 2, Active = 3, Suspended = 4, OnLeave = 5, Terminated = 6, Archived = 7 }
public enum EmployeeRelationshipType { SponsoredInternal = 1, OutsideRider = 2 }
public enum RiderStatus { Draft = 1, Active = 2, Suspended = 3, Ended = 4, Archived = 5 }
public enum Gender { Male = 1, Female = 2, Other = 3 }
public enum MaritalStatus { Unmarried = 1, Married = 2, Divorced = 3, Widowed = 4 }
public enum HousingStatus { Draft = 1, Active = 2, Inactive = 3, Closed = 4, Archived = 5 }
public enum DocumentStatus { Active = 1, Expired = 2, Superseded = 3, Archived = 4 }
