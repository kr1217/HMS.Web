-- SQL Script to setup/update database for Doctor Module
USE [HospitalManagement];
GO

-- 1. Departments Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Departments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [DepartmentId] [int] IDENTITY(1,1) PRIMARY KEY,
        [DepartmentName] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](max) NULL
    );
END

-- Check if Description column exists (in case table was created differently before)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Departments]') AND name = 'Description')
BEGIN
    ALTER TABLE [dbo].[Departments] ADD [Description] [nvarchar](max) NULL;
END

-- Seed data using dynamic SQL to avoid compile-time errors
IF NOT EXISTS (SELECT * FROM [dbo].[Departments])
BEGIN
    EXEC('INSERT INTO [dbo].[Departments] (DepartmentName, Description)
    VALUES (''Cardiology'', ''Heart related issues''),
           (''Orthopedics'', ''Bone and joint related issues''),
           (''Pediatrics'', ''Child healthcare''),
           (''Neurology'', ''Nervous system related issues''),
           (''Dermatology'', ''Skin related issues'')');
END
GO

-- 2. Doctors Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Doctors] (
        [DoctorId] [int] IDENTITY(1,1) PRIMARY KEY,
        [UserId] [nvarchar](450) NOT NULL,
        [FullName] [nvarchar](100) NOT NULL,
        [Gender] [nvarchar](20) NOT NULL,
        [ContactNumber] [nvarchar](50) NOT NULL,
        [Email] [nvarchar](100) NOT NULL,
        [Qualification] [nvarchar](100) NOT NULL,
        [Specialization] [nvarchar](100) NOT NULL,
        [MedicalLicenseNumber] [nvarchar](100) NOT NULL,
        [LicenseIssuingAuthority] [nvarchar](100) NULL,
        [YearsOfExperience] [int] NOT NULL,
        [DepartmentId] [int] NOT NULL,
        [HospitalJoiningDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [ConsultationFee] [decimal](18, 2) NOT NULL DEFAULT 0,
        [FollowUpFee] [decimal](18, 2) NOT NULL DEFAULT 0,
        [AvailableDays] [nvarchar](200) NULL,
        [AvailableTimeSlots] [nvarchar](200) NULL,
        [RoomNumber] [nvarchar](50) NULL,
        [IsOnCall] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsVerified] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [IsAvailable] [bit] NOT NULL DEFAULT 1
    );
END
ELSE
BEGIN
    -- Add missing columns if table exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'UserId')
        ALTER TABLE [dbo].[Doctors] ADD [UserId] [nvarchar](450) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'Gender')
        ALTER TABLE [dbo].[Doctors] ADD [Gender] [nvarchar](20) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'ContactNumber')
        ALTER TABLE [dbo].[Doctors] ADD [ContactNumber] [nvarchar](50) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'Email')
        ALTER TABLE [dbo].[Doctors] ADD [Email] [nvarchar](100) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'Qualification')
        ALTER TABLE [dbo].[Doctors] ADD [Qualification] [nvarchar](100) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'Specialization')
        ALTER TABLE [dbo].[Doctors] ADD [Specialization] [nvarchar](100) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'MedicalLicenseNumber')
        ALTER TABLE [dbo].[Doctors] ADD [MedicalLicenseNumber] [nvarchar](100) NOT NULL DEFAULT '';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'LicenseIssuingAuthority')
        ALTER TABLE [dbo].[Doctors] ADD [LicenseIssuingAuthority] [nvarchar](100) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'YearsOfExperience')
        ALTER TABLE [dbo].[Doctors] ADD [YearsOfExperience] [int] NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'HospitalJoiningDate')
        ALTER TABLE [dbo].[Doctors] ADD [HospitalJoiningDate] [datetime] NOT NULL DEFAULT GETDATE();

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'ConsultationFee')
        ALTER TABLE [dbo].[Doctors] ADD [ConsultationFee] [decimal](18, 2) NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'FollowUpFee')
        ALTER TABLE [dbo].[Doctors] ADD [FollowUpFee] [decimal](18, 2) NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'AvailableDays')
        ALTER TABLE [dbo].[Doctors] ADD [AvailableDays] [nvarchar](200) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'AvailableTimeSlots')
        ALTER TABLE [dbo].[Doctors] ADD [AvailableTimeSlots] [nvarchar](200) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'RoomNumber')
        ALTER TABLE [dbo].[Doctors] ADD [RoomNumber] [nvarchar](50) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'IsOnCall')
        ALTER TABLE [dbo].[Doctors] ADD [IsOnCall] [bit] NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'IsActive')
        ALTER TABLE [dbo].[Doctors] ADD [IsActive] [bit] NOT NULL DEFAULT 1;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'IsAvailable')
        ALTER TABLE [dbo].[Doctors] ADD [IsAvailable] [bit] NOT NULL DEFAULT 1;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'IsVerified')
        ALTER TABLE [dbo].[Doctors] ADD [IsVerified] [bit] NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Doctors]') AND name = 'CreatedAt')
        ALTER TABLE [dbo].[Doctors] ADD [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE();
END

-- 3. Appointments Table expansion
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = 'DoctorNotes')
    ALTER TABLE [dbo].[Appointments] ADD [DoctorNotes] [nvarchar](max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = 'RejectionReason')
    ALTER TABLE [dbo].[Appointments] ADD [RejectionReason] [nvarchar](max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = 'RescheduledDate')
    ALTER TABLE [dbo].[Appointments] ADD [RescheduledDate] [datetime] NULL;

-- 4. Reports Table expansion
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND name = 'DoctorId')
    ALTER TABLE [dbo].[Reports] ADD [DoctorId] [int] NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND name = 'AppointmentId')
    ALTER TABLE [dbo].[Reports] ADD [AppointmentId] [int] NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Reports]') AND name = 'Observations')
    ALTER TABLE [dbo].[Reports] ADD [Observations] [nvarchar](max) NULL;

-- 5. Prescriptions Table expansion
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'AppointmentId')
    ALTER TABLE [dbo].[Prescriptions] ADD [AppointmentId] [int] NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'IsLocked')
    ALTER TABLE [dbo].[Prescriptions] ADD [IsLocked] [bit] NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Prescriptions]') AND name = 'DigitalSignature')
    ALTER TABLE [dbo].[Prescriptions] ADD [DigitalSignature] [nvarchar](max) NULL;

-- 6. Notifications Table expansion
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'DoctorId')
    ALTER TABLE [dbo].[Notifications] ADD [DoctorId] [int] NULL;

-- Make PatientId nullable if it isn't
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'PatientId')
    ALTER TABLE [dbo].[Notifications] ALTER COLUMN [PatientId] [int] NULL;
GO
