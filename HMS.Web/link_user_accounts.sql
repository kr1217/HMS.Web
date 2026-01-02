-- Link AspNetUsers to Patients and Doctors
USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'LINKING USER ACCOUNTS TO PATIENTS/DOCTORS';
PRINT '========================================';

-- Update Patients table to link to AspNetUsers
UPDATE p
SET p.UserId = u.Id
FROM Patients p
INNER JOIN AspNetUsers u ON p.Email = u.Email
WHERE p.Email LIKE 'patient%@email.com';

PRINT 'Linked ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' patient accounts';

-- Update Doctors table to link to AspNetUsers  
UPDATE d
SET d.UserId = u.Id
FROM Doctors d
INNER JOIN AspNetUsers u ON d.Email = u.Email
WHERE d.Email != 'admin@hospital.com';

PRINT 'Linked ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' doctor accounts';

-- Verify linkage
SELECT 
    'Patients Linked' as Info,
    COUNT(*) as Count
FROM Patients p
INNER JOIN AspNetUsers u ON p.UserId = u.Id
WHERE p.Email LIKE 'patient%@email.com';

SELECT 
    'Doctors Linked' as Info,
    COUNT(*) as Count
FROM Doctors d
INNER JOIN AspNetUsers u ON d.UserId = u.Id
WHERE d.Email != 'admin@hospital.com';

PRINT '========================================';
PRINT 'ACCOUNT LINKING COMPLETE!';
PRINT '========================================';
GO
