-- Quick Patient Seeding Script
USE [HospitalManagement];
GO

-- Seed 1000 Patients
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO Patients (FullName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup, EmergencyContact, MedicalHistory)
    VALUES (
        'Patient ' + CAST(@i AS NVARCHAR),
        DATEADD(YEAR, -(20 + (@i % 60)), GETDATE()),
        CASE WHEN @i % 2 = 0 THEN 'Male' ELSE 'Female' END,
        '+1' + RIGHT('0000000000' + CAST(@i AS NVARCHAR), 10),
        'patient' + CAST(@i AS NVARCHAR) + '@email.com',
        CAST(@i AS NVARCHAR) + ' Hospital Street, Medical City',
        CASE (@i % 8) WHEN 0 THEN 'A+' WHEN 1 THEN 'A-' WHEN 2 THEN 'B+' WHEN 3 THEN 'B-' WHEN 4 THEN 'O+' WHEN 5 THEN 'O-' WHEN 6 THEN 'AB+' ELSE 'AB-' END,
        '+1' + RIGHT('0000000000' + CAST(@i AS NVARCHAR), 10),
        CASE WHEN @i % 5 = 0 THEN 'Diabetes' WHEN @i % 7 = 0 THEN 'Hypertension' ELSE 'None' END
    );
    SET @i = @i + 1;
END

PRINT 'Seeded 1000 Patients successfully';
GO
