-- =============================================
-- CLEANUP QUERY: Remove Users Without Roles
-- =============================================
-- This script removes all users who were created before the doctor module
-- was implemented and do not have a defined role (Patient or Doctor).
-- 
-- This fixes the routing issue where:
-- - Patients were being routed to the doctor dashboard
-- - Doctors were being routed to the patient form
--
-- ⚠️ WARNING: This will permanently delete users and their associated data!
-- Make sure to backup your database before running this script.
-- =============================================

USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'Starting User Cleanup Process...';
PRINT '========================================';
PRINT '';

-- Step 1: Identify users without roles
PRINT 'Step 1: Identifying users without assigned roles...';
SELECT Id INTO #UsersToDelete FROM AspNetUsers 
WHERE Id NOT IN (SELECT UserId FROM AspNetUserRoles);

DECLARE @UserCount INT;
SELECT @UserCount = COUNT(*) FROM #UsersToDelete;
PRINT 'Found ' + CAST(@UserCount AS VARCHAR(10)) + ' users without roles.';
PRINT '';

IF @UserCount = 0
BEGIN
    PRINT 'No users to delete. All users have assigned roles.';
    DROP TABLE #UsersToDelete;
END
ELSE
BEGIN
    -- Step 2: Delete related appointments
    PRINT 'Step 2: Deleting related appointments...';
    DELETE FROM [dbo].[Appointments] 
    WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
       OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
    PRINT 'Appointments deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 3: Delete related bills
    PRINT 'Step 3: Deleting related bills...';
    DELETE FROM [dbo].[Bills] 
    WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
    PRINT 'Bills deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 4: Delete related notifications
    PRINT 'Step 4: Deleting related notifications...';
    DELETE FROM [dbo].[Notifications] 
    WHERE PatientId IN (SELECT PatientId FROM [dbo].[Patients] WHERE UserId IN (SELECT Id FROM #UsersToDelete))
       OR DoctorId IN (SELECT DoctorId FROM [dbo].[Doctors] WHERE UserId IN (SELECT Id FROM #UsersToDelete));
    PRINT 'Notifications deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 5: Delete patient records
    PRINT 'Step 5: Deleting patient records...';
    DELETE FROM [dbo].[Patients] 
    WHERE UserId IN (SELECT Id FROM #UsersToDelete);
    PRINT 'Patient records deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 6: Delete doctor records
    PRINT 'Step 6: Deleting doctor records...';
    DELETE FROM [dbo].[Doctors] 
    WHERE UserId IN (SELECT Id FROM #UsersToDelete);
    PRINT 'Doctor records deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 7: Delete identity-related data
    PRINT 'Step 7: Deleting identity claims...';
    DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM #UsersToDelete);
    PRINT 'Claims deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    PRINT 'Step 8: Deleting identity logins...';
    DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM #UsersToDelete);
    PRINT 'Logins deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    PRINT 'Step 9: Deleting identity tokens...';
    DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM #UsersToDelete);
    PRINT 'Tokens deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Step 10: Delete the users themselves
    PRINT 'Step 10: Deleting user accounts...';
    DELETE FROM AspNetUsers WHERE Id IN (SELECT Id FROM #UsersToDelete);
    PRINT 'User accounts deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Cleanup
    DROP TABLE #UsersToDelete;
    
    PRINT '';
    PRINT '========================================';
    PRINT 'Cleanup Complete!';
    PRINT '========================================';
    PRINT 'Total users removed: ' + CAST(@UserCount AS VARCHAR(10));
    PRINT '';
    PRINT 'Next Steps:';
    PRINT '1. Restart your application';
    PRINT '2. Create new user accounts with proper roles';
    PRINT '3. Test login and routing for both Patient and Doctor roles';
    PRINT '';
END

GO
