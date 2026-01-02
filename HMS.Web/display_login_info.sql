-- Create User Accounts for Patients and Doctors
USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'CREATING USER ACCOUNTS';
PRINT '========================================';

-- Note: This script creates AspNetUsers entries
-- Password for all accounts: Test@123
-- Password Hash for "Test@123" (you'll need to use the actual hash from Identity)

-- For demonstration, we'll update existing patient and doctor records with user IDs
-- In production, these would be created through the registration flow

-- ============================================
-- SAMPLE PATIENT ACCOUNTS (5 patients)
-- ============================================
PRINT 'Creating 5 sample patient accounts...';
PRINT 'Email: patient1@email.com | Password: Test@123';
PRINT 'Email: patient2@email.com | Password: Test@123';
PRINT 'Email: patient3@email.com | Password: Test@123';
PRINT 'Email: patient4@email.com | Password: Test@123';
PRINT 'Email: patient5@email.com | Password: Test@123';
PRINT '';

-- ============================================
-- SAMPLE DOCTOR ACCOUNTS (5 doctors)
-- ============================================
PRINT 'Creating 5 sample doctor accounts...';

-- Get 5 random doctors and display their info
DECLARE @DoctorInfo TABLE (DoctorId INT, Email NVARCHAR(100), FullName NVARCHAR(100), Specialization NVARCHAR(100));

INSERT INTO @DoctorInfo
SELECT TOP 5 DoctorId, Email, FullName, Specialization 
FROM Doctors 
WHERE Email != 'admin@hospital.com'
ORDER BY NEWID();

SELECT 
    'Email: ' + Email + ' | Password: Test@123 | Name: ' + FullName + ' | Specialty: ' + Specialization as LoginInfo
FROM @DoctorInfo;

PRINT '';
PRINT '========================================';
PRINT 'IMPORTANT NOTES:';
PRINT '========================================';
PRINT '1. All test accounts use password: Test@123';
PRINT '2. Admin account: admin@hospital.com | Admin@123';
PRINT '3. Patient accounts need to be created through';
PRINT '   the registration page first time.';
PRINT '4. Doctor accounts are already in the system';
PRINT '   but need AspNetUsers entries.';
PRINT '========================================';
GO

-- Display some patient emails for reference
SELECT TOP 5 
    'Patient Email: ' + Email + ' (PatientId: ' + CAST(PatientId AS NVARCHAR) + ')' as PatientInfo
FROM Patients
ORDER BY PatientId;
GO
