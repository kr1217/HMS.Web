USE [HospitalManagement];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO


-- Disable constraints to allow easy clearing
EXEC sp_msforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"

-- Delete data from dependent tables first
DELETE FROM [dbo].[Notifications];
DELETE FROM [dbo].[Reports];
DELETE FROM [dbo].[Prescriptions];
DELETE FROM [dbo].[Bills];
DELETE FROM [dbo].[PatientOperations];
DELETE FROM [dbo].[SupportTickets]; -- Inferred from Entities.cs

-- Delete core business data
DELETE FROM [dbo].[Appointments];
DELETE FROM [dbo].[DoctorShifts];

-- Delete Profiles
DELETE FROM [dbo].[Patients];
DELETE FROM [dbo].[Doctors];

-- Delete Identity Data
DELETE FROM [dbo].[AspNetUserRoles];
DELETE FROM [dbo].[AspNetUserClaims];
DELETE FROM [dbo].[AspNetUserLogins];
DELETE FROM [dbo].[AspNetUserTokens];
DELETE FROM [dbo].[AspNetUsers];

-- Re-enable constraints
EXEC sp_msforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"
GO
