SET NOCOUNT ON;
SELECT Id,EntityType,EntityId,Action,BeforeJson,AfterJson,CurrentHash,PreviousHash FROM audit.AuditEntries WHERE (BeforeJson LIKE N'%BW502%' OR AfterJson LIKE N'%BW502%') AND EntityId NOT IN('01a10b3c-19d1-709b-9b10-20b779f5e71c','01a10ca5-7a1c-7038-8cd5-b594bcc5e0a3');
