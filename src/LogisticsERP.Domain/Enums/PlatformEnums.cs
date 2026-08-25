namespace LogisticsERP.Domain.Enums;

public enum CompanyStatus { Setup = 1, Active = 2, Suspended = 3, Archived = 4 }
public enum CatalogStatus { Active = 1, Disabled = 2, Archived = 3 }
public enum UserAccountStatus { PendingTemporaryPassword = 1, Active = 2, Locked = 3, Suspended = 4, Archived = 5 }
public enum RoleStatus { Draft = 1, Active = 2, Disabled = 3, Archived = 4 }
public enum PermissionEffect { Grant = 1, Deny = 2 }
public enum SupportAccessStatus { Pending = 1, Approved = 2, Rejected = 3, Active = 4, Revoked = 5, Expired = 6 }
public enum AccessScopeType { Housing = 1, ClientPlatform = 2, ClientContract = 3 }

[Flags]
public enum SupportedPlatformPaymentModels
{
    None = 0,
    PayPerOrder = 1,
    Salary = 2
}

public enum PlatformAccountPaymentModel
{
    PayPerOrder = 1,
    Salary = 2
}
