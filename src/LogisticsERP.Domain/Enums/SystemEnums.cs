namespace LogisticsERP.Domain.Enums;

public enum NotificationSeverity { Information = 1, Success = 2, Warning = 3, Error = 4, Critical = 5 }
public enum ExportFormat { Csv = 1, Excel = 2 }
public enum ExportStatus { Pending = 1, Running = 2, Completed = 3, Failed = 4, Expired = 5 }
public enum CredentialPurpose { InitialActivation = 1, PasswordReset = 2 }
