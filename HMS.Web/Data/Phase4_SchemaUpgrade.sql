-- Phase 4.2 Schema Upgrade: Enterprise Doctor Settlements

-- 1. Update Doctors Table (3-Part Pay Profile)
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'SurgeryCommission' AND Object_ID = Object_ID(N'Doctors'))
BEGIN
    ALTER TABLE Doctors ADD SurgeryCommission DECIMAL(18,2) NOT NULL DEFAULT 0.00;
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'RecommendationCommission' AND Object_ID = Object_ID(N'Doctors'))
BEGIN
    ALTER TABLE Doctors ADD RecommendationCommission DECIMAL(18,2) NOT NULL DEFAULT 0.00;
END

-- 2. Update PatientOperations Table (Operating Surgeon)
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OperatingSurgeonId' AND Object_ID = Object_ID(N'PatientOperations'))
BEGIN
    ALTER TABLE PatientOperations ADD OperatingSurgeonId INT NULL;
    -- Note: We can add FK constraint later or rely on app logic for now to avoid migration complexity
END

-- 3. Create DoctorLedger Table (Immutable Financial Record)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DoctorLedger]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DoctorLedger](
        [LedgerId] [int] IDENTITY(1,1) NOT NULL,
        [DoctorId] [int] NOT NULL,
        [TransactionDate] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [TransactionType] [nvarchar](50) NOT NULL, -- 'Consultation', 'Surgery', 'Recommendation'
        [ReferenceId] [int] NOT NULL, -- AppointmentId or OperationId
        [Amount] [decimal](18, 2) NOT NULL,
        [Description] [nvarchar](255) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Paid'
        [SettledDate] [datetime2](7) NULL,
        CONSTRAINT [PK_DoctorLedger] PRIMARY KEY CLUSTERED ([LedgerId] ASC)
    );
END

-- 4. Create DoctorPayments Table (Legacy Settlement Support)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DoctorPayments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DoctorPayments](
        [PaymentId] [int] IDENTITY(1,1) NOT NULL,
        [DoctorId] [int] NOT NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [PaymentDate] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [PeriodStart] [datetime2](7) NOT NULL,
        [PeriodEnd] [datetime2](7) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Paid',
        [Notes] [nvarchar](max) NULL,
        CONSTRAINT [PK_DoctorPayments] PRIMARY KEY CLUSTERED ([PaymentId] ASC)
    );
END
