-- Comprehensive Hospital Data Seeding Script
-- This script clears existing data (except admin) and seeds realistic hospital data
USE [HospitalManagement];
GO

-- Disable foreign key constraints temporarily
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Clear existing data (preserve admin user and related records)
DELETE FROM DoctorPayments;
DELETE FROM Prescriptions WHERE DoctorId != (SELECT DoctorId FROM Doctors WHERE Email = 'admin@hospital.com');
DELETE FROM Reports WHERE DoctorId != (SELECT DoctorId FROM Doctors WHERE Email = 'admin@hospital.com');
DELETE FROM Notifications WHERE DoctorId IS NOT NULL AND DoctorId != (SELECT DoctorId FROM Doctors WHERE Email = 'admin@hospital.com');
DELETE FROM Appointments WHERE DoctorId != (SELECT DoctorId FROM Doctors WHERE Email = 'admin@hospital.com');
DELETE FROM DoctorShifts WHERE DoctorId != (SELECT DoctorId FROM Doctors WHERE Email = 'admin@hospital.com');
DELETE FROM Bills;
DELETE FROM Admissions;
DELETE FROM PatientOperations;
DELETE FROM SupportTickets;
DELETE FROM Notifications WHERE PatientId IS NOT NULL;
DELETE FROM Patients;
DELETE FROM Doctors WHERE Email != 'admin@hospital.com';
DELETE FROM UserShifts WHERE UserId != (SELECT Id FROM AspNetUsers WHERE Email = 'admin@hospital.com');
DELETE FROM Staff WHERE Email != 'admin@hospital.com';

-- Update bed statuses to Available
UPDATE Beds SET Status = 'Available';
GO

-- Re-enable foreign key constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

PRINT 'Existing data cleared (admin preserved)';
GO

-- ============================================
-- SEED PATIENTS (1000 records)
-- ============================================
DECLARE @PatientCounter INT = 1;
DECLARE @FirstNames TABLE (Name NVARCHAR(50));
DECLARE @LastNames TABLE (Name NVARCHAR(50));

INSERT INTO @FirstNames VALUES 
('James'),('Mary'),('John'),('Patricia'),('Robert'),('Jennifer'),('Michael'),('Linda'),('William'),('Elizabeth'),
('David'),('Barbara'),('Richard'),('Susan'),('Joseph'),('Jessica'),('Thomas'),('Sarah'),('Charles'),('Karen'),
('Christopher'),('Nancy'),('Daniel'),('Lisa'),('Matthew'),('Betty'),('Anthony'),('Margaret'),('Mark'),('Sandra'),
('Donald'),('Ashley'),('Steven'),('Kimberly'),('Paul'),('Emily'),('Andrew'),('Donna'),('Joshua'),('Michelle'),
('Kenneth'),('Dorothy'),('Kevin'),('Carol'),('Brian'),('Amanda'),('George'),('Melissa'),('Edward'),('Deborah');

INSERT INTO @LastNames VALUES 
('Smith'),('Johnson'),('Williams'),('Brown'),('Jones'),('Garcia'),('Miller'),('Davis'),('Rodriguez'),('Martinez'),
('Hernandez'),('Lopez'),('Gonzalez'),('Wilson'),('Anderson'),('Thomas'),('Taylor'),('Moore'),('Jackson'),('Martin'),
('Lee'),('Perez'),('Thompson'),('White'),('Harris'),('Sanchez'),('Clark'),('Ramirez'),('Lewis'),('Robinson'),
('Walker'),('Young'),('Allen'),('King'),('Wright'),('Scott'),('Torres'),('Nguyen'),('Hill'),('Flores'),
('Green'),('Adams'),('Nelson'),('Baker'),('Hall'),('Rivera'),('Campbell'),('Mitchell'),('Carter'),('Roberts');

WHILE @PatientCounter <= 1000
BEGIN
    DECLARE @FirstName NVARCHAR(50) = (SELECT TOP 1 Name FROM @FirstNames ORDER BY NEWID());
    DECLARE @LastName NVARCHAR(50) = (SELECT TOP 1 Name FROM @LastNames ORDER BY NEWID());
    DECLARE @Gender NVARCHAR(10) = CASE WHEN @PatientCounter % 2 = 0 THEN 'Male' ELSE 'Female' END;
    DECLARE @Age INT = 18 + (@PatientCounter % 70);
    DECLARE @DOB DATE = DATEADD(YEAR, -@Age, GETDATE());
    DECLARE @Phone NVARCHAR(20) = '+1' + CAST((2000000000 + @PatientCounter) AS NVARCHAR);
    DECLARE @Email NVARCHAR(100) = LOWER(@FirstName + '.' + @LastName + CAST(@PatientCounter AS NVARCHAR) + '@email.com');
    DECLARE @Address NVARCHAR(200) = CAST(@PatientCounter AS NVARCHAR) + ' Main Street, City, State ' + CAST((10000 + @PatientCounter) AS NVARCHAR);
    
    INSERT INTO Patients (FullName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup, EmergencyContact, MedicalHistory)
    VALUES (
        @FirstName + ' ' + @LastName,
        @DOB,
        @Gender,
        @Phone,
        @Email,
        @Address,
        CASE (@PatientCounter % 8) 
            WHEN 0 THEN 'A+' WHEN 1 THEN 'A-' WHEN 2 THEN 'B+' WHEN 3 THEN 'B-'
            WHEN 4 THEN 'O+' WHEN 5 THEN 'O-' WHEN 6 THEN 'AB+' ELSE 'AB-' END,
        @Phone,
        CASE WHEN @PatientCounter % 5 = 0 THEN 'Diabetes' 
             WHEN @PatientCounter % 7 = 0 THEN 'Hypertension'
             WHEN @PatientCounter % 11 = 0 THEN 'Asthma'
             ELSE 'None' END
    );
    
    SET @PatientCounter = @PatientCounter + 1;
END
GO

PRINT 'Seeded 1000 Patients';
GO

-- ============================================
-- SEED DOCTORS (100 records - realistic ratio)
-- ============================================
DECLARE @DoctorCounter INT = 1;
DECLARE @DoctorFirstNames TABLE (Name NVARCHAR(50));
DECLARE @DoctorLastNames TABLE (Name NVARCHAR(50));
DECLARE @Specializations TABLE (Spec NVARCHAR(100), DeptId INT);

INSERT INTO @DoctorFirstNames VALUES 
('Dr. Rajesh'),('Dr. Priya'),('Dr. Amit'),('Dr. Sneha'),('Dr. Vikram'),('Dr. Anjali'),('Dr. Arjun'),('Dr. Kavya'),
('Dr. Rohan'),('Dr. Neha'),('Dr. Karan'),('Dr. Pooja'),('Dr. Siddharth'),('Dr. Riya'),('Dr. Aditya'),('Dr. Meera'),
('Dr. Rahul'),('Dr. Divya'),('Dr. Varun'),('Dr. Shreya'),('Dr. Nikhil'),('Dr. Ananya'),('Dr. Harsh'),('Dr. Ishita'),
('Dr. Kunal'),('Dr. Tanvi'),('Dr. Manish'),('Dr. Simran'),('Dr. Abhishek'),('Dr. Nidhi');

INSERT INTO @DoctorLastNames VALUES 
('Sharma'),('Verma'),('Singh'),('Kumar'),('Patel'),('Gupta'),('Reddy'),('Iyer'),('Nair'),('Mehta'),
('Joshi'),('Desai'),('Rao'),('Pillai'),('Menon'),('Agarwal'),('Bansal'),('Malhotra'),('Kapoor'),('Chopra');

INSERT INTO @Specializations VALUES 
('Cardiologist', 1),('Cardiac Surgeon', 1),('Interventional Cardiologist', 1),
('Orthopedic Surgeon', 2),('Sports Medicine Specialist', 2),('Joint Replacement Surgeon', 2),
('Pediatrician', 3),('Neonatologist', 3),('Pediatric Surgeon', 3),
('Neurologist', 4),('Neurosurgeon', 4),('Epileptologist', 4),
('Dermatologist', 5),('Cosmetic Dermatologist', 5),('Pediatric Dermatologist', 5);

WHILE @DoctorCounter <= 100
BEGIN
    DECLARE @DocFirstName NVARCHAR(50) = (SELECT TOP 1 Name FROM @DoctorFirstNames ORDER BY NEWID());
    DECLARE @DocLastName NVARCHAR(50) = (SELECT TOP 1 Name FROM @DoctorLastNames ORDER BY NEWID());
    DECLARE @DocEmail NVARCHAR(100) = LOWER(REPLACE(@DocFirstName, 'Dr. ', '') + '.' + @DocLastName + CAST(@DoctorCounter AS NVARCHAR) + '@hospital.com');
    DECLARE @DocPhone NVARCHAR(20) = '+91' + CAST((9000000000 + @DoctorCounter) AS NVARCHAR);
    DECLARE @Spec NVARCHAR(100), @DeptId INT;
    
    SELECT TOP 1 @Spec = Spec, @DeptId = DeptId FROM @Specializations ORDER BY NEWID();
    
    DECLARE @Experience INT = 5 + (@DoctorCounter % 25);
    DECLARE @ConsultFee DECIMAL(18,2) = 500 + (@DoctorCounter % 20) * 100;
    DECLARE @CommissionRate DECIMAL(5,2) = 70 + (@DoctorCounter % 4) * 10; -- 70%, 80%, 90%, 100%
    
    INSERT INTO Doctors (
        UserId, FullName, Gender, ContactNumber, Email, Qualification, Specialization,
        MedicalLicenseNumber, LicenseIssuingAuthority, YearsOfExperience, DepartmentId,
        HospitalJoiningDate, ConsultationFee, FollowUpFee, AvailableDays, AvailableTimeSlots,
        RoomNumber, IsOnCall, IsActive, IsVerified, CreatedAt, IsAvailable, CommissionRate
    )
    VALUES (
        NEWID(),
        @DocFirstName + ' ' + @DocLastName,
        CASE WHEN @DoctorCounter % 2 = 0 THEN 'Male' ELSE 'Female' END,
        @DocPhone,
        @DocEmail,
        CASE (@DoctorCounter % 3) WHEN 0 THEN 'MBBS, MD' WHEN 1 THEN 'MBBS, MS' ELSE 'MBBS, DNB' END,
        @Spec,
        'MCI' + CAST((100000 + @DoctorCounter) AS NVARCHAR),
        'Medical Council of India',
        @Experience,
        @DeptId,
        DATEADD(YEAR, -@Experience, GETDATE()),
        @ConsultFee,
        @ConsultFee * 0.6,
        'Monday,Tuesday,Wednesday,Thursday,Friday',
        '09:00-17:00',
        'R' + CAST((100 + @DoctorCounter) AS NVARCHAR),
        CASE WHEN @DoctorCounter % 10 = 0 THEN 1 ELSE 0 END,
        1,
        1,
        DATEADD(DAY, -@DoctorCounter, GETDATE()),
        1,
        @CommissionRate
    );
    
    SET @DoctorCounter = @DoctorCounter + 1;
END
GO

PRINT 'Seeded 100 Doctors';
GO

-- ============================================
-- SEED APPOINTMENTS (1000 records)
-- ============================================
DECLARE @ApptCounter INT = 1;
WHILE @ApptCounter <= 1000
BEGIN
    DECLARE @PatientId INT = 1 + (@ApptCounter % 1000);
    DECLARE @DoctorId INT = (SELECT TOP 1 DoctorId FROM Doctors WHERE Email != 'admin@hospital.com' ORDER BY NEWID());
    DECLARE @ApptDate DATETIME = DATEADD(DAY, -(@ApptCounter % 90), GETDATE());
    DECLARE @ApptStatus NVARCHAR(50) = CASE (@ApptCounter % 5)
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Approved'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Rejected'
        ELSE 'Completed' END;
    
    INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentMode, Status, Reason)
    VALUES (
        @PatientId,
        @DoctorId,
        @ApptDate,
        CASE WHEN @ApptCounter % 3 = 0 THEN 'Virtual' ELSE 'Physical' END,
        @ApptStatus,
        CASE (@ApptCounter % 10)
            WHEN 0 THEN 'Routine checkup'
            WHEN 1 THEN 'Follow-up consultation'
            WHEN 2 THEN 'Chest pain'
            WHEN 3 THEN 'Fever and cough'
            WHEN 4 THEN 'Back pain'
            WHEN 5 THEN 'Skin rash'
            WHEN 6 THEN 'Headache'
            WHEN 7 THEN 'Joint pain'
            WHEN 8 THEN 'Digestive issues'
            ELSE 'General consultation' END
    );
    
    SET @ApptCounter = @ApptCounter + 1;
END
GO

PRINT 'Seeded 1000 Appointments';
GO

-- ============================================
-- SEED BILLS (500 records - for completed appointments)
-- ============================================
DECLARE @BillCounter INT = 1;
WHILE @BillCounter <= 500
BEGIN
    DECLARE @BillPatientId INT = 1 + (@BillCounter % 1000);
    DECLARE @BillAmount DECIMAL(18,2) = 1000 + (@BillCounter % 50) * 100;
    DECLARE @PaidAmount DECIMAL(18,2) = CASE WHEN @BillCounter % 3 = 0 THEN @BillAmount ELSE @BillAmount * 0.5 END;
    
    INSERT INTO Bills (PatientId, TotalAmount, PaidAmount, DueAmount, Status, BillDate, ShiftId, CreatedBy)
    VALUES (
        @BillPatientId,
        @BillAmount,
        @PaidAmount,
        @BillAmount - @PaidAmount,
        CASE WHEN @PaidAmount >= @BillAmount THEN 'Paid' ELSE 'Partial' END,
        DATEADD(DAY, -(@BillCounter % 60), GETDATE()),
        NULL,
        NULL
    );
    
    SET @BillCounter = @BillCounter + 1;
END
GO

PRINT 'Seeded 500 Bills';
GO

-- ============================================
-- SEED ADMISSIONS (200 records)
-- ============================================
DECLARE @AdmCounter INT = 1;
WHILE @AdmCounter <= 200
BEGIN
    DECLARE @AdmPatientId INT = 1 + (@AdmCounter % 1000);
    DECLARE @BedId INT = (SELECT TOP 1 BedId FROM Beds WHERE Status = 'Available' ORDER BY NEWID());
    
    IF @BedId IS NOT NULL
    BEGIN
        INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, Notes)
        VALUES (
            @AdmPatientId,
            @BedId,
            DATEADD(DAY, -(@AdmCounter % 30), GETDATE()),
            CASE WHEN @AdmCounter % 5 = 0 THEN 'Discharged' ELSE 'Admitted' END,
            'Admitted for observation and treatment'
        );
        
        -- Update bed status
        IF @AdmCounter % 5 != 0
            UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId;
    END
    
    SET @AdmCounter = @AdmCounter + 1;
END
GO

PRINT 'Seeded 200 Admissions';
GO

-- ============================================
-- SEED PRESCRIPTIONS (800 records)
-- ============================================
DECLARE @PrescCounter INT = 1;
WHILE @PrescCounter <= 800
BEGIN
    DECLARE @PrescPatientId INT = 1 + (@PrescCounter % 1000);
    DECLARE @PrescDoctorId INT = (SELECT TOP 1 DoctorId FROM Doctors WHERE Email != 'admin@hospital.com' ORDER BY NEWID());
    
    INSERT INTO Prescriptions (PatientId, DoctorId, AppointmentId, Details, PrescribedDate, Medications, IsLocked)
    VALUES (
        @PrescPatientId,
        @PrescDoctorId,
        NULL,
        'Prescription for patient treatment',
        DATEADD(DAY, -(@PrescCounter % 90), GETDATE()),
        CASE (@PrescCounter % 5)
            WHEN 0 THEN 'Paracetamol 500mg - 1 tablet twice daily for 5 days'
            WHEN 1 THEN 'Amoxicillin 250mg - 1 capsule three times daily for 7 days'
            WHEN 2 THEN 'Ibuprofen 400mg - 1 tablet as needed for pain'
            WHEN 3 THEN 'Cetirizine 10mg - 1 tablet once daily for allergies'
            ELSE 'Multivitamin - 1 tablet daily' END,
        CASE WHEN @PrescCounter % 10 = 0 THEN 1 ELSE 0 END
    );
    
    SET @PrescCounter = @PrescCounter + 1;
END
GO

PRINT 'Seeded 800 Prescriptions';
GO

-- ============================================
-- SEED PATIENT OPERATIONS (150 records)
-- ============================================
DECLARE @OpCounter INT = 1;
WHILE @OpCounter <= 150
BEGIN
    DECLARE @OpPatientId INT = 1 + (@OpCounter % 1000);
    DECLARE @OpDoctorId INT = (SELECT TOP 1 DoctorId FROM Doctors WHERE Email != 'admin@hospital.com' AND Specialization LIKE '%Surgeon%' ORDER BY NEWID());
    
    INSERT INTO PatientOperations (PatientId, PackageId, Status, ScheduledDate, Notes, DoctorId, Urgency, OperationName)
    VALUES (
        @OpPatientId,
        NULL,
        CASE (@OpCounter % 4) WHEN 0 THEN 'Scheduled' WHEN 1 THEN 'Completed' WHEN 2 THEN 'Cancelled' ELSE 'In Progress' END,
        DATEADD(DAY, (@OpCounter % 60) - 30, GETDATE()),
        'Surgical procedure notes',
        @OpDoctorId,
        CASE WHEN @OpCounter % 5 = 0 THEN 'Emergency' ELSE 'Routine' END,
        CASE (@OpCounter % 8)
            WHEN 0 THEN 'Appendectomy'
            WHEN 1 THEN 'Knee Replacement'
            WHEN 2 THEN 'Cataract Surgery'
            WHEN 3 THEN 'Hernia Repair'
            WHEN 4 THEN 'Gallbladder Removal'
            WHEN 5 THEN 'Hip Replacement'
            WHEN 6 THEN 'Cardiac Bypass'
            ELSE 'General Surgery' END
    );
    
    SET @OpCounter = @OpCounter + 1;
END
GO

PRINT 'Seeded 150 Patient Operations';
GO

-- ============================================
-- SEED STAFF (50 records)
-- ============================================
DECLARE @StaffCounter INT = 1;
WHILE @StaffCounter <= 50
BEGIN
    DECLARE @StaffFirstName NVARCHAR(50) = (SELECT TOP 1 Name FROM @FirstNames ORDER BY NEWID());
    DECLARE @StaffLastName NVARCHAR(50) = (SELECT TOP 1 Name FROM @LastNames ORDER BY NEWID());
    DECLARE @StaffEmail NVARCHAR(100) = LOWER(@StaffFirstName + '.' + @StaffLastName + CAST(@StaffCounter AS NVARCHAR) + '@hospital.com');
    
    INSERT INTO Staff (UserId, FullName, Role, Department, Shift, Salary, JoinDate, IsActive, Email, PhoneNumber)
    VALUES (
        NULL,
        @StaffFirstName + ' ' + @StaffLastName,
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
        @StaffEmail,
        '+91' + CAST((8000000000 + @StaffCounter) AS NVARCHAR)
    );
    
    SET @StaffCounter = @StaffCounter + 1;
END
GO

PRINT 'Seeded 50 Staff members';
GO

-- ============================================
-- SEED NOTIFICATIONS (500 records)
-- ============================================
DECLARE @NotifCounter INT = 1;
WHILE @NotifCounter <= 500
BEGIN
    INSERT INTO Notifications (PatientId, DoctorId, Title, Message, CreatedDate, IsRead)
    VALUES (
        CASE WHEN @NotifCounter % 2 = 0 THEN 1 + (@NotifCounter % 1000) ELSE NULL END,
        CASE WHEN @NotifCounter % 2 = 1 THEN (SELECT TOP 1 DoctorId FROM Doctors WHERE Email != 'admin@hospital.com' ORDER BY NEWID()) ELSE NULL END,
        CASE (@NotifCounter % 6)
            WHEN 0 THEN 'Appointment Confirmed'
            WHEN 1 THEN 'Prescription Ready'
            WHEN 2 THEN 'Test Results Available'
            WHEN 3 THEN 'Payment Reminder'
            WHEN 4 THEN 'Appointment Reminder'
            ELSE 'General Notification' END,
        'This is an automated notification regarding your hospital services.',
        DATEADD(DAY, -(@NotifCounter % 30), GETDATE()),
        CASE WHEN @NotifCounter % 3 = 0 THEN 1 ELSE 0 END
    );
    
    SET @NotifCounter = @NotifCounter + 1;
END
GO

PRINT 'Seeded 500 Notifications';
GO

-- ============================================
-- FINAL SUMMARY
-- ============================================
PRINT '========================================';
PRINT 'DATA SEEDING COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT 'Summary:';
PRINT '- Patients: 1000';
PRINT '- Doctors: 100';
PRINT '- Appointments: 1000';
PRINT '- Bills: 500';
PRINT '- Admissions: 200';
PRINT '- Prescriptions: 800';
PRINT '- Patient Operations: 150';
PRINT '- Staff: 50';
PRINT '- Notifications: 500';
PRINT '========================================';
PRINT 'Admin user and related data preserved.';
PRINT '========================================';
GO
