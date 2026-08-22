namespace LogisticsERP.Domain.Enums;

public enum LeaveWorkflowStatus
{
    Draft = 1,
    PendingApproval = 2,
    ReturnedForChanges = 3,
    Approved = 4,
    Active = 5,
    Completed = 6,
    Rejected = 7,
    CancellationPending = 8,
    Cancelled = 9,
    Expired = 10
}

public enum LeaveHrStatus { NotRequired = 1, PendingDocuments = 2, InProgress = 3, Ready = 4, Completed = 5 }
public enum LeaveDecisionType { Approved = 1, Rejected = 2, ReturnedForChanges = 3 }
public enum LeaveChangeRequestStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
public enum LeaveDocumentKind { Ticket = 1, ExitReentryVisa = 2, ApprovalLetter = 3, Other = 4 }
public enum LeaveApprovalScopeSource { CompanyWide = 1, EmployeeHousing = 2, ActiveClientPlatform = 3, ActiveClientContract = 4 }
public enum AbsenceCasePath { ReportedToAuthorities = 1, ExitOrSystemOutage = 2 }
public enum AbsenceCaseStatus { Open = 1, UnderReview = 2, DeadlineApproaching = 3, Overdue = 4, Resolved = 5, Cancelled = 6, Closed = 7 }
public enum AbsenceCaseEventType { Opened = 1, PathChanged = 2, NotesUpdated = 3, DeadlineChanged = 4, Resolved = 5, Cancelled = 6, Closed = 7, Corrected = 8 }
public enum EmployeeStatusChangeRequestStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
