-- Phase 4.3: Audit Trail Schema

-- 1. Create AuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL, -- User performing the action
        [UserName] [nvarchar](256) NULL, -- Cached for display performance
        [Action] [nvarchar](100) NOT NULL, -- e.g., 'Update_Cost', 'Delete_Bill'
        [EntityName] [nvarchar](100) NOT NULL, -- e.g., 'PatientOperations', 'Bills'
        [RecordId] [nvarchar](50) NOT NULL, -- ID of the entity modified
        [OldValue] [nvarchar](MAX) NULL, -- JSON or string representation
        [NewValue] [nvarchar](MAX) NULL,
        [Timestamp] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [Details] [nvarchar](500) NULL, -- Human readable summary
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
END
