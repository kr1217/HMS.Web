-- SQL Script to update Patients table with new fields
USE [HospitalManagement];
GO

-- Add Mandatory Fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'CNIC')
    ALTER TABLE [dbo].[Patients] ADD [CNIC] NVARCHAR(50) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'BloodGroup')
    ALTER TABLE [dbo].[Patients] ADD [BloodGroup] NVARCHAR(10) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'MaritalStatus')
    ALTER TABLE [dbo].[Patients] ADD [MaritalStatus] NVARCHAR(50) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'EmergencyContactName')
    ALTER TABLE [dbo].[Patients] ADD [EmergencyContactName] NVARCHAR(100) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'EmergencyContactNumber')
    ALTER TABLE [dbo].[Patients] ADD [EmergencyContactNumber] NVARCHAR(50) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'RelationshipToEmergencyContact')
    ALTER TABLE [dbo].[Patients] ADD [RelationshipToEmergencyContact] NVARCHAR(50) NOT NULL DEFAULT '';

-- Add Secondary Fields (Optional)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'Allergies')
    ALTER TABLE [dbo].[Patients] ADD [Allergies] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'ChronicDiseases')
    ALTER TABLE [dbo].[Patients] ADD [ChronicDiseases] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'CurrentMedications')
    ALTER TABLE [dbo].[Patients] ADD [CurrentMedications] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'DisabilityStatus')
    ALTER TABLE [dbo].[Patients] ADD [DisabilityStatus] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'RegistrationDate')
    ALTER TABLE [dbo].[Patients] ADD [RegistrationDate] DATETIME NOT NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'IsActive')
    ALTER TABLE [dbo].[Patients] ADD [IsActive] BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'PatientType')
    ALTER TABLE [dbo].[Patients] ADD [PatientType] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'Email')
    ALTER TABLE [dbo].[Patients] ADD [Email] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'City')
    ALTER TABLE [dbo].[Patients] ADD [City] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'Country')
    ALTER TABLE [dbo].[Patients] ADD [Country] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'LastVisitDate')
    ALTER TABLE [dbo].[Patients] ADD [LastVisitDate] DATETIME NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Patients]') AND name = 'PrimaryDoctorId')
    ALTER TABLE [dbo].[Patients] ADD [PrimaryDoctorId] NVARCHAR(128) NULL;
