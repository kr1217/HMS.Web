-- Working Patient Seeding Script (with UserId)
USE [HospitalManagement];
GO

-- Seed 1000 Patients
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO Patients (
        UserId, FullName, DateOfBirth, Gender, ContactNumber, Address,
        CNIC, BloodGroup, MaritalStatus, EmergencyContactName, EmergencyContactNumber,
        RelationshipToEmergencyContact, Allergies, ChronicDiseases, CurrentMedications,
        DisabilityStatus, RegistrationDate, IsActive, PatientType, Email,
        City, Country, LastVisitDate, PrimaryDoctorId
    )
    VALUES (
        NEWID(), -- Generate a unique GUID for UserId
        'Patient ' + CAST(@i AS NVARCHAR),
        DATEADD(YEAR, -(20 + (@i % 60)), GETDATE()),
        CASE WHEN @i % 2 = 0 THEN 'Male' ELSE 'Female' END,
        '+1' + RIGHT('0000000000' + CAST(@i AS NVARCHAR), 10),
        CAST(@i AS NVARCHAR) + ' Hospital Street, Medical City',
        '12345-' + RIGHT('0000000' + CAST(@i AS NVARCHAR), 7) + '-1',
        CASE (@i % 8) WHEN 0 THEN 'A+' WHEN 1 THEN 'A-' WHEN 2 THEN 'B+' WHEN 3 THEN 'B-' WHEN 4 THEN 'O+' WHEN 5 THEN 'O-' WHEN 6 THEN 'AB+' ELSE 'AB-' END,
        CASE (@i % 3) WHEN 0 THEN 'Single' WHEN 1 THEN 'Married' ELSE 'Divorced' END,
        'Emergency Contact ' + CAST(@i AS NVARCHAR),
        '+1' + RIGHT('0000000000' + CAST((@i + 5000) AS NVARCHAR), 10),
        CASE (@i % 4) WHEN 0 THEN 'Spouse' WHEN 1 THEN 'Parent' WHEN 2 THEN 'Sibling' ELSE 'Friend' END,
        CASE WHEN @i % 10 = 0 THEN 'Penicillin' WHEN @i % 15 = 0 THEN 'Peanuts' ELSE NULL END,
        CASE WHEN @i % 5 = 0 THEN 'Diabetes' WHEN @i % 7 = 0 THEN 'Hypertension' WHEN @i % 11 = 0 THEN 'Asthma' ELSE NULL END,
        CASE WHEN @i % 8 = 0 THEN 'Metformin 500mg' WHEN @i % 12 = 0 THEN 'Lisinopril 10mg' ELSE NULL END,
        CASE WHEN @i % 20 = 0 THEN 'Wheelchair' WHEN @i % 30 = 0 THEN 'Visual Impairment' ELSE NULL END,
        DATEADD(DAY, -(@i % 365), GETDATE()),
        1,
        CASE WHEN @i % 10 = 0 THEN 'VIP' WHEN @i % 5 = 0 THEN 'Insurance' ELSE 'Regular' END,
        'patient' + CAST(@i AS NVARCHAR) + '@email.com',
        'City ' + CAST((@i % 20) AS NVARCHAR),
        'USA',
        CASE WHEN @i % 3 = 0 THEN DATEADD(DAY, -(@i % 90), GETDATE()) ELSE NULL END,
        NULL
    );
    
    IF @i % 100 = 0
        PRINT 'Seeded ' + CAST(@i AS NVARCHAR) + ' patients...';
    
    SET @i = @i + 1;
END

PRINT '========================================';
PRINT 'Successfully seeded 1000 Patients!';
PRINT '========================================';
GO
