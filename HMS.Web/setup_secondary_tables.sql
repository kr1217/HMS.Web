-- Create missing tables for Secondary Patient Features

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Bills] (
        [BillId] [int] IDENTITY(1,1) PRIMARY KEY,
        [PatientId] [int] NOT NULL,
        [TotalAmount] [decimal](18, 2) NOT NULL,
        [PaidAmount] [decimal](18, 2) NOT NULL,
        [DueAmount] [decimal](18, 2) NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        [BillDate] [datetime] NOT NULL
    );
    
    -- Seed some data
    INSERT INTO [dbo].[Bills] (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate)
    VALUES (1, 500.00, 500.00, 0.00, 'Paid', GETDATE()),
           (1, 1200.00, 0.00, 1200.00, 'Pending', DATEADD(day, -5, GETDATE()));
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationPackages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OperationPackages] (
        [PackageId] [int] IDENTITY(1,1) PRIMARY KEY,
        [PackageName] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](max) NULL,
        [Cost] [decimal](18, 2) NOT NULL
    );

    -- Seed data
    INSERT INTO [dbo].[OperationPackages] (PackageName, Description, Cost)
    VALUES ('Standard Appendectomy', 'All-inclusive package for appendix removal. Includes 2 days stay, medication, and kit.', 2500.00),
           ('Complete Knee Replacement', 'Comprehensive package for knee surgery. Includes physiotherapy and follow-up.', 8000.00),
           ('Basic Dental Surgery', 'Standard dental procedure package.', 800.00);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PatientOperations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PatientOperations] (
        [OperationId] [int] IDENTITY(1,1) PRIMARY KEY,
        [PatientId] [int] NOT NULL,
        [PackageId] [int] NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        [ScheduledDate] [datetime] NOT NULL,
        [Notes] [nvarchar](max) NULL
    );

    -- Seed data
    INSERT INTO [dbo].[PatientOperations] (PatientId, PackageId, Status, ScheduledDate, Notes)
    VALUES (1, 1, 'Scheduled', DATEADD(day, 10, GETDATE()), 'Patient requested room 302.');
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SupportTickets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SupportTickets] (
        [TicketId] [int] IDENTITY(1,1) PRIMARY KEY,
        [PatientId] [int] NOT NULL,
        [Subject] [nvarchar](200) NOT NULL,
        [Message] [nvarchar](max) NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [Response] [nvarchar](max) NULL
    );

    -- Seed data
    INSERT INTO [dbo].[SupportTickets] (PatientId, Subject, Message, Status, CreatedDate, Response)
    VALUES (1, 'Login Issue', 'I cannot see my old reports.', 'Closed', DATEADD(day, -2, GETDATE()), 'Your reports have been imported now. Please check.');
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] [int] IDENTITY(1,1) PRIMARY KEY,
        [PatientId] [int] NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Message] [nvarchar](max) NOT NULL,
        [CreatedDate] [datetime] NOT NULL,
        [IsRead] [bit] NOT NULL
    );

    -- Seed data
    INSERT INTO [dbo].[Notifications] (PatientId, Title, Message, CreatedDate, IsRead)
    VALUES (1, 'Report Ready', 'Your Blood Test report is now available.', GETDATE(), 0),
           (1, 'Appointment Confirmed', 'Your appointment with Dr. Sarah is confirmed for tomorrow.', GETDATE(), 0);
END
