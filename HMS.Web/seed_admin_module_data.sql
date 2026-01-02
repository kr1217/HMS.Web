-- Admin Module Data Seeding + User Accounts Creation
USE [HospitalManagement];
GO

PRINT '========================================';
PRINT 'SEEDING ADMIN MODULE DATA';
PRINT '========================================';

-- ============================================
-- SEED MORE STAFF (50 total including admin)
-- ============================================
DECLARE @StaffCounter INT = 1;
WHILE @StaffCounter <= 49
BEGIN
    INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber)
    VALUES (
        NULL,
        'Staff Member ' + CAST(@StaffCounter AS NVARCHAR),
        CASE (@StaffCounter % 5)
            WHEN 0 THEN 'Nurse'
            WHEN 1 THEN 'Receptionist'
            WHEN 2 THEN 'Lab Technician'
            WHEN 3 THEN 'Pharmacist'
            ELSE 'Administrative Staff' END,
        CASE (@StaffCounter % 5)
            WHEN 0 THEN 'Nursing'
            WHEN 1 THEN 'Front Desk'
            WHEN 2 THEN 'Laboratory'
            WHEN 3 THEN 'Pharmacy'
            ELSE 'Administration' END,
        CASE (@StaffCounter % 3) WHEN 0 THEN 'Morning' WHEN 1 THEN 'Evening' ELSE 'Night' END,
        25000 + (@StaffCounter % 10) * 5000,
        DATEADD(YEAR, -(@StaffCounter % 10), GETDATE()),
        1,
        'staff' + CAST(@StaffCounter AS NVARCHAR) + '@hospital.com',
        '+91' + CAST((8000000000 + @StaffCounter) AS NVARCHAR)
    );
    
    SET @StaffCounter = @StaffCounter + 1;
END
GO

PRINT 'Seeded 49 additional Staff members (Total: 50)';
GO

-- ============================================
-- SEED USER SHIFTS (Simulate staff on duty)
-- ============================================
DECLARE @ShiftCounter INT = 1;
DECLARE @AdminUserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@hospital.com');

-- Create some historical shifts
WHILE @ShiftCounter <= 100
BEGIN
    DECLARE @ShiftStart DATETIME = DATEADD(DAY, -(@ShiftCounter % 60), DATEADD(HOUR, 8, CAST(CAST(GETDATE() AS DATE) AS DATETIME)));
    DECLARE @ShiftEnd DATETIME = DATEADD(HOUR, 8, @ShiftStart);
    DECLARE @ShiftStatus NVARCHAR(50) = CASE WHEN @ShiftCounter % 10 = 0 THEN 'Open' ELSE 'Closed' END;
    
    INSERT INTO UserShifts (UserId, StartTime, EndTime, StartingCash, EndingCash, ActualCash, Status, Notes)
    VALUES (
        @AdminUserId,
        @ShiftStart,
        CASE WHEN @ShiftStatus = 'Closed' THEN @ShiftEnd ELSE NULL END,
        5000,
        CASE WHEN @ShiftStatus = 'Closed' THEN 5000 + (@ShiftCounter % 20) * 500 ELSE NULL END,
        CASE WHEN @ShiftStatus = 'Closed' THEN 5000 + (@ShiftCounter % 20) * 500 ELSE NULL END,
        @ShiftStatus,
        CASE WHEN @ShiftStatus = 'Closed' THEN 'Shift completed successfully' ELSE NULL END
    );
    
    SET @ShiftCounter = @ShiftCounter + 1;
END
GO

PRINT 'Seeded 100 User Shifts (10 currently open)';
GO

-- ============================================
-- UPDATE BEDS STATUS (Simulate occupancy)
-- ============================================
-- Mark some beds as occupied
DECLARE @BedCounter INT = 1;
DECLARE @TotalBeds INT = (SELECT COUNT(*) FROM Beds WHERE IsActive = 1);
DECLARE @BedsToOccupy INT = @TotalBeds / 2; -- Occupy 50% of beds

WHILE @BedCounter <= @BedsToOccupy
BEGIN
    DECLARE @BedId INT = (SELECT TOP 1 BedId FROM Beds WHERE Status = 'Available' AND IsActive = 1 ORDER BY NEWID());
    
    IF @BedId IS NOT NULL
    BEGIN
        UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId;
    END
    
    SET @BedCounter = @BedCounter + 1;
END

-- Mark some beds as under maintenance
SET @BedCounter = 1;
WHILE @BedCounter <= 5
BEGIN
    DECLARE @MaintenanceBedId INT = (SELECT TOP 1 BedId FROM Beds WHERE Status = 'Available' AND IsActive = 1 ORDER BY NEWID());
    
    IF @MaintenanceBedId IS NOT NULL
    BEGIN
        UPDATE Beds SET Status = 'Maintenance' WHERE BedId = @MaintenanceBedId;
    END
    
    SET @BedCounter = @BedCounter + 1;
END
GO

PRINT 'Updated Bed Statuses (50% Occupied, 5 in Maintenance)';
GO

-- ============================================
-- SEED MORE ADMISSIONS (200 total)
-- ============================================
DECLARE @AdmCounter INT = 1;
WHILE @AdmCounter <= 199
BEGIN
    DECLARE @AdmPatientId INT = 1 + (@AdmCounter % 1000);
    DECLARE @AdmBedId INT = (SELECT TOP 1 BedId FROM Beds WHERE Status = 'Occupied' ORDER BY NEWID());
    
    IF @AdmBedId IS NOT NULL
    BEGIN
        INSERT INTO Admissions (PatientId, BedId, AdmissionDate, DischargeDate, Status, Notes)
        VALUES (
            @AdmPatientId,
            @AdmBedId,
            DATEADD(DAY, -(@AdmCounter % 30), GETDATE()),
            CASE WHEN @AdmCounter % 5 = 0 THEN DATEADD(DAY, -(@AdmCounter % 15), GETDATE()) ELSE NULL END,
            CASE WHEN @AdmCounter % 5 = 0 THEN 'Discharged' ELSE 'Admitted' END,
            'Patient admitted for ' + CASE (@AdmCounter % 5)
                WHEN 0 THEN 'observation'
                WHEN 1 THEN 'surgery'
                WHEN 2 THEN 'treatment'
                WHEN 3 THEN 'recovery'
                ELSE 'emergency care' END
        );
    END
    
    SET @AdmCounter = @AdmCounter + 1;
END
GO

PRINT 'Seeded 199 additional Admissions (Total: 200)';
GO

-- ============================================
-- UPDATE PATIENT OPERATIONS (More surgeries)
-- ============================================
-- Add more surgeries scheduled for today
DECLARE @TodaySurgeryCounter INT = 1;
WHILE @TodaySurgeryCounter <= 10
BEGIN
    DECLARE @SurgeryPatientId INT = 1 + (@TodaySurgeryCounter % 1000);
    DECLARE @SurgeryDoctorId INT = (SELECT TOP 1 DoctorId FROM Doctors WHERE Specialization LIKE '%Surgeon%' ORDER BY NEWID());
    
    INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, OperationName)
    VALUES (
        @SurgeryPatientId,
        NULL,
        CASE WHEN @TodaySurgeryCounter % 3 = 0 THEN 'Completed' ELSE 'Scheduled' END,
        DATEADD(HOUR, @TodaySurgeryCounter, CAST(CAST(GETDATE() AS DATE) AS DATETIME)),
        'Surgical procedure scheduled',
        @SurgeryDoctorId,
        CASE WHEN @TodaySurgeryCounter % 4 = 0 THEN 'Emergency' ELSE 'Routine' END,
        CASE (@TodaySurgeryCounter % 6)
            WHEN 0 THEN 'Appendectomy'
            WHEN 1 THEN 'Knee Replacement'
            WHEN 2 THEN 'Cardiac Bypass'
            WHEN 3 THEN 'Hip Replacement'
            WHEN 4 THEN 'Gallbladder Removal'
            ELSE 'Hernia Repair' END
    );
    
    SET @TodaySurgeryCounter = @TodaySurgeryCounter + 1;
END
GO

PRINT 'Added 10 surgeries scheduled for today';
GO

-- ============================================
-- TAG SOME BILLS WITH SHIFTS
-- ============================================
UPDATE TOP (250) Bills 
SET ShiftId = (SELECT TOP 1 ShiftId FROM UserShifts WHERE Status = 'Closed' ORDER BY NEWID()),
    CreatedBy = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@hospital.com')
WHERE ShiftId IS NULL;
GO

PRINT 'Tagged 250 bills with shift information';
GO

-- ============================================
-- SEED DOCTOR PAYMENTS (Settlement History)
-- ============================================
DECLARE @PaymentCounter INT = 1;
WHILE @PaymentCounter <= 50
BEGIN
    DECLARE @PaymentDoctorId INT = (SELECT TOP 1 DoctorId FROM Doctors ORDER BY NEWID());
    DECLARE @PaymentAmount DECIMAL(18,2) = 10000 + (@PaymentCounter % 50) * 1000;
    DECLARE @PeriodStart DATETIME = DATEADD(MONTH, -(@PaymentCounter % 6), DATEADD(DAY, -30, GETDATE()));
    DECLARE @PeriodEnd DATETIME = DATEADD(DAY, 30, @PeriodStart);
    
    INSERT INTO DoctorPayments (DoctorId, Amount, PaymentDate, PeriodStart, PeriodEnd, Status, Notes)
    VALUES (
        @PaymentDoctorId,
        @PaymentAmount,
        DATEADD(DAY, -(@PaymentCounter % 180), GETDATE()),
        @PeriodStart,
        @PeriodEnd,
        'Processed',
        'Monthly settlement for completed consultations'
    );
    
    SET @PaymentCounter = @PaymentCounter + 1;
END
GO

PRINT 'Seeded 50 Doctor Payment records';
GO

PRINT '========================================';
PRINT 'ADMIN MODULE DATA SEEDING COMPLETE!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '- Staff: 50 total';
PRINT '- User Shifts: 100 (10 currently open)';
PRINT '- Beds: ~50% Occupied, 5 in Maintenance';
PRINT '- Admissions: 200 total';
PRINT '- Surgeries Today: 10';
PRINT '- Tagged Bills: 250';
PRINT '- Doctor Payments: 50';
PRINT '========================================';
GO
