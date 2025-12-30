USE [HospitalManagement];
GO

SELECT Id INTO #UsersToDelete FROM AspNetUsers 
WHERE Id NOT IN (SELECT UserId FROM AspNetUserRoles);

DELETE FROM [dbo].[Appointments] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
   OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));

DELETE FROM [dbo].[Bills] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
DELETE FROM [dbo].[Notifications] WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
   OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));

DELETE FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete);

DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM #UsersToDelete);
DELETE FROM AspNetUsers WHERE Id IN (SELECT Id FROM #UsersToDelete);

DROP TABLE #UsersToDelete;
GO
